using Unity.Mathematics;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 발사 입력 없이 BulletSimulationSystem.Spawn을 호출하는 디버그 헬퍼.
    /// 키를 누르고 있는 동안 매 프레임 1발씩 muzzle.forward 방향으로 발사.
    /// 회귀 테스트 및 시각 디버그용. 실제 게임플레이에서는 사용 안 함.
    /// </summary>
    public class BulletSystemDebugger : MonoBehaviour
    {
        [SerializeField] private BulletInfo testBulletInfo;
        [SerializeField] private Transform muzzle;
        [SerializeField] private KeyCode fireKey = KeyCode.F;
        [SerializeField] private bool playerBullet = true;

        private void Update()
        {
            if (!Input.GetKey(fireKey)) return;
            if (BulletSimulationSystem.Instance == null) return;
            if (testBulletInfo == null || muzzle == null) return;

            BulletSimulationSystem.Instance.Spawn(
                (float3)muzzle.position,
                (float3)muzzle.forward,
                testBulletInfo,
                playerBullet);
        }
    }
}
