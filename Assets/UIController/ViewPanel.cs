// --------------------------------------------------------------
// ViewPanel.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace VLab
{
    public class ViewPanel : MonoBehaviour
    {
        public VLUIController uimanager;
        public RenderTexture rendertexture;
        public GameObject viewport;

        public void UpdateView()
        {
            var vpsize = (viewport.transform as RectTransform).rect.size;
            rendertexture = new RenderTexture((int)vpsize.x, (int)vpsize.y, 24);
            uimanager.exmanager.el.envmanager.maincamera.targetTexture = rendertexture;

            var ri = viewport.GetComponent<RawImage>();
            ri.texture = rendertexture;
            ri.color = new Color(1, 1, 1, 1);
        }

    }
}