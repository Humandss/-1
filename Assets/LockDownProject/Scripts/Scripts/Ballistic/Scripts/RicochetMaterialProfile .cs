
using UnityEngine;
[CreateAssetMenu(menuName = "Ballistics/Ricochet Material Profile")]
public class RicochetMaterialProfile : ScriptableObject
{

    [Header("Material Factor")]
    [Range(0f, 3.0f)] public float materialFactor = 0.2f;
    
}
