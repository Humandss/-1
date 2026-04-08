using UnityEngine;

public class WeaponFireController : FireController
{
    private IWeaponFireContext fireContext;

    public void Configure(IWeaponFireContext context)
    {
        fireContext = context;
    }

    public override void FireBullet()
    {
        if (!ValidateRefs()) return;

        Vector3 dir = muzzle.forward.normalized;
        bool isPlayerShot = true;

        if (fireContext != null)
        {
            var ownerType = fireContext.GetOwnerType();
            isPlayerShot = ownerType == WeaponOwnerType.Player || ownerType == WeaponOwnerType.TutorialDummy;

            if (ownerType == WeaponOwnerType.Enemy &&
                fireContext.TryGetEnemyFireContext(out var directionProvider) &&
                directionProvider != null)
            {
                dir = BuildEnemyDirection(directionProvider);
            }
        }

        if (!TrySpawnBullet(dir, out var bullet)) return;

        bullet.Initialize(bullet.transform.position, dir, isPlayerShot);
        SpawnMuzzleFlash(dir);
    }

    private Vector3 BuildEnemyDirection(IGetBulletDirection directionProvider)
    {
        Vector3 baseDir = directionProvider.GetBulletDirection();
        float horizontalOffset = directionProvider.GetHoriontalOffset();
        float verticalOffset = directionProvider.GetVerticalOffset();

        float h = Random.Range(-horizontalOffset, horizontalOffset);
        float v = Random.Range(-verticalOffset, verticalOffset);

        Quaternion spreadRot = Quaternion.AngleAxis(h, muzzle.right) * Quaternion.AngleAxis(v, muzzle.up);
        Vector3 dir = (spreadRot * baseDir).normalized;

        if (dir.sqrMagnitude < 0.0001f ||
            float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z))
        {
            return muzzle.forward;
        }

        return dir;
    }
}
