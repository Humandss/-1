using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.Progress;


public class HotbarDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public ConsumableItemManager item;
    [SerializeField] private Image icon;
    [SerializeField] private Image dragGhost;
    [SerializeField] private UIManager uiManager;

    [Header("Slot")]
    public int slotIndex = 4;

    private void Start()
    {
        if (uiManager) item = uiManager.GetSlot(slotIndex);
        else Debug.LogWarning("[HotbarDragController] uiManager is NULL");
    }
    public void OnBeginDrag(PointerEventData e)
    {
        Debug.Log("[Drag] Begin");

        Debug.Log($"ui? {uiManager != null}, panelOpen? {uiManager?.IsHealthPanelOpen}");
        Debug.Log($"item? {item != null}, remaining={(item != null ? item.remaining : -1)}");
        Debug.Log($"ghost? {dragGhost != null}");

        if (!uiManager || !uiManager.IsHealthPanelOpen) return;
        if (item == null || item.remaining <= 0) return;

        var s = (item.def && item.def.icon) ? item.def.icon : (icon ? icon.sprite : null);
        if (s == null) { Debug.LogWarning("sprite is NULL"); return; }

        dragGhost.sprite = s;
        dragGhost.color = Color.white;
        dragGhost.raycastTarget = false;
        dragGhost.gameObject.SetActive(true);
 
        OnDrag(e);

    }
    public void OnDrag(PointerEventData e)
    {
        if (!dragGhost || !dragGhost.gameObject.activeSelf) return;
        RectTransform canvasRt = dragGhost.canvas.transform as RectTransform;
        RectTransform ghostRt = dragGhost.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, e.position, e.pressEventCamera, out var local))
            ghostRt.anchoredPosition = local;
    }
    public void OnEndDrag(PointerEventData e)
    {
        Debug.Log("[Drag] End");
        if (dragGhost) dragGhost.gameObject.SetActive(false);
  
    }
   
}
