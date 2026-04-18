using UnityEngine;

public enum ArmorMaterial
{
    Composite,
    Kevlar,
    Titanium,
    Steel,
    Ceramic,
}
public enum ArmorClass
{
   Level1,
   Level2A,
   Level2,
   Level3A,
   Level3,
   Level4
}
[CreateAssetMenu(menuName = "Armor/Armor Info")]
public class ArmorInfo : ScriptableObject
{
    [Header("Name")]
    public string armorName = "";
    [TextArea] public string description;

    [Header("Info")]
    public ArmorMaterial material;
    public ArmorClass armorClass;
    [Range(0.0f, 100.0f)] public float durability = 0.0f;
    
   
}
