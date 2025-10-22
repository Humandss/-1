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
    float totalWeight;
    float valueLerpSpeed = 10.0f;
    float curVig;
    static readonly Color TunnelBaseColor = Color.black;

    [SerializeField] AnimationCurve hpCurve; // 인스펙터에서 보이게
    [SerializeField] float half = 0.5f, low = 0.2f, eps = 0.05f;


    [Header("Bleeding Effect Options)")]
    [SerializeField, Range(0f, 1f)] float bleed;         
    [SerializeField] Color bleedColor = new(0.45f, 0.05f, 0.05f, 1f);
    [SerializeField] bool bleedAffectsColor = true;   
    [SerializeField] bool bleedAffectsColorAdjust = true;
    [SerializeField] float bleedSatMin = 0f, bleedSatMax = -0.12f; 
    [SerializeField] float bleedExpMin = 0f, bleedExpMax = -0.10f;

    float iTunnel;  
    float iBleed;   



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
       PlayVisionEffect();
      
    }
    private float CheckBleedingCount()
    {
        //과다 출혈이 2개일 경우 최대값 발행
        if (healthStateProvider.GetNumberHeavyBleeding() >= 2) 
            return 0.75f;

        if (healthStateProvider.GetNumberHeavyBleeding() == 1) 
            return  0.6f;

        if (healthStateProvider.GetNumberLightBleeding() >= 2)
            return 0.5f;

        return 0.0f;
    }
    private void PlayVisionEffect()
    {
        if (vig == null) return;

        float hp01 = healthStateProvider.GetTotalHP() / maxHP;

        iTunnel = Mathf.Clamp01(hpCurve.Evaluate(hp01));

        iBleed = CheckBleedingCount();
        
        totalWeight = 1.0f - (1.0f - iTunnel) * (1.0f - iBleed);

        volume.weight=  Mathf.MoveTowards(volume.weight, totalWeight, weightLerpSpeed*Time.deltaTime);

        float baseVig = Mathf.Lerp(min, max, volume.weight);

        if (speed > 0.0f && iTunnel > 0.0f)
        {
            float pulse = 0.5f * (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f);
            baseVig = Mathf.Lerp(baseVig * 0.9f, baseVig * 1.1f, pulse);
        }
        curVig = Mathf.Lerp(curVig, baseVig, 1.0f - Mathf.Exp(-valueLerpSpeed * Time.deltaTime));
        vig.intensity.Override(curVig);
        vig.smoothness.Override(0.95f);

        if (bleedAffectsColor)
        {
            Color finalColor = Color.Lerp(TunnelBaseColor, bleedColor, iBleed);
            vig.color.Override(finalColor);
        }
        else
        {
            vig.color.Override(TunnelBaseColor);
        }

        
        if (colorAdj && bleedAffectsColorAdjust)
        {
            colorAdj.saturation.Override(Mathf.Lerp(bleedSatMin, bleedSatMax, iBleed));
            colorAdj.postExposure.Override(Mathf.Lerp(bleedExpMin, bleedExpMax, iBleed));
        }
    }
}
