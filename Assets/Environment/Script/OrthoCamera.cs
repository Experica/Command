/*
OrthoCamera.cs is part of the Experica.
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
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;
using System.Collections.Generic;


namespace Experica.Environment
{
    public class OrthoCamera : NetworkBehaviour
    {
        /// <summary>
        /// Distance from screen to eye in arbitory unit
        /// </summary>
        public NetworkVariable<float> ScreenToEye = new(57f);
        /// <summary>
        /// Height of the camera viewport(i.e. height of the display if full screen), same unit as `ScreenToEye`
        /// </summary>
        public NetworkVariable<float> ScreenHeight = new(30f);
        /// <summary>
        /// Aspect ratio(width/height) of the camera viewport
        /// </summary>
        public NetworkVariable<float> ScreenAspect = new(4f/3f);
        /// <summary>
        /// Background color of the camera
        /// </summary>
        public NetworkVariable<Color> BGColor = new(Color.gray);
        /// <summary>
        /// Turn On/Off postprocessing of tonemapping
        /// </summary>
        public NetworkVariable<bool> CLUT = new(true);

        /// <summary>
        /// Height of the camera viewport in visual field angle(degree)
        /// </summary>
        public float Height
        {
            get { return camera.orthographicSize * 2f; }
        }

        /// <summary>
        /// Width of the camera viewport in visual field angle(degree)
        /// </summary>
        public float Width
        {
            get { return camera.orthographicSize * 2f * camera.aspect; }
        }

        public float NearPlane
        {
            get { return camera.nearClipPlane; }
        }

        public float FarPlane
        {
            get { return camera.farClipPlane; }
        }

        public Action OnCameraChange;
        new Camera camera;
        HDAdditionalCameraData cameraHD;

        void Awake()
        {
            tag = "MainCamera";
            camera = gameObject.GetComponent<Camera>();
            cameraHD = gameObject.GetComponent<HDAdditionalCameraData>();
            transform.localPosition = new Vector3(0f, 0f, -1001f);
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 2001f;
#if COMMAND
            // OnCameraChange += uicontroller.viewpanel.UpdateViewport;
#endif
        }

        public override void OnNetworkSpawn()
        {
            ScreenToEye.OnValueChanged += OnScreenToEye;
            ScreenHeight.OnValueChanged += OnScreenHeight;
            ScreenAspect.OnValueChanged += OnScreenAspect;
            BGColor.OnValueChanged += OnBGColor;
            CLUT.OnValueChanged += OnCLUT;
        }

        public override void OnNetworkDespawn()
        {
            ScreenToEye.OnValueChanged -= OnScreenToEye;
            ScreenHeight.OnValueChanged -= OnScreenHeight;
            ScreenAspect.OnValueChanged -= OnScreenAspect;
            BGColor.OnValueChanged -= OnBGColor;
            CLUT.OnValueChanged -= OnCLUT;
        }

        void OnScreenToEye(float p, float c)
        {
            camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(ScreenHeight.Value / 2f, c);
            OnCameraChange?.Invoke();
        }

        void OnScreenHeight(float p, float c)
        {
            camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(c / 2f, ScreenToEye.Value);
            OnCameraChange?.Invoke();
        }

        void OnScreenAspect(float p, float c)
        {
            camera.aspect = c;
            OnCameraChange?.Invoke();
        }

        void OnBGColor(Color p, Color c)
        {
            cameraHD.backgroundColorHDR = c;
        }

        void OnCLUT(bool p, bool c)
        {
            // if (uicontroller.postprocessing.profile.TryGet(out Tonemapping tonemapping))
            // {
            //     tonemapping.active = c;
            // }
        }


    }
}