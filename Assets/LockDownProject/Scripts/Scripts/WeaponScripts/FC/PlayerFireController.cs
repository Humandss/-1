using UnityEngine;

public class PlayerFireController : FireController
{
    public override void FireBullet()
    {
        if (!ValidateRefs()) return;

        Vector3 dir = muzzle.forward.normalized;
        if (!TrySpawnBullet(dir, out var bullet)) return;

        bullet.Initialize(bullet.transform.position, dir, true);
        SpawnMuzzleFlash(dir);
    }
}
