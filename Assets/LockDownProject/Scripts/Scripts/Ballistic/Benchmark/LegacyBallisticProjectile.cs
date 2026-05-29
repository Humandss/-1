using System.Collections.Generic;
using UnityEngine;

namespace LockDown.Ballistic.Benchmark
{
    /// <summary>
    /// 성능 비교용. 기존 BallisticProjectile을 최대한 그대로 복원한 버전.
    /// 삭제된 의존성(BulletSoundController/IBulletSoundProvider)에 해당하는 사운드 호출만 제거하고,
    /// 물리 적분 + Raycast + 도탄 + 지형/방탄/인체 관통 + 입사각 계산 +
    /// HealthManager/ArmorManager/MaterialManager 조회 등 메인스레드 계산 로직은 전부 유지.
    ///
    /// 즉 "진짜 기존 시스템이 1발당 메인스레드에서 하던 일"을 그대로 재현 → NEW(Job/Burst)와 정확 비교.
    /// VFX 스폰(metalImpactVFX/hitSmoke)은 prefab 미할당 시 자동 skip.
    /// </summary>
    public class LegacyBallisticProjectile : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BulletInfo ammo;
        [SerializeField] private GameObject metalImpactVFX;
        [SerializeField] private GameObject hitSmoke;
        [SerializeField, Tooltip("ammo.lifeTime이 0이면 이 값 사용")]
        private float lifeTimeOverride = 3.0f;

        private Collider[] projectileColliders;
        private Rigidbody projectileRigidbody;
        private MaterialManager materialManager;
        private HealthManager healthManager;
        private ArmorManager armorManager;
        private LayerMask layerMask;

        // Providers (기존과 동일, 사운드만 제외)
        private IMaterialInfoProvider materialInfoProvider;
        private IHealthStateProvider healthStateProvider;
        private IGetFactorAfterPentrateBodyProvider factorAfterPentrateBodyProvider;
        private IArmorInfoProviders armorInfoProvider;

        private int id = 0;
        private static int idSeq = 0;
        private Vector3 velocity;
        private Vector3 pos;
        private Vector3 prevPos;
        private Vector3 dir;
        private float refArea;
        private float flightTime;
        private float effectiveLifeTime;
        private int ricochetChance = 0;
        private float speed;
        private float pen;
        private bool isPenetratingTerrain = false;
        private float armorDam;

        private float airDensity = 1.225f;
        private Vector3 windWorld = Vector3.zero;
        private float k;
        private float energyLostPerM = 100.0f;
        private const float exit = 0.004f;
        private const float enter = 0.002f;
        private float probeDist = 8.0f;
        private float maxDist = 20.0f;

        private bool isPlayerShot = false;

        private readonly HashSet<int> hitTargets = new HashSet<int>();

        public static int ActiveCount { get; private set; }
        private void OnEnable() { ActiveCount++; ConfigureProjectilePhysics(); }
        private void OnDisable() { ActiveCount--; }

        private void Start()
        {
            layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm",
                "Right_arm", "Left_leg", "Right_leg", "Default", "Armor");
        }

        private void ConfigureProjectilePhysics()
        {
            if (projectileColliders == null || projectileColliders.Length == 0)
                projectileColliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < projectileColliders.Length; i++)
            {
                var c = projectileColliders[i];
                if (c == null) continue;
                c.isTrigger = true;
            }

            if (projectileRigidbody == null)
                projectileRigidbody = GetComponent<Rigidbody>();
            if (projectileRigidbody == null) return;

            projectileRigidbody.isKinematic = true;
            projectileRigidbody.useGravity = false;
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
        }

        public void Initialize(Vector3 position, Vector3 direction, bool isPlayerBullet)
        {
            if (layerMask == 0)
                layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm",
                    "Right_arm", "Left_leg", "Right_leg", "Default", "Armor");

            hitTargets.Clear();
            id = System.Threading.Interlocked.Increment(ref idSeq);
            isPenetratingTerrain = false;
            ricochetChance = 0;
            isPlayerShot = isPlayerBullet;
            flightTime = 0f;
            pos = position;
            prevPos = pos;
            dir = direction.normalized;
            pen = ammo.penetrationPower;
            armorDam = ammo.armorDamage;
            velocity = dir * ammo.muzzleVelocity;

            float invMass = 1f / Mathf.Max(1e-6f, ammo.mass);
            float r = Mathf.Max(1e-6f, ammo.caliberMm * 0.001f) * 0.5f;
            refArea = Mathf.PI * r * r * ammo.refAreaScale;
            k = 0.5f * airDensity * ammo.dragCoeff * refArea * invMass;

            effectiveLifeTime = ammo.lifeTime > 0.01f ? ammo.lifeTime : lifeTimeOverride;

            transform.position = pos;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            flightTime += dt;
            if (flightTime > effectiveLifeTime) { PoolManager.Instance.Return(gameObject); return; }

            prevPos = pos;
            Vector3 vRel = velocity - windWorld;
            speed = vRel.magnitude + 1e-6f;
            Vector3 g = Physics.gravity + (-k * vRel * speed);
            velocity += g * dt;
            pos += velocity * dt;

            HandleImpact(prevPos);
            transform.position = pos;
        }

        private void HandleImpact(Vector3 prevPos)
        {
            Vector3 seg = pos - prevPos;
            float segLen = seg.magnitude;
            if (segLen <= 0f) return;

            Vector3 segDir = seg / segLen;
            if (!Physics.Raycast(prevPos, segDir, out var hit, segLen, layerMask, QueryTriggerInteraction.Ignore))
                return;

            float cosToNormal = Mathf.Clamp(Vector3.Dot(-segDir, hit.normal.normalized), -1f, 1f);
            float incAngleToPlane = 90f - Mathf.Acos(cosToNormal) * Mathf.Rad2Deg;
            float compensateAngle = ammo.baseRicochetAngleDeg * GetMaterialRicochetFactor(hit.collider);

            string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);

            if (layerName == "Default")
            {
                if (isPenetratingTerrain) return;
                if (compensateAngle >= incAngleToPlane && ricochetChance < 1)
                    HandleRicochet(hit, segDir);
                else
                {
                    isPenetratingTerrain = true;
                    HandleTerrainPenetration(hit, segDir);
                }
                SpawnImpactVfx(hit);
                SpawnImpactSmoke(hit);
                return;
            }

            if (layerName == "Armor")
            {
                if (compensateAngle >= incAngleToPlane && ricochetChance < 1)
                {
                    HandleRicochet(hit, segDir);
                    HandleArmorDamageAfterRicochet(hit);
                }
                else
                {
                    HandleArmorPenetration(hit);
                }
                SpawnImpactVfx(hit);
                SpawnImpactSmoke(hit);
                return;
            }

            CheckBulletHitBody(hit);
            HandleBodyPentration(hit);
            CheckHitSuppressionCondition(hit);
        }

        private void SpawnImpactSmoke(RaycastHit hit)
        {
            if (hitSmoke == null) return;
            var rot = Quaternion.LookRotation(hit.normal);
            PoolManager.Instance.Spawn(hitSmoke, hit.point + hit.normal * exit, rot);
        }

        private void SpawnImpactVfx(RaycastHit hit)
        {
            string mat = GetMaterialName(hit.collider);
            if (mat != "Metal" && mat != "Steel_Plate") return;
            if (metalImpactVFX == null) return;
            var rot = Quaternion.LookRotation(hit.normal);
            PoolManager.Instance.Spawn(metalImpactVFX, hit.point + hit.normal * exit, rot);
        }

        private void HandleBodyPentration(RaycastHit hit)
        {
            healthManager = hit.collider.GetComponentInParent<HealthManager>();
            factorAfterPentrateBodyProvider = healthManager as IGetFactorAfterPentrateBodyProvider;
            if (factorAfterPentrateBodyProvider == null) { PoolManager.Instance.Return(gameObject); return; }

            float newPen = Mathf.Max(0f, factorAfterPentrateBodyProvider.GetPenetrationAfterPenBody());
            float newSpeed = Mathf.Max(0f, factorAfterPentrateBodyProvider.GetSpeedAfterPenBody());
            pen = newPen;
            speed = newSpeed;
            if (pen <= 0 || speed <= 0) { PoolManager.Instance.Return(gameObject); return; }

            pos = hit.point + dir * exit;
            velocity = dir * speed;
            transform.position = pos;
        }

        private void CheckHitSuppressionCondition(RaycastHit hit)
        {
            if (isPlayerShot) return;
            var suppression = hit.collider.GetComponentInParent<PlayerSuppressionController>();
            if (suppression != null) suppression.AddHitSuppression(hit.point);
        }

        private void HandleTerrainPenetration(RaycastHit hit, Vector3 dirN)
        {
            dirN.Normalize();
            GetMaterialManager(hit.collider);
            if (materialInfoProvider == null) { PoolManager.Instance.Return(gameObject); return; }
            if (materialInfoProvider.GetMaterialType() == MaterialType.Floor) { PoolManager.Instance.Return(gameObject); return; }
            if (!materialInfoProvider.GetIsPentrable()) { PoolManager.Instance.Return(gameObject); return; }

            RaycastHit exitHit;
            bool found = false;
            if (hit.collider.Raycast(new Ray(hit.point + dirN * enter, dirN), out exitHit, maxDist))
                found = true;
            else if (hit.collider.Raycast(new Ray(hit.point + dirN * probeDist, -dirN), out exitHit, probeDist * 1.5f))
                found = true;
            if (!found) { PoolManager.Instance.Return(gameObject); return; }

            float thicknessM = (exitHit.point - hit.point).magnitude;
            float materialMul = Mathf.Max(0.01f, materialInfoProvider.GetMaterialPenetrationFactor());
            float totalCost = thicknessM * energyLostPerM * materialMul;
            pen -= totalCost;
            if (pen <= 0f) { PoolManager.Instance.Return(gameObject); return; }

            float speedLoss = Mathf.Clamp01(totalCost * 0.002f);
            speed *= (1f - speedLoss);
            velocity = dirN * speed;
            pos = exitHit.point + dirN * exit;
            transform.position = pos;
            isPenetratingTerrain = false;
        }

        private void HandleArmorPenetration(RaycastHit hit)
        {
            GetArmorManager(hit);
            GetHealthManager(hit);
            if (armorInfoProvider == null) { PoolManager.Instance.Return(gameObject); return; }

            float remainingPenPower = pen - armorInfoProvider.GetArmorClass();
            float penPower01 = 1f;

            if (remainingPenPower <= 0f)
            {
                if (healthStateProvider != null) healthStateProvider.GetBluntDamage(ammo.bluntDamage);
                armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, false);
                PoolManager.Instance.Return(gameObject);
                return;
            }

            if (remainingPenPower < 10f && remainingPenPower > 0f)
            {
                penPower01 = Mathf.Clamp01(remainingPenPower / 10f);
                if (UnityEngine.Random.value > penPower01)
                {
                    if (healthStateProvider != null) healthStateProvider.GetBluntDamage(ammo.bluntDamage);
                    armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, false);
                    PoolManager.Instance.Return(gameObject);
                    return;
                }
            }

            armorInfoProvider.HandleArmorDurabilityAfterHit(armorDam, true);
            pen *= penPower01;
            speed *= penPower01;
            pos = hit.point + dir * exit;
            velocity = dir * speed;
            transform.position = pos;
        }

        private void HandleRicochet(RaycastHit hit, Vector3 dirN)
        {
            Vector3 recochetAngle = Vector3.Reflect(dirN, hit.normal).normalized;
            Vector3 axis = Vector3.Cross(hit.normal, recochetAngle);
            axis.Normalize();
            float maxRecochetAngle = Mathf.Lerp(0f, 6f, Mathf.Clamp01(ammo.randomRicochetAngle));
            float angle = UnityEngine.Random.Range(-maxRecochetAngle, maxRecochetAngle);
            recochetAngle = (Quaternion.AngleAxis(angle, axis) * recochetAngle).normalized;
            float afterRicochetSpeed = speed * ammo.afterRicochetEnergyPercent;
            velocity = recochetAngle * afterRicochetSpeed;
            pos = hit.point + hit.normal * exit;
            transform.position = pos;
            ricochetChance++;
        }

        private void HandleArmorDamageAfterRicochet(RaycastHit hit)
        {
            GetArmorManager(hit);
            if (armorInfoProvider != null) armorInfoProvider.HandleArmorDurabilityAfterRicochet(armorDam);
        }

        private void CheckBulletHitBody(RaycastHit hit)
        {
            int targetID = hit.collider.GetInstanceID();
            if (!hitTargets.Add(targetID)) return;
            GetHealthManager(hit);
            if (healthStateProvider == null) return;
            healthStateProvider.CheckBodyHit(hit.collider, ammo.damage, ammo.criticalChance, ammo.criticalDamMultiplier, speed, pen);
            healthStateProvider.CheckEffectTrigger(hit.collider, ammo.lightBleedingChance, ammo.heavyBleedingChance, ammo.fractureChance);
        }

        private float GetMaterialRicochetFactor(Collider col, float defaultFactor = 0.5f)
        {
            materialManager = col.GetComponent<MaterialManager>();
            if (materialManager == null) return defaultFactor;
            materialInfoProvider = materialManager as IMaterialInfoProvider;
            if (materialInfoProvider == null) return defaultFactor;
            return materialInfoProvider.GetMaterialRicochetFactor();
        }

        private string GetMaterialName(Collider col)
        {
            GetMaterialManager(col);
            return materialInfoProvider != null ? materialInfoProvider.GetMaterialName() : string.Empty;
        }

        private void GetMaterialManager(Collider col)
        {
            materialManager = col.GetComponent<MaterialManager>();
            materialInfoProvider = materialManager as IMaterialInfoProvider;
        }

        private void GetHealthManager(RaycastHit hit)
        {
            healthManager = hit.collider.GetComponentInParent<HealthManager>();
            healthStateProvider = healthManager as IHealthStateProvider;
        }

        private void GetArmorManager(RaycastHit hit)
        {
            armorManager = hit.collider.GetComponent<ArmorManager>();
            armorInfoProvider = armorManager as IArmorInfoProviders;
        }
    }
}
