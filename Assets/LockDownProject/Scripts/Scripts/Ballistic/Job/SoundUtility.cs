using System.Collections.Generic;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 사운드 헬퍼. AudioSource.PlayClipAtPoint 대신 미리 만들어둔 AudioSource 풀을 라운드로빈으로 재사용한다.
    /// PlayClipAtPoint는 호출마다 GameObject + AudioSource를 새로 만들어서 다발 히트 시
    /// Instantiate/AddComponent/Destroy 폭탄이 됨 (프로파일러에서 11ms 이상 차지).
    /// 풀로 바꾸면 첫 1회 비용 외에는 거의 0.
    ///
    /// BulletSimulationSystem.Awake가 InitializePool(parent, poolSize)을 호출해 초기화한다.
    /// </summary>
    public static class SoundUtility
    {
        public static List<AudioClip> RicochetClips;
        public static List<AudioClip> DefaultImpactClips;
        public static List<AudioClip> MetalImpactClips;
        public static List<AudioClip> BodyImpactClips;
        public static List<AudioClip> HeadImpactClips;

        public static float RicochetVolume = 1f;
        public static float DefaultImpactVolume = 1f;
        public static float MetalImpactVolume = 1f;
        public static float BodyImpactVolume = 1f;
        public static float HeadImpactVolume = 1f;

        /// <summary>
        /// 같은 카테고리 사운드가 한 프레임에 너무 많이 트리거되지 않도록 throttle.
        /// 0 이하면 비활성.
        /// </summary>
        public static int MaxSoundsPerFramePerCategory = 4;

        /// <summary>
        /// 리스너로부터 이 거리보다 멀면 재생 안 함. 0 이하면 비활성.
        /// </summary>
        public static float MaxAudibleDistance = 80f;

        // ---- AudioSource 풀 ----
        private static AudioSource[] pool;
        private static int nextSourceIndex;
        private static Transform poolRoot;

        // 카테고리별 프레임 카운터 (5 categories)
        private static int frameNumber = -1;
        private static int[] categoryPlayCount = new int[5];

        private const int CAT_RICOCHET = 0;
        private const int CAT_DEFAULT_IMPACT = 1;
        private const int CAT_METAL_IMPACT = 2;
        private const int CAT_BODY_IMPACT = 3;
        private const int CAT_HEAD_IMPACT = 4;

        public static void InitializePool(Transform parent, int poolSize)
        {
            if (pool != null && pool.Length == poolSize) return;

            // 기존 풀 정리 (도메인 리로드 대비)
            if (pool != null)
            {
                for (int i = 0; i < pool.Length; i++)
                    if (pool[i] != null) Object.Destroy(pool[i].gameObject);
            }

            poolRoot = parent;
            pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"BulletAudio_{i}");
                if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1.0f;     // 3D
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.maxDistance = MaxAudibleDistance > 0f ? MaxAudibleDistance : 100f;
                src.minDistance = 1f;
                src.dopplerLevel = 0f;        // 다발 발사 시 도플러 노이즈 방지
                pool[i] = src;
            }
            nextSourceIndex = 0;
        }

        public static void PlayRicochet(Vector3 pos) => Play(CAT_RICOCHET, RicochetClips, pos, RicochetVolume);
        public static void PlayDefaultImpact(Vector3 pos) => Play(CAT_DEFAULT_IMPACT, DefaultImpactClips, pos, DefaultImpactVolume);
        public static void PlayMetalImpact(Vector3 pos) => Play(CAT_METAL_IMPACT, MetalImpactClips, pos, MetalImpactVolume);
        public static void PlayBodyImpact(Vector3 pos) => Play(CAT_BODY_IMPACT, BodyImpactClips, pos, BodyImpactVolume);
        public static void PlayHeadImpact(Vector3 pos) => Play(CAT_HEAD_IMPACT, HeadImpactClips, pos, HeadImpactVolume);

        private static void Play(int category, List<AudioClip> clips, Vector3 pos, float volume)
        {
            if (clips == null || clips.Count == 0) return;
            if (pool == null || pool.Length == 0) return;

            // 카테고리별 프레임 throttle
            if (MaxSoundsPerFramePerCategory > 0)
            {
                int currentFrame = Time.frameCount;
                if (currentFrame != frameNumber)
                {
                    frameNumber = currentFrame;
                    for (int i = 0; i < categoryPlayCount.Length; i++) categoryPlayCount[i] = 0;
                }
                if (categoryPlayCount[category] >= MaxSoundsPerFramePerCategory) return;
                categoryPlayCount[category]++;
            }

            // 거리 컬링
            if (MaxAudibleDistance > 0f)
            {
                var listener = AudioListener();
                if (listener != null)
                {
                    float distSq = (listener.position - pos).sqrMagnitude;
                    float maxSq = MaxAudibleDistance * MaxAudibleDistance;
                    if (distSq > maxSq) return;
                }
            }

            var clip = clips[Random.Range(0, clips.Count)];
            if (clip == null) return;

            // 풀에서 다음 소스 가져오기 (라운드 로빈, 이미 재생 중이면 그냥 덮어씀)
            var src = pool[nextSourceIndex];
            nextSourceIndex = (nextSourceIndex + 1) % pool.Length;
            if (src == null) return;

            src.transform.position = pos;
            src.volume = volume;
            src.PlayOneShot(clip);
        }

        private static Transform cachedListenerTransform;
        private static int listenerFrameChecked = -1;
        private static Transform AudioListener()
        {
            int f = Time.frameCount;
            if (cachedListenerTransform != null && f == listenerFrameChecked)
                return cachedListenerTransform;
            listenerFrameChecked = f;
            var l = Object.FindObjectOfType<UnityEngine.AudioListener>();
            cachedListenerTransform = l != null ? l.transform : null;
            return cachedListenerTransform;
        }
    }
}
