using UnityEngine;

public class ConsumableItemManager
{
    public ConsumableItems def;
    public int remaining;

    public ConsumableItemManager(ConsumableItems def, int remaining)
    {
        this.def = def;
        this.remaining = remaining;
     
    }
    public bool ApplyAll(HealthManager healthManager, BodyParts? target = null)
    {
        if (healthManager == null || remaining <= 0 || def == null || def.effects == null) return false;

        bool any = false;

        foreach (var e in def.effects) any |= e.ApplyHealthEffects(healthManager, target);

        if (any) remaining--;   
      
        return any;
    }
    public bool CanApplyAll(HealthManager healthManager, BodyParts? target = null)
    {
        if (healthManager == null || remaining <= 0 || def == null || def.effects == null) return false;
        //효과 적용이 가능하다 판단되면 true반환
        foreach (var e in def.effects)
        {
            if (e != null && e.CanApply(healthManager, target)) return true;
        }
        return false;
    }
}
