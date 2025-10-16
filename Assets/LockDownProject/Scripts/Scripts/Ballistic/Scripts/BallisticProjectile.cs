
using UnityEngine;


public class BallisticProjectile : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BulletInfo ammo;
    private BulletSoundController bulletSoundController;
    private MaterialManager materialManager;
    private HealthManager healthManager;
    private LayerMask layerMask;

    [Header("Providers")]
    private IBulletSoundProvider bulletSoundProvider;
    private IMaterialInfoProvider materialInfoProvider;
    private ICheckBodyHit bodyHitProvider;

    [Header("Bullet Value")]
    private Vector3 velocity;
    private float refArea;
    private Vector3 pos;
    private Vector3 prevPos;
    private Vector3 dir;
    private float flightTime;
    private float k; // 공기저항
    private int ricochetChance=0;
    float speed;

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
    private void Start()
    {
        bulletSoundController = GetComponent<BulletSoundController>();
        if(bulletSoundController == null)
        {
            Debug.LogWarning("[BallisticProjectile] bulletSoundController is NULL");
        }

        bulletSoundProvider = bulletSoundController as IBulletSoundProvider;
        if (bulletSoundProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile]  bulletSoundProvide is NULL");
        }

     
        layerMask = LayerMask.GetMask("Head","Thorax","Stomach", "Left_arm", "Right_arm", "Left_leg", "Right_leg","Default");

    }
    public void Initialize(Vector3 position, Vector3 direction)
    {
        pos=position;
        dir=direction; 

        velocity = dir.normalized * ammo.muzzleVelocity;   // 초기 속도 

        float invMass = 1.0f / Mathf.Max(1e-6f, ammo.mass); // 1/중량

        float r = Mathf.Max(1e-6f, (ammo.caliberMm * 0.001f)) * 0.5f; // m로 바꾸기
        refArea = Mathf.PI * r * r * ammo.refAreaScale; // 단면적(m)

        k = 0.5f * airDensity * ammo.dragCoeff * refArea * invMass;

        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        gameObject.SetActive(true);

    }
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        flightTime += dt;
        if (flightTime > ammo.lifeTime) { Destroy(gameObject); return; }

        prevPos = pos;
        //바람 저항
        Vector3 vRel = velocity - windWorld;
        //숫자 0이 되지 않게끔
        speed = vRel.magnitude + 1e-6f;
        //중력 계수*공기저항
        Vector3 g = Physics.gravity + (-k * vRel * speed);

        //속도 및 포지션 변환
        velocity += g * dt;
        pos += velocity * dt;

        HandleImpact(prevPos);
       
        transform.position = pos;

       //Debug.Log($"ammo type ={ammo.name}, pos={pos}, Vector_velocity={velocity}, time={flightTime}, distance={(flightTime*velocity).z}");
    }

    private void HandleImpact(Vector3 prevPos)
    {
        //seg가 갱신될때마다 Layer체크
        Vector3 seg = pos - prevPos;
        float segLen = seg.magnitude;
        if (segLen > 0.0f)
        {
            //매 업데이트마다 총알 방향
            Vector3 segDir = seg / segLen;
            if (Physics.Raycast(prevPos, segDir, out var hit, segLen, layerMask, QueryTriggerInteraction.Ignore))
            {
                
                //피탄 지점과 탄 방향을 내적
                float cosToNormal = Mathf.Clamp(Vector3.Dot(-segDir, hit.normal.normalized), -1.0f, 1.0f);
                // 90도에서 내적을 빼면 입사각도
                float incAngleToPlane = 90.0f - Mathf.Acos(cosToNormal) * Mathf.Rad2Deg;

                //일단 기본레이어 벽같은 거에 닿았을 때 처리
                if (LayerMask.LayerToName(hit.collider.gameObject.layer) == "Default") 
                {
                    //각 재질에 따른 보정각
                    float compensateAngle= ammo.baseRicochetAngleDeg*GetMaterialRicochetFactor(hit.collider);

                    //Debug.Log($"[HIT] {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} dist={hit.distance:F3}");
                    if (compensateAngle >= incAngleToPlane && ricochetChance < 1) 
                    {
                        HandleRicochet(hit, segDir);
                        bulletSoundProvider.PlayRicochetSound();
                        return;
                    }
                    else
                    {
                        if (GetMaterialName(hit.collider) == "Metal")
                        {
                            bulletSoundController.PlayMetalImpactSound(hit.point);
                        }
                        if(GetMaterialName(hit.collider) == "Floor" || GetMaterialName(hit.collider) == "Concrete")
                        {
                            bulletSoundController.PlayDefaultImpactSound(hit.point);
                        }

                        Destroy(gameObject);
                        return;
                    }
                   
                }
                //그외 사람한테 닿았을 경우
                else
                {
                    //Debug.Log($"[HIT] {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} dist={hit.distance:F3}");
                    CheckBulletHitBody(hit.collider);
                    Destroy(gameObject);
                    return;
                }
                
            }
            else
            {
                //Debug.Log($"[MISS] mask={layerMask.value} len={segLen:F3}");
            }
        }
        
    }
    private void HandleRicochet(RaycastHit hit, Vector3 vDir)
    {
        Vector3 recochetAngle = Vector3.Reflect(vDir, hit.normal).normalized;
        //도탄후 랜덤으로 도탄될 각 기준 정하기
        Vector3 axis = Vector3.Cross(hit.normal, recochetAngle);
        axis.Normalize();
        //도탄 됐을 경우 퍼질 수 있는 최대각
        float maxRecochetAngle = Mathf.Lerp(0.0f, 6.0f, Mathf.Clamp01(ammo.randomRicochetAngle));
        float angle = UnityEngine.Random.Range(-maxRecochetAngle, maxRecochetAngle);
        //최종 도탄 앵글
        recochetAngle = (Quaternion.AngleAxis(angle, axis)* recochetAngle).normalized;
        //도탄 후 에너지
        float aterRicochetSpeed = speed * ammo.afterRicochetEnergyPercent;
        //최종 계산
        velocity = recochetAngle * aterRicochetSpeed;
        pos = hit.point+hit.normal * 0.002f;
        transform.position = pos;
        ricochetChance++;
            
    }
 
    private void CheckBulletHitBody(Collider col)
    {
        healthManager = col.GetComponentInParent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[BallisticProjectile]  healthManager is NULL");
        }

        bodyHitProvider = healthManager as ICheckBodyHit;
        if (bodyHitProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile]  bodyHitProvider is NULL");

        }

        bodyHitProvider.CheckBodyHit(col, ammo.damage, ammo.criticalChance, ammo.criticalDamMultiplier);
    }
    float GetMaterialRicochetFactor(Collider col, float defaultFactor = 0.5f)
    {
        materialManager = col.GetComponent<MaterialManager>();
        if(materialManager == null)
        {
            Debug.LogWarning("[BallisticProjectile] materialManager is NULL");
            return defaultFactor;
        }

        materialInfoProvider = materialManager as IMaterialInfoProvider;
        if (materialInfoProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile] materialFactorProvider is NULL");
            return defaultFactor;
        }

        return materialInfoProvider.GetMaterialFactor();

    }

    string GetMaterialName(Collider col)
    {
        materialManager = col.GetComponent<MaterialManager>();
        if (materialManager == null)
        {
            Debug.LogWarning("[BallisticProjectile] materialManager is NULL");
          
        }

        materialInfoProvider = materialManager as IMaterialInfoProvider;
        if (materialInfoProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile] materialFactorProvider is NULL");
            
        }

        return materialInfoProvider.GetMaterialName();

    }
}
