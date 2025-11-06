using UnityEngine;

public abstract class ItemEffects : ScriptableObject
{
    public abstract bool ApplyHealthEffects(HealthManager healthManager, BodyParts? target = null);
  
    //다른 기타 효과는 추후 추가 예정
}
