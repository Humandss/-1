using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityRandom = Unity.Mathematics.Random;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 탄도 시뮬레이션의 중앙 오케스트레이터. 매 FixedUpdate에 Burst Job 체인 실행.
    /// 발사 API: Spawn(pos, dir, BulletInfo, isPlayerShot).
    /// 적 감지 노출: GetActivePlayerBulletsForDetection().
    /// </summary>
    public class BulletSimulationSystem : MonoBehaviour
    {
        public static BulletSimulationSystem Instance { get; private set; }

        [Header("Capacity")]
        [SerializeField, Tooltip("동시에 활성 가능한 총알 최대 수")]
        private int capacity = 2048;

        [Header("Layer Mask (히트 대상)")]
        [SerializeField]
        private string[] hitLayerNames = new[]
        {
            "Head", "Thorax", "Stomach", "Left_arm", "Right_arm",
            "Left_leg", "Right_leg", "Default", "Armor"
        };

        [Header("World")]
        [SerializeField] private Vector3 windWorld = Vector3.zero;
        [SerializeField] private float airDensity = 1.225f;

        [Header("BulletInfo Registration")]
        [SerializeField, Tooltip("비워두면 Resources.LoadAll<BulletInfo>(\"\")로 자동 탐색")]
        private List<BulletInfo> registeredBulletInfos = new List<BulletInfo>();

        [Header("Effects (Hit VFX)")]
        [SerializeField] private GameObject metalImpactVfx;
        [SerializeField] private GameObject hitSmoke;
        [SerializeField, Range(0, 512), Tooltip("게임 시작 시 미리 풀에 적재할 metalImpactVfx 개수")]
        private int prewarmMetalImpactVfx = 128;
        [SerializeField, Range(0, 512), Tooltip("게임 시작 시 미리 풀에 적재할 hitSmoke 개수")]
        private int prewarmHitSmoke = 256;
        [SerializeField, Range(0, 64), Tooltip("한 프레임 최대 metalImpactVfx 스폰 수")]
        private int maxMetalImpactPerFrame = 16;
        [SerializeField, Range(0, 64), Tooltip("한 프레임 최대 hitSmoke 스폰 수")]
        private int maxSmokePerFrame = 24;
        [SerializeField, Range(0f, 200f), Tooltip("리스너 기준 이 거리 이상이면 VFX 스폰 생략. 0이면 비활성")]
        private float maxVfxVisibleDistance = 60f;

        [Header("Sounds (전역 클립 + 볼륨)")]
        [SerializeField] private List<AudioClip> ricochetClips = new List<AudioClip>();
        [SerializeField] private List<AudioClip> defaultImpactClips = new List<AudioClip>();
        [SerializeField] private List<AudioClip> metalImpactClips = new List<AudioClip>();
        [SerializeField] private List<AudioClip> bodyImpactClips = new List<AudioClip>();
        [SerializeField] private List<AudioClip> headImpactClips = new List<AudioClip>();
        [SerializeField, Range(0f, 2f)] private float ricochetVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float defaultImpactVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float metalImpactVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float bodyImpactVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float headImpactVolume = 1f;
        [SerializeField, Range(8, 128), Tooltip("재사용할 AudioSource 풀 크기 (다발 사격 시 충분히 크게)")]
        private int audioPoolSize = 32;
        [SerializeField, Range(0, 32), Tooltip("카테고리별 한 프레임 최대 사운드 수. 0이면 throttle 없음")]
        private int maxSoundsPerFramePerCategory = 4;
        [SerializeField, Range(0f, 200f), Tooltip("리스너로부터 이 거리 이상이면 사운드 무시. 0이면 비활성")]
        private float maxAudibleDistance = 80f;

        // 컨테이너
        private BulletSlotAllocator allocator;
        private NativeList<int> activeIndices;
        private NativeArray<RaycastCommand> commands;
        private NativeArray<RaycastHit> results;
        private NativeQueue<HitEvent> hitQueue;
        private NativeList<BulletDetectionData> playerBulletDetectionCache;

        // 자료
        private BulletInfoTable infoTable;
        private BulletHitProcessor hitProcessor;
        private QueryParameters queryParams;
        private LayerMask hitLayerMask;
        private float3 cachedWindWorld;

        // ID 시퀀스 (기존 idSeq 동작 유지: 게임 세션 동안 단조 증가)
        private static int idSeq = 0;

        // "Pool 가득참" 경고 throttle (콘솔 도배 방지)
        private float lastPoolFullWarnTime = -10f;
        private int poolFullDropCount = 0;
        private const float PoolFullWarnInterval = 1f;

        public BulletInfoTable InfoTable => infoTable;
        public int Capacity => capacity;
        public int Active => allocator?.Active ?? 0;
        public float AirDensity => airDensity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            allocator = new BulletSlotAllocator();
            allocator.Initialize(capacity);
            activeIndices = new NativeList<int>(capacity, Allocator.Persistent);
            commands = new NativeArray<RaycastCommand>(capacity, Allocator.Persistent);
            results = new NativeArray<RaycastHit>(capacity, Allocator.Persistent);
            hitQueue = new NativeQueue<HitEvent>(Allocator.Persistent);
            playerBulletDetectionCache = new NativeList<BulletDetectionData>(capacity, Allocator.Persistent);

            hitLayerMask = LayerMask.GetMask(hitLayerNames);
            queryParams = new QueryParameters(
                layerMask: hitLayerMask,
                hitMultipleFaces: false,
                hitTriggers: QueryTriggerInteraction.Ignore,
                hitBackfaces: false);

            cachedWindWorld = (float3)windWorld;

            infoTable = new BulletInfoTable();
            infoTable.Build(LoadAllBulletInfos());

            if (infoTable.Count == 0)
                Debug.LogWarning("[BulletSimulationSystem] BulletInfo가 하나도 등록되지 않았습니다. Inspector에 할당하거나 Resources에 배치하세요.");

            // 정적 효과/사운드 슬롯 주입 (도메인 리로드 시 매번 채워야 함)
            BulletEffectsRegistry.MetalImpactVfx = metalImpactVfx;
            BulletEffectsRegistry.HitSmoke = hitSmoke;
            EffectsBudget.MaxMetalImpactPerFrame = maxMetalImpactPerFrame;
            EffectsBudget.MaxSmokePerFrame = maxSmokePerFrame;
            EffectsBudget.MaxVisibleDistance = maxVfxVisibleDistance;
            SoundUtility.RicochetClips = ricochetClips;
            SoundUtility.DefaultImpactClips = defaultImpactClips;
            SoundUtility.MetalImpactClips = metalImpactClips;
            SoundUtility.BodyImpactClips = bodyImpactClips;
            SoundUtility.HeadImpactClips = headImpactClips;
            SoundUtility.RicochetVolume = ricochetVolume;
            SoundUtility.DefaultImpactVolume = defaultImpactVolume;
            SoundUtility.MetalImpactVolume = metalImpactVolume;
            SoundUtility.BodyImpactVolume = bodyImpactVolume;
            SoundUtility.HeadImpactVolume = headImpactVolume;
            SoundUtility.MaxSoundsPerFramePerCategory = maxSoundsPerFramePerCategory;
            SoundUtility.MaxAudibleDistance = maxAudibleDistance;
            SoundUtility.InitializePool(transform, audioPoolSize);

            hitProcessor = new BulletHitProcessor(allocator, infoTable);
        }

        private void Start()
        {
            // 씬에 있는 hit-layer 콜라이더에 ColliderRegistrar를 동적 부착.
            // MaterialManager가 있는 콜라이더는 이미 자가 등록되므로 중복 부착은 무해 (Dict[id] = col).
            // 한계: 런타임에 새로 스폰되는 콜라이더는 자가 등록 컴포넌트(MaterialManager 또는 ColliderRegistrar) 필요.
            RegisterAllSceneCollidersInHitMask();

            // VFX 풀 워밍업 — 첫 발사 폭주 시 Instantiate 0 만들기
            if (PoolManager.Instance != null)
            {
                if (metalImpactVfx != null) PoolManager.Instance.Prewarm(metalImpactVfx, prewarmMetalImpactVfx);
                if (hitSmoke != null) PoolManager.Instance.Prewarm(hitSmoke, prewarmHitSmoke);
            }
            else
            {
                Debug.LogWarning("[BulletSimulationSystem] PoolManager.Instance가 NULL — VFX 워밍업 건너뜀. 씬에 PoolManager 배치 확인 필요.");
            }
        }

        private void RegisterAllSceneCollidersInHitMask()
        {
            var all = FindObjectsOfType<Collider>(includeInactive: true);
            int added = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null) continue;
                int layer = c.gameObject.layer;
                if ((hitLayerMask.value & (1 << layer)) == 0) continue;
                if (c.GetComponent<ColliderRegistrar>() != null) continue;
                if (c.GetComponentInParent<MaterialManager>() != null) continue;  // MaterialManager가 처리
                c.gameObject.AddComponent<ColliderRegistrar>();
                added++;
            }
            if (added > 0)
                Debug.Log($"[BulletSimulationSystem] ColliderRegistrar 동적 부착: {added}개");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            allocator?.Dispose();
            infoTable?.Dispose();
            if (activeIndices.IsCreated) activeIndices.Dispose();
            if (commands.IsCreated) commands.Dispose();
            if (results.IsCreated) results.Dispose();
            if (hitQueue.IsCreated) hitQueue.Dispose();
            if (playerBulletDetectionCache.IsCreated) playerBulletDetectionCache.Dispose();
        }

        /// <summary>
        /// 적 AI가 근접 플레이어 총알을 감지할 때 사용하는 활성 총알 뷰.
        /// 매 FixedUpdate 끝에 갱신된다. 호출자는 read-only로만 사용해야 함.
        /// </summary>
        public NativeArray<BulletDetectionData> GetActivePlayerBulletsForDetection()
        {
            if (!playerBulletDetectionCache.IsCreated)
                return default;
            return playerBulletDetectionCache.AsArray();
        }

        private void RefreshDetectionCache()
        {
            playerBulletDetectionCache.Clear();
            var states = allocator.States;
            for (int i = 0; i < states.Length; i++)
            {
                var s = states[i];
                if (s.isAlive == 0 || s.isPlayerShot == 0) continue;
                float3 dir = math.normalizesafe(s.velocity, new float3(0f, 0f, 1f));
                playerBulletDetectionCache.Add(new BulletDetectionData
                {
                    pos = s.pos,
                    travelDir = dir,
                });
            }
        }

        private IEnumerable<BulletInfo> LoadAllBulletInfos()
        {
            if (registeredBulletInfos != null && registeredBulletInfos.Count > 0)
                return registeredBulletInfos;

            return Resources.LoadAll<BulletInfo>("");
        }

        // ---- Spawn API ----

        public bool Spawn(float3 pos, float3 dir, BulletInfo so, bool isPlayerShot)
        {
            int infoIndex = infoTable.Resolve(so);
            if (infoIndex < 0)
            {
                Debug.LogWarning($"[BulletSimulationSystem] BulletInfoTable에 없는 SO: {so?.name}. Inspector 또는 Resources 등록을 확인하세요.");
                return false;
            }

            if (!allocator.TryAllocate(out int slot))
            {
                poolFullDropCount++;
                float now = Time.unscaledTime;
                if (now - lastPoolFullWarnTime >= PoolFullWarnInterval)
                {
                    Debug.LogWarning(
                        $"[BulletSimulationSystem] Pool 가득참 (Active={allocator.Active}/{capacity}), " +
                        $"최근 {PoolFullWarnInterval}s에 {poolFullDropCount}발 드롭. " +
                        "Capacity를 늘리거나 BulletInfo.lifeTime을 줄이거나 발사 빈도를 낮추세요.");
                    lastPoolFullWarnTime = now;
                    poolFullDropCount = 0;
                }
                return false;
            }

            var info = infoTable.Infos[infoIndex];
            var states = allocator.States;
            var s = states[slot];

            s.id = System.Threading.Interlocked.Increment(ref idSeq);
            s.bulletInfoIndex = infoIndex;
            s.isPlayerShot = (byte)(isPlayerShot ? 1 : 0);
            s.isPenetratingTerrain = 0;
            s.isAlive = 1;
            s.ricochetChance = 0;
            s.flightTime = 0f;

            float3 dirN = math.normalizesafe(dir, new float3(0f, 0f, 1f));
            s.pos = pos;
            s.prevPos = pos;
            s.dir = dirN;
            s.velocity = dirN * info.muzzleVelocity;
            s.speed = info.muzzleVelocity;
            s.pen = info.penetrationPower;
            s.armorDam = info.armorDamage;

            float invMass = 1f / math.max(1e-6f, info.mass);
            float r = math.max(1e-6f, info.caliberMm * 0.001f) * 0.5f;
            s.refArea = math.PI * r * r * info.refAreaScale;
            s.k = 0.5f * airDensity * info.dragCoeff * s.refArea * invMass;

            s.hitTargets = new FixedList64Bytes<int>();
            // 슬롯별 시드 — 도탄 각도와 방탄 관통 확률용. id로 시드하면 0 회피 + 분산.
            uint seed = (uint)s.id * 2654435761u | 1u;
            s.rng = new UnityRandom(seed);

            states[slot] = s;
            return true;
        }

        public bool Spawn(Vector3 pos, Vector3 dir, BulletInfo so, bool isPlayerShot)
            => Spawn((float3)pos, (float3)dir, so, isPlayerShot);

        // ---- FixedUpdate Job 체인 ----

        private void FixedUpdate()
        {
            if (allocator == null) return;
            if (allocator.Active == 0) return;

            float dt = Time.fixedDeltaTime;

            var buildActiveJob = new BuildActiveIndicesJob
            {
                states = allocator.States,
                activeIndices = activeIndices,
            };
            JobHandle buildActiveHandle = buildActiveJob.Schedule();

            var physicsJob = new BulletPhysicsJob
            {
                activeIndices = activeIndices.AsDeferredJobArray(),
                infos = infoTable.Infos,
                states = allocator.States,
                gravity = (float3)Physics.gravity,
                windWorld = cachedWindWorld,
                deltaTime = dt,
            };
            JobHandle physicsHandle = physicsJob.Schedule(activeIndices, 32, buildActiveHandle);

            var buildRaysJob = new BuildRaycastCommandsJob
            {
                activeIndices = activeIndices.AsDeferredJobArray(),
                states = allocator.States,
                commands = commands,
                queryParams = queryParams,
            };
            JobHandle buildRaysHandle = buildRaysJob.Schedule(activeIndices, 32, physicsHandle);

            JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
                commands, results, minCommandsPerJob: 32, maxHits: 1, dependsOn: buildRaysHandle);

            var collectJob = new CollectHitsJob
            {
                activeIndices = activeIndices.AsDeferredJobArray(),
                states = allocator.States,
                results = results,
                hitQueue = hitQueue,
            };
            JobHandle collectHandle = collectJob.Schedule(raycastHandle);

            collectHandle.Complete();

            // 메인 스레드 히트 드레인 (매니지드 매니저 접근)
            while (hitQueue.TryDequeue(out var ev))
            {
                hitProcessor.Process(ev);
            }

            // 수명 만료 슬롯 회수 (히트 드레인 중 isAlive=0 처리된 것들 + Physics job에서 lifeTime 만료 처리된 것들)
            var states = allocator.States;
            for (int i = 0; i < activeIndices.Length; i++)
            {
                int slot = activeIndices[i];
                if (states[slot].isAlive == 0)
                    allocator.Release(slot);
            }

            // 적 감지용 활성 플레이어 총알 캐시 갱신 (EnemyController.Detection이 다음 프레임에 사용)
            RefreshDetectionCache();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (allocator == null || !allocator.States.IsCreated) return;
            var states = allocator.States;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].isAlive == 0) continue;
                Gizmos.DrawSphere(states[i].pos, 0.05f);
                Gizmos.DrawLine(states[i].prevPos, states[i].pos);
            }
        }
#endif
    }
}
