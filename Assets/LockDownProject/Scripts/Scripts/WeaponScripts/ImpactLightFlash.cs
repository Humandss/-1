using UnityEngine;

/// <summary>
/// 도탄/충격 VFX 프리팹에 부착되는 라이트 페이드 컴포넌트.
/// OnEnable 시점에 라이트 강도가 켜지고, 지정한 곡선/시간에 따라 0으로 감쇠.
/// 감쇠 끝나면 라이트만 끄거나(returnToPool=false) 풀로 반환(returnToPool=true).
///
/// 사용 방법:
/// 1. Spark1 같은 VFX 프리팹의 자식 GameObject에 Light 컴포넌트 추가
/// 2. 같은 GameObject에 이 ImpactLightFlash 부착
/// 3. Inspector에서 Color/Duration/MaxIntensity 설정
/// 4. Particle System과 같이 쓰면 EffectsAutoReturn이 풀 반환 담당 → returnToPool=false
///    독립 사용이면 returnToPool=true
///
/// 도탄/임팩트별 권장 색상 (참고):
///   - Ricochet (도탄):       주황 (1.0, 0.6, 0.1)
///   - Metal Impact (금속):    노랑-주황 (1.0, 0.8, 0.3)
///   - Default Impact (벽):    옅은 백색 (0.9, 0.9, 0.8)
///   - Body Impact (인체):     어두운 빨강 (0.6, 0.1, 0.1)
///   - Head Impact (머리):     빨강 (0.9, 0.1, 0.1)
/// </summary>
[RequireComponent(typeof(Light))]
public class ImpactLightFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField, Tooltip("페이드 총 시간(초)")]
    private float duration = 0.12f;

    [SerializeField, Tooltip("시작 시 최대 라이트 강도")]
    private float maxIntensity = 6f;

    [SerializeField, Tooltip("시간에 따른 강도 곡선 (0~1 범위, x=시간 정규화, y=강도 비율)")]
    private AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -5f),   // 시작: 최대
        new Keyframe(0.2f, 0.4f),         // 빠르게 감쇠
        new Keyframe(1f, 0f)              // 끝: 0
    );

    [Header("Color")]
    [SerializeField, Tooltip("라이트 색상. Inspector에서 도탄/금속/인체 등 변종별로 설정")]
    private Color color = new Color(1.0f, 0.6f, 0.1f);   // 기본: 주황 (도탄 색)

    [SerializeField, Tooltip("색상도 시간에 따라 변경하려면 ✅ (예: 노랑→주황→빨강)")]
    private bool useColorGradient = false;

    [SerializeField, Tooltip("Color Gradient 사용 시 색상 변화 곡선")]
    private Gradient colorGradient;

    [Header("Range")]
    [SerializeField, Tooltip("라이트 도달 거리(0이면 Light 컴포넌트의 기본값 사용)")]
    private float lightRange = 0f;

    [SerializeField, Tooltip("Range도 시간에 따라 줄이려면 ✅ (충격 후 빛이 작아지는 효과)")]
    private bool fadeRange = false;

    [Header("Auto Behavior")]
    [SerializeField, Tooltip("페이드 끝나면 풀로 반환할지. Particle System(EffectsAutoReturn)과 같이 쓰면 false 권장")]
    private bool returnToPool = false;

    [SerializeField, Tooltip("페이드 끝나면 Light 컴포넌트만 비활성화. 풀 반환 안 할 때 유용")]
    private bool disableLightWhenDone = true;

    // ---- 내부 상태 ----
    private Light lightComponent;
    private float elapsed;
    private float baseRange;
    private bool finished;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        baseRange = lightComponent.range;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        finished = false;

        // 시작 상태 세팅
        lightComponent.enabled = true;
        lightComponent.color = color;
        lightComponent.intensity = maxIntensity;
        lightComponent.range = lightRange > 0f ? lightRange : baseRange;
    }

    private void Update()
    {
        if (finished) return;

        elapsed += Time.deltaTime;
        float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        // 강도 적용
        float intensityFactor = intensityCurve.Evaluate(t);
        lightComponent.intensity = maxIntensity * intensityFactor;

        // 색상 그라데이션 적용 (옵션)
        if (useColorGradient && colorGradient != null)
        {
            lightComponent.color = colorGradient.Evaluate(t);
        }

        // 거리 페이드 적용 (옵션)
        if (fadeRange)
        {
            float r = lightRange > 0f ? lightRange : baseRange;
            lightComponent.range = r * (1f - t * 0.5f);   // 끝에 50% 줄어듦
        }

        // 종료 처리
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
                gameObject.SetActive(false);   // 풀 없으면 그냥 비활성
            return;
        }

        if (disableLightWhenDone)
            lightComponent.enabled = false;
    }

    /// <summary>
    /// 런타임에 색상 강제 변경. 외부에서 BulletHitProcessor 등이 호출해서
    /// "같은 프리팹을 다른 색으로" 재사용할 때 유용.
    /// Spawn 직후 호출 권장 (OnEnable 다음 프레임 이후).
    /// </summary>
    public void SetColor(Color c)
    {
        color = c;
        if (lightComponent != null) lightComponent.color = c;
    }

    /// <summary>
    /// 런타임 강도 변경.
    /// </summary>
    public void SetMaxIntensity(float v)
    {
        maxIntensity = v;
    }

    /// <summary>
    /// 런타임 지속시간 변경.
    /// </summary>
    public void SetDuration(float seconds)
    {
        duration = Mathf.Max(0.01f, seconds);
    }
}
