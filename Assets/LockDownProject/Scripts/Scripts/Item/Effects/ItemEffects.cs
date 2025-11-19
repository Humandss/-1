using UnityEngine;

public abstract class ItemEffects : ScriptableObject
{
    //효과 적용
    public abstract bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null);

    //효과 적용 가능한지 판단
    public virtual bool CanApply(HealthManager healthManager, BodyParts? target = null) => true;
}
