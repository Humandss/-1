using UnityEngine;

public interface IWeaponFireContext
{
    WeaponOwnerType GetOwnerType();
    bool TryGetEnemyFireContext(out IGetBulletDirection directionProvider);
}

