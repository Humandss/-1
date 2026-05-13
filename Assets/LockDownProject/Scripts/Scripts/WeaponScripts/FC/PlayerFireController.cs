using UnityEngine;

public class PlayerFireController : FireController
{
    public override void FireBullet()
    {
        if (!ValidateRefs()) return;

        Vector3 dir = muzzle.forward.normalized;
        LockDown.Ballistic.Job.BulletSimulationSystem.Instance.Spawn(
            muzzle.position, dir, bulletInfo, isPlayerShot: true);

        SpawnMuzzleFlash(dir);
    }
}
