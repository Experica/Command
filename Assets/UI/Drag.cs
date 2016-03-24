using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Drag : MonoBehaviour,IPointerDownHandler,IDragHandler {

    private RectTransform dragbleRectTransform;
    private RectTransform dragbleparentRectTransform;
    private Vector2 originalLocalPointerPosition;
    private Vector3 originalDragbleLocalPosition;


    void Awake()
    {
        dragbleRectTransform = transform.parent as RectTransform;
        dragbleparentRectTransform = dragbleRectTransform.parent as RectTransform;
    }

    public void OnPointerDown(PointerEventData ped)
    {
        dragbleRectTransform.SetAsLastSibling();
        originalDragbleLocalPosition = dragbleRectTransform.localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragbleparentRectTransform,
            ped.position, ped.pressEventCamera, out originalLocalPointerPosition);
    }

    public void OnDrag(PointerEventData ped)
    {
        if(dragbleRectTransform==null || dragbleparentRectTransform==null)
        {
            return;
        }
        Vector2 localPointerPosition;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(dragbleparentRectTransform,ped.position,ped.pressEventCamera,out localPointerPosition))
        {
            Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
            dragbleRectTransform.localPosition = originalDragbleLocalPosition + offsetToOriginal;
        }

        //ClampToWindow();
    }

    void ClampToWindow()
    {
        Vector3 pos = dragbleRectTransform.localPosition;

        Vector3 minPosition = dragbleparentRectTransform.rect.min - dragbleRectTransform.rect.min;
        Vector3 maxPosition = dragbleparentRectTransform.rect.max - dragbleRectTransform.rect.max;

        pos.x = Mathf.Clamp(dragbleparentRectTransform.localPosition.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(dragbleparentRectTransform.localPosition.y, minPosition.y, maxPosition.y);

        dragbleparentRectTransform.localPosition = pos;
    }
}
