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

namespace Experica.NetEnv
{
    public class Quad : NetEnvVisual
    {
        /// <summary>
        /// Rotation of Quad(degree) around z axis
        /// </summary>
        public NetworkVariable<float> Ori = new(0f);
        public NetworkVariable<float> OriOffset = new(0f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<float> Diameter = new(1f);
        public NetworkVariable<Color> Color = new(UnityEngine.Color.white);
        public NetworkVariable<MaskType> MaskType = new(NetEnv.MaskType.None);
        public NetworkVariable<float> MaskRadius = new(0.5f);
        public NetworkVariable<float> MaskSigma = new(0.15f);
        /// <summary>
        /// Rotate `PositionOffset` by `Ori + OriOffset`, then add to `Position`
        /// </summary>
        public NetworkVariable<bool> OriPositionOffset = new(false);
        public NetworkVariable<Vector3> PositionOffset = new(Vector3.zero);

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
            PositionOffset.OnValueChanged += OnPositionOffset;
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
            PositionOffset.OnValueChanged -= OnPositionOffset;
        }

        void OnOri(float p, float c)
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
                transform.localPosition = c + PositionOffset.Value;
            }
        }

        void OnPositionOffset(Vector3 p, Vector3 c)
        {
            if (OriPositionOffset.Value)
            {
                transform.localPosition = Position.Value + c.RotateZCCW(Ori.Value + OriOffset.Value);
            }
            else
            {
                transform.localPosition = Position.Value + c;
            }
        }

        void OnSize(Vector3 p, Vector3 c)
        {
            transform.localScale = c;
        }

        void OnDiameter(float p, float c)
        {
            if (IsServer)
            {
                // Only Server has write permission of NetworkVariable
                Size.Value = new Vector3(c, c, c);
            }
        }

        void OnColor(Color p, Color c)
        {
            renderer.material.SetColor("_Color", c);
        }

        void OnMaskType(MaskType p, MaskType c)
        {
            renderer.material.SetInt("_MaskType", (int)c);
        }

        void OnMaskRadius(float p, float c)
        {
            renderer.material.SetFloat("_MaskRadius", c);
        }

        void OnMaskSigma(float p, float c)
        {
            renderer.material.SetFloat("_MaskSigma", c);
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