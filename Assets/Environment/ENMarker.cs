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
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class ENMarker : NetworkBehaviour
    {
        [SyncVar(hook = "onmarkersize")]
        public float MarkerSize = 2;
        [SyncVar(hook = "onmarkerposition")]
        public Vector3 MarkerPosition = Vector3.zero;
        [SyncVar(hook = "onmarkercorner")]
        public Corner MarkerCorner = Corner.BottomRight;
        [SyncVar(hook = "onmark")]
        public bool Mark = false;
        [SyncVar(hook = "onmarkoncolor")]
        public Color MarkOnColor = Color.white;
        [SyncVar(hook = "onmarkoffcolor")]
        public Color MarkOffColor = Color.black;

        public ENCamera encamera;
        public Renderer renderer;
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
#endif
            encamera = FindObjectOfType<ENCamera>();
            encamera.CameraChange += UpdatePosition;
            UpdatePosition();
        }

        Vector3 CornerPosition(Corner c, float m = 0)
        {
            var hh = encamera.camera.orthographicSize;
            var hw = hh * encamera.camera.aspect;
            switch (c)
            {
                case Corner.TopLeft:
                    return new Vector3(-(hw - MarkerSize / 2.0f - m), hh - MarkerSize / 2.0f - m, transform.localPosition.z);
                case Corner.TopRight:
                    return new Vector3(hw - MarkerSize / 2.0f - m, hh - MarkerSize / 2.0f - m, transform.localPosition.z);
                case Corner.BottomRight:
                    return new Vector3(hw - MarkerSize / 2.0f - m, -(hh - MarkerSize / 2.0f - m), transform.localPosition.z);
                case Corner.BottomLeft:
                    return new Vector3(-(hw - MarkerSize / 2.0f - m), -(hh - MarkerSize / 2.0f - m), transform.localPosition.z);
                default:
                    return new Vector3(0, 0, transform.localPosition.z);
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
            transform.localScale = new Vector3(s, s, 1);
            MarkerSize = s;
            UpdatePosition();
        }

        void onmarkerposition(Vector3 p)
        {
            OnMarkerPosition(p);
        }
        public virtual void OnMarkerPosition(Vector3 p)
        {
            transform.localPosition = p;
            MarkerPosition = p;
        }

        void onmarkercorner(Corner c)
        {
            OnMarkerCorner(c);
        }
        public virtual void OnMarkerCorner(Corner c)
        {
            MarkerCorner = c;
            UpdatePosition();
        }

        void onmark(bool m)
        {
            OnMark(m);
        }
        public virtual void OnMark(bool m)
        {
            if (m)
            {
                renderer.material.SetColor("col", MarkOnColor);
            }
            else
            {
                renderer.material.SetColor("col", MarkOffColor);
            }
            Mark = m;
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