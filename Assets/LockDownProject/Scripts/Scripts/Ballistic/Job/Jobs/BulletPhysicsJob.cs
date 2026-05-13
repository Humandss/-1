using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 활성 총알별 물리 적분 (드래그 + 중력 + 바람). 수명 초과 시 isAlive = 0으로 마킹.
    /// 기존 BallisticProjectile.FixedUpdate의 적분 부분을 Burst로 이식.
    /// </summary>
    [BurstCompile]
    public struct BulletPhysicsJob : IJobParallelForDefer
    {
        [ReadOnly] public NativeArray<int> activeIndices;
        [ReadOnly] public NativeArray<BlittableBulletInfo> infos;
        [ReadOnly] public float3 gravity;
        [ReadOnly] public float3 windWorld;
        [ReadOnly] public float deltaTime;

        // activeIndices에 중복이 없다는 불변성에 의존 (BuildActiveIndicesJob이 보장).
        // 각 워커는 서로 다른 slot에만 쓰므로 alias 검사를 비활성화한다.
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<BulletState> states;

        public void Execute(int activeIdx)
        {
            int slot = activeIndices[activeIdx];
            var s = states[slot];

            // 수명 체크
            s.flightTime += deltaTime;
            var info = infos[s.bulletInfoIndex];
            if (s.flightTime > info.lifeTime)
            {
                s.isAlive = 0;
                states[slot] = s;
                return;
            }

            // 드래그 + 중력
            s.prevPos = s.pos;
            float3 vRel = s.velocity - windWorld;
            float speed = math.length(vRel) + 1e-6f;
            float3 g = gravity + (-s.k * vRel * speed);

            s.velocity += g * deltaTime;
            s.pos += s.velocity * deltaTime;
            s.speed = math.length(s.velocity);

            states[slot] = s;
        }
    }
}
