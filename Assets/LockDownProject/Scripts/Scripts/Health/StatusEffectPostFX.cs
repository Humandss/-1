using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class StatusEffectPostFX : MonoBehaviour
{
    float maxHP = 0.0f;

    [Header("Refs")]
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private Volume volume;
    private Vignette vig;
    private ColorAdjustments colorAdj;

    [Header("Providers")]
    private IHealthStateProvider healthStateProvider;

    [Header("TunnelVision Options")]
    [SerializeField, Range(0f, 10f)] private float speed = 0.5f;   
    [SerializeField] private float min = 0.25f;
    [SerializeField] private float max = 0.7f;

    [Header("TunnelVisionBlend")]
    [SerializeField] float weightLerpSpeed = 4.0f;
    [SerializeField]
    float valueLerpSpeed = 10.0f;
    float targetWeight;
    float curVig;
    static readonly Color TunnelBaseColor = Color.black;

    [SerializeField] AnimationCurve hpCurve; // 인스펙터에서 보이게
    [SerializeField] float half = 0.5f, low = 0.2f, eps = 0.05f;


    [Header("출혈 비전(외부에서 강도 0~1 세팅)")]
    [SerializeField, Range(0f, 1f)] float bleed;         
    [SerializeField] Color bleedColor = new(0.45f, 0.05f, 0.05f, 1f);
    [SerializeField] bool bleedAffectsColor = true;   
    [SerializeField] bool bleedAffectsColorAdjust = true;
    [SerializeField] float bleedSatMin = 0f, bleedSatMax = -0.12f; 
    [SerializeField] float bleedExpMin = 0f, bleedExpMax = -0.10f;

    float iTunnel;   // 0~1 (HP 기반 계산 결과)
    float iBleed;    // 0~1 (SetBleed로 들어오는 값)
    float wCur;      // 현재 weight
    float vigCur;    // 현재 비넷 적용값

    public static AnimationCurve MakeHpCurve(float half = 0.5f, float low = 0.2f, float eps = 0.05f)
    {
        var keys = new[]
        {
        new Keyframe(1.00f, 0.00f),   // 1
        new Keyframe(half + eps, 0.00f), // 0.5직전
        new Keyframe(half, 0.70f),    // 0.5
        new Keyframe(low  + eps, 0.70f), // 0.2직전
        new Keyframe(low, 1.00f),     // 0.2 
        new Keyframe(0.00f, 1.00f)    // 0
    };

        var curve = new AnimationCurve(keys);
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        return curve;
    }
    private void Awake()
    {
        volume = GetComponent<Volume>();
        if( volume == null )
        {
            Debug.LogWarning("[StatusEffectPostFX]  volume is NULL");
        }

        healthStateProvider = healthManager as IHealthStateProvider;
        if (healthStateProvider == null)
        {
            Debug.LogWarning("[StatusEffectPostFX]   healthStateProvider is NULL");
        }

        if (hpCurve == null || hpCurve.length == 0)
            hpCurve = MakeHpCurve(half, low, eps);

        volume.profile.TryGet(out vig);
        volume.profile.TryGet(out colorAdj);

    }
    private void Start()
    {
        maxHP = healthStateProvider.GetMaxHP();
        
    }
    private void LateUpdate()
    {
       PlayTunnelVisionEffect();
     
    }
    private void PlayTunnelVisionEffect()
    {
        if (vig == null) return;

        float hp01 = healthStateProvider.GetTotalHP() / maxHP;

        float fxTarget = Mathf.Clamp01(hpCurve.Evaluate(hp01));

        targetWeight = fxTarget;
        volume.weight=  Mathf.MoveTowards(volume.weight, targetWeight, weightLerpSpeed*Time.deltaTime);

        float baseVig = Mathf.Lerp(min, max, volume.weight);

        if (speed > 0.0f)
        {
            float pulse = 0.5f * (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f);
            baseVig = Mathf.Lerp(baseVig * 0.9f, baseVig * 1.1f, pulse);
        }
        curVig = Mathf.Lerp(curVig, baseVig, 1.0f - Mathf.Exp(-valueLerpSpeed * Time.deltaTime));
        vig.intensity.Override(curVig);
    }
}
