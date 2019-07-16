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
using UnityEngine.Networking;
using System.Collections.Generic;
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class Camera : NetworkBehaviour
    {
        [SyncVar(hook = "onbgcolor")]
        public Color BGColor = Color.gray;
        [SyncVar(hook = "onscreenhalfheight")]
        public float ScreenHalfHeight = 15;
        [SyncVar(hook = "onscreentoeye")]
        public float ScreenToEye = 57;
        [SyncVar(hook = "onscreenaspect")]
        public float ScreenAspect = 4.0f / 3.0f;

        public Action CameraChange;
        public UnityEngine.Camera camera;
#if COMMAND
        NetManager netmanager;
#endif
        void Awake()
        {
            OnAwake();
        }
        public virtual void OnAwake()
        {
            camera = gameObject.GetComponent<UnityEngine.Camera>();
#if COMMAND
            netmanager = FindObjectOfType<NetManager>();
#endif
        }

        void Start()
        {
            OnStart();
        }
        public virtual void OnStart()
        {
#if COMMAND
            CameraChange += netmanager.uicontroller.viewpanel.UpdateViewport;
#endif
        }

        void onbgcolor(Color c)
        {
            OnBGColor(c);
        }
        public virtual void OnBGColor(Color c)
        {
            if (camera != null)
            {
                camera.backgroundColor = c;
            }
            BGColor = c;
        }

        void onscreenhalfheight(float shh)
        {
            OnScreenHalfHeight(shh);
        }
        public virtual void OnScreenHalfHeight(float shh)
        {
            if (camera != null)
            {
                camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(shh, ScreenToEye);
            }
            ScreenHalfHeight = shh;
            CameraChange?.Invoke();
        }

        void onscreentoeye(float ste)
        {
            OnScreenToEye(ste);
        }
        public virtual void OnScreenToEye(float ste)
        {
            if (camera != null)
            {
                camera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(ScreenHalfHeight, ste);
            }
            ScreenToEye = ste;
            CameraChange?.Invoke();
        }

        void onscreenaspect(float apr)
        {
            OnScreenAspect(apr);
        }
        public virtual void OnScreenAspect(float apr)
        {
            if (camera != null)
            {
                camera.aspect = apr;
            }
            ScreenAspect = apr;
            CameraChange?.Invoke();
        }

#if COMMAND
        void OnPostRender()
        {
            netmanager.uicontroller.exmanager.el.framerecorder.Grab();
        }

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