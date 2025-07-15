/*
Tag.cs is part of the Experica.
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
    public class Tag : NetworkBehaviour
    {
        /// <summary>
        /// Square Tag size in visual field angle(degree)
        /// </summary>
        public NetworkVariable<float> TagSize = new(2f);
        public NetworkVariable<float> TagMargin = new(2f);
        public NetworkVariable<AprilTag> TagID = new(AprilTag.tag25_09_00000);
        public NetworkVariable<Corner> TagCorner = new(Corner.BottomLeft);

        INetEnvCamera netenvcamera;
        new Renderer renderer;

        void Awake()
        {
            renderer = GetComponent<Renderer>();
        }

        public override void OnNetworkSpawn()
        {
            netenvcamera = GetComponentInParent<INetEnvCamera>();
            netenvcamera.OnCameraChange += UpdateTagPosition;
            TagSize.OnValueChanged += OnTagSize;
            TagMargin.OnValueChanged += OnTagMargin;
            TagID.OnValueChanged += OnTagID;
            TagCorner.OnValueChanged += OnTagCorner;
        }

        public override void OnNetworkDespawn()
        {
            netenvcamera.OnCameraChange -= UpdateTagPosition;
            TagSize.OnValueChanged -= OnTagSize;
            TagMargin.OnValueChanged -= OnTagMargin;
            TagID.OnValueChanged -= OnTagID;
            TagCorner.OnValueChanged -= OnTagCorner;
        }

        Vector3 gettagposition(Corner corner, float size, float margin = 0f)
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

        void UpdateTagPosition(INetEnvCamera _ = null)
        {
            transform.localPosition = gettagposition(TagCorner.Value, TagSize.Value, TagMargin.Value);
        }

        void OnTagSize(float p, float c)
        {
            transform.localScale = new Vector3(c, c, 1f);
            UpdateTagPosition();
        }

        void OnTagMargin(float p, float c)
        {
            UpdateTagPosition();
        }

        void OnTagCorner(Corner p, Corner c)
        {
            UpdateTagPosition();
        }

        void OnTagID(AprilTag p, AprilTag c)
        {
            if ($"Assets/NetEnv/Element/AprilTag/{c}.svg".QueryTexture(out Texture t))
            {
                renderer.material.SetTexture("_Image", t);
            }
        }

        public float TagSurfaceMargin => TagMargin.Value + TagSize.Value * (TagID.Value.ToString().StartsWith("tag25") ? 1f / 9f : 1f / 10f);
    }
}