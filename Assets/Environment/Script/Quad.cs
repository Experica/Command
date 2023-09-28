/*
Quad.cs is part of the Experica.
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

namespace Experica.Environment
{
    public class Quad : EnvNetVisual
    {
        /// <summary>
        /// Rotation of Quad(degree)
        /// </summary>
        public NetworkVariable<float> Ori = new(0f);
        public NetworkVariable<float> OriOffset = new(0f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<float> Diameter = new(1f);
        public NetworkVariable<Color> Color = new(Color.white);
        public NetworkVariable<MaskType> MaskType = new(MaskType.None);
        public NetworkVariable<float> MaskRadius = new(0.5f);
        public NetworkVariable<float> MaskSigma = new(0.15f);
        public NetworkVariable<bool> OriPositionOffset = new(false);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Ori.OnValueChanged += OnOri;
            OriOffset.OnValueChanged += OnOriOffset;
            Size.OnValueChanged += OnSize;
            Diameter.OnValueChanged += OnDiameter;
            Color.OnValueChanged += OnColor;
            MaskType.OnValueChanged += OnMaskType;
            MaskRadius.OnValueChanged += OnMaskRadius;
            MaskSigma.OnValueChanged += OnMaskSigma;
            OriPositionOffset.OnValueChanged += OnOriPositionOffset;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Ori.OnValueChanged -= OnOri;
            OriOffset.OnValueChanged -= OnOriOffset;
            Size.OnValueChanged -= OnSize;
            Diameter.OnValueChanged -= OnDiameter;
            Color.OnValueChanged -= OnColor;
            MaskType.OnValueChanged -= OnMaskType;
            MaskRadius.OnValueChanged -= OnMaskRadius;
            MaskSigma.OnValueChanged -= OnMaskSigma;
            OriPositionOffset.OnValueChanged -= OnOriPositionOffset;
        }

        void OnOri(float p,float c)
        {
            var theta = c + OriOffset.Value;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, theta);
            if (OriPositionOffset.Value)
            {
                transform.localPosition = Position.Value + PositionOffset.Value.RotateZCCW(theta);
            }
        }

        void OnOriOffset(float p, float c)
        {
            var theta = c + Ori.Value;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, theta);
            if (OriPositionOffset.Value)
            {
                transform.localPosition = Position.Value + PositionOffset.Value.RotateZCCW(theta);
            }
        }

        protected override void OnPosition(Vector3 p, Vector3 c)
        {
            if (OriPositionOffset.Value)
            {
                transform.localPosition = c + PositionOffset.Value.RotateZCCW(Ori.Value + OriOffset.Value);
            }
            else
            {
                base.OnPosition(p, c);
            }
        }

        protected override void OnPositionOffset(Vector3 p, Vector3 c)
        {
            if (OriPositionOffset.Value)
            {
                transform.localPosition = Position.Value + c.RotateZCCW(Ori.Value + OriOffset.Value);
            }
            else
            {
              base.OnPositionOffset(p, c);
            }
        }

        void OnSize(Vector3 p, Vector3 c)
        {
            transform.localScale = c;
        }

        void OnDiameter(float p, float c)
        {
            Size.Value = new Vector3(c,c,c);
        }

        void OnColor(Color p, Color c)
        {
            renderer.material.SetColor("_color", c);
        }

        void OnMaskType(MaskType p, MaskType c)
        {
            renderer.material.SetInt("_masktype", (int)c);
        }

        void OnMaskRadius(float p, float c)
        {
            renderer.material.SetFloat("_maskradius", c);
        }

        void OnMaskSigma(float p, float c)
        {
            renderer.material.SetFloat("_masksigma", c);
        }

        void OnOriPositionOffset(bool p, bool c)
        {
            if (c)
            {
                transform.localPosition = Position.Value + PositionOffset.Value.RotateZCCW(Ori.Value + OriOffset.Value);
            }
            else
            {
                transform.localPosition = Position.Value + PositionOffset.Value;
            }
        }
    }
}