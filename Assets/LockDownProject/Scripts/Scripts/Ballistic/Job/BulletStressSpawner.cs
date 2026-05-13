using Unity.Mathematics;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 매 프레임 다수의 총알을 콘 모양으로 발사하여 BulletSimulationSystem 부하 테스트.
    /// 포트폴리오용 스트레스 데모씬에서 사용. 일반 게임플레이용 아님.
    ///
    /// 사용:
    /// 1. 빈 GameObject에 부착
    /// 2. muzzle Transform 할당 (위치 + 정면 방향 결정)
    /// 3. bulletInfo SO 할당
    /// 4. bulletsPerFrame로 활성 총알 수 조정 (보통 17~30이면 활성 1000발 유지)
    /// </summary>
    public class BulletStressSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BulletInfo bulletInfo;
        [SerializeField] private Transform muzzle;

        [Header("Settings")]
        [SerializeField, Range(1, 200)] private int bulletsPerFrame = 17;
        [SerializeField, Range(0f, 45f)] private float coneHalfAngleDeg = 15f;
        [SerializeField] private bool playerBullet = true;
        [SerializeField] private bool fireOnStart = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.G;

        private bool firing;

        private void Start()
        {
            firing = fireOnStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) firing = !firing;
            if (!firing) return;

            var sys = BulletSimulationSystem.Instance;
            if (sys == null || bulletInfo == null || muzzle == null) return;

            Vector3 origin = muzzle.position;
            Quaternion baseRot = muzzle.rotation;
            for (int i = 0; i < bulletsPerFrame; i++)
            {
                float yaw = UnityEngine.Random.Range(-coneHalfAngleDeg, coneHalfAngleDeg);
                float pitch = UnityEngine.Random.Range(-coneHalfAngleDeg, coneHalfAngleDeg);
                Vector3 dir = baseRot * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                sys.Spawn((float3)origin, (float3)dir, bulletInfo, playerBullet);
            }
        }

        private void OnGUI()
        {
            var sys = BulletSimulationSystem.Instance;
            int active = sys != null ? sys.Active : 0;
            int capacity = sys != null ? sys.Capacity : 0;
            GUI.Label(new Rect(10, 10, 400, 24),
                $"BulletStress: firing={firing}  active={active}/{capacity}  (G: toggle)");
        }
    }
}
