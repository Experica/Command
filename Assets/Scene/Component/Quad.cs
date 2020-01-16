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
using UnityEngine.Networking;

namespace Experica
{
    public class Quad : EnvNet
    {
        [SyncVar(hook = "onrotation")]
        public Vector3 Rotation = Vector3.zero;
        [SyncVar(hook = "onrotationoffset")]
        public Vector3 RotationOffset = Vector3.zero;
        [SyncVar(hook = "onori")]
        public float Ori = 0;
        [SyncVar(hook = "onorioffset")]
        public float OriOffset = 0;
        [SyncVar(hook = "ondiameter")]
        public float Diameter = 10;
        [SyncVar(hook = "onsize")]
        public Vector3 Size = new Vector3(10, 10, 1);
        [SyncVar(hook = "oncolor")]
        public Color Color = Color.white;
        [SyncVar(hook = "onmasktype")]
        public MaskType MaskType = MaskType.None;
        [SyncVar(hook = "onmaskradius")]
        public float MaskRadius = 0.5f;
        [SyncVar(hook = "onsigma")]
        public float Sigma = 0.15f;
        [SyncVar(hook = "onoripositionoffset")]
        public bool OriPositionOffset = false;

        void onrotation(Vector3 r)
        {
            transform.localEulerAngles = r + RotationOffset;
            Rotation = r;
        }

        void onrotationoffset(Vector3 roffset)
        {
            transform.localEulerAngles = Rotation + roffset;
            RotationOffset = roffset;
        }

        void onori(float o)
        {
            transform.localEulerAngles = new Vector3(0, 0, o + OriOffset);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(OriOffset + o);
            }
            Ori = o;
        }

        void onorioffset(float ooffset)
        {
            transform.localEulerAngles = new Vector3(0, 0, ooffset + Ori);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(Ori + ooffset);
            }
            OriOffset = ooffset;
        }

        public override void OnPosition(Vector3 p)
        {
            if (OriPositionOffset)
            {
                transform.localPosition = p + PositionOffset.RotateZCCW(Ori + OriOffset);
                Position = p;
            }
            else
            {
                base.OnPosition(p);
            }
        }

        public override void OnPositionOffset(Vector3 poffset)
        {
            if (OriPositionOffset)
            {
                transform.localPosition = Position + poffset.RotateZCCW(Ori + OriOffset);
                PositionOffset = poffset;
            }
            else
            {
                base.OnPositionOffset(poffset);
            }
        }

        void onsize(Vector3 s)
        {
            transform.localScale = s;
            Size = s;
        }

        void ondiameter(float d)
        {
            onsize(new Vector3(d, d, Size.z));
            Diameter = d;
        }

        void oncolor(Color c)
        {
            renderer.material.SetColor("_color", c);
            Color = c;
        }

        void onmasktype(MaskType t)
        {
            renderer.material.SetInt("_masktype", (int)t);
            MaskType = t;
        }

        void onmaskradius(float r)
        {
            renderer.material.SetFloat("_maskradius", r);
            MaskRadius = r;
        }

        void onsigma(float s)
        {
            renderer.material.SetFloat("_sigma", s);
            Sigma = s;
        }

        void onoripositionoffset(bool opo)
        {
            if (opo)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(Ori + OriOffset);
            }
            else
            {
                transform.localPosition = Position + PositionOffset;
            }
            OriPositionOffset = opo;
        }
    }
}