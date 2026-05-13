using UnityEngine;

public interface IFireBulletProvider
{
    void FireBullet();
}

/// <summary>
/// 무기 발사의 공통 베이스. Job/Burst 시뮬레이션으로 이전됨에 따라
/// 풀에서 BallisticProjectile GameObject를 꺼내는 대신 BulletSimulationSystem.Spawn 호출.
/// 발사 방향은 서브클래스가 결정.
/// </summary>
public abstract class FireController : MonoBehaviour, IFireBulletProvider
{
    [Header("Refs")]
    [SerializeField] protected Transform muzzle;
    [SerializeField] protected GameObject muzzleFlash;
    [SerializeField, Tooltip("발사할 탄종 SO. BulletSimulationSystem에 등록되어 있어야 함")]
    protected BulletInfo bulletInfo;

    public abstract void FireBullet();

    protected bool ValidateRefs()
    {
        if (muzzle == null)
        {
            Debug.LogWarning("[FireController] muzzle is NULL");
            return false;
        }

        if (bulletInfo == null)
        {
            Debug.LogWarning("[FireController] bulletInfo is NULL");
            return false;
        }

        if (LockDown.Ballistic.Job.BulletSimulationSystem.Instance == null)
        {
            Debug.LogWarning("[FireController] BulletSimulationSystem.Instance is NULL");
            return false;
        }

        return true;
    }

    protected void SpawnMuzzleFlash(Vector3 dir)
    {
        if (muzzleFlash == null || muzzle == null) return;

        PoolManager.Instance.Spawn(muzzleFlash, muzzle.position, Quaternion.LookRotation(dir));
    }

    public string GetAmmoName()
    {
        return bulletInfo != null ? bulletInfo.ammoName : string.Empty;
    }

    public BulletInfo GetBulletInfo() => bulletInfo;

    private void OnDrawGizmosSelected()
    {
        if (muzzle)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(muzzle.position, muzzle.forward * 0.5f);
        }
    }
}
