using UnityEngine;

/// <summary>
/// HealthManager가 출혈 상태일 때 발 밑에 주기적으로 피 데칼 스폰.
/// KriptoFX AttachedBlood 같은 표면 부착형 VFX와 함께 사용.
///
/// 동작:
/// - 매 spawnInterval 초마다 GetNumberHeavyBleeding() 체크
/// - > 0 이면 발 밑 랜덤 오프셋 위치에서 아래로 raycast → 바닥 표면 찾음
/// - 그 위치에 prefab 스폰 (PoolManager 사용)
///
/// 부착 위치: HealthManager가 있는 GameObject (Player 또는 Enemy 모두)
/// </summary>
[RequireComponent(typeof(HealthManager))]
public class BleedingBloodTrail : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField, Tooltip("바닥에 스폰할 피 데칼 프리팹 (KriptoFX AttachedBlood 등). " +
                              "비워두면 LockDown.Ballistic.Job.BulletEffectsRegistry.AttachedBloodPrefab 사용")]
    private GameObject attachedBloodPrefab;

    [Header("Trigger Conditions")]
    [SerializeField, Tooltip("True면 heavy bleeding만 트리거. False면 light/heavy 둘 다")]
    private bool heavyBleedingOnly = true;

    [SerializeField, Tooltip("출혈 부위 개수에 따른 스폰 빈도 배수 (출혈 많을수록 자주)")]
    private bool scaleByBleedingCount = true;

    [Header("Spawn Settings")]
    [SerializeField, Tooltip("스폰 주기 (초). heavy bleeding 1개 기준")]
    private float spawnInterval = 0.5f;

    [SerializeField, Tooltip("발 밑 수평 랜덤 오프셋 반경 (m)")]
    private float horizontalOffsetRadius = 0.25f;

    [SerializeField, Tooltip("바닥 탐색 raycast 시작 높이 (transform.position 기준 +Y, m)")]
    private float probeStartHeight = 0.5f;

    [SerializeField, Tooltip("바닥 탐색 raycast 최대 거리 (m). 캐릭터가 공중에 있으면 스폰 안 됨")]
    private float floorProbeDistance = 3f;

    [SerializeField, Tooltip("바닥으로 인정할 레이어")]
    private LayerMask floorLayerMask = ~0;   // 기본: 모든 레이어 (사용자가 Inspector에서 좁힘)

    [Header("Visual Variety")]
    [SerializeField, Tooltip("Y축 무작위 회전")]
    private bool randomYRotation = true;

    [SerializeField, Tooltip("스폰 시 적용할 무작위 스케일 (X=최소, Y=최대). 1,1이면 변동 없음")]
    private Vector2 randomScaleRange = new Vector2(0.8f, 1.4f);

    [Header("Throttle")]
    [SerializeField, Tooltip("같은 위치 근처에 너무 자주 안 쌓이게 최소 이동 거리(m). 이동 안 하면 스폰 간격 늘림")]
    private float minMoveBetweenSpawns = 0.15f;

    [Header("Instantiation")]
    [SerializeField, Tooltip("풀 대신 직접 Instantiate (KriptoFX 같은 풀-비호환 프리팹용). " +
                              "체크 시 lifetime 후 자체 Destroy")]
    private bool bypassPool = false;

    [SerializeField, Tooltip("bypassPool=true일 때 자동 Destroy까지 시간(초)")]
    private float fallbackLifetime = 12f;

    // ---- 내부 상태 ----
    private HealthManager healthManager;
    private float nextSpawnTime;
    private Vector3 lastSpawnPos = new Vector3(float.PositiveInfinity, 0f, 0f);

    private void Awake()
    {
        healthManager = GetComponent<HealthManager>();
    }

    private void Update()
    {
        if (healthManager == null) return;
        if (Time.time < nextSpawnTime) return;

        int heavyCount = healthManager.GetNumberHeavyBleeding();
        int lightCount = heavyBleedingOnly ? 0 : healthManager.GetNumberLightBleeding();
        int totalBleeding = heavyCount + lightCount;

        if (totalBleeding == 0) return;

        // 마지막 스폰 위치에서 너무 가까우면 간격 늘림 (제자리 서있으면 덜 자주)
        float intervalMul = 1f;
        if (lastSpawnPos.x != float.PositiveInfinity)
        {
            float moved = Vector3.Distance(transform.position, lastSpawnPos);
            if (moved < minMoveBetweenSpawns)
                intervalMul = 2.5f;   // 제자리면 간격 2.5배
        }

        // 출혈 부위 수에 따라 빈도 가속 (heavy=1.0, 2개=0.7배 시간, 3개=0.5배 시간)
        if (scaleByBleedingCount && totalBleeding > 1)
            intervalMul /= Mathf.Sqrt(totalBleeding);

        if (TrySpawnBloodOnFloor())
        {
            lastSpawnPos = transform.position;
            nextSpawnTime = Time.time + spawnInterval * intervalMul;
        }
        else
        {
            // 바닥 못 찾았으면 짧게 재시도
            nextSpawnTime = Time.time + 0.2f;
        }
    }

    private bool TrySpawnBloodOnFloor()
    {
        var prefab = attachedBloodPrefab;
        if (prefab == null) prefab = LockDown.Ballistic.Job.BulletEffectsRegistry.AttachedBloodPrefab;
        if (prefab == null) return false;
        if (!bypassPool && PoolManager.Instance == null) return false;

        // 발 밑 랜덤 위치 → 위로 살짝 올린 뒤 아래로 raycast
        Vector2 rand2D = Random.insideUnitCircle * horizontalOffsetRadius;
        Vector3 origin = transform.position
                       + new Vector3(rand2D.x, probeStartHeight, rand2D.y);

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                             floorProbeDistance + probeStartHeight,
                             floorLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        // 표면 노멀 따라 회전 (바닥에 평평하게 붙음)
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
        if (randomYRotation)
            rot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 spawnPos = hit.point + hit.normal * 0.005f;

        GameObject go;
        if (bypassPool)
        {
            // 풀 비호환 프리팹용 — 직접 Instantiate + 자체 Destroy 스케줄
            go = Instantiate(prefab, spawnPos, rot);
            if (fallbackLifetime > 0f) Destroy(go, fallbackLifetime);
        }
        else
        {
            go = PoolManager.Instance.Spawn(prefab, spawnPos, rot);
        }

        if (go == null) return false;

        // 스케일 변동 (풀에서 꺼낼 때 항상 새로 적용)
        if (randomScaleRange.y > randomScaleRange.x)
        {
            float scale = Random.Range(randomScaleRange.x, randomScaleRange.y);
            go.transform.localScale = Vector3.one * scale;
        }
        else
        {
            go.transform.localScale = Vector3.one;
        }

        return true;
    }
}
