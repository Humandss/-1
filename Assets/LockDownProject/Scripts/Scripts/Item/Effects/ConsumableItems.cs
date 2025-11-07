using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableItems : ScriptableObject
{

    public string displayName;
    public float useTime = 3.0f;
    public int remaining = 1;      
    // ³²Àº È½¼ö
    public ItemEffects[] effects;

  
}
