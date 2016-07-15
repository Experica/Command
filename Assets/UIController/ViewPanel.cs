// --------------------------------------------------------------
// ViewPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
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
        public VLUIController uicontroller;
        public RenderTexture rendertexture;
        public GameObject viewportcontent;
        public float aspectratio = 4.0f / 3.0f;

        public void UpdateView(PointerEventData eventData=null)
        {
            var el = uicontroller.exmanager.el;
            if (el != null)
            {
                var maincamera = el.envmanager.maincamera;
                if (maincamera != null)
                {
                    var vpcsize = (viewportcontent.transform as RectTransform).rect.size;
                    float width, height;
                    if (vpcsize.x/vpcsize.y >= aspectratio)
                    {
                        width = vpcsize.y * aspectratio;
                        height = vpcsize.y;
                    }
                    else
                    {
                        width = vpcsize.x;
                        height = vpcsize.x / aspectratio;
                    }

                    var ri = viewportcontent.GetComponentInChildren<RawImage>();
                    var rirt = ri.gameObject.transform as RectTransform;
                    rirt.sizeDelta = new Vector2(width,height);
                    ri.color = new Color(1, 1, 1, 1);
                    if(ri.texture!=null)
                    {
                        Destroy(ri.texture);
                    }
                    rendertexture = new RenderTexture((int)width, (int)height, 24);
                    rendertexture.generateMips = false;
                    rendertexture.antiAliasing =  (int)uicontroller.appmanager.config[VLCFG.AntiAliasing];
                    rendertexture.anisoLevel = (int)uicontroller.appmanager.config[VLCFG.AnisotropicFilterLevel];
                    maincamera.targetTexture = rendertexture;
                    ri.texture = rendertexture;
                }
            }
        }

        public void OnEndResize(PointerEventData eventData)
        {
            UpdateView(eventData);
        }

    }
}