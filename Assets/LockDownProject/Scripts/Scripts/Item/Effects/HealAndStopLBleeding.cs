using UnityEngine;

[CreateAssetMenu(menuName = "Items/Effects/HealAndStopLight")]
public class HealAndStopLBleeding : ItemEffects
{
    [SerializeField] private float healingAmounts = 0.0f;

    public override bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForHealing();

        if (part == BodyParts.None) return false;

        bool changed = false;
        
        changed |= healthManager.GetHealEffects(part, healingAmounts);
        changed |= healthManager.StopBleedingEffects(part, true, false);

        return changed;
    }
    public override bool CanApply(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForHealing();

        if (part == BodyParts.None) return false;

        // 해당 부위가 블랙 아웃 or 풀피면 힐 불가, 라이트 출혈 있으면 OK
        if(healthManager.GetHasBlackout(part)) return false;

        bool canHeal = healthManager.GetPartHP(part) < healthManager.GetPartMaxHP(part);
        bool hasLight = healthManager.GetHasLightBleed(part);

        return canHeal || hasLight;
    }

}
