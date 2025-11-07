using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Effects/FixBlackout")]
public class FixBlackout : ItemEffects
{
    public override bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentPartForFixBlackout();

        if(part == BodyParts.None) return false;

        return healthManager.FixBlackoutEffects(part);
    }
    public override bool CanApply(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentPartForFixBlackout();
        return part != BodyParts.None && healthManager.GetHasBlackout(part);
    }
}
