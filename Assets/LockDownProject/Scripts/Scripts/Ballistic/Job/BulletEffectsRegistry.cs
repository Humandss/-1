using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 히트 시 스폰할 VFX 프리팹을 정적 슬롯으로 보유. BulletSimulationSystem.Awake에서 채워준다.
    /// 도메인 리로드 시 초기화되므로 매번 재할당 필요.
    /// </summary>
    public static class BulletEffectsRegistry
    {
        public static GameObject MetalImpactVfx;
        public static GameObject HitSmoke;
    }
}
