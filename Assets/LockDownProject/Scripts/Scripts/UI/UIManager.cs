using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public interface IUIStateProvider
{
    void CheckUIPanelOn(bool value);
    void UseItem(int index, BodyParts? target = null);
    void CheckLeftAmmo();
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

[System.Serializable]
public struct HotbarSlotInit
{
    public ConsumableItems def;   
    public int startRemaining;
}
[System.Serializable]
public struct HotbarSlotViewForItem
{
    public Image icon;
    public TextMeshProUGUI keyText;
    public TextMeshProUGUI remainingText;
}
[System.Serializable]
public struct HotbarSlotViewForWeapon
{
    public Image icon;
    public TextMeshProUGUI keyText;
}
public class UIManager : MonoBehaviour, IUIStateProvider
{
   
    [Header("Hotbar Init")]
    public HotbarSlotInit slot1Init, slot2Init, slot3Init, slot4Init;

    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI totalHP;
    [SerializeField] private List<PartRowRefs> rows;
    public PlayerHealthManager healthManager;
    private PlayerInputController inputController;
    private MovementSettings movementSettings;
    private Player player;
    private Weapon weapon;
    private HealthSound healthSound;

    private IPlayerMoveInfoProvider playerMoveInfoProvider;
    private IGetActiveWeaponProvider activeWeaponProvider;

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
    [SerializeField] private GameObject inGamepanel;


    [SerializeField] private List<BodyImageRef> bodyImages = new();
    private Dictionary<BodyParts, BodyImageRef> bodyMap;

    [Header("Item UI")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private GameObject useUIRoot;
    [SerializeField] private Image radial;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemRemaining;
    [SerializeField] private ConsumableItemManager slot4;
    [SerializeField] private ConsumableItemManager slot5;
    [SerializeField] private ConsumableItemManager slot6;
    [SerializeField] private ConsumableItemManager slot7;
    private bool isUsing;
    private float lastUseStartTime;

    [Header("Hotbar HUD")]
    [SerializeField] private HotbarSlotViewForWeapon[] hotbarViewsForWeapon = new HotbarSlotViewForWeapon[3];
    [SerializeField] private HotbarSlotViewForItem[] hotbarViewsForItem = new HotbarSlotViewForItem[7];
    [SerializeField] private Sprite emptyIcon;

    [Header("LeftAmmo UI")]
    [SerializeField] private GameObject checkAmmoPanel;
    [SerializeField] private TextMeshProUGUI leftAmmo;
    [SerializeField] private TextMeshProUGUI ammoType;

    [Header("Time")]
    [SerializeField] float tickInterval = 5.0f;
    float nextTick;
    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("Dead")]
    [SerializeField] private GameObject deadPanel;

    public bool IsHealthPanelOpen => panel && panel.activeSelf;

    private void Awake()
    {

        healthManager = GetComponent<PlayerHealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[UIManager] healthManager is NULL");
        }
        inputController = GetComponent<PlayerInputController>();
        if (inputController == null)
        {
            Debug.LogWarning("[UIManager] inputController is NULL");
        }
        movementSettings = GetComponent<MovementSettings>();
        if (movementSettings == null)
        {
            Debug.LogWarning("[UIManager]  movementSettings is NULL");
        }

        player = GetComponentInChildren<Player>();
        if (player == null)
        {
            Debug.LogWarning("[UIManager]player is NULL");
        }

        activeWeaponProvider = player as IGetActiveWeaponProvider;
        if (activeWeaponProvider == null)
        {
            Debug.LogWarning("[UIManager] activeWeaponProvider is NULL");
        }

        playerMoveInfoProvider = movementSettings as IPlayerMoveInfoProvider;
        if (playerMoveInfoProvider == null)
        {
            Debug.LogWarning("[UIManager] playerMoveInfoProvider is NULL");
        }
        healthSound = GetComponent<HealthSound>();
        if (healthSound == null)
        {
            Debug.LogWarning("[UIManager] healthSound is NULL");
        }
        
        map = new Dictionary<BodyParts, PartRowRefs>();
        foreach (var r in rows) map[r.part] = r;

        bodyMap = new();
        foreach (var b in bodyImages) bodyMap[b.part] = b;

        InitializeItemsSlot();
        InitializeUI();

        // Debug.Log($"[Hotbar] slot1 after init  remaining={slot1?.remaining}  so={slot1Init.def?.remaining}  startField={slot1Init.startRemaining}");


    }
    private void OnEnable()
    {
        if (healthManager == null || player == null) return;

        healthManager.OnBatchChanged += UpdateBatch;
        healthManager.OnOverallChanged += UpdateOverall;
        player.OnWeaponChanged += UpdateHotbarForWeapon;
    }
    private void OnDisable()
    {
        if (healthManager == null || player == null) return;

        healthManager.OnBatchChanged -= UpdateBatch;
        healthManager.OnOverallChanged -= UpdateOverall;
        player.OnWeaponChanged -= UpdateHotbarForWeapon;
    }
    private void Start()
    {

        RefreshAll();
        UpdateHotbarForItem();
        UpdateHotbarForWeapon();
   
    }

    private void Update()
    {
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + tickInterval;
            checkAmmoPanel.SetActive(false);
        }

        if (healthManager == null)
        {
            deadPanel.SetActive(false);
            return;
        }

        if (healthManager.CheckIsDead())
        {
            deadPanel.SetActive(true);
        }

        else deadPanel.SetActive(false);
    }
    private void InitializeItemsSlot()
    {
        slot4 = InitializeItems(slot1Init);
        slot5 = InitializeItems(slot2Init);
        slot6 = InitializeItems(slot3Init);
        slot7 = InitializeItems(slot4Init);
    }
   
    private ConsumableItemManager InitializeItems(HotbarSlotInit init)
    {
        if (!init.def) return null;

        int charges = (init.startRemaining > 0)
       ? init.startRemaining
       : Mathf.Max(0, init.def.remaining);

        var result = new ConsumableItemManager(init.def, charges);
        //Debug.Log($"[Hotbar] make {init.def.name}  charges={charges}  so={init.def.remaining}  startField={init.startRemaining}");
        return result;
    }
    private void InitializeUI()
    {
        panel.SetActive(false);
        inGamepanel.SetActive(true);
        inGameIconLight.SetActive(false);
        inGameIconHeavy.SetActive(false);
        inGameIconFracture.SetActive(false);
        inGameIconBlackout.SetActive(false);
        itemPanel.SetActive(false);
        checkAmmoPanel.SetActive(false);
        deadPanel.SetActive(false);

    }
 
    public void RefreshAll()
    {
        if (healthManager == null) return;

        //? ???? ??
        foreach (var r in rows) UpdateRow(healthManager.GetSnapshot(r.part));
        // ?? ??? ??
        UpdateOverall(healthManager.GetOverallSnapshot());
    }

    private void UpdateBatch(IReadOnlyList<PartSnapshot> snaps)
    {
        for (int i = 0; i < snaps.Count; i++) UpdateRow(snaps[i]);
        /*
        bool anyLight = false;
        bool anyHeavy = false;
        bool anyFrac = false;
        bool anyBlackout = false;

        for (int i = 0; i < snaps.Count; i++)
        {
            anyLight |= snaps[i].light;
            anyHeavy |= snaps[i].heavy;
            anyFrac |= snaps[i].fracture;
            anyBlackout |= snaps[i].blackout;
        }
        
        UpdateInGameEffectIcons(anyLight, anyHeavy, anyFrac, anyBlackout);
        */
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
        // Debug.Log($"hp = {snap.hp}, maxhp ={snap.maxHp}, ratio = {snap.hp / snap.maxHp}");
        UpdateBodyColor(snap);


    }
    private void UpdateInGameEffectIcons(bool anyLight, bool anyHeavy, bool anyFrac, bool anyBlack)
    {

        Toggle(inGameIconLight, anyLight);
        Toggle(inGameIconHeavy, anyHeavy);
        Toggle(inGameIconFracture, anyFrac);
        Toggle(inGameIconBlackout, anyBlack);

    }
    private void UpdateInGameEffectIcon(OverallSnapshot overall)
    {

        Toggle(inGameIconLight, overall.anyLight);
        Toggle(inGameIconHeavy, overall.anyHeavy);
        Toggle(inGameIconFracture, overall.anyFracture);
        Toggle(inGameIconBlackout, overall.anyBlackout);

    }
    private void UpdateOverallHPBar(OverallSnapshot overall)
    {
        inGameTotalHPBar.maxValue = 0.0f;
        inGameTotalHPBar.maxValue = overall.totalMaxHp;
        inGameTotalHPBar.value = Mathf.Clamp(overall.totalHp, 0.0f, overall.totalMaxHp);

        SetInGameTotalHPColorSmooth(inGameTotalHPBar, overall.totalHp <= 0.0f ? 0.0f : overall.totalHp / overall.totalMaxHp);
    }

    public void UpdateHotbarForItem()
    {
        if (hotbarViewsForItem == null || hotbarViewsForItem.Length == 0) return;

        for (int i = 0; i < hotbarViewsForItem.Length; i++)
        {
            var view = hotbarViewsForItem[i];

            int slotIndex = i + 4;
            var item = GetSlot(slotIndex);

            if (item == null || item.def == null)
            {
                if (view.icon) view.icon.sprite = emptyIcon;
                if (view.remainingText) view.remainingText.text = "";
                continue;
            }

            if (view.icon)
                view.icon.sprite = item.def.icon ? item.def.icon : emptyIcon;

            if (view.remainingText)
                view.remainingText.text = item.remaining > 0 ? $"{item.remaining}" : "0";


        }
    }

    private void UpdateHotbarForWeapon()
    {
        
        if (hotbarViewsForWeapon == null || hotbarViewsForWeapon.Length ==0) return;
     
        for (int i = 0; i < hotbarViewsForWeapon.Length; i++)
        {

            var view = hotbarViewsForWeapon[i];
            var gun = activeWeaponProvider.GetWeaponList(i);

            if (gun == null)
            {
                if (view.icon) view.icon.sprite = emptyIcon;
                continue;
            }
            if (view.icon)
                view.icon.sprite = gun.icon_gun ? gun.icon_gun : emptyIcon;


        }
    }
    public void CheckLeftAmmo()
    {
        checkAmmoPanel.SetActive(true);
  
        int curAmmo = activeWeaponProvider.GetActiveWeapon().GetActiveAmmo();
        int maxAmmo = activeWeaponProvider.GetActiveWeapon().GetMaxAmmo();

        float ammoRation01 = Mathf.Clamp01(((float)curAmmo / (float)maxAmmo));

        if (ammoRation01 == 1.0f) leftAmmo.text = "Full";

        else if (ammoRation01 >= 0.8f && ammoRation01 < 1.0f) leftAmmo.text = "Almost full";

        else if (ammoRation01 >= 0.6f && ammoRation01 < 0.8f) leftAmmo.text = "More than half";

        else if (ammoRation01 >= 0.4f && ammoRation01 < 0.6f) leftAmmo.text = "About half";

        else if (ammoRation01 >= 0.2f && ammoRation01 < 0.4f) leftAmmo.text = "Less than half";

        else if (ammoRation01 > 0.0f && ammoRation01 < 0.2f) leftAmmo.text = "Almost Empty";

        if (ammoRation01 == 0.0f) leftAmmo.text = "Empty";

        ammoType.text= activeWeaponProvider.GetActiveWeapon().GetAmmoName();
    }
   
    public void CheckUIPanelOn(bool value)
    {
       panel.SetActive(value);
       tutorialPanel.SetActive(!value);
       inGamepanel.SetActive(!value);
    }
   
    private void UpdateOverall(OverallSnapshot overall)
    {
        totalHP.text = $"{Mathf.RoundToInt(overall.totalHp)} / {Mathf.RoundToInt(overall.totalMaxHp)}";

        UpdateOverallHPBar(overall);
        UpdateInGameEffectIcon(overall);
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
        if (blackout || ratio <= 0.0f)
        {
            fill.color = zeroOrBlackoutColor;
            return;
        }
        else
        {
            fill.color = hpGradient.Evaluate(Mathf.Clamp01(ratio)); 
        }
    }
    private void UpdateBodyColor(PartSnapshot snap)
    {
        if (!bodyMap.TryGetValue(snap.part, out var refset)) return;

        if (!map.TryGetValue(snap.part, out var refset2)) return;

        float ratio = snap.maxHp <= 0.0f ? 0.0f : Mathf.Clamp01(snap.hp / snap.maxHp);
        
        if (snap.blackout || ratio <= 0f)
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

    public void UseItem(int index, BodyParts? target = null)
    {
        if (isUsing) return;
        var item = GetSlot(index); 

        if (item == null || item.remaining <= 0) { Debug.Log("아이템 없음/충전 0"); return; }
        //적용할 대상 없으면 return
        if (!item.CanApplyAll(healthManager, target)) return;
       
        StartCoroutine(CoUseItem(item, target));
    
    }
    public ConsumableItemManager GetSlot(int idx)
    {
        switch (idx)
        {
            case 4: return slot4;
            case 5: return slot5;
            case 6: return slot6;
            case 7: return slot7;
            default: return null;
        }
    }

    private IEnumerator CoUseItem(ConsumableItemManager item, BodyParts? target)
    {
        isUsing = true;
        lastUseStartTime = Time.time;

        float dur = Mathf.Max(0.05f, item.def.useTime);

        // UI 세팅
        if (itemPanel) itemPanel.SetActive(true);
        if (useUIRoot) useUIRoot.SetActive(true);
        if (itemName) itemName.text = item.def.displayName;
        if (itemRemaining) itemRemaining.text = item.remaining.ToString();
        if (radial)
        {
            radial.type = Image.Type.Filled;
            radial.fillMethod = Image.FillMethod.Radial360;
            radial.fillOrigin = (int)Image.Origin360.Top;  // Origin 설정
            radial.fillClockwise = true;
            radial.fillAmount = 0.0f;
        }

       
        float time = 0.0f;
        while (time < dur)
        {
            if (WasInterrupted()) { CancelUseUI(); isUsing = false; yield break; } // 취소
            time += Time.deltaTime;
            if (radial) radial.fillAmount = Mathf.Clamp01(time / dur);
            yield return null;
        }
        if (radial) radial.fillAmount = 1.0f;
        // 완료 시 효과 '한 번' 적용
        bool ok = item.ApplyAll(healthManager, target);

        // UI 마무리
        FinishUseUI(ok);

        // 헬스 패널 새로고침
        RefreshAll();
        UpdateHotbarForItem();
        //Debug.Log($"[{item.def.displayName}] remain={item.remaining}");
        isUsing = false;
    }
    public void UseItemOnTarget(ConsumableItemManager item, BodyParts target)
    {
        if (isUsing) return;
        if (item == null || item.remaining <= 0) return;

        StartCoroutine(CoUseItem(item, target));
    }
    private void CancelUseUI()
    {
        if (radial) radial.fillAmount = 0.0f;
        if (itemPanel) itemPanel.SetActive(false);
        if (useUIRoot) useUIRoot.SetActive(false);

        //Debug.Log("사용 취소");
    }

    private void FinishUseUI(bool ok)
    {
        if (itemPanel) itemPanel.SetActive(false);
        if (useUIRoot) useUIRoot.SetActive(false);
       // if (!ok) Debug.Log("적용할 상태 없음");
    }
    private bool WasInterrupted()
    {
        if (inputController.Fire || inputController.Aim || inputController.Reload || inputController.Jump) return true;

        if (playerMoveInfoProvider.GetDesiredGait() >= 2.0f) return true;

        return false;
    }

}


