using System.Collections.Generic;
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

        /// <summary>
        /// 인체 히트 시 무작위로 스폰할 피 VFX 프리팹들 (KriptoFX VolumetricBloodFX 등).
        /// 비어있으면 피 VFX 안 띄움.
        /// </summary>
        public static List<GameObject> BodyImpactBloodPrefabs;

        /// <summary>
        /// 헤드샷 전용 피 VFX (없으면 BodyImpactBloodPrefabs 사용).
        /// </summary>
        public static List<GameObject> HeadImpactBloodPrefabs;

        /// <summary>
        /// 벽에 묻는 피 데칼 (있으면 벽 뒤 표면에 추가 스폰). 옵션.
        /// </summary>
        public static GameObject AttachedBloodPrefab;
    }
}
