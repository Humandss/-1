using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// RaycastCommand 결과를 순회하며 콜라이더가 잡힌 슬롯을 HitEvent로 큐에 넣는다.
    /// 매니지드 객체 접근(Collider, GameObject, Layer)은 메인 스레드 드레인 단계에서 처리.
    /// </summary>
    [BurstCompile]
    public struct CollectHitsJob : IJob
    {
        [ReadOnly] public NativeArray<int> activeIndices;
        [ReadOnly] public NativeArray<BulletState> states;
        [ReadOnly] public NativeArray<RaycastHit> results;

        public NativeQueue<HitEvent> hitQueue;

        public void Execute()
        {
            for (int i = 0; i < activeIndices.Length; i++)
            {
                var hit = results[i];
                int colliderID = hit.colliderInstanceID;
                if (colliderID == 0) continue;  // 미스 (RaycastHit.colliderInstanceID == 0)

                int slot = activeIndices[i];
                var s = states[slot];
                if (s.isAlive == 0) continue;

                float segLen = math.length(s.pos - s.prevPos);

                hitQueue.Enqueue(new HitEvent
                {
                    slotIndex = slot,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    hitColliderInstanceID = colliderID,
                    segLen = segLen,
                });
            }
        }
    }
}
