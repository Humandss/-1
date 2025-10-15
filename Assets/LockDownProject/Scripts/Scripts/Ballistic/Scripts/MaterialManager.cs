using System.Collections.Generic;
using UnityEngine;

public interface IMaterialInfoProvider
{
    float GetMaterialFactor();
    string GetMaterialName();
}
public class MaterialManager : MonoBehaviour, IMaterialInfoProvider
{
   
    [SerializeField] private RicochetMaterialProfile profile;

    public float GetMaterialFactor()
    {
        return profile.materialFactor;
    }
    public string GetMaterialName()
    {
        return profile.MaterialName;
    }
}
