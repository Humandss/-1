using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public interface IHealthStateProvider
{
    void GetBluntDamage(float bluntDam);
    int GetNumberHeavyBleeding();
    int GetNumberLightBleeding();
    float GetMaxHP();
    float GetTotalHP();
    bool GetIsLeftArmFracture();
    bool GetIsRightArmFracture();
    bool GetIsLeftLegFracture();
    bool GetIsRightLegFracture();
    bool GetIsLeftArmBlackout();
    bool GetIsRightArmBlackout();
    bool GetIsLeftLegBlackout();
    bool GetIsRightLegBlackout();
    void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul, float speed, float pen);
    void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance);
}

public interface IGetFactorAfterPentrateBodyProvider
{
    float GetSpeedAfterPenBody();
    float GetPenetrationAfterPenBody();
}

[System.Serializable]
public readonly struct PlayerInjuryState
{
    public readonly bool leftArmInjured;
    public readonly bool rightArmInjured;
    public readonly bool leftLegInjured;
    public readonly bool rightLegInjured;

    public bool HasAnyArmInjury => leftArmInjured || rightArmInjured;
    public bool HasBothArmInjuries => leftArmInjured && rightArmInjured;
    public bool HasAnyLegInjury => leftLegInjured || rightLegInjured;
    public bool HasBothLegInjuries => leftLegInjured && rightLegInjured;

    public PlayerInjuryState(bool leftArmInjured, bool rightArmInjured, bool leftLegInjured, bool rightLegInjured)
    {
        this.leftArmInjured = leftArmInjured;
        this.rightArmInjured = rightArmInjured;
        this.leftLegInjured = leftLegInjured;
        this.rightLegInjured = rightLegInjured;
    }
}

public abstract class HealthManager : MonoBehaviour, IHealthStateProvider, IGetFactorAfterPentrateBodyProvider
{
    [Header("Refs")]
    [SerializeField] protected HealthProfile health;

    [Header("Health Component")]
    protected float totalHP = 0.0f;
    protected readonly Dictionary<BodyParts, float> hp = new();
    protected readonly Dictionary<BodyParts, float> maxHp = new();
    protected readonly Dictionary<BodyParts, float> damMul = new();
    protected readonly Dictionary<BodyParts, float> penSpeedDecreaseMul = new();
    protected readonly Dictionary<BodyParts, float> armorFactorForBody = new();
    protected readonly Dictionary<BodyParts, InjuryMask> allowedInjury = new();

    protected struct LimbStatus
    {
        public bool light;
        public bool heavy;
        public bool fracture;
        public bool blackout;
    }

    protected readonly Dictionary<BodyParts, LimbStatus> status = new();

    [Header("Time")]
    [SerializeField] protected float tickInterval = 2.5f;
    protected float nextTick;

    protected float afterPen = 0.0f;
    protected float afterSpeed = 0.0f;

    protected virtual void Awake()
    {
        Initialize();
        GetMaxHP();
        FlushTrackedChanges();
    }

    public virtual void Initialize()
    {
        hp.Clear();
        maxHp.Clear();
        damMul.Clear();
        penSpeedDecreaseMul.Clear();
        armorFactorForBody.Clear();
        allowedInjury.Clear();
        status.Clear();

        if (health == null || health.parts == null) return;

        foreach (var p in health.parts)
        {
            hp[p.parts] = Mathf.Max(0.0f, p.maxHP);
            maxHp[p.parts] = Mathf.Max(0.0f, p.maxHP);
            damMul[p.parts] = p.damageDistributeMul;
            penSpeedDecreaseMul[p.parts] = p.penetrationEnergyDecreaseMul;
            armorFactorForBody[p.parts] = p.armorForBody;
            allowedInjury[p.parts] = p.allowed;
            status[p.parts] = new LimbStatus
            {
                light = p.startLight,
                heavy = p.startHeavy,
                fracture = p.startFrac,
                blackout = p.startBlackout
            };
            OnPartStateChanged(p.parts);
        }
    }

    protected virtual void FixedUpdate()
    {
        FlushTrackedChanges();
        CheckIsDead();

        if (Time.time < nextTick) return;

        nextTick = Time.time + tickInterval;
        CheckBleedingEffects();
        CheckBlackoutEffects();
    }

    protected virtual void OnPartStateChanged(BodyParts part)
    {
    }

    protected virtual void FlushTrackedChanges()
    {
    }

    public PlayerInjuryState GetPlayerInjuryState()
    {
        bool leftArmInjured = status[BodyParts.LeftArm].fracture || status[BodyParts.LeftArm].blackout;
        bool rightArmInjured = status[BodyParts.RightArm].fracture || status[BodyParts.RightArm].blackout;
        bool leftLegInjured = status[BodyParts.LeftLeg].fracture || status[BodyParts.LeftLeg].blackout;
        bool rightLegInjured = status[BodyParts.RightLeg].fracture || status[BodyParts.RightLeg].blackout;

        return new PlayerInjuryState(leftArmInjured, rightArmInjured, leftLegInjured, rightLegInjured);
    }

    private void CheckBlackoutEffects()
    {
        foreach (var part in hp)
        {
            if (part.Value > 0.0f) continue;

            LimbStatus s = status[part.Key];
            if (s.blackout) continue;

            s.blackout = true;
            status[part.Key] = s;
            OnPartStateChanged(part.Key);
        }
    }

    public void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul, float speed, float pen)
    {
        if (!TryGetHitBodyPart(col, out BodyParts bodyPart)) return;

        float totalDamage = CalculateDamage(ammoDamage, ammoCriticalChance, ammoCriticalDamMul);
        ApplyBodyDamage(bodyPart, totalDamage);

        afterSpeed = speed * penSpeedDecreaseMul[bodyPart];
        afterPen = pen - armorFactorForBody[bodyPart];
    }

    public float GetSpeedAfterPenBody()
    {
        return afterSpeed;
    }

    public float GetPenetrationAfterPenBody()
    {
        return afterPen;
    }

    public void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance)
    {
        if (!TryGetEffectBodyPart(col, out BodyParts bodyPart)) return;

        bool isLightBleeding = UnityEngine.Random.value <= lightBleedingChance;
        bool isHeavyBleeding = UnityEngine.Random.value <= heavyBleedingChance;
        bool isFracture = UnityEngine.Random.value <= fractureChance;

        if (!isLightBleeding && !isHeavyBleeding && !isFracture) return;
        if (isLightBleeding && isHeavyBleeding) isLightBleeding = false;

        LimbStatus s = status[bodyPart];
        if (isLightBleeding) s.light = true;
        if (isHeavyBleeding) s.heavy = true;
        if (isFracture) s.fracture = true;

        status[bodyPart] = s;
        OnPartStateChanged(bodyPart);
    }

    private void CheckBleedingEffects()
    {
        List<BodyParts> aliveParts = GetAliveParts();
        ApplyBleedingTick(BodyParts.LeftArm, aliveParts);
        ApplyBleedingTick(BodyParts.RightArm, aliveParts);
        ApplyBleedingTick(BodyParts.LeftLeg, aliveParts);
        ApplyBleedingTick(BodyParts.RightLeg, aliveParts);
    }

    private void ApplyBleedingTick(BodyParts sourcePart, List<BodyParts> aliveParts)
    {
        if (!status[sourcePart].light && !status[sourcePart].heavy) return;

        float tickDam = status[sourcePart].light ? health.lightPerTickDam : health.heavyPerTickDam;
        bool sourceDead = hp[sourcePart] <= 0.0f;

        foreach (var part in aliveParts)
        {
            float damage = tickDam;

            if (sourceDead)
            {
                damage += health.tickPenaltyMul;
            }
            else if (part == sourcePart)
            {
                damage += 1.0f;
            }

            hp[part] = Mathf.Max(0.0f, hp[part] - damage);
            OnPartStateChanged(part);
        }
    }

    private List<BodyParts> GetAliveParts()
    {
        List<BodyParts> aliveParts = new();

        foreach (var part in hp)
        {
            if (part.Value > 0.0f) aliveParts.Add(part.Key);
        }

        return aliveParts;
    }

    private List<BodyParts> GetAllParts()
    {
        List<BodyParts> allParts = new();

        foreach (var part in hp)
        {
            allParts.Add(part.Key);
        }

        return allParts;
    }

    private void DistributeDamageToOtherParts(float damage)
    {
        float remaining = damage;
        int maxRound = 0;

        List<BodyParts> allParts = GetAllParts();
        if (allParts.Count == 0) return;

        while (remaining > 1.0f && maxRound < 5)
        {
            float distributeDamage = remaining / allParts.Count;
            if (distributeDamage < 0.05f) break;

            float overflowDam = 0.0f;
            foreach (var part in allParts)
            {
                if (distributeDamage > hp[part])
                {
                    overflowDam += distributeDamage - hp[part];

                    if (part == BodyParts.Head || part == BodyParts.Thorax)
                    {
                        hp[part] = -1.0f;
                    }
                    else
                    {
                        hp[part] = 0.0f;
                    }

                    OnPartStateChanged(part);
                    continue;
                }

                if (part == BodyParts.Head || part == BodyParts.Thorax)
                {
                    hp[part] -= distributeDamage * damMul[part];
                }
                else
                {
                    hp[part] -= distributeDamage * health.defaultDamageDistributeMul;
                }

                OnPartStateChanged(part);
            }

            maxRound++;
            remaining = overflowDam;
        }
    }

    private void ApplyBodyDamage(BodyParts bodyPart, float totalDamage)
    {
        if (bodyPart == BodyParts.Head || bodyPart == BodyParts.Thorax)
        {
            hp[bodyPart] -= totalDamage;
            OnPartStateChanged(bodyPart);
            return;
        }

        if (totalDamage > hp[bodyPart])
        {
            float restDamage = totalDamage - hp[bodyPart];
            hp[bodyPart] = 0.0f;
            OnPartStateChanged(bodyPart);
            DistributeDamageToOtherParts(restDamage);
            return;
        }

        hp[bodyPart] -= totalDamage;
        OnPartStateChanged(bodyPart);
    }

    private static bool TryGetHitBodyPart(Collider col, out BodyParts bodyPart)
    {
        switch (col.name)
        {
            case "head":
                bodyPart = BodyParts.Head;
                return true;
            case "thorax":
            case "thorax_back":
            case "thorax_back_neck":
                bodyPart = BodyParts.Thorax;
                return true;
            case "stomach":
                bodyPart = BodyParts.Stomach;
                return true;
            case "left_arm":
            case "left_forearm":
            case "left_hand":
                bodyPart = BodyParts.LeftArm;
                return true;
            case "right_arm":
            case "right_forearm":
            case "right_hand":
                bodyPart = BodyParts.RightArm;
                return true;
            case "left_thigh":
            case "left_shin":
            case "left_foot":
                bodyPart = BodyParts.LeftLeg;
                return true;
            case "right_thigh":
            case "right_shin":
            case "right_foot":
                bodyPart = BodyParts.RightLeg;
                return true;
            default:
                bodyPart = BodyParts.None;
                return false;
        }
    }

    private static bool TryGetEffectBodyPart(Collider col, out BodyParts bodyPart)
    {
        if (!TryGetHitBodyPart(col, out bodyPart)) return false;
        return bodyPart != BodyParts.Head && bodyPart != BodyParts.Thorax && bodyPart != BodyParts.Stomach;
    }

    public bool CheckIsDead()
    {
        totalHP = GetTotalHP();
        return hp[BodyParts.Head] < 0.0f || hp[BodyParts.Thorax] < 0.0f || totalHP <= 0.0f;
    }

    public void GetBluntDamage(float bluntDam)
    {
        float distributeDam = bluntDam / GetAllParts().Count;
        DistributeDamageToOtherParts(distributeDam);
    }

    public float GetTotalHP()
    {
        float currentTotalHp = 0.0f;
        foreach (var part in hp)
        {
            currentTotalHp += part.Value;
        }

        return currentTotalHp;
    }

    public float GetMaxHP()
    {
        float currentMaxHp = 0.0f;
        foreach (var part in maxHp)
        {
            currentMaxHp += part.Value;
        }

        return currentMaxHp;
    }

    private float CalculateDamage(float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
        bool isCritical = UnityEngine.Random.value <= ammoCriticalChance;
        return ammoDamage + (isCritical ? ammoDamage * ammoCriticalDamMul : 0.0f);
    }

    public BodyParts GetUrgentPartForFixBlackout()
    {
        foreach (var part in status)
        {
            if (part.Key == BodyParts.Head || part.Key == BodyParts.Thorax) continue;
            if (part.Value.blackout) return part.Key;
        }

        return BodyParts.None;
    }

    public BodyParts GetUrgentBodyPartForStopHBleeding()
    {
        foreach (var part in status)
        {
            if (part.Value.heavy) return part.Key;
        }

        return BodyParts.None;
    }

    public BodyParts GetUrgentBodyPartForFixFracture()
    {
        foreach (var part in status)
        {
            if (part.Value.fracture) return part.Key;
        }

        return BodyParts.None;
    }

    public BodyParts GetUrgentBodyPartForHealing()
    {
        float minHP = float.PositiveInfinity;
        BodyParts urgentParts = BodyParts.None;

        foreach (var part in status)
        {
            if (part.Value.light) return part.Key;
        }

        foreach (var part in hp)
        {
            if (maxHp[part.Key] == part.Value) continue;
            if (part.Value <= 0.0f) continue;

            if (part.Value < minHP)
            {
                minHP = part.Value;
                urgentParts = part.Key;
            }
        }

        return urgentParts;
    }

    public bool GetHealEffects(BodyParts bodyParts, float healAmounts)
    {
        if (!hp.ContainsKey(bodyParts) || healAmounts <= 0.0f) return false;
        if (hp[bodyParts] == maxHp[bodyParts]) return false;

        if (hp[bodyParts] + healAmounts > maxHp[bodyParts])
        {
            hp[bodyParts] += maxHp[bodyParts] - hp[bodyParts];
        }
        else
        {
            hp[bodyParts] += healAmounts;
        }

        OnPartStateChanged(bodyParts);
        return true;
    }

    public bool FixBlackoutEffects(BodyParts bodyParts)
    {
        if (!status.ContainsKey(bodyParts)) return false;
        if (!status[bodyParts].blackout) return false;

        LimbStatus s = status[bodyParts];
        hp[bodyParts] = 1.0f;
        s.blackout = false;
        maxHp[bodyParts] *= 0.8f;
        status[bodyParts] = s;

        OnPartStateChanged(bodyParts);
        return true;
    }

    public bool FixFractureEffects(BodyParts bodyParts)
    {
        if (!status.ContainsKey(bodyParts)) return false;
        if (!status[bodyParts].fracture) return false;

        LimbStatus s = status[bodyParts];
        s.fracture = false;
        status[bodyParts] = s;

        OnPartStateChanged(bodyParts);
        return true;
    }

    public bool StopBleedingEffects(BodyParts bodyParts, bool lightB, bool heavyB)
    {
        if (!status.ContainsKey(bodyParts)) return false;
        if (!status[bodyParts].light && !status[bodyParts].heavy) return false;

        LimbStatus s = status[bodyParts];
        if (lightB) s.light = false;
        if (heavyB) s.heavy = false;
        status[bodyParts] = s;

        OnPartStateChanged(bodyParts);
        return true;
    }

    public bool GetIsLeftArmFracture() => status[BodyParts.LeftArm].fracture;
    public bool GetIsRightArmFracture() => status[BodyParts.RightArm].fracture;
    public bool GetIsLeftLegFracture() => status[BodyParts.LeftLeg].fracture;
    public bool GetIsRightLegFracture() => status[BodyParts.RightLeg].fracture;
    public bool GetIsLeftArmBlackout() => status[BodyParts.LeftArm].blackout;
    public bool GetIsRightArmBlackout() => status[BodyParts.RightArm].blackout;
    public bool GetIsLeftLegBlackout() => status[BodyParts.LeftLeg].blackout;
    public bool GetIsRightLegBlackout() => status[BodyParts.RightLeg].blackout;

    public int GetNumberLightBleeding()
    {
        int count = 0;
        foreach (var part in status)
        {
            if (part.Value.light) count++;
        }

        return count;
    }

    public int GetNumberHeavyBleeding()
    {
        int count = 0;
        foreach (var part in status)
        {
            if (part.Value.heavy) count++;
        }

        return count;
    }

    public float GetPartHP(BodyParts part) => hp[part];
    public float GetPartMaxHP(BodyParts part) => maxHp[part];
    public bool GetHasLightBleed(BodyParts part) => status[part].light;
    public bool GetHasHeavyBleed(BodyParts part) => status[part].heavy;
    public bool GetHasFracture(BodyParts part) => status[part].fracture;
    public bool GetHasBlackout(BodyParts part) => status[part].blackout;

    public void GetLBleeding()
    {
        LimbStatus s = status[BodyParts.LeftArm];
        s.light = true;
        status[BodyParts.LeftArm] = s;
        OnPartStateChanged(BodyParts.LeftArm);
    }

    public void GetHBleeding()
    {
        LimbStatus s = status[BodyParts.LeftArm];
        s.heavy = true;
        status[BodyParts.LeftArm] = s;
        OnPartStateChanged(BodyParts.LeftArm);
    }

    public void GetFracture()
    {
        LimbStatus s = status[BodyParts.LeftArm];
        s.fracture = true;
        status[BodyParts.LeftArm] = s;
        OnPartStateChanged(BodyParts.LeftArm);
    }

    public void GetBlackout()
    {
        hp[BodyParts.LeftArm] = 0.0f;
        OnPartStateChanged(BodyParts.LeftArm);
    }

    public void GetInitializeHealth()
    {
        Initialize();
        FlushTrackedChanges();
    }
}
