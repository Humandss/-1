using UnityEngine;

public interface IMaterialFactorProvider
{
    float GetMaterialFactor();
}
public class MaterialManager : MonoBehaviour, IMaterialFactorProvider
{
    [SerializeField] private RicochetMaterialProfile profile;


    public float GetMaterialFactor()
    {
        return profile.materialFactor;
    }
}
