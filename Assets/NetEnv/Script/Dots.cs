/*
Dots.cs is part of the Experica.
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
using UnityEngine.VFX;

namespace Experica.NetEnv
{
    public class Dots : NetEnvVisual
    {
        public NetworkVariable<Vector3> Rotation = new(Vector3.zero);
        public NetworkVariable<Vector3> RotationOffset = new(Vector3.zero);
        public NetworkVariable<float> Dir = new(0f);
        public NetworkVariable<float> DirOffset = new(0f);
        public NetworkVariable<float> Speed = new(1f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<float> Diameter = new(1f);
        public NetworkVariable<uint> NDots = new(30);
        public NetworkVariable<Color> DotColor = new(Color.white);
        public NetworkVariable<Vector2> DotSize = new(Vector2.one);
        public NetworkVariable<float> Coherence = new(0f);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Rotation.OnValueChanged += OnRotation;
            RotationOffset.OnValueChanged += OnRotationOffset;
            Dir.OnValueChanged += OnDir;
            DirOffset.OnValueChanged += OnDirOffset;
            Speed.OnValueChanged += OnSpeed;
            Size.OnValueChanged += OnSize;
            Diameter.OnValueChanged += OnDiameter;
            NDots.OnValueChanged += OnNDots;
            DotColor.OnValueChanged += OnDotColor;
            DotSize.OnValueChanged += OnDotSize;
            Coherence.OnValueChanged += OnCoherence;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Rotation.OnValueChanged -= OnRotation;
            RotationOffset.OnValueChanged -= OnRotationOffset;
            Dir.OnValueChanged -= OnDir;
            DirOffset.OnValueChanged -= OnDirOffset;
            Speed.OnValueChanged -= OnSpeed;
            Size.OnValueChanged -= OnSize;
            Diameter.OnValueChanged -= OnDiameter;
            NDots.OnValueChanged -= OnNDots;
            DotColor.OnValueChanged -= OnDotColor;
            DotSize.OnValueChanged -= OnDotSize;
            Coherence.OnValueChanged -= OnCoherence;
        }

        void OnRotation(Vector3 p, Vector3 c)
        {
            transform.localEulerAngles = c + RotationOffset.Value;
        }

        void OnRotationOffset(Vector3 p, Vector3 c)
        {
            transform.localEulerAngles = Rotation.Value + c;
        }

        void OnDir(float p, float c)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (c + DirOffset.Value));
            visualeffect.Reinit();
        }

        void OnDirOffset(float p, float c)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (c + Dir.Value));
            visualeffect.Reinit();
        }

        void OnSpeed(float p, float c)
        {
            visualeffect.SetFloat("Speed", c);
            visualeffect.Reinit();
        }

        void OnSize(Vector3 p, Vector3 c)
        {
            visualeffect.SetVector3("Size", c);
        }

        void OnDiameter(float p, float c)
        {
            Size.Value = new Vector3(c, c, c);
        }

        void OnNDots(uint p, uint c)
        {
            visualeffect.SetUInt("NDots", c);
            visualeffect.Reinit();
        }

        void OnDotColor(Color p, Color c)
        {
            visualeffect.SetVector4("DotColor", c);
        }

        void OnDotSize(Vector2 p, Vector2 c)
        {
            visualeffect.SetVector2("DotSize", c);
        }

        void OnCoherence(float p, float c)
        {
            visualeffect.SetFloat("Coherence", c);
            visualeffect.Reinit();
        }

        protected override void OnVisible(bool p, bool c)
        {
            // reset dots when reappear
            if (c)
            {
                visualeffect.Reinit();
            }
            base.OnVisible(p, c);
        }
    }
}