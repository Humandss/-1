using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// VFX 스폰 예산 관리. 한 프레임당 카테고리별 스폰 수 제한 + 거리 컬링.
    /// 다발 사격 시 풀 폭주 + 가시화되지 않는 거리에서의 낭비를 방지한다.
    /// BulletSimulationSystem.Awake가 설정 주입.
    /// </summary>
    public static class EffectsBudget
    {
        public static int MaxMetalImpactPerFrame = 16;
        public static int MaxSmokePerFrame = 24;
        public static int MaxBloodPerFrame = 12;
        public static float MaxVisibleDistance = 60f;   // 0 이하면 거리 컬링 비활성

        private static int currentFrame = -1;
        private static int metalCount;
        private static int smokeCount;
        private static int bloodCount;

        private static Transform cachedListener;
        private static int listenerFrame = -1;

        public static bool TryConsumeMetal(Vector3 pos)
        {
            TickFrame();
            if (metalCount >= MaxMetalImpactPerFrame) return false;
            if (!IsAudible(pos)) return false;
            metalCount++;
            return true;
        }

        public static bool TryConsumeSmoke(Vector3 pos)
        {
            TickFrame();
            if (smokeCount >= MaxSmokePerFrame) return false;
            if (!IsAudible(pos)) return false;
            smokeCount++;
            return true;
        }

        public static bool TryConsumeBlood(Vector3 pos)
        {
            TickFrame();
            if (bloodCount >= MaxBloodPerFrame) return false;
            if (!IsAudible(pos)) return false;
            bloodCount++;
            return true;
        }

        private static void TickFrame()
        {
            int f = Time.frameCount;
            if (f == currentFrame) return;
            currentFrame = f;
            metalCount = 0;
            smokeCount = 0;
            bloodCount = 0;
        }

        private static bool IsAudible(Vector3 pos)
        {
            if (MaxVisibleDistance <= 0f) return true;
            var listener = GetListener();
            if (listener == null) return true;
            float distSq = (listener.position - pos).sqrMagnitude;
            float maxSq = MaxVisibleDistance * MaxVisibleDistance;
            return distSq <= maxSq;
        }

        private static Transform GetListener()
        {
            int f = Time.frameCount;
            if (cachedListener != null && f == listenerFrame) return cachedListener;
            listenerFrame = f;
            var l = Object.FindObjectOfType<AudioListener>();
            cachedListener = l != null ? l.transform : null;
            return cachedListener;
        }
    }
}
