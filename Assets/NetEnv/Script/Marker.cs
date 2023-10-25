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
using Unity.Netcode;

namespace Experica.NetEnv
{
    public class Marker : NetworkBehaviour
    {
        /// <summary>
        /// Square marker size in visual field angle(degree)
        /// </summary>
        public NetworkVariable<float> MarkerSize = new(2f);
        public NetworkVariable<Corner> MarkerCorner = new(Corner.BottomLeft);
        /// <summary>
        /// Mark On/Off
        /// </summary>
        public NetworkVariable<bool> Mark = new(false);

        INetEnvCamera netenvcamera;
        new Renderer renderer;

        void Awake()
        {
            renderer = GetComponent<Renderer>();
        }

        public override void OnNetworkSpawn()
        {
            netenvcamera = GetComponentInParent<INetEnvCamera>();
            netenvcamera.OnCameraChange += UpdateMarkerPosition;
            MarkerSize.OnValueChanged += OnMarkerSize;
            MarkerCorner.OnValueChanged += OnMarkerCorner;
            Mark.OnValueChanged += OnMark;
        }

        public override void OnNetworkDespawn()
        {
            netenvcamera.OnCameraChange -= UpdateMarkerPosition;
            MarkerSize.OnValueChanged -= OnMarkerSize;
            MarkerCorner.OnValueChanged -= OnMarkerCorner;
            Mark.OnValueChanged -= OnMark;
        }

        Vector3 getmarkerposition(Corner corner, float size, float margin = 0f)
        {
            var h = netenvcamera.Height;
            var w = netenvcamera.Width;
            var z = netenvcamera.NearPlane;
            return corner switch
            {
                Corner.TopLeft => new Vector3((-w + size) / 2f + margin, (h - size) / 2f - margin, z),
                Corner.TopRight => new Vector3((w - size) / 2f - margin, (h - size) / 2f - margin, z),
                Corner.BottomLeft => new Vector3((-w + size) / 2f + margin, (-h + size) / 2f + margin, z),
                Corner.BottomRight => new Vector3((w - size) / 2f - margin, (-h + size) / 2f + margin, z),
                _ => new Vector3(0f, 0f, z),
            };
        }

        void UpdateMarkerPosition(INetEnvCamera _ = null)
        {
            transform.localPosition = getmarkerposition(MarkerCorner.Value, MarkerSize.Value);
        }

        void OnMarkerSize(float p, float c)
        {
            transform.localScale = new Vector3(c, c, 1f);
            UpdateMarkerPosition();
        }

        void OnMarkerCorner(Corner p, Corner c)
        {
            UpdateMarkerPosition();
        }

        void OnMark(bool p, bool c)
        {
            renderer.material.SetColor("_Color", c ? Color.white : Color.black);
        }

    }
}