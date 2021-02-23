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
        /// <summary>
        /// Marker Quad size in visual field degree
        /// </summary>
        [SyncVar(hook = "onmarkersize")]
        public float MarkerSize = 2;
        [SyncVar(hook = "onmarkerposition")]
        public Vector3 MarkerPosition = Vector3.zero;
        [SyncVar(hook = "onmarkercorner")]
        public Corner MarkerCorner = Corner.BottomLeft;
        /// <summary>
        /// Mark On/Off
        /// </summary>
        [SyncVar(hook = "onmark")]
        public bool Mark = false;
        [SyncVar(hook = "onmarkoncolor")]
        public Color MarkOnColor = Color.white;
        [SyncVar(hook = "onmarkoffcolor")]
        public Color MarkOffColor = Color.black;

        // can not been found in Awake(), so statically set the reference in a scene
        public OrthoCamera orthocamera;
        Renderer renderer;
#if COMMAND
        NetManager netmanager;
#endif

        void Awake()
        {
#if COMMAND
            netmanager = FindObjectOfType<NetManager>();
#endif
            renderer = gameObject.GetComponent<Renderer>();
            orthocamera.OnCameraChange += UpdatePosition;
        }

        Vector3 getmarkerposition(Corner corner, float size, float margin = 0)
        {
            var h = orthocamera.Height;
            var w = orthocamera.Width;
            var z = orthocamera.NearPlane;
            switch (corner)
            {
                case Corner.TopLeft:
                    return new Vector3((-w + size) / 2 + margin, (h - size) / 2 - margin, z);
                case Corner.TopRight:
                    return new Vector3((w - size) / 2 - margin, (h - size) / 2 - margin, z);
                case Corner.BottomLeft:
                    return new Vector3((-w + size) / 2 + margin, (-h + size) / 2 + margin, z);
                case Corner.BottomRight:
                    return new Vector3((w - size) / 2 - margin, (-h + size) / 2 + margin, z);
                default:
                    return new Vector3(0, 0, z);
            }
        }

        void UpdatePosition()
        {
            onmarkerposition(getmarkerposition(MarkerCorner, MarkerSize));
        }

        void onmarkersize(float s)
        {
            transform.localScale = new Vector3(s, s, 1);
            MarkerSize = s;
            UpdatePosition();
        }

        void onmarkerposition(Vector3 p)
        {
            transform.localPosition = p;
            MarkerPosition = p;
        }

        void onmarkercorner(Corner c)
        {
            MarkerCorner = c;
            UpdatePosition();
        }

        void onmark(bool m)
        {
            renderer.material.SetColor("_color", m ? MarkOnColor : MarkOffColor);
            Mark = m;
        }

        void onmarkoncolor(Color c)
        {
            MarkOnColor = c;
            onmark(Mark);
        }

        void onmarkoffcolor(Color c)
        {
            MarkOffColor = c;
            onmark(Mark);
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