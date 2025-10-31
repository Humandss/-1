using System.Collections.Generic;
using UnityEngine;

public interface IMaterialInfoProvider
{
    float GetMaterialRicochetFactor();
    string GetMaterialName();
    float GetMaterialPenetrationFactor();
    MaterialType GetMaterialType();
    bool GetIsPentrable();
}
public class MaterialManager : MonoBehaviour, IMaterialInfoProvider
{
    [Header("Refs")]
    [SerializeField] private MaterialProfile profile;
    public bool penetrable = true;
    public float GetMaterialPenetrationFactor()
    {
        if (profile.materialType == MaterialType.Metal) return 0.8f;

        if (profile.materialType == MaterialType.Concrete) return 0.5f;

        if (profile.materialType == MaterialType.Wood) return 0.1f;

        return 1.0f;
    }

    public bool GetIsPentrable()
    {
        return penetrable;
    }
    public MaterialType GetMaterialType()
    {
        return profile.materialType;
    }
    public float GetMaterialRicochetFactor()
    {
        return profile.materialRicochetFactor;
    }
    public string GetMaterialName()
    {
        return profile.materialName;
    }
}
