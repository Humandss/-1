using UnityEngine;

public class EnemyFireController : FireController
{
    private EnemyController enemyController;
    private IGetBulletDirection getBulletDirection;

    private void Awake()
    {
        CacheEnemyRefs();
    }

    public override void FireBullet()
    {
        if (!ValidateRefs()) return;
        if (!CacheEnemyRefs()) return;

        Vector3 baseDir = getBulletDirection.GetBulletDirection();
        float horizontalOffset = getBulletDirection.GetHorizontalOffset();
        float verticalOffset = getBulletDirection.GetVerticalOffset();

        float h = Random.Range(-horizontalOffset, horizontalOffset);
        float v = Random.Range(-verticalOffset, verticalOffset);

        Quaternion spreadRot = Quaternion.AngleAxis(h, muzzle.right) * Quaternion.AngleAxis(v, muzzle.up);
        Vector3 dir = (spreadRot * baseDir).normalized;

        if (dir.sqrMagnitude < 0.0001f ||
            float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z))
        {
            Debug.LogWarning("[EnemyFireController] dir is invalid");
            dir = muzzle.forward;
        }

        if (!TrySpawnBullet(dir, out var bullet)) return;

        bullet.Initialize(bullet.transform.position, dir, false);
        SpawnMuzzleFlash(dir);
    }

    private bool CacheEnemyRefs()
    {
        if (enemyController == null)
        {
            enemyController = GetComponentInParent<EnemyController>();
        }

        if (enemyController == null)
        {
            Debug.LogWarning("[EnemyFireController] enemyController is NULL");
            return false;
        }

        if (getBulletDirection == null)
        {
            getBulletDirection = enemyController as IGetBulletDirection;
        }

        if (getBulletDirection == null)
        {
            Debug.LogWarning("[EnemyFireController] getBulletDirection is NULL");
            return false;
        }

        return true;
    }
}
