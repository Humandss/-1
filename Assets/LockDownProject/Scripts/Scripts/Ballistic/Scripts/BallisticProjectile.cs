
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class BallisticProjectile : MonoBehaviour
{
    [SerializeField] private BulletInfo ammo;

    [Header("Bullet Value")]
    private Vector3 velocity;
    private float refArea;
    private Vector3 pos;
    private Vector3 dir;
    private float flightTime;
    private float k; // 공기저항
    [Header("World")]
    private float airDensity = 1.225f;
    private Vector3 windWorld = Vector3.zero;
#if true // 탄 트레일 남기는 로직
    TrailRenderer trailRenderer;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();

        trailRenderer.time = 0.45f;              // 궤적이 남아있는 시간
        trailRenderer.minVertexDistance = 0.005f;
        trailRenderer.startWidth = 0.9f;       // 살짝 굵게
        trailRenderer.endWidth = 0.0f;
        trailRenderer.emitting = true;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
    }
#endif
    public void Initialize(Vector3 position, Vector3 direction)
    {
        pos=position;
        dir=direction; 

        velocity = dir.normalized * ammo.muzzleVelocity;   // 초기 속도 

        float invMass = 1.0f / Mathf.Max(1e-6f, ammo.mass); // 1/중량

        float r = Mathf.Max(1e-6f, (ammo.caliberMm * 0.001f)) * 0.5f; // m로 바꾸기
        refArea = Mathf.PI * r * r * (ammo.refAreaScale * 0.001f); // 단면적(m)

        k = 0.5f * airDensity * ammo.dragCoeff * refArea * invMass;

        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        gameObject.SetActive(true);

    }
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        flightTime += dt;
        if (flightTime > ammo.lifeTime) { Destroy(gameObject); return; }

        //바람 저항
        Vector3 vRel = velocity - windWorld;
        //숫자 0이 되지 않게끔
        float speed = vRel.magnitude + 1e-6f;
        //중력 계수*공기저항
        Vector3 g = Physics.gravity + (-k*vRel*speed);

        velocity += g * dt;
        pos += velocity * dt;
        transform.position = pos;

        Debug.Log($"ammo type ={ammo.name}, pos={pos}, Vector_velocity={velocity.z}, time={flightTime}, distance={(flightTime*velocity).z}");
    }
}
