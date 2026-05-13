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

    private Collider[] cachedColliders;

    private void OnEnable()
    {
        // 자가 콜라이더(자식 포함) 모두 ColliderMaterialRegistry + ColliderRegistry에 등록.
        // BulletHitProcessor가 colliderInstanceID로 MaterialManager + Collider 조회 가능.
        cachedColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            LockDown.Ballistic.Job.ColliderRegistry.Register(cachedColliders[i]);
            LockDown.Ballistic.Job.ColliderMaterialRegistry.Register(cachedColliders[i], this);
        }
    }

    private void OnDisable()
    {
        if (cachedColliders == null) return;
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            LockDown.Ballistic.Job.ColliderRegistry.Unregister(cachedColliders[i]);
            LockDown.Ballistic.Job.ColliderMaterialRegistry.Unregister(cachedColliders[i]);
        }
    }

    public float GetMaterialPenetrationFactor()
    {
        if (profile.materialType == MaterialType.Metal) return 0.8f;

        if (profile.materialType == MaterialType.Concrete) return 0.6f;

        if (profile.materialType == MaterialType.Wood) return 0.3f;

        if (profile.materialType == MaterialType.Body) return 0.0f;

        if (profile.materialType == MaterialType.Head) return 0.0f;

        return 0.5f;
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
