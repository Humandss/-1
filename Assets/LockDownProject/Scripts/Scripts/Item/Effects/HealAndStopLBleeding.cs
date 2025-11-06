using UnityEngine;

[CreateAssetMenu(menuName = "Items/Effects/HealAndStopLight")]
public class HealAndStopLBleeding : ItemEffects
{
    [SerializeField] private float healingAmounts = 0.0f;

    public override bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartsForHealing();
        bool changed = false;
        
        changed |= healthManager.GetHealEffects(part, healingAmounts);
        changed |= healthManager.StopBleedingEffects(part, true, false);

        return changed;
    }
}
