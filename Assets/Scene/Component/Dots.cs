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
using UnityEngine.Networking;
using UnityEngine.VFX;

namespace Experica
{
    public class Dots : EnvNet
    {
        [SyncVar(hook = "onrotation")]
        public Vector3 Rotation = Vector3.zero;
        [SyncVar(hook = "onrotationoffset")]
        public Vector3 RotationOffset = Vector3.zero;
        [SyncVar(hook = "ondir")]
        public float Dir = 0;
        [SyncVar(hook = "ondiroffset")]
        public float DirOffset = 0;
        [SyncVar(hook = "onspeed")]
        public float Speed = 1f;
        [SyncVar(hook = "ondiameter")]
        public float Diameter = 10;
        [SyncVar(hook = "onsize")]
        public Vector3 Size = new Vector3(10, 10, 1);
        [SyncVar(hook = "ondotcolor")]
        public Color DotColor = Color.white;
        [SyncVar(hook = "ondotsize")]
        public Vector2 DotSize = new Vector2(1, 1);
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

        void ondir(float d)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (d + DirOffset));
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(DirOffset + d);
            }
            Dir = d;
        }

        void ondiroffset(float doffset)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (doffset + Dir));
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(Dir + doffset);
            }
            DirOffset = doffset;
        }

        void onspeed(float s)
        {
            visualeffect.SetFloat("Speed", s);
            Speed = s;
        }

        public override void OnPosition(Vector3 p)
        {
            if (OriPositionOffset)
            {
                transform.localPosition = p + PositionOffset.RotateZCCW(Dir + DirOffset);
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
                transform.localPosition = Position + poffset.RotateZCCW(Dir + DirOffset);
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

        void ondotcolor(Color c)
        {
            visualeffect.SetVector4("DotColor", c);
            DotColor = c;
        }

        void ondotsize(Vector2 s)
        {
            visualeffect.SetVector2("DotSize", s);
            DotSize = s;
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
                transform.localPosition = Position + PositionOffset.RotateZCCW(Dir + DirOffset);
            }
            else
            {
                transform.localPosition = Position + PositionOffset;
            }
            OriPositionOffset = opo;
        }
    }
}