// --------------------------------------------------------------
// Drag.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Drag : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform parentRectTransform;
    private RectTransform parentparentRectTransform;
    private Vector2 originalLocalPointerPosition;
    private Vector3 originalParentLocalPosition;

    void Awake()
    {
        parentRectTransform = transform.parent as RectTransform;
        parentparentRectTransform = parentRectTransform.parent as RectTransform;
    }

    public void OnPointerDown(PointerEventData ped)
    {
        parentRectTransform.SetAsLastSibling();
        originalParentLocalPosition = parentRectTransform.localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentparentRectTransform, ped.position, ped.pressEventCamera, out originalLocalPointerPosition);
    }

    public void OnDrag(PointerEventData ped)
    {
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentparentRectTransform, ped.position, ped.pressEventCamera, out localPointerPosition);
        Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

        parentRectTransform.localPosition = originalParentLocalPosition + offsetToOriginal;
    }
}
