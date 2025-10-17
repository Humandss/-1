using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Ballistics/Bullet Info")]
public class BulletInfo : ScriptableObject
{

    [Header("Info")]
    public string ammoName = "5.56x45mm M855";
    [TextArea] public string description;

    [Header("Ballistics")]
    public string ammoKey = "5.56x45NATO";   // 구경(텍스트)
    [Range(4.0f, 15.0f)] public float caliberMm = 5.56f; // 구경 mm단위
    [Range(0.001f, 0.05f)] public float mass = 0.004f;       // g
    [Range(100.0f, 1500.0f)] public float muzzleVelocity = 920.0f; // 속도 m/s
    [Range(0.0f, 1.0f)] public float dragCoeff = 0.3f;    // 탄두 형상에 따른 공지 저항 계수
    [Range(0.0f, 15.0f)] public float refAreaScale = 1.0f;   // 단면적 스케일 mm단위
    [Range(5.0f, 120.0f)] public float penetrationPower = 35.0f; // 관통력
    public bool tracer; // 예광 유무

    [Header("Ricochet")]
    [Range(0.0f, 100.0f)] public float baseRicochetAngleDeg = 24.0f;
    [Range(0.0f, 1.0f)] public float afterRicochetEnergyPercent = 0.62f;
    [Range(0.0f, 5.0f)] public float randomRicochetAngle = 0.28f;

    [Header("Damage")]
    [Range(37.0f, 150.0f)] public float damage = 45.0f;
    [Range(0.000f, 1.0f)] public float criticalChance =  0.035f;
    [Range(0.0f, 1.0f)] public float criticalDamMultiplier = 0.21f;

    [Header("Effects")]
    [Range(0.000f, 1.0f)] public float lightBleedingChance = 0.001f;
    [Range(0.000f, 1.0f)] public float heavyBleedingChance = 0.001f;
    [Range(0.000f, 1.0f)] public float fractureChance = 0.001f;

    [Header("LifeTime")]
    [Range(0.0f, 15.0f)] public float lifeTime = 0.0f;
}
