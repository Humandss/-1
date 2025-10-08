using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class WeaponFireController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private BulletInfo ammo;
    [SerializeField] private BallisticProjectile projectile;
    private LayerMask layer;


    private void Initialize()
    {
        //총알 인스턴스화
        var bullet = Instantiate(projectile);
        //총 발사 방향 세팅
        Vector3 dir = muzzle.forward.normalized;
        //총알 스폰 장소 세팅
        Vector3 spawnPos = muzzle.position + dir * 0.01f;
        //속도
        float velocity = ammo.muzzleVelocity;
    }
    public void FireBullet()
    {
         Initialize();

    }
}
