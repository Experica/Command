/*
ENQuad.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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
using System.Collections;

namespace VLab
{
    public enum MaskType
    {
        None,
        Disk,
        Gaussian,
        DiskFade
    }

    public class ENQuad : EnvNet
    {
        [SyncVar(hook = "onori")]
        public float Ori = 0;
        [SyncVar(hook = "onorioffset")]
        public float OriOffset = 0;
        [SyncVar(hook = "ondiameter")]
        public float Diameter = 2;
        [SyncVar(hook = "onsize")]
        public Vector3 Size = new Vector3(2, 2, 1);
        [SyncVar(hook = "oncolor")]
        public Color Color = Color.white;
        [SyncVar(hook = "onmasktype")]
        public MaskType MaskType;
        [SyncVar(hook = "onmaskradius")]
        public float MaskRadius=0.5f;
        [SyncVar(hook = "onsigma")]
        public float Sigma=0.15f;

        public VLTimer t = new VLTimer();

        void onori(float o)
        {
            OnOri(o);
        }
        public virtual void OnOri(float o)
        {
            transform.eulerAngles = new Vector3(0, 0, o + OriOffset);
            transform.position = Position + PositionOffset.RotateZCCW(OriOffset + o);
            Ori = o;
        }

        void onorioffset(float ooffset)
        {
            OnOriOffset(ooffset);
        }
        public virtual void OnOriOffset(float ooffset)
        {
            transform.eulerAngles = new Vector3(0, 0, ooffset + Ori);
            transform.position = Position + PositionOffset.RotateZCCW(Ori + ooffset);
            OriOffset = ooffset;
        }

        public override void OnPosition(Vector3 p)
        {
            transform.position = p + PositionOffset.RotateZCCW(Ori + OriOffset);
            Position = p;
        }

        public override void OnPositionOffset(Vector3 poffset)
        {
            transform.position = Position + poffset.RotateZCCW(Ori + OriOffset);
            PositionOffset = poffset;
        }

        void onsize(Vector3 s)
        {
            OnSize(s);
        }
        public virtual void OnSize(Vector3 s)
        {
            transform.localScale = s;
            renderer.material.SetFloat("sizex", s.x);
            renderer.material.SetFloat("sizey", s.y);
            Size = s;
        }

        void ondiameter(float d)
        {
            OnDiameter(d);
        }
        public virtual void OnDiameter(float d)
        {
            Size = new Vector3(d, d, Size.z);
            transform.localScale = Size;
            renderer.material.SetFloat("sizex", d);
            renderer.material.SetFloat("sizey", d);
            Diameter = d;
        }

        void oncolor(Color c)
        {
            OnColor(c);
        }
        public virtual void OnColor(Color c)
        {
            renderer.material.SetColor("col", c);
            Color = c;
        }

        void onmasktype(MaskType t)
        {
            OnMaskType(t);
        }
        public virtual void OnMaskType(MaskType t)
        {
            renderer.material.SetInt("masktype", (int)t);
            MaskType = t;
        }

        void onmaskradius(float r)
        {
            OnMaskRadius(r);
        }
        public virtual void OnMaskRadius(float r)
        {
            renderer.material.SetFloat("maskradius", r);
            MaskRadius = r;
        }

        void onsigma(float s)
        {
            OnSigma(s);
        }
        public virtual void OnSigma(float s)
        {
            renderer.material.SetFloat("sigma", s);
            Sigma = s;
        }
    }
}