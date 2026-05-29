using UnityEngine;

namespace LockDown.Ballistic.Benchmark
{
    /// <summary>
    /// 성능 비교용 레거시 총알. 기존 BallisticProjectile의 물리 적분 + Raycast 로직을
    /// 그대로 추출했다. VFX/사운드/데미지 등 매니지드 처리는 제외 — 순수 "다발 총알
    /// 시뮬레이션 비용"을 새 Burst/Job 시스템과 공정하게 비교하기 위함.
    ///
    /// 측정 방식: 이 컴포넌트가 붙은 총알 프리팹을 PoolManager로 1000발 동시 발사 후
    /// Profiler에서 메인 스레드 FixedUpdate 시간을 BulletSimulationSystem과 비교.
    ///
    /// 핵심 비용 요소 (새 시스템에는 없는 것):
    ///   - 총알 1발당 MonoBehaviour.FixedUpdate 디스패치 (1000번)
    ///   - 총알 1발당 Physics.Raycast 개별 호출 (메인 스레드)
    ///   - 총알 1발당 transform.position 갱신
    /// </summary>
    public class LegacyBulletBenchmark : MonoBehaviour
    {
        [SerializeField] private BulletInfo ammo;
        [SerializeField, Tooltip("ammo.lifeTime이 0이면 이 값으로 대체 (스트레스 테스트용)")]
        private float lifeTimeOverride = 3.0f;

        private Vector3 velocity;
        private Vector3 pos;
        private Vector3 prevPos;
        private Vector3 dir;
        private float speed;
        private float pen;
        private float refArea;
        private float k;
        private float flightTime;
        private float effectiveLifeTime;

        private readonly float airDensity = 1.225f;
        private Vector3 windWorld = Vector3.zero;
        private LayerMask layerMask;
        private bool layerMaskReady;

        private static int idSeq = 0;
        private int id;

        /// <summary>활성(비활성 아닌) 레거시 총알 수. 스포너 OnGUI 표시용.</summary>
        public static int ActiveCount { get; private set; }

        private void OnEnable() => ActiveCount++;
        private void OnDisable() => ActiveCount--;

        private void EnsureLayerMask()
        {
            if (layerMaskReady) return;
            layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm",
                "Right_arm", "Left_leg", "Right_leg", "Default", "Armor");
            layerMaskReady = true;
        }

        /// <summary>
        /// 기존 BallisticProjectile.Initialize와 동일한 물리 파라미터 셋업.
        /// </summary>
        public void Initialize(Vector3 position, Vector3 direction)
        {
            EnsureLayerMask();

            id = System.Threading.Interlocked.Increment(ref idSeq);
            flightTime = 0f;
            pos = position;
            prevPos = pos;
            dir = direction.normalized;
            pen = ammo.penetrationPower;
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
            if (flightTime > effectiveLifeTime)
            {
                PoolManager.Instance.Return(gameObject);
                return;
            }

            prevPos = pos;

            // 드래그 + 중력 (기존 BallisticProjectile.FixedUpdate와 동일)
            Vector3 vRel = velocity - windWorld;
            speed = vRel.magnitude + 1e-6f;
            Vector3 g = Physics.gravity + (-k * vRel * speed);
            velocity += g * dt;
            pos += velocity * dt;

            // Raycast 충돌 판정 (메인 스레드 개별 호출)
            Vector3 seg = pos - prevPos;
            float segLen = seg.magnitude;
            if (segLen > 0f)
            {
                Vector3 segDir = seg / segLen;
                if (Physics.Raycast(prevPos, segDir, out _, segLen, layerMask, QueryTriggerInteraction.Ignore))
                {
                    // 벤치마크 공정성: 히트 시 데미지/VFX/사운드 없이 풀 반환만
                    PoolManager.Instance.Return(gameObject);
                    return;
                }
            }

            transform.position = pos;
        }
    }
}
