/*
ImageList.cs is part of the Experica.
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
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Experica.NetEnv
{
    public class ImageList : NetEnvVisual
    {
        public NetworkVariable<float> Ori = new(0f);
        public NetworkVariable<float> OriOffset = new(0f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<float> Diameter = new(1f);
        public NetworkVariable<MaskType> MaskType = new(NetEnv.MaskType.None);
        public NetworkVariable<float> MaskRadius = new(0.5f);
        public NetworkVariable<float> MaskSigma = new(0.15f);
        public NetworkVariable<Color> MinColor = new(Color.black);
        public NetworkVariable<Color> MaxColor = new(Color.white);
        public NetworkVariable<FixedString512Bytes> ImageSet = new("TestImageSet.UInt8");
        public NetworkVariable<uint> Image = new(0);
        public NetworkVariable<ColorChannel> ChannelModulate = new(ColorChannel.None);

        ImageSet imgset = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Ori.OnValueChanged += OnOri;
            OriOffset.OnValueChanged += OnOriOffset;
            Size.OnValueChanged += OnSize;
            Diameter.OnValueChanged += OnDiameter;
            MaskType.OnValueChanged += OnMaskType;
            MaskRadius.OnValueChanged += OnMaskRadius;
            MaskSigma.OnValueChanged += OnMaskSigma;
            MinColor.OnValueChanged += OnMinColor;
            MaxColor.OnValueChanged += OnMaxColor;
            ImageSet.OnValueChanged += OnImageSet;
            Image.OnValueChanged += OnImage;
            ChannelModulate.OnValueChanged += OnChannelModulate;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Ori.OnValueChanged -= OnOri;
            OriOffset.OnValueChanged -= OnOriOffset;
            Size.OnValueChanged -= OnSize;
            Diameter.OnValueChanged -= OnDiameter;
            MaskType.OnValueChanged -= OnMaskType;
            MaskRadius.OnValueChanged -= OnMaskRadius;
            MaskSigma.OnValueChanged -= OnMaskSigma;
            MinColor.OnValueChanged -= OnMinColor;
            MaxColor.OnValueChanged -= OnMaxColor;
            ImageSet.OnValueChanged -= OnImageSet;
            Image.OnValueChanged -= OnImage;
            ChannelModulate.OnValueChanged -= OnChannelModulate;
        }

        void OnOri(float p, float c)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, c + OriOffset.Value);
        }

        void OnOriOffset(float p, float c)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, c + Ori.Value);
        }

        void OnSize(Vector3 p, Vector3 c)
        {
            transform.localScale = c;
        }

        void OnDiameter(float p, float c)
        {
            Size.Value = new Vector3(c, c, c);
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

        void OnMinColor(Color p, Color c)
        {
            renderer.material.SetColor("_MinColor", c);
        }

        void OnMaxColor(Color p, Color c)
        {
            renderer.material.SetColor("_MaxColor", c);
        }

        void OnChannelModulate(ColorChannel p, ColorChannel c)
        {
            renderer.material.SetInt("_Channel", (int)c);
        }

        void OnImage(uint p, uint c)
        {
            if (c < imgset.Images.Length)
            {
                renderer.material.SetTexture("_Image", imgset.Images[c]);
            }
        }

        void OnImageSet(FixedString512Bytes p, FixedString512Bytes c)
        {
            if (c.ToString().QueryImageSet(out imgset))
            {
                OnImage(0, 0);
            }
        }

    }
}