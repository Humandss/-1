
using System;
using UnityEngine;



public class BallisticProjectile : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BulletInfo ammo;
    private BulletSoundController bulletSoundController;
    private MaterialManager materialManager;
    private HealthManager healthManager;
    private ArmorManager armorManager;
    private LayerMask layerMask;

    [Header("Providers")]
    private IBulletSoundProvider bulletSoundProvider;
    private IMaterialInfoProvider materialInfoProvider;
    private IHealthStateProvider healthStateProvider;
    private IGetFactorAfterPentrateBodyProvider factorAfterPentrateBodyProvider;
    private IArmorInfoProviders armorInfoProvider;


    [Header("Bullet Value")]
    private int id = 0; // 총알 아이디
    private static int idSeq = 0;
    private Vector3 velocity; //벡터 속력
    private Vector3 pos; //현 위치
    private Vector3 prevPos; // 이전 위치
    private Vector3 dir; //총알 방향
    private float refArea; //총알 면적
    private float flightTime;
    private int ricochetChance = 0;
    private float speed; //총알 속도
    private float pen; //총알 관통력
    private bool isPenetratingTerrain = false;
    private float armorDam;

    [Header("World")]
    private float airDensity = 1.225f;
    private Vector3 windWorld = Vector3.zero;
    private float k; // 공기저항
    private float energyLostPerM = 100.0f; // Terrain 관통했을 경우 에너지 감소율
    private const float exit = 0.004f;
    private const float enter = 0.002f;
    private float probeDist = 8.0f;// 역방향 탐침 거리
    private float maxDist = 20.0f; //최대 관통 거리

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

        id = System.Threading.Interlocked.Increment(ref idSeq);
        layerMask = LayerMask.GetMask("Head","Thorax","Stomach", "Left_arm", "Right_arm", "Left_leg", "Right_leg","Default", "Armor");

    }
    public void Initialize(Vector3 position, Vector3 direction)
    {
        //isHitOnce.Clear();

        pos=position;
        dir=direction.normalized;
        pen = ammo.penetrationPower;
        armorDam = ammo.armorDamage;

        velocity = dir * ammo.muzzleVelocity;   // 초기 속도 

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
                //각 재질에 따른 보정각
                float compensateAngle = ammo.baseRicochetAngleDeg * GetMaterialRicochetFactor(hit.collider);

                //기본레이어 벽같은 거에 닿았을 때 처리
                if (LayerMask.LayerToName(hit.collider.gameObject.layer) == "Default")
                {
                    if (isPenetratingTerrain) return;

                    //Debug.Log($"[HIT] {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} dist={hit.distance:F3}");
                    if (compensateAngle >= incAngleToPlane && ricochetChance < 1)
                    {
                        HandleRicochet(hit, segDir);
                        bulletSoundProvider.PlayRicochetSound();
                        return;
                    }
                    else
                    {
                        isPenetratingTerrain = true;
                        PlaySoundByMaterialName(hit);
                        HandleTerrainPenetration(hit, segDir);
                        return;
                    }

                }
                //사람이 착용한 방탄판에 닿았을경우
                if (LayerMask.LayerToName(hit.collider.gameObject.layer) == "Armor")
                {
                  
                    if (compensateAngle >= incAngleToPlane && ricochetChance < 1)
                    {
                        HandleRicochet(hit, segDir);
                        bulletSoundProvider.PlayRicochetSound();
                        HandleArmorDamageAfterRicochet(hit);
                        return;
                    }
                    else
                    {
                        PlaySoundByMaterialName(hit);
                        HandleArmorPenetration(hit);
                        return;
                    }
                    
                }
                //그외 사람한테 닿았을 경우
                else
                {
                    //Debug.Log($"[HIT] {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} dist={hit.distance:F3}");
                    CheckBulletHitBody(hit);
                    HandleBodyPentration(hit);
                    return;
                }
                
            }
            else
            {
                //Debug.Log($"[MISS] mask={layerMask.value} len={segLen:F3}");
            }
        }
        
    }

    private void PlaySoundByMaterialName(RaycastHit hit)
    {
        if (GetMaterialName(hit.collider) == "Metal" || GetMaterialName(hit.collider) == "Steel_Plate")
        {
            bulletSoundController.PlayMetalImpactSound(hit.point);
        }
        if (GetMaterialName(hit.collider) == "Floor" || GetMaterialName(hit.collider) == "Concrete" || GetMaterialName(hit.collider) == "Kevlar" || GetMaterialName(hit.collider) == "Compsite_Armor")
        {
            bulletSoundController.PlayDefaultImpactSound(hit.point);
        }
       
    }
    private void HandleBodyPentration(RaycastHit hit)
    {
        healthManager = hit.collider.GetComponentInParent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[BallisticProjectile]  healthManager is NULL");
        }

        factorAfterPentrateBodyProvider = healthManager as IGetFactorAfterPentrateBodyProvider;
        if (factorAfterPentrateBodyProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile]factorAfterPentrateBodyProviderr is NULL");
        }

        float newPen = Mathf.Max(0.0f,factorAfterPentrateBodyProvider.GetPenetrationAfterPenBody());
        float newSpeed = Mathf.Max(0.0f, factorAfterPentrateBodyProvider.GetSpeedAfterPenBody());

        pen = newPen;
        speed= newSpeed;

        if (pen <= 0 || speed <= 0)
        {
            Destroy(gameObject);
        }

        pos = hit.point + dir * exit;
        velocity = dir * speed;
        transform.position = pos;

        //Debug.Log($"pen={pen} speed={speed}");

    }

    private void HandleTerrainPenetration(RaycastHit hit, Vector3 dirN)
    {
        dirN.Normalize();

        GetMaterialManager(hit.collider);
        // 바닥은 관통 x
        if (materialInfoProvider.GetMaterialType() == MaterialType.Floor) { Destroy(gameObject); return; }
        //관통 불가능한 오브젝트라면 파괴
        if(!materialInfoProvider.GetIsPentrable()) { Destroy(gameObject); return; }

        RaycastHit exitHit;
        bool found = false;
        //정방향
        if (hit.collider.Raycast(new Ray(hit.point + dirN * enter, dirN), out exitHit, maxDist))
        {
            found = true;
        }
        //역방향 보완
        else if (hit.collider.Raycast(new Ray(hit.point + dirN * probeDist, -dirN), out exitHit, probeDist * 1.5f))
        {
            found = true;
        }
        //역방향 정방향일 때도 못찾을 경우 출구 없다고 판단
        if (!found)
        {
            Debug.Log("관통 실패! (출구 없음)");
            Destroy(gameObject);
            return;
        }

        // 내부 이동 거리(두께)
        float thicknessM = (exitHit.point - hit.point).magnitude; // m
        float materialMul = Mathf.Max(0.01f, materialInfoProvider.GetMaterialPenetrationFactor());
        float totalCost = thicknessM * energyLostPerM * materialMul;

        pen -= totalCost;

        //관통실패
        if (pen <= 0.0f)
        {
            Destroy(gameObject);
            return;
        }

        //완전 관통후 처리
        float speedLoss = Mathf.Clamp01(totalCost * 0.002f);

        speed *= (1.0f - speedLoss);
        velocity = dirN * speed;
        pos = exitHit.point + dirN * exit;

        transform.position = pos;
        isPenetratingTerrain = false;
  

        Debug.Log($"목표 관통! thickness={thicknessM:F3}, cost={totalCost:F1}, pen={pen:F1}");
    }
    private void HandleArmorPenetration(RaycastHit hit)
    {
        GetArmorManager(hit);
        GetHealthManager(hit);

        float remainingPenPower = pen - armorInfoProvider.GetArmorClass();
        float penPower01 = 1.0f;

        //관통 실패
        if (remainingPenPower <= 0.0f)
        {
            healthStateProvider.GetBluntDamage(ammo.bluntDamage);
            armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, false);
            Destroy(gameObject);
            return;
        }
        //부분 관통
        if (remainingPenPower < 10.0f && remainingPenPower > 0.0f) 
        {
            penPower01 = Mathf.Clamp01(remainingPenPower / 10.0f);

            if (UnityEngine.Random.value > penPower01)
            {
                healthStateProvider.GetBluntDamage(ammo.bluntDamage);
                armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, false);
                Destroy(gameObject);
                return;
            }
             
        }

        armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, true);

        //완전 관통후 처리
        pen *= penPower01;
        speed *= penPower01;
        
        pos = hit.point + dir * exit;
        velocity = dir * speed;
        transform.position = pos;
       // Debug.Log("관통성공!");
        //Debug.Log($"speed={speed}, pen={pen}");
    }
    private void HandleRicochet(RaycastHit hit, Vector3 dirN)
    {
   
        Vector3 recochetAngle = Vector3.Reflect(dirN, hit.normal).normalized;
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
        pos = hit.point + hit.normal * exit;

        transform.position = pos;
        ricochetChance++;
   
            
    }
 
    private void HandleArmorDamageAfterRicochet(RaycastHit hit)
    {
        GetArmorManager(hit);
        armorInfoProvider.HandleArmorDurablityAfterRicochet(armorDam);
    }
    private void CheckBulletHitBody(RaycastHit hit)
    {
        GetHealthManager(hit);

        healthStateProvider.CheckBodyHit(hit.collider, ammo.damage, ammo.criticalChance, ammo.criticalDamMultiplier, speed, pen, id);
        healthStateProvider.CheckEffectTrigger(hit.collider, ammo.lightBleedingChance, ammo.heavyBleedingChance, ammo.fractureChance);
       
    }
    private float GetMaterialRicochetFactor(Collider col, float defaultFactor = 0.5f)
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

        return materialInfoProvider.GetMaterialRicochetFactor();

    }

    private string GetMaterialName(Collider col)
    {
        GetMaterialManager(col);
        return materialInfoProvider.GetMaterialName();
    }

    private void GetMaterialManager(Collider col)
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
    }
    private void GetHealthManager(RaycastHit hit)
    {
        healthManager = hit.collider.GetComponentInParent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[BallisticProjectile]  healthManager is NULL");
        }

        healthStateProvider = healthManager as IHealthStateProvider;
        if (healthStateProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile] healthStateProvider is NULL");
        }
    }

    private void GetArmorManager(RaycastHit hit)
    {
        armorManager = hit.collider.GetComponent<ArmorManager>();
        if (armorManager == null)
        {
            Debug.LogWarning("[BallisticProjectile] armorManager is NULL");
        }

        armorInfoProvider = armorManager as IArmorInfoProviders;
        if (armorInfoProvider == null)
        {
            Debug.LogWarning("[BallisticProjectile] armorInfoProvider is NULL");
        }
    }
}
