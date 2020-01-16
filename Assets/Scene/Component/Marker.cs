/*
Marker.cs is part of the Experica.
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
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class Marker : NetworkBehaviour
    {
        [SyncVar(hook = "OnMarkerSize")]
        public float MarkerSize = 2;
        [SyncVar(hook = "OnMarkerPosition")]
        public Vector3 MarkerPosition = Vector3.zero;
        [SyncVar(hook = "OnMarkerCorner")]
        public Corner MarkerCorner = Corner.BottomLeft;
        [SyncVar(hook = "OnMark")]
        public bool Mark = false;
        [SyncVar(hook = "OnMarkOnColor")]
        public Color MarkOnColor = Color.white;
        [SyncVar(hook = "OnMarkOffColor")]
        public Color MarkOffColor = Color.black;

        public OrthoCamera encamera;
        public Renderer renderer;
#if COMMAND
        NetManager netmanager;
#endif

        void Awake()
        {
            renderer = gameObject.GetComponent<Renderer>();
#if COMMAND
            netmanager = FindObjectOfType<NetManager>();
#endif
            encamera = FindObjectOfType<OrthoCamera>();
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

        void OnMarkerSize(float s)
        {
            transform.localScale = new Vector3(s, s, 1);
            MarkerSize = s;
            UpdatePosition();
        }

        void OnMarkerPosition(Vector3 p)
        {
            transform.localPosition = p;
            MarkerPosition = p;
        }

        void OnMarkerCorner(Corner c)
        {
            MarkerCorner = c;
            UpdatePosition();
        }

        void OnMark(bool m)
        {
            renderer.material.SetColor("_color", m ? MarkOnColor : MarkOffColor);
            Mark = m;
        }

        void OnMarkOnColor(Color c)
        {
            MarkOnColor = c;
            OnMark(Mark);
        }

        void OnMarkOffColor(Color c)
        {
            MarkOffColor = c;
            OnMark(Mark);
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