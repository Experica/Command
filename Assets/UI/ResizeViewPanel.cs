// --------------------------------------------------------------
// ResizeViewPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VLab
{
    public class ResizeViewPanel : Resize
    {
        public ViewPanel viewpanel;

        public override void OnEndDrag(PointerEventData eventData)
        {
            viewpanel.OnEndResize(eventData);
        }
    }
}