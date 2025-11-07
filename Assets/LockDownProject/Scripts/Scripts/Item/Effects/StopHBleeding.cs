using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Effects/StopHeavyBleed")]
public class StopHBleeding : ItemEffects
{
    public override bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForStopHBleeding();

        if(part == BodyParts.None) return false;

        return healthManager.StopBleedingEffects(part, false, true);
    }
    public override bool CanApply(HealthManager healthManager, BodyParts? target = null)
    {
        var part = target ?? healthManager.GetUrgentBodyPartForStopHBleeding();
        return part != BodyParts.None && healthManager.GetHasHeavyBleed(part);
    }
}
