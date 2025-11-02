using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        panel.SetActive(false);
        InGamepanel.SetActive(true);   

        map = new Dictionary<BodyParts, PartRowRefs>();
        foreach (var r in rows) map[r.part] = r;

        bodyMap = new();
        foreach (var b in bodyImages) bodyMap[b.part] = b;
       
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
    }

    private void UpdateRow(PartSnapshot s)
    {
        if (!map.TryGetValue(s.part, out var row)) return;

        if (row.bar)
        {
            row.bar.minValue = 0.0f;
            row.bar.maxValue = s.maxHp;
            row.bar.value = Mathf.Clamp(s.hp, 0f, s.maxHp);
        }

        if (row.valueText) row.valueText.text = $"{Mathf.RoundToInt(s.hp)} / {Mathf.RoundToInt(s.maxHp)}";

        Toggle(row.iconLight, s.light);
        Toggle(row.iconHeavy, s.heavy);
        Toggle(row.iconFracture, s.fracture);
        Toggle(row.iconBlackout, s.blackout);

        SetBarColorSmooth(row.bar, s.maxHp <= 0.0f ? 0.0f : s.hp / s.maxHp, s.blackout);
        UpdateBodyColor(s);
    }
    public void CheckUIPanelOn(bool value)
    {
       panel.SetActive(value);
       InGamepanel.SetActive(!value);
    }
    private void UpdateOverall(OverallSnapshot overall)
    {
        totalHP.text = $"{Mathf.RoundToInt(overall.totalHp)} / {Mathf.RoundToInt(overall.totalMaxHp)}";
    }
    private void Toggle(GameObject go, bool value) 
    { 
        if (go && go.activeSelf != value) go.SetActive(value); 
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
