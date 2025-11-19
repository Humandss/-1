using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IFireBulletProvider
{
    void FireBullet();

    //void EnemyFireBullet();
}

public class WeaponFireController : MonoBehaviour, IFireBulletProvider
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private BallisticProjectile projectile;

    private void Initialize()
    {
        if(muzzle == null)
        {
            Debug.LogWarning("[WeaponFireController] muzzle is NULL");
        }
        if(projectile == null)
        {
            Debug.LogWarning("[WeaponFireController] projectile is NULL");
        }
        var bullet = Instantiate(projectile);
        //총 발사 방향 세팅
        Vector3 dir = muzzle.forward.normalized;
        //총알 스폰 장소 세팅
        Vector3 spawnPos = muzzle.position + dir * 0.01f;

        bullet.Initialize(spawnPos, dir);
    }
    public void FireBullet()
    {
         Initialize();    
    }
    void OnDrawGizmosSelected()
    {
        if (muzzle) { Gizmos.color = Color.yellow; Gizmos.DrawRay(muzzle.position, muzzle.forward * 0.5f); }
    }

    public string GetAmmoName()
    {
        return projectile.name;
    }
   
}
