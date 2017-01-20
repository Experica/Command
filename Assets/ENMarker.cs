/*
ENMarker.cs is part of the VLAB project.
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
using UnityEngine.Networking;
using System.Collections.Generic;

namespace VLab
{
    public enum OnOff
    {
        On,
        Off
    }

    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class ENMarker : NetworkBehaviour
    {
        [SyncVar(hook = "onmarkersize")]
        public float MarkerSize = 2;
        [SyncVar(hook = "onmarkerposition")]
        public Vector3 MarkerPosition = new Vector3();
        [SyncVar(hook = "onmarkercorner")]
        public Corner MarkerCorner = Corner.BottomRight;
        [SyncVar(hook = "onmark")]
        public OnOff Mark = OnOff.Off;
        [SyncVar(hook = "onmarkoncolor")]
        public Color MarkOnColor = Color.black;
        [SyncVar(hook = "onmarkoffcolor")]
        public Color MarkOffColor = Color.white;

        public new Camera camera;
        public new Renderer renderer;
#if VLAB
        VLNetManager netmanager;
#endif

        void Awake()
        {
            OnAwake();
        }
        public virtual void OnAwake()
        {
            renderer = gameObject.GetComponent<Renderer>();
#if VLAB
            netmanager = FindObjectOfType<VLNetManager>();
            netmanager.uicontroller.viewpanel.OnViewUpdated += UpdatePosition;
#endif
        }

        void Start()
        {
            OnMarkerPosition(CornerPosition(MarkerCorner));
        }

        Vector3 CornerPosition(Corner c)
        {
            var hh = camera.orthographicSize;
            var hw = hh * camera.aspect;
            var m = 0.0f;
            switch (c)
            {
                case Corner.TopLeft:
                    return new Vector3(-(hw - MarkerSize / 2.0f - m), hh - MarkerSize / 2.0f - m, transform.position.z);
                case Corner.TopRight:
                    return new Vector3(hw - MarkerSize / 2.0f - m, hh - MarkerSize / 2.0f - m, transform.position.z);
                case Corner.BottomRight:
                    return new Vector3(hw - MarkerSize / 2.0f - m, -(hh - MarkerSize / 2.0f - m), transform.position.z);
                case Corner.BottomLeft:
                    return new Vector3(-(hw - MarkerSize / 2.0f - m), -(hh - MarkerSize / 2.0f - m), transform.position.z);
                default:
                    return new Vector3(0, 0, transform.position.z);
            }
        }

        void UpdatePosition()
        {
            OnMarkerPosition(CornerPosition(MarkerCorner));
        }

        void onmarkersize(float s)
        {
            OnMarkerSize(s);
        }
        public virtual void OnMarkerSize(float s)
        {
            MarkerSize = s;
            transform.localScale = new Vector3(s, s, 1);
            OnMarkerPosition(CornerPosition(MarkerCorner));
        }

        void onmarkerposition(Vector3 p)
        {
            OnMarkerPosition(p);
        }
        public virtual void OnMarkerPosition(Vector3 p)
        {
            transform.position = p;
            MarkerPosition = p;
        }

        void onmarkercorner(Corner c)
        {
            OnMarkerCorner(c);
        }
        public virtual void OnMarkerCorner(Corner c)
        {
            OnMarkerPosition(CornerPosition(c));
            MarkerCorner = c;
        }

        void onmark(OnOff oo)
        {
            OnMark(oo);
        }
        public virtual void OnMark(OnOff oo)
        {
            if (oo == OnOff.On)
            {
                renderer.material.SetColor("col", MarkOnColor);
            }
            else
            {
                renderer.material.SetColor("col", MarkOffColor);
            }
            Mark = oo;
        }

        void onmarkoncolor(Color c)
        {
            OnMarkOnColor(c);
        }
        public virtual void OnMarkOnColor(Color c)
        {
            MarkOnColor = c;
            OnMark(Mark);
        }

        void onmarkoffcolor(Color c)
        {
            OnMarkOffColor(c);
        }
        public virtual void OnMarkOffColor(Color c)
        {
            MarkOffColor = c;
            OnMark(Mark);
        }

#if VLAB
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return netmanager.IsConnectionPeerType(conn, VLPeerType.VLabEnvironment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var vcs = netmanager.GetPeerTypeConnection(VLPeerType.VLabEnvironment);
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