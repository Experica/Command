// --------------------------------------------------------------
// Focus.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Focus : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData ped)
    {
        transform.SetAsLastSibling();
    }
}
