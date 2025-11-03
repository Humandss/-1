using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public interface IUIStateProvider
{
    void CheckUIPanelOn(bool value);
}
[System.Serializable]
public class PartRowRefs
{
    public BodyParts part;
    public List<Image> images;
    public TextMeshProUGUI label;
    public Slider bar;
    public TextMeshProUGUI valueText;
    public GameObject iconLight;
    public GameObject iconHeavy;
    public GameObject iconFracture;
    public GameObject iconBlackout;
}
[System.Serializable]
public class BodyImageRef
{
    public BodyParts part;
    public List<Image> images; 
}
public class UIManager : MonoBehaviour, IUIStateProvider
{

    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI totalHP;
    private HealthManager healthManager;
    [SerializeField] private List<PartRowRefs> rows;

    [Header("Panel UI")]
    private Dictionary<BodyParts, PartRowRefs> map;
  
    [Header("HP Gradient")]
    [SerializeField] private Gradient hpGradient; 
    [SerializeField] private Color zeroOrBlackoutColor = new Color(0.0f, 0.0f, 0.0f);

    [Header("In-Game UI")]
    [SerializeField] private Slider inGameTotalHPBar;
    [SerializeField] private GameObject inGameIconLight;
    [SerializeField] private GameObject inGameIconHeavy;
    [SerializeField] private GameObject inGameIconFracture;
    [SerializeField] private GameObject inGameIconBlackout;
    [SerializeField] private GameObject InGamepanel;
    [SerializeField] private List<BodyImageRef> bodyImages = new();
    private Dictionary<BodyParts, BodyImageRef> bodyMap;

    private void Awake()
    {
             
        healthManager = GetComponent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[UIManager] healthManager is NULL");
        }

        map = new Dictionary<BodyParts, PartRowRefs>();
        foreach (var r in rows) map[r.part] = r;

        bodyMap = new();
        foreach (var b in bodyImages) bodyMap[b.part] = b;

        InitializeUI();
       
    }
    private void OnEnable()
    {
       // healthManager.OnPartChanged += UpdateRow;              
        healthManager.OnBatchChanged += UpdateBatch;           
        healthManager.OnOverallChanged += UpdateOverall;  
       
    }
    private void OnDisable()
    {
       // healthManager.OnPartChanged -= UpdateRow;
        healthManager.OnBatchChanged -= UpdateBatch;
        healthManager.OnOverallChanged -= UpdateOverall;
    }
    private void Start()
    {
        RefreshAll();
    }
    private void InitializeUI()
    {
        panel.SetActive(false);
        InGamepanel.SetActive(true);
        inGameIconLight.SetActive(false);
        inGameIconHeavy.SetActive(false);
        inGameIconFracture.SetActive(false);
        inGameIconBlackout.SetActive(false);
    }
    private void RefreshAll()
    {
        //각 파트마다 갱신
        foreach (var r in rows) UpdateRow(healthManager.GetSnapshot(r.part));
       // 전체 오버롤 갱신
        UpdateOverall(healthManager.GetOverallSnapshot());
    }

    private void UpdateBatch(IReadOnlyList<PartSnapshot> snaps)
    {
        for (int i = 0; i < snaps.Count; i++) UpdateRow(snaps[i]);

        bool anyLight = false;
        bool anyHeavy = false;
        bool anyFrac = false;
        bool anyBlackout = false;

        for(int i=0; i<snaps.Count; i++)
        {
            anyLight |= snaps[i].light;
            anyHeavy |= snaps[i].heavy;
            anyFrac |= snaps[i].fracture;
            anyBlackout |= snaps[i].blackout;
        }
        UpdateInGameEffectIcons(anyLight, anyHeavy, anyFrac, anyBlackout);
    }

    private void UpdateRow(PartSnapshot snap)
    {
        if (!map.TryGetValue(snap.part, out var row)) return;

        if (row.bar)
        {
            row.bar.minValue = 0.0f;
            row.bar.maxValue = snap.maxHp;
            row.bar.value = Mathf.Clamp(snap.hp, 0.0f, snap.maxHp);
        }

        if (row.valueText) row.valueText.text = $"{Mathf.RoundToInt(snap.hp)} / {Mathf.RoundToInt(snap.maxHp)}";

        Toggle(row.iconLight, snap.light);
        Toggle(row.iconHeavy, snap.heavy);
        Toggle(row.iconFracture, snap.fracture);
        Toggle(row.iconBlackout, snap.blackout);

        SetBarColorSmooth(row.bar, snap.maxHp <= 0.0f ? 0.0f : snap.hp / snap.maxHp, snap.blackout);
        UpdateBodyColor(snap);
        
        
    }
    private void UpdateInGameEffectIcons(bool anyLight, bool anyHeavy, bool anyFrac, bool anyBlack)
    {
       
       Toggle(inGameIconLight, anyLight);
       Toggle(inGameIconHeavy, anyHeavy);
       Toggle(inGameIconFracture, anyFrac);
       Toggle(inGameIconBlackout, anyBlack);

    }
    
    private void UpdateOverallHPBar(OverallSnapshot overall)
    {
        inGameTotalHPBar.maxValue = 0.0f;
        inGameTotalHPBar.maxValue= overall.totalMaxHp;
        inGameTotalHPBar.value = Mathf.Clamp(overall.totalHp, 0.0f, overall.totalMaxHp);

        SetInGameTotalHPColorSmooth(inGameTotalHPBar, overall.totalHp <= 0.0f ? 0.0f : overall.totalHp / overall.totalMaxHp);
    }
    public void CheckUIPanelOn(bool value)
    {
       panel.SetActive(value);
       InGamepanel.SetActive(!value);
    }
    private void UpdateOverall(OverallSnapshot overall)
    {
        totalHP.text = $"{Mathf.RoundToInt(overall.totalHp)} / {Mathf.RoundToInt(overall.totalMaxHp)}";

        UpdateOverallHPBar(overall);
    }
    private void Toggle(GameObject obj, bool value) 
    { 
        if (obj && obj.activeSelf != value) obj.SetActive(value); 
    }
    private void SetInGameTotalHPColorSmooth(Slider bar, float ratio)
    {
        if (!bar) return;

        var fill = bar.fillRect ? bar.fillRect.GetComponent<Image>() : null;

        if (!fill) return;

        if (ratio <= 0f)
        {
            fill.color = zeroOrBlackoutColor;
            return;
        }

        else
        {
            fill.color = hpGradient.Evaluate(Mathf.Clamp01(ratio));
        }

    }
    private void SetBarColorSmooth(Slider bar, float ratio, bool blackout)
    {
        if (!bar) return;

        var fill = bar.fillRect ? bar.fillRect.GetComponent<Image>() : null;

        if (!fill) return;

        //블랙 아웃일 경우
        if (blackout || ratio <= 0f)
        {
            fill.color = zeroOrBlackoutColor;
            return;
        }
        else
        {
            fill.color = hpGradient.Evaluate(Mathf.Clamp01(ratio)); 
        }
    }
    private void UpdateBodyColor(PartSnapshot s)
    {
        if (!bodyMap.TryGetValue(s.part, out var refset)) return;

        if (!map.TryGetValue(s.part, out var refset2)) return;

        float ratio = s.maxHp <= 0.0f ? 0.0f : Mathf.Clamp01(s.hp / s.maxHp);
        
        if (s.blackout || ratio <= 0f)
        {
            foreach (var img in refset.images)
                if (img) img.color = zeroOrBlackoutColor;  // 완전 검정

            foreach (var img2 in refset2.images)
                if (img2) img2.color = zeroOrBlackoutColor;
            return;
        }

        // 정상 그라디언트/보간
        var target = hpGradient.Evaluate(ratio);
        foreach (var img in refset.images)
            if (img) img.color = Color.Lerp(img.color, target, 0.35f);

        foreach (var img2 in refset2.images)
            if (img2) img2.color = Color.Lerp(img2.color, target, 0.35f);
    }
}
