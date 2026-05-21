using UnityEngine;

/// <summary>
/// 머즐 플래시 전용 라이트 페이드 컴포넌트.
/// 임팩트 라이트보다 짧고 강한 스파이크 + 매 발사마다 약간씩 다른 강도/색상으로
/// 반복감 없는 자연스러운 발사 느낌을 만든다.
///
/// 부착 위치: 머즐 플래시 프리팹의 Light 자식 GameObject
///
/// 동작:
/// - OnEnable에 즉시 최대 강도로 점등 (스파이크)
/// - intensityCurve를 따라 빠르게 감쇠 (기본 60ms)
/// - 색상/강도/반경에 무작위 변동(jitter) 적용 → 매 발사가 미세하게 다름
/// - 페이드 끝나면 Light만 끔 (풀 반환은 보통 EffectsAutoReturn이 담당)
/// </summary>
[RequireComponent(typeof(Light))]
public class MuzzleFlashLight : MonoBehaviour
{
    [Header("Flash Spike")]
    [SerializeField, Tooltip("페이드 총 시간(초). 매우 짧게 (0.04~0.08 권장)")]
    private float duration = 0.06f;

    [SerializeField, Tooltip("최대 강도 (기본값, jitter 전)")]
    private float maxIntensity = 12f;

    [SerializeField, Tooltip("시간→강도 곡선. 0에서 1로 즉시 점등 후 빠르게 0으로 감쇠")]
    private AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -25f),       // 시작: 100% 즉시
        new Keyframe(0.3f, 0.3f, -3f, -3f),   // 30% 시점에 30%까지 급감
        new Keyframe(1f, 0f, -1f, 0f)         // 끝: 0
    );

    [Header("Color")]
    [SerializeField, Tooltip("머즐 플래시 기본 색상. 일반적으로 노란-주황 (1.0, 0.85, 0.5)")]
    private Color color = new Color(1.0f, 0.85f, 0.5f);

    [Header("Range")]
    [SerializeField, Tooltip("라이트 반경 (m). 0이면 Light 컴포넌트의 기본값")]
    private float lightRange = 0f;

    [Header("Jitter (매 발사마다 미세 변동)")]
    [SerializeField, Tooltip("강도 랜덤 변동 폭 (예: 0.15 = ±15%)")]
    [Range(0f, 1f)] private float intensityJitter = 0.15f;

    [SerializeField, Tooltip("반경 랜덤 변동 폭 (예: 0.1 = ±10%)")]
    [Range(0f, 1f)] private float rangeJitter = 0.10f;

    [SerializeField, Tooltip("지속시간 랜덤 변동 폭 (예: 0.2 = ±20%)")]
    [Range(0f, 1f)] private float durationJitter = 0.15f;

    [SerializeField, Tooltip("색 hue 랜덤 변동 폭 (HSV의 H, 0~1 범위). 0.03 정도면 노랑~주황 사이")]
    [Range(0f, 0.2f)] private float hueJitter = 0.02f;

    [Header("End Behavior")]
    [SerializeField, Tooltip("페이드 끝나면 Light 컴포넌트 비활성")]
    private bool disableLightWhenDone = true;

    [SerializeField, Tooltip("페이드 끝나면 풀로 반환. EffectsAutoReturn과 같이 쓰면 false 권장")]
    private bool returnToPool = false;

    // ---- 내부 상태 ----
    private Light lightComponent;
    private float elapsed;
    private float baseRange;
    private bool finished;

    // 매 점등마다 결정되는 jittered 값들
    private float currentDuration;
    private float currentMaxIntensity;
    private float currentRange;
    private Color currentColor;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        baseRange = lightComponent.range;
    }

    private void OnEnable()
    {
        // 이번 발사용 jitter 결정
        currentDuration = duration * (1f + Random.Range(-durationJitter, durationJitter));
        currentMaxIntensity = maxIntensity * (1f + Random.Range(-intensityJitter, intensityJitter));

        float r = lightRange > 0f ? lightRange : baseRange;
        currentRange = r * (1f + Random.Range(-rangeJitter, rangeJitter));

        // 색상 jitter (HSV의 hue만 살짝 흔듦)
        if (hueJitter > 0f)
        {
            Color.RGBToHSV(color, out float h, out float sat, out float v);
            h = Mathf.Repeat(h + Random.Range(-hueJitter, hueJitter), 1f);
            currentColor = Color.HSVToRGB(h, sat, v);
        }
        else
        {
            currentColor = color;
        }

        // 시작 상태
        elapsed = 0f;
        finished = false;
        lightComponent.enabled = true;
        lightComponent.color = currentColor;
        lightComponent.intensity = currentMaxIntensity;
        lightComponent.range = currentRange;
    }

    private void Update()
    {
        if (finished) return;

        elapsed += Time.deltaTime;
        float t = currentDuration > 0f ? Mathf.Clamp01(elapsed / currentDuration) : 1f;

        lightComponent.intensity = currentMaxIntensity * intensityCurve.Evaluate(t);

        if (t >= 1f)
        {
            finished = true;
            OnFinish();
        }
    }

    private void OnFinish()
    {
        lightComponent.intensity = 0f;

        if (returnToPool)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.Return(gameObject);
            else
                gameObject.SetActive(false);
            return;
        }

        if (disableLightWhenDone)
            lightComponent.enabled = false;
    }

    /// <summary>
    /// 런타임 강제 점등 (예: 발사 외 특수 효과). OnEnable과 동일 효과.
    /// </summary>
    public void Trigger()
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        else { enabled = false; enabled = true; }  // OnEnable 재실행
    }
}
