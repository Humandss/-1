using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 전체 BulletState 슬롯 중 isAlive == 1인 인덱스만 압축 리스트로 모은다.
    /// 이후 단계 Job들이 활성 슬롯만 순회하도록 함.
    /// </summary>
    [BurstCompile]
    public struct BuildActiveIndicesJob : IJob
    {
        [ReadOnly] public NativeArray<BulletState> states;
        public NativeList<int> activeIndices;

        public void Execute()
        {
            activeIndices.Clear();
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].isAlive == 1)
                    activeIndices.Add(i);
            }
        }
    }
}
