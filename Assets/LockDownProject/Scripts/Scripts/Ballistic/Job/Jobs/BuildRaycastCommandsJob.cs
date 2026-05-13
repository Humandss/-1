using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 활성 총알의 (prevPos → pos) 세그먼트로부터 RaycastCommand를 빌드.
    /// 결과는 RaycastCommand.ScheduleBatch가 사용.
    /// </summary>
    [BurstCompile]
    public struct BuildRaycastCommandsJob : IJobParallelForDefer
    {
        [ReadOnly] public NativeArray<int> activeIndices;
        [ReadOnly] public NativeArray<BulletState> states;
        [ReadOnly] public QueryParameters queryParams;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RaycastCommand> commands;

        public void Execute(int activeIdx)
        {
            int slot = activeIndices[activeIdx];
            var s = states[slot];

            float3 seg = s.pos - s.prevPos;
            float segLen = math.length(seg);
            float3 segDir = segLen > 0f ? seg / segLen : new float3(0f, 0f, 1f);

            // 빈 슬롯 또는 이동량 0: distance 0짜리 raycast (히트 없음 보장).
            float useLen = (s.isAlive == 0 || segLen <= 0f) ? 0f : segLen;

            commands[activeIdx] = new RaycastCommand(
                from: s.prevPos,
                direction: segDir,
                queryParameters: queryParams,
                distance: useLen
            );
        }
    }
}
