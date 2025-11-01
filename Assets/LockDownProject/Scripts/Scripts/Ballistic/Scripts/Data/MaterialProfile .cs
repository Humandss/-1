using UnityEngine;

public enum MaterialType
{
    Floor,
    Concrete,
    Metal,
    Armor,
    Wood,
    Human,
    None,

}
[CreateAssetMenu(menuName = "Ballistics/Material Profile")]
public class MaterialProfile : ScriptableObject
{
    [Header("Material")]
    public string materialName = "";
    public MaterialType materialType;
   
    [Header("Material Factor")]
    [Range(0f, 3.0f)] public float materialRicochetFactor = 0.2f;
   
}
