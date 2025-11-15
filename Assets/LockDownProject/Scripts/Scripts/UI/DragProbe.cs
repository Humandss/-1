using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragProbe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e) { Debug.Log("BEGIN " + name); }
    public void OnDrag(UnityEngine.EventSystems.PointerEventData e) { }
    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData e) { Debug.Log("END " + name); }
}
