/*
ViewPanel.cs is part of the Experica.
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
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Experica.Command
{
    public class ViewPanel : MonoBehaviour
    {
        public UIController uicontroller;
        RenderTexture rendertexture;
        public GameObject viewportcontent;
        public Toggle togglegrid;
        public Grid grid;
        public InputField gridcenterinput;
        public Action OnViewUpdated;
        RenderTextureDescriptor RenderTextureDescriptor;

        void Start()
        {
            rendertexture = new RenderTexture(new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex2D,
                depthBufferBits = 32,
                autoGenerateMips = false,
                msaaSamples = uicontroller.config.AntiAliasing,
                colorFormat = RenderTextureFormat.ARGBHalf,
                sRGB = false,
                width = 1,
                height = 1,
                volumeDepth = 1
            });
            rendertexture.anisoLevel = uicontroller.config.AnisotropicFilterLevel;

            SetGridCenter(new Vector3(0, 0, 50));
        }

        void OnApplicationQuit()
        {
            rendertexture.Release();
        }

        void SetGridCenter(Vector3 c, bool notifyui = true)
        {
            grid.Center = c;
            if (notifyui)
            {
                gridcenterinput.text = c.Convert<string>();
            }
        }

        void UpdateGridSize(bool isupdatetick = true)
        {
            var maincamera = uicontroller.exmanager.el.envmanager.maincamera_scene;
            grid.Size = new Vector3
                    (maincamera.aspect * maincamera.orthographicSize + Mathf.Abs(grid.Center.x),
                    maincamera.orthographicSize + Mathf.Abs(grid.Center.y), 1);
            if (isupdatetick)
            {
                grid.UpdateTick(grid.TickInterval);
                grid.TickSize = grid.Size;
            }
        }

        void UpdateGridLineWidth()
        {
            var maincamera = uicontroller.exmanager.el.envmanager.maincamera_scene;
            grid.UpdateAxisLineWidth(maincamera.orthographicSize);
            grid.UpdateTickLineWidth(maincamera.orthographicSize);
        }

        public void UpdateViewport()
        {
            if (uicontroller.IsFullViewport) { return; }
            var envmanager = uicontroller.exmanager.el.envmanager;
            var maincamera = envmanager.maincamera_scene;
            if (maincamera != null)
            {
                // Get Render Size
                var vpcsize = (viewportcontent.transform as RectTransform).rect.size;
                float width, height;
                if (vpcsize.x / vpcsize.y >= maincamera.aspect)
                {
                    width = vpcsize.y * maincamera.aspect;
                    height = vpcsize.y;
                }
                else
                {
                    width = vpcsize.x;
                    height = vpcsize.x / maincamera.aspect;
                }
                // Set Render Size and Target
                var ri = viewportcontent.GetComponentInChildren<RawImage>();
                var rirt = ri.gameObject.transform as RectTransform;
                rirt.sizeDelta = new Vector2(width, height);

                rendertexture.Release();
                rendertexture.width = Math.Max(1, Mathf.FloorToInt(width));
                rendertexture.height = Math.Max(1, Mathf.FloorToInt(height));

                maincamera.targetTexture = rendertexture;
                ri.texture = rendertexture;

                UpdateGridSize();
                UpdateGridLineWidth();
            }
        }

        public void OnEndResize(PointerEventData eventData)
        {
            UpdateViewport();
        }

        public void OnToggleGrid(bool ison)
        {
            grid.gameObject.SetActive(ison);
        }

        public void OnGridCenter(string p)
        {
            grid.Center = p.Convert<Vector3>();
            UpdateGridSize();
        }

    }
}