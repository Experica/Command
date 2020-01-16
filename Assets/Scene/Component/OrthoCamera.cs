/*
Camera.cs is part of the Experica.
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
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Networking;
using System.Collections.Generic;
#if COMMAND
using Experica.Command;
#endif
#if ENVIRONMENT
using Experica.Environment;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class OrthoCamera : NetworkBehaviour
    {
        [SyncVar(hook = "onscreenhalfheight")]
        public float ScreenHalfHeight = 15;
        [SyncVar(hook = "onscreentoeye")]
        public float ScreenToEye = 57;
        [SyncVar(hook = "onscreenaspect")]
        public float ScreenAspect = 4.0f / 3.0f;
        [SyncVar(hook = "onbgcolor")]
        public Color BGColor = Color.gray;
        [SyncVar(hook = "onclut")]
        public bool CLUT = true;

        public Action CameraChange;

        public Camera camera;
        NetManager netmanager;

        void Awake()
        {
            camera = gameObject.GetComponent<Camera>();
            netmanager = FindObjectOfType<NetManager>();
        }

        void Start()
        {
#if COMMAND
            CameraChange += netmanager.uicontroller.viewpanel.UpdateViewport;
#endif
        }

        void onscreenhalfheight(float shh)
        {
            camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(shh, ScreenToEye);
            ScreenHalfHeight = shh;
            CameraChange?.Invoke();
        }

        void onscreentoeye(float ste)
        {
            camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(ScreenHalfHeight, ste);
            ScreenToEye = ste;
            CameraChange?.Invoke();
        }

        void onscreenaspect(float apr)
        {
            camera.aspect = apr;
            ScreenAspect = apr;
            CameraChange?.Invoke();
        }

        void onbgcolor(Color bc)
        {
            camera.backgroundColor = bc;
            BGColor = bc;
        }

        void onclut(bool cluton)
        {
            netmanager.uicontroller.ToggleColorGrading(cluton);
#if COMMAND
            int width, height;
            var data = netmanager.uicontroller.SerializeCLUT(out width, out height);
            if (data != null)
            {
                RpcCLUT(data, width, height);
            }
#endif
            CLUT = cluton;
        }

        [ClientRpc]
        void RpcCLUT(byte[] clut, int width, int height)
        {
#if ENVIRONMENT
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false,true);
            tex.LoadRawTextureData(clut);
            tex.Apply();
            netmanager.uicontroller.SetCLUT(tex);
#endif
        }

#if COMMAND
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return netmanager.IsConnectionPeerType(conn, PeerType.Environment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var vcs = netmanager.GetPeerTypeConnection(PeerType.Environment);
            if (vcs.Count > 0)
            {
                foreach (var c in vcs)
                {
                    observers.Add(c);
                }
                return true;
            }
            return false;
        }
#endif

    }
}