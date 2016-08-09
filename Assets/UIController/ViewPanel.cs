/*
ViewPanel.cs is part of the VLAB project.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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

        float aspectratio = 4.0f / 3.0f;
        public float AspectRatio
        {
            get { return aspectratio; }
            set
            {
                if(aspectratio!=value)
                {
                    aspectratio = value;
                    UpdateView();
                }
            }
        }

        public void UpdateView(PointerEventData eventData = null)
        {
            var maincamera = uicontroller.exmanager.el.envmanager.maincamera;
            if (maincamera != null)
            {
                // Get Render Size
                var vpcsize = (viewportcontent.transform as RectTransform).rect.size;
                float width, height;
                if (vpcsize.x / vpcsize.y >= aspectratio)
                {
                    width = vpcsize.y * aspectratio;
                    height = vpcsize.y;
                }
                else
                {
                    width = vpcsize.x;
                    height = vpcsize.x / aspectratio;
                }
                // Set Render Size and Target
                var ri = viewportcontent.GetComponentInChildren<RawImage>();
                var rirt = ri.gameObject.transform as RectTransform;
                rirt.sizeDelta = new Vector2(width, height);
                if (ri.texture != null)
                {
                    Destroy(ri.texture);
                }
                rendertexture = new RenderTexture((int)width, (int)height, 24);
                rendertexture.generateMips = false;
                rendertexture.antiAliasing = (int)uicontroller.appmanager.config[VLCFG.AntiAliasing];
                rendertexture.anisoLevel = (int)uicontroller.appmanager.config[VLCFG.AnisotropicFilterLevel];
                maincamera.targetTexture = rendertexture;
                ri.texture = rendertexture;
            }
        }

        public void OnEndResize(PointerEventData eventData)
        {
            UpdateView(eventData);
        }

    }
}