using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableItems : ScriptableObject
{
    public string name;
    public Sprite icon;
    public float useTime = 3.0f;
    public bool consumeOnCancel = false;
    public int charges = 1;                // ³²Àº È½¼ö
    public ItemEffects[] effects;

    public bool ApplyAll(HealthManager healthManager, BodyParts? target = null)
    {
        bool any = false;
        foreach (var e in effects) any |= e.ApplyHealthEffects(healthManager, target);
        if (any && charges > 0) charges--;
        return any;
    }
}
