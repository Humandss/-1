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
        //수술 가능여부 체크 -> 파트가 머리거나, 흉부일 경우는 false반환
        if (part == BodyParts.None || part == BodyParts.Head || part == BodyParts.Thorax) return false;

        return healthManager.GetHasBlackout(part);
    }
}
