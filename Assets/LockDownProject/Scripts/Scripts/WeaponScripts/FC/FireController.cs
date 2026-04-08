using UnityEngine;

public interface IFireBulletProvider
{
    void FireBullet();
}

public abstract class FireController : MonoBehaviour, IFireBulletProvider
{
    [Header("Refs")]
    [SerializeField] protected Transform muzzle;
    [SerializeField] protected GameObject muzzleFlash;
    [SerializeField] protected BallisticProjectile projectile;

    public abstract void FireBullet();

    protected bool ValidateRefs()
    {
        if (muzzle == null)
        {
            Debug.LogWarning("[FireController] muzzle is NULL");
            return false;
        }

        if (projectile == null)
        {
            Debug.LogWarning("[FireController] projectile is NULL");
            return false;
        }

        return true;
    }

    protected bool TrySpawnBullet(Vector3 dir, out BallisticProjectile bullet)
    {
        bullet = null;

        Vector3 spawnPos = muzzle.position + dir * 0.01f;
        GameObject bulletObj = PoolManager.Instance.Spawn(projectile.gameObject, spawnPos, Quaternion.LookRotation(dir));
        if (bulletObj == null)
        {
            Debug.LogWarning("[FireController] bulletObj is NULL from PoolManager");
            return false;
        }

        bullet = bulletObj.GetComponent<BallisticProjectile>();
        if (bullet == null)
        {
            Debug.LogWarning("[FireController] Bullet component is NULL on spawned object");
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
        return projectile != null ? projectile.name : string.Empty;
    }

    private void OnDrawGizmosSelected()
    {
        if (muzzle)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(muzzle.position, muzzle.forward * 0.5f);
        }
    }
}
