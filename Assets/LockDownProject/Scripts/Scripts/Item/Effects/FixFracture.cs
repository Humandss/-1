using UnityEngine;

[CreateAssetMenu(menuName = "Items/Effects/FixFracture")]
public class FixFracture : ItemEffects
{
    public override bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForFixFracture();

        if(part == BodyParts.None) return false;

        return healthManager.FixFractureEffects(part);
    }
    public override bool CanApply(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForFixFracture();
        return part != BodyParts.None && healthManager.GetHasFracture(part);
    }
}
