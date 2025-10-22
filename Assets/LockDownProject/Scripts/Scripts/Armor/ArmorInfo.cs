using UnityEngine;

public enum ArmorMaterial
{
    Compsite,
    Kevlar,
    Titanium,
    Steel,
    Ceramic,
}
[CreateAssetMenu(menuName = "Armor/Armor Info")]
public class ArmorInfo : ScriptableObject
{
    [Header("Name")]
    public string armorName = "";
    [TextArea] public string description;

    [Header("Info")]
    public ArmorMaterial material;
    [Range(0.0f, 100.0f)] public float durability = 0.0f;
    [Range(0.0f, 100.0f)] public float armorClass = 0.0f;
   
}
