using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEditor.Progress;
using static UnityEngine.GraphicsBuffer;

public class BodyPartsDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private BodyParts part;
    [SerializeField] private UIManager uiManager;
 
    public void OnDrop(PointerEventData e)
    {
        if (!uiManager || !uiManager.IsHealthPanelOpen) return;

        var src = e.pointerDrag ? e.pointerDrag.GetComponent<HotbarDragController>() : null;
        if (src == null || src.item == null) return;

        bool canApply = src.item.CanApplyAll(uiManager.healthManager, part);
   
        if (canApply)
        {
            uiManager.UseItemOnTarget(src.item, part);
            uiManager.RefreshAll();      // 체력 상태 갱신
            uiManager.UpdateHotbarForItem(); // 핫바 갱신
        }

    }
}
