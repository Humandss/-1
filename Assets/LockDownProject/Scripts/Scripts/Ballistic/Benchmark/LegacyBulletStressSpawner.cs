using UnityEngine;

namespace LockDown.Ballistic.Benchmark
{
    /// <summary>
    /// 레거시(MonoBehaviour) 총알을 매 프레임 콘 모양으로 발사하는 벤치마크 스포너.
    /// 새 시스템의 BulletStressSpawner와 동일한 파라미터(bulletsPerFrame, coneHalfAngleDeg)로
    /// 설정하여 공정 비교한다.
    ///
    /// 사용:
    /// 1. 빈 GameObject에 부착
    /// 2. legacyBulletPrefab: LegacyBulletBenchmark가 붙은 총알 프리팹
    /// 3. muzzle Transform 할당
    /// 4. bulletsPerFrame을 BulletStressSpawner와 동일하게 설정
    /// 5. Play → 동일 시나리오로 Profiler 캡처 후 BulletSimulationSystem과 비교
    /// </summary>
    public class LegacyBulletStressSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("레거시 총알 프리팹. LegacyBallisticProjectile(기존 풀버전) 또는 " +
                                  "LegacyBulletBenchmark(물리만) + Rigidbody(kinematic) + Collider(trigger)")]
        private GameObject legacyBulletPrefab;
        [SerializeField] private Transform muzzle;

        /// <summary>두 종류 레거시 총알 활성 수 합산.</summary>
        public static int LegacyActiveCount =>
            LegacyBulletBenchmark.ActiveCount + LegacyBallisticProjectile.ActiveCount;

        [Header("Settings (BulletStressSpawner와 동일하게)")]
        [SerializeField, Range(1, 200)] private int bulletsPerFrame = 17;
        [SerializeField, Range(0f, 45f)] private float coneHalfAngleDeg = 15f;
        [SerializeField] private bool fireOnStart = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.H;
        [SerializeField, Tooltip("활성 총알 상한. NEW 시스템 capacity와 동일하게 맞춰 공정 비교 (2048). 0이면 무제한")]
        private int maxActiveBullets = 2048;

        [Header("Prewarm")]
        [SerializeField, Range(0, 4096), Tooltip("게임 시작 시 풀에 미리 적재할 총알 수")]
        private int prewarmCount = 1024;

        private bool firing;

        private void Start()
        {
            firing = fireOnStart;
            if (legacyBulletPrefab != null && PoolManager.Instance != null && prewarmCount > 0)
                PoolManager.Instance.Prewarm(legacyBulletPrefab, prewarmCount);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) firing = !firing;
            if (!firing) return;
            if (legacyBulletPrefab == null || muzzle == null || PoolManager.Instance == null) return;

            Vector3 origin = muzzle.position;
            Quaternion baseRot = muzzle.rotation;
            for (int i = 0; i < bulletsPerFrame; i++)
            {
                // 활성 총알 상한 도달 시 더 안 쏨 (NEW 시스템 capacity와 동일하게 공정 비교)
                if (maxActiveBullets > 0 && LegacyActiveCount >= maxActiveBullets)
                    break;

                float yaw = Random.Range(-coneHalfAngleDeg, coneHalfAngleDeg);
                float pitch = Random.Range(-coneHalfAngleDeg, coneHalfAngleDeg);
                Vector3 dir = baseRot * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;

                var go = PoolManager.Instance.Spawn(legacyBulletPrefab, origin, Quaternion.LookRotation(dir));
                if (go == null) continue;

                // 풀버전(LegacyBallisticProjectile) 우선, 없으면 물리만 버전(LegacyBulletBenchmark)
                var full = go.GetComponent<LegacyBallisticProjectile>();
                if (full != null) { full.Initialize(origin, dir, true); continue; }

                var bench = go.GetComponent<LegacyBulletBenchmark>();
                if (bench != null) bench.Initialize(origin, dir);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 460, 24),
                $"LEGACY Bench: firing={firing}  active={LegacyActiveCount}  ({toggleKey}: toggle)");
        }
    }
}
