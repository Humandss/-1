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
    void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul, float speed, float pen, int bulletId);
    void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance);


}
public interface IGetFactorAfterPentrateBodyProvider
{
    float GetSpeedAfterPenBody();
    float GetPenetrationAfterPenBody();
}
//부위당 상태 집계
[System.Serializable]
public readonly struct PartSnapshot
{
    public readonly BodyParts part;
    public readonly float hp;
    public readonly float maxHp;
    public readonly bool light, heavy, fracture, blackout;

    public PartSnapshot(BodyParts part, float hp, float maxHp, bool light, bool heavy, bool fracture, bool blackout)
    {
        this.part = part;
        this.hp = hp;
        this.maxHp = maxHp;
        this.light = light;
        this.heavy = heavy;
        this.fracture = fracture;
        this.blackout = blackout;
    }
}
//전체 상태 집계
[System.Serializable]
public readonly struct OverallSnapshot
{
    public readonly float totalHp;
    public readonly float totalMaxHp;
    public readonly bool anyLight, anyHeavy, anyFracture, anyBlackout; // 한군대로 이상상태 발견하면 true반환
    public OverallSnapshot(float totalHp, float totalMaxHp, bool anyLight, bool anyHeavy, bool anyFracture, bool anyBlackout)
    {
        this.totalHp = totalHp;
        this.totalMaxHp = totalMaxHp;
        this.anyLight = anyLight;
        this.anyHeavy = anyHeavy;
        this.anyFracture = anyFracture;
        this.anyBlackout = anyBlackout;
    }
}
public class HealthManager : MonoBehaviour,IHealthStateProvider, IGetFactorAfterPentrateBodyProvider
{
    [SerializeField] private bool areYouPlayer = false;
    [Header("Refs")]
    [SerializeField] private HealthProfile health;

    [Header("Health Component")]
    private float totalHP = 0.0f;
    private Dictionary<BodyParts, float> hp = new();
    private Dictionary<BodyParts, float> maxHp = new();
    private Dictionary<BodyParts, float> damMul = new();
    private Dictionary<BodyParts, float> penSpeedDecreaseMul = new();
    private Dictionary<BodyParts, float> armorFactorForBody = new();
    private Dictionary<BodyParts, InjuryMask> allowedInjury = new();
    private struct LimbStatus { public bool light, heavy, fracture, blackout; }
    private Dictionary<BodyParts, LimbStatus> status = new();

    //스냅샷 전용 딕셔너리
    private readonly List<BodyParts> changedParts = new(8);       
    private readonly List<PartSnapshot> tmpSnapshots = new(8);    

    [Header("Time")]
    [SerializeField] float tickInterval =2.5f;
    float nextTick;

    float afterPen = 0.0f;
    float afterSpeed = 0.0f;

    //같은 콜라이더 중첩으로 때리는거(관통 했을 경우) 방지하는 헤쉬셋
    private readonly HashSet<int> isHitOnce = new HashSet<int>();

    //public event System.Action<PartSnapshot> OnPartChanged;                 
    public event System.Action<IReadOnlyList<PartSnapshot>> OnBatchChanged; // 한 틱에 여러 부위 갱신
    public event System.Action<OverallSnapshot> OnOverallChanged;           // 전체 합계 갱신


    private void Awake()
    {
        InitializeHealthProfile();
        GetMaxHP();
     }
    private void InitializeHealthProfile()
    {
        hp.Clear(); maxHp.Clear(); damMul.Clear(); penSpeedDecreaseMul.Clear();
        armorFactorForBody.Clear(); allowedInjury.Clear(); status.Clear();

        foreach (var p in health.parts)
        {
            hp[p.parts] = Mathf.Max(0, p.maxHP);
            maxHp[p.parts] = Mathf.Max(0, p.maxHP);
            damMul[p.parts] = p.damageDistributeMul;
            penSpeedDecreaseMul[p.parts] =p.penetrationEnergyDecreaseMul;
            armorFactorForBody[p.parts] = p.armorForBody;
            allowedInjury[p.parts] = p.allowed;
            status[p.parts] = new LimbStatus
            {
                light = p.startLight,
                heavy = p.startHeavy,
                fracture = p.startFrac,
                blackout = p.startBlackout
            };
            MarkDirty(p.parts);
        }
        /*
        headHP = health.headHP;
        thoraxHP = health.thoraxHP;
        stomachHP = health.stomachHP;
        leftArmHP = health.leftArmHP;
        rightArmHP = health.rightArmHP;
        leftLegHP = health.leftLegHP;
        rightLegHP = health.rightLegHP;

        hp = new Dictionary<BodyParts, float>()
        {
            {BodyParts.Head, headHP},
            {BodyParts.Thorax, thoraxHP},
            {BodyParts.Stomach, stomachHP},
            {BodyParts.LeftArm, leftArmHP},
            {BodyParts.RightArm, rightArmHP},
            {BodyParts.LeftLeg, leftLegHP},
            {BodyParts.RightLeg, rightLegHP},

        };*/
    }
    private PartSnapshot MakeSnapshot(BodyParts p)
    {
        var s = status[p];
        return new PartSnapshot(
            p,
            hp[p],
            maxHp[p],
            s.light, s.heavy, s.fracture, s.blackout
        );
    }
    public PartSnapshot GetSnapshot(BodyParts p) => MakeSnapshot(p);
    public OverallSnapshot GetOverallSnapshot()
    {
        bool anyLight = false;
        bool anyHeavy = false;
        bool anyFrac = false;
        bool anyBlackout = false;

        float totalHP = 0.0f, totalMaxHP = 0.0f;
        foreach (var part in hp.Keys) { totalHP += hp[part]; totalMaxHP += maxHp[part]; }

        foreach (var part in status)
        {
            anyLight |= part.Value.light;
            anyHeavy |= part.Value.heavy;
            anyFrac |= part.Value.fracture;
            anyBlackout |= part.Value.blackout;
        }
        return new OverallSnapshot(totalHP, totalMaxHP, anyLight, anyHeavy, anyFrac, anyBlackout);
    }
    private void FixedUpdate()
    {
        NotifyDirty();
        CheckHP();
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + tickInterval;
            CheckBleedingEffects();
            CheckBlackoutEffects();
           // Debug.Log($"머리 체력 : {hp[BodyParts.Head]}, 흉부 체력 : {hp[BodyParts.Thorax]}, 복부 체력 :{hp[BodyParts.Stomach]}");
            
        }
       
    }
    private void CheckBlackoutEffects()
    {
        foreach(var parts in hp)
        {
            if (parts.Value > 0.0f) continue;

            var s = status[parts.Key];
            s.blackout = true;
            status[parts.Key] = s;
            MarkDirty(parts.Key);
            /*
            if (parts.Value <= 0)
            {
                if (parts.Key == BodyParts.Head)
                {
                    var s = status[BodyParts.Head];
                    s.blackout = true;
                    status[BodyParts.Head] = s;
                }
                if (parts.Key == BodyParts.Thorax)
                {
                    var s = status[BodyParts.Thorax];
                    s.blackout = true;
                    status[BodyParts.Thorax] = s;
                }
                if (parts.Key == BodyParts.Stomach)
                {
                    var s = status[BodyParts.Stomach];
                    s.blackout = true;
                    status[BodyParts.Stomach] = s;
                }
                if (parts.Key == BodyParts.LeftArm)
                {
                    var s = status[BodyParts.LeftArm];
                    s.blackout = true;
                    status[BodyParts.LeftArm] = s;
                }
                if (parts.Key == BodyParts.RightArm)
                {
                    var s = status[BodyParts.RightArm];
                    s.blackout = true;
                    status[BodyParts.RightArm] = s;
                }
                if (parts.Key == BodyParts.LeftLeg)
                {
                    var s = status[BodyParts.LeftLeg];
                    s.blackout = true;
                    status[BodyParts.LeftLeg] = s;
                }
                if (parts.Key == BodyParts.RightLeg)
                {
                    var s = status[BodyParts.RightLeg];
                    s.blackout = true;
                    status[BodyParts.RightLeg] = s;
                }
            }       
           */
        }
    }
    public void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul, float speed, float pen, int bulletId)
    {
       // Debug.Log($"[HIT] {col.name} layer={LayerMask.LayerToName(col.gameObject.layer)}");
        //한번 맞은 총알은 대미지X
        if (!isHitOnce.Add(bulletId)) return;

        float totalDamage = CalculateDamage(ammoDamage, ammoCriticalChance, ammoCriticalDamMul);
       
        
        if (col.name == "head")
        {
            Debug.Log("머리에 맞음!");       
            hp[BodyParts.Head] -= totalDamage;
            speed *= penSpeedDecreaseMul[BodyParts.Head];
            pen -= armorFactorForBody[BodyParts.Head];
            MarkDirty(BodyParts.Head);
        }

        if(col.name == "thorax" || col.name == "thorax_back"|| col.name == "thorax_back_neck")
        {
            Debug.Log("흉부에 맞음!");
            hp[BodyParts.Thorax] -= totalDamage;
            speed *= penSpeedDecreaseMul[BodyParts.Thorax];
            pen -= armorFactorForBody[BodyParts.Thorax];
            MarkDirty(BodyParts.Thorax);
        }

        if (col.name == "stomach")
        {
            Debug.Log("복부에 맞음!");
            if(totalDamage > hp[BodyParts.Stomach])
            {
                float restDamage = totalDamage - hp[BodyParts.Stomach];
                hp[BodyParts.Stomach] = 0.0f;
                DistributeDamageToOtherParts(restDamage);
                
            }
            else hp[BodyParts.Stomach] -= totalDamage; 

            speed *= penSpeedDecreaseMul[BodyParts.Stomach];
            pen -= armorFactorForBody[BodyParts.Stomach];
            MarkDirty(BodyParts.Stomach);
        }

        if (col.name == "left_arm" || col.name == "left_forearm" || col.name == "left_hand")
        {
            Debug.Log("왼팔에 맞음!");
            if (totalDamage > hp[BodyParts.LeftArm])
            {
                float restDamage = totalDamage - hp[BodyParts.LeftArm];
                hp[BodyParts.LeftArm] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.LeftArm] -= totalDamage;

            speed *= penSpeedDecreaseMul[BodyParts.LeftArm];
            pen -= armorFactorForBody[BodyParts.LeftArm];
            MarkDirty(BodyParts.LeftArm);
        }

        if (col.name == "right_arm" || col.name == "right_forearm" || col.name == "right_hand")
        {
            Debug.Log("오른팔에 맞음!");
            if (totalDamage > hp[BodyParts.RightArm])
            {
                float restDamage = totalDamage - hp[BodyParts.RightArm];
                hp[BodyParts.RightArm] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.RightArm] -= totalDamage;

            speed *= penSpeedDecreaseMul[BodyParts.RightArm];
            pen -= armorFactorForBody[BodyParts.RightArm];
            MarkDirty(BodyParts.RightArm);
        }

        if (col.name == "left_thigh" || col.name == "left_shin" || col.name == "left_foot")
        {
            Debug.Log("왼다리에 맞음!");
            if (totalDamage > hp[BodyParts.LeftLeg])
            {
                float restDamage = totalDamage - hp[BodyParts.LeftLeg];
                hp[BodyParts.LeftLeg] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.LeftLeg] -= totalDamage;

            speed *= penSpeedDecreaseMul[BodyParts.LeftLeg];
            pen -= armorFactorForBody[BodyParts.LeftLeg];
            MarkDirty(BodyParts.LeftLeg);
        }

        if (col.name == "right_thigh" || col.name == "right_shin" || col.name == "right_foot")
        {
            Debug.Log("오른다리에 맞음!");
            if (totalDamage > hp[BodyParts.RightLeg])
            {
                float restDamage = totalDamage - hp[BodyParts.RightLeg];
                hp[BodyParts.RightLeg] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.RightLeg] -= totalDamage;

            speed *= penSpeedDecreaseMul[BodyParts.RightLeg];
            pen -= armorFactorForBody[BodyParts.RightLeg];
            MarkDirty(BodyParts.RightLeg);
        }

        afterPen = pen;
        afterSpeed = speed;
    }

    public float GetSpeedAfterPenBody()
    {

        //Debug.Log(afterSpeed);
        return afterSpeed;
    }

    public float GetPenetrationAfterPenBody()
    {

        //Debug.Log(afterPen);
        return afterPen;
    }
    public void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance)
    {
        if (col.name == "head") return;

        if (col.name == "thorax" || col.name == "thorax_back" || col.name == "thorax_back_neck") return;

        if (col.name == "stomach") return;
       // Debug.Log($"Lc = {lightBleedingChance}, hc={heavyBleedingChance},  {fractureChance}, ");
        bool isLightBleeding = (UnityEngine.Random.value <= lightBleedingChance);
        bool isHeavyBleeding = (UnityEngine.Random.value <= heavyBleedingChance);
        bool isFracture = (UnityEngine.Random.value <= fractureChance);
        //확률 안걸리면 스킵
        if (!isLightBleeding && !isHeavyBleeding && !isFracture) return;
        //상호베타, 과다출혈과 일반 출혈 동시 발생시 과다출혈만 인정
        if (isLightBleeding && isHeavyBleeding) isLightBleeding = false; 

        if (col.name == "left_arm" || col.name == "left_forearm" || col.name == "left_hand")
        {
            var s = status[BodyParts.LeftArm];

            if(isLightBleeding) s.light = true; 
           
            if(isHeavyBleeding) s.heavy = true; 
           
            if(isFracture) s.fracture = true;
            
            status[BodyParts.LeftArm] = s;
           // Debug.Log($"lb = {s.light}, hb={s.heavy},  f{s.fracture}, ");
            MarkDirty(BodyParts.LeftArm);
            return;
        }

        if (col.name == "right_arm" || col.name == "right_forearm" || col.name == "right_hand")
        {
            var s = status[BodyParts.RightArm];

            if (isLightBleeding) s.light= true; 

            if (isHeavyBleeding) s.heavy = true; 

            if (isFracture) s.fracture = true;

            status[BodyParts.RightArm] = s;
            MarkDirty(BodyParts.RightArm);
            return;
        }

        if (col.name == "left_thigh" || col.name == "left_shin" || col.name == "left_foot")
        {
            var s = status[BodyParts.LeftLeg];

            if (isLightBleeding) s.light = true; 

            if (isHeavyBleeding) s.heavy = true; 
        
            if (isFracture) s.fracture = true;

            status[BodyParts.LeftLeg] = s;
            MarkDirty(BodyParts.LeftLeg);
            return;
        }

        if (col.name == "right_thigh" || col.name == "right_shin" || col.name == "right_foot")
        {
            var s = status[BodyParts.RightLeg];

            if (isLightBleeding) s.light = true;
      
            if (isHeavyBleeding) s.heavy = true;
       
            if (isFracture) s.fracture = true;

            status[BodyParts.RightLeg] = s;
            MarkDirty(BodyParts.RightLeg);
            return;
        }
    
    }
    
    private void CheckBleedingEffects()
    {
        var aliveParts = GetAliveParts();
        //왼팔
        if (status[BodyParts.LeftArm].light || status[BodyParts.LeftArm].heavy)
        {
            float tickDam = (status[BodyParts.LeftArm].light? health.lightPerTickDam : health.heavyPerTickDam);
        
            if (hp[BodyParts.LeftArm] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part]-(tickDam + health.tickPenaltyMul));
                    MarkDirty(part);
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {

                    if (part == BodyParts.LeftArm)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                        MarkDirty(part);
                    }
                    else
                    {
                       hp[part] = Mathf.Max(0, hp[part] - tickDam); 
                       MarkDirty(part); 
                    }
                }
            }
          
        }
       
        //오른팔
        if(status[BodyParts.RightArm].light || status[BodyParts.RightArm].heavy)
        {
            float tickDam = (status[BodyParts.RightArm].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.RightArm] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                    MarkDirty(part);
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.RightArm)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                        MarkDirty(part);
                    }
                    else
                    {
                        hp[part] = Mathf.Max(0, (hp[part] - tickDam)); 
                        MarkDirty(part);
                    } 
                }
            }
        }
       
        //왼다리
        if (status[BodyParts.LeftLeg].light || status[BodyParts.LeftLeg].heavy)
        {
            float tickDam = (status[BodyParts.LeftLeg].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.LeftLeg] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                    MarkDirty(part);
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.LeftLeg)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                        MarkDirty(part);
                    }
                    else
                    {
                        hp[part] = Mathf.Max(0, hp[part] - tickDam);
                        MarkDirty(part);
                    }
                    
                }
            }
        }
       
        //오른다리
        if (status[BodyParts.RightLeg].light || status[BodyParts.RightLeg].heavy)
        {
            float tickDam = (status[BodyParts.RightLeg].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.RightLeg] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                    MarkDirty(part);
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.RightLeg)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                        MarkDirty(part);
                    }
                    else
                    {
                        hp[part] = Mathf.Max(0, hp[part] - tickDam); 
                        MarkDirty(part);
                    }
                }
            }
        }

    }

    private List<BodyParts> GetAliveParts()
    {
        List<BodyParts> aliveParts = new List<BodyParts>();

        //살아 있는 부분만 계산
        foreach (var parts in hp)
        {
            if (parts.Value > 0.0f) aliveParts.Add(parts.Key);
            // Debug.Log($"살아있는 부위 ={parts}, 해당 부위 체력 ={parts.Value}");

        }

        return aliveParts;
    }
    private List<BodyParts> GetAllParts()
    {
        List<BodyParts> allParts = new List<BodyParts>();


        foreach (var parts in hp)
        {
            allParts.Add(parts.Key);
            
        }

        return allParts;
    }
    private void DistributeDamageToOtherParts(float damage)
    {
       
        float remaining = damage;
       // Debug.Log(damage);
        int maxRound = 0;

        var allParts = GetAllParts();
        if (allParts.Count == 0) return;

        //잔여 피해 x, 최대 5번까지만 진행
        while (remaining > 1.0f && maxRound < 5) 
        {

            float distributeDamage = remaining / allParts.Count;
            //너무 낮은 분산 대미지는 패스
            if (distributeDamage < 0.05f) break;

            float overflowDam = 0.0f;
            // 분배 대미지 각 부분에 적용, 만약 분배 대미지 얻다가 부위 사망시 => 반복적으로 호출
            foreach (var parts in allParts)
            {
                if (distributeDamage > hp[parts])
                {
                    overflowDam += distributeDamage - hp[parts];
                    //머리와 흉부는 초과 대미지 받을시 -로 => 사망
                    if (parts == BodyParts.Head)
                    {
                        hp[parts] -= 1.0f;
                        MarkDirty(parts);

                    }
                    else if (parts == BodyParts.Thorax)
                    {
                        hp[parts] -= 1.0f;
                        MarkDirty(parts);
                    }
                    else hp[parts] = 0.0f; MarkDirty(parts);

                }
                else
                {
                    //부위별 대미지 적용
                    if (parts == BodyParts.Head)
                    {                      
                        hp[parts] -= distributeDamage * damMul[BodyParts.Head];
                        MarkDirty(parts);


                    }
                    else if(parts == BodyParts.Thorax)
                    {
                        hp[parts] -= distributeDamage * damMul[BodyParts.Thorax];
                        MarkDirty(parts);
                    }
                    else
                    {                     
                        hp[parts] -= distributeDamage * health.defaultDamageDistributeMul;
                        MarkDirty(parts);
                    }
                       
                }
               // Debug.Log($"현재 부위 ={parts}, 체력 ={hp[parts]} 받은 대미지 ={distributeDamage}");

            }
            /*//테스트문
            foreach (var parts in hp)
            {
                if (parts.Value > 0.0f) Debug.Log($"살아있는 부위 ={parts.Key}, 해당 부위 체력 ={parts.Value}");

            }*/
           // Debug.Log($"토탈 대미지= {remaining}, 각 부위별 분산 대미지 = {distributeDamage}, 잔여 대미지 ={overflowDam}");

            maxRound++;
            remaining = overflowDam;
        }
       
     
    }
    public bool CheckHP()
    {
        totalHP = GetTotalHP();
       
        if (hp[BodyParts.Head] < 0.0f || hp[BodyParts.Thorax] < 0.0f || totalHP <= 0)
        {
            return true;
        }
        
        return false;
    }
    public void GetBluntDamage(float bluntDam)
    {
        
        float distributeDam = bluntDam / (float)GetAllParts().Count;

        var parts = GetAllParts();
        //Debug.Log(distributeDam);
        foreach (var part in parts)
        {
            hp[part] = Mathf.Max(0, hp[part] - distributeDam); 
            MarkDirty(part);
        }
     }
    public float GetTotalHP()
    {
        float totalHP = 0.0f;
        foreach (var part in hp)
        {
            totalHP += part.Value;
        }

        return totalHP;
    }
    public float GetMaxHP()
    {
        float maxHP = 0.0f;
        foreach (var part in maxHp)
        {
            maxHP += part.Value;
        }

        return maxHP;
    }
    private float CalculateDamage(float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
 
        bool isCritical = (UnityEngine.Random.value <= ammoCriticalChance);

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

        //출혈부위 먼저
        foreach(var part in status)
        {
            if (part.Value.light) return part.Key;
        }

        // 다음에 피가 가장적은 부위 먼저 치료
        foreach (var part in hp)
        {
            //체력 맥스는 제외
            if (maxHp[part.Key] == part.Value) continue;
            //블랙 아웃도 제외
            if (part.Value <= 0.0f) continue;

            if (part.Value < minHP && part.Value > 0.0f) 
            {   
                minHP = part.Value; 
                urgentParts = part.Key; 
            }
        }
        //Debug.Log(urgentParts);
        return urgentParts;
    }
    public bool GetHealEffects(BodyParts bodyParts, float healAmounts)
    {
        if (!hp.ContainsKey(bodyParts) || healAmounts <= 0.0f) return false;

        if (hp[bodyParts] == maxHp[bodyParts]) return false;

        var parts = bodyParts;
        //Debug.Log($"before hp = {hp[parts]}, before max hp = {maxHp[parts]}, parts ={parts}");
        //치료양이 전체보다 많으면 꽉 채우고 아니면 힐량만큼 채우기
        if (hp[parts] + healAmounts > maxHp[parts])
        {
            hp[parts] += maxHp[parts] - hp[parts];
        }
        else hp[parts] += healAmounts;

        //Debug.Log($"after hp = {hp[parts]}, after max hp = {maxHp[parts]}");
        MarkDirty(parts);

        return true;

    }

    public bool FixBlackoutEffects(BodyParts bodyParts)
    {
        if (!status.ContainsKey(bodyParts)) return false;

        if (!status[bodyParts].blackout) return false;

        var part = bodyParts;
        var s = status[part];
        
        hp[part] = 1.0f;
        s.blackout = false;
        maxHp[part] *= 0.8f;

        status[part] = s;

        MarkDirty(part);
        return true;
    }
    public bool FixFractureEffects(BodyParts bodyParts)
    {
        if (!status.ContainsKey(bodyParts)) return false;

        if (!status[bodyParts].fracture) return false;

        var part = bodyParts;
        var s = status[part];

        s.fracture = false;
        
        status[part] = s;
      
        MarkDirty(part);
        return true;
        
    }
    public bool StopBleedingEffects(BodyParts bodyParts, bool lightB, bool heavyB)
    {
        if (!status.ContainsKey(bodyParts)) return false;

        if (!status[bodyParts].light && !status[bodyParts].heavy) return false;

        var part = bodyParts;

        if (lightB)
        {
            var s = status[part];
            s.light=false;
            status[part] = s;
        }
        if (heavyB)
        {
            var s = status[part];
            s.heavy = false;
            status[part] = s;
        }

        MarkDirty(part);
        return true;

    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkDirty(BodyParts p)
    {
        if (!areYouPlayer) return;
        // 중복 삽입 방지
        if (!changedParts.Contains(p)) changedParts.Add(p);
    }

    private void NotifyDirty()
    {
        if (changedParts.Count == 0 || !areYouPlayer) return;

        tmpSnapshots.Clear();
        //변경된 부분이 있다면 해당 부분을 리스트에 넣고 이벤트 호출
        for (int i = 0; i < changedParts.Count; i++)
        {
            var p = changedParts[i];
            var snap = MakeSnapshot(p);
            tmpSnapshots.Add(snap);
            //OnPartChanged?.Invoke(snap); 
        }
        //전체으로 바뀐 목록 반환
        OnBatchChanged?.Invoke(tmpSnapshots);
        OnOverallChanged?.Invoke(GetOverallSnapshot());

        changedParts.Clear();
    }

    public bool GetIsLeftArmFracture()
    {
        return status[BodyParts.LeftArm].fracture;
    }
    public bool GetIsRightArmFracture()
    {
        return status[BodyParts.RightArm].fracture;
    }
    public bool GetIsLeftLegFracture()
    {
        return status[BodyParts.LeftLeg].fracture;
    }
    public bool GetIsRightLegFracture()
    {
        return status[BodyParts.RightLeg].fracture;
    }
    public bool GetIsLeftArmBlackout()
    {
        return status[BodyParts.LeftArm].blackout;
    }
    public bool GetIsRightArmBlackout()
    {
        return status[BodyParts.RightArm].blackout;
    }
    public bool GetIsLeftLegBlackout()
    {
        return status[BodyParts.LeftLeg].blackout;
    }
    public bool GetIsRightLegBlackout()
    {
        return status[BodyParts.RightLeg].blackout;
    }
    public int GetNumberLightBleeding()
    {
        int count = 0;
        foreach(var parts in status)
        {
            if (parts.Value.light) count++;
        }

        return count;
    }
    public int GetNumberHeavyBleeding()
    {
        int count = 0;
        foreach (var parts in status)
        {
            if (parts.Value.heavy) count++;
        }

        return count;
    }
    public float GetPartHP(BodyParts part)
    {
        return hp[part];
    }
    public float GetPartMaxHP(BodyParts part)
    {
        return maxHp[part];
    }
    public bool GetHasLightBleed(BodyParts part)
    {
        return status[part].light;
    }
    public bool GetHasHeavyBleed(BodyParts part)
    {
        return status[part].heavy;
    }
    public bool GetHasFracture(BodyParts part)
    {
        return status[part].fracture;
    }
    public bool GetHasBlackout(BodyParts part)
    {
        return status[part].blackout;
    }
}
