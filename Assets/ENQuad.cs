// -----------------------------------------------------------------------------
// ENQuad.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class ENQuad : EnvNet
    {
        [SyncVar(hook = "onori")]
        public float Ori = 0;
        [SyncVar(hook ="onorioffset")]
        public float OriOffset = 0;
        [SyncVar(hook ="onsize")]
        public Vector3 Size = new Vector3(1, 1, 1);
        [SyncVar(hook ="ondiameter")]
        public float Diameter = 1;
        [SyncVar(hook = "oncolor")]
        public Color Color = new Color();
        [SyncVar(hook = "onmasktype")]
        public int MaskType = 0;

        public VLTimer t = new VLTimer();

        public override void OnAwake()
        {
            base.OnAwake();
            t.Start();
        }

        void onori(float o)
        {
            OnOri(o);
        }
        public virtual void OnOri(float o)
        {
            transform.eulerAngles = new Vector3(0, 0, o+OriOffset);
            Ori = o;
        }

        void onorioffset(float ooffset)
        {
            OnOriOffset(ooffset);
        }
        public virtual void OnOriOffset(float ooffset)
        {
            transform.eulerAngles = new Vector3(0, 0, ooffset+Ori);
            OriOffset = ooffset;
        }

        void onsize(Vector3 s)
        {
            OnSize(s);
        }
        public virtual void OnSize(Vector3 s)
        {
            transform.localScale = s;
            renderer.material.SetFloat("length", s.x);
            renderer.material.SetFloat("width", s.y);
            Size = s;
        }

        void ondiameter(float d)
        {
            OnDiameter(d);
        }
        public virtual void OnDiameter(float d)
        {
            transform.localScale = new Vector3(d,d,d);
            renderer.material.SetFloat("length", d);
            renderer.material.SetFloat("width", d);
            Diameter = d;
        }

        void oncolor(Color c)
        {
            OnColor(c);
        }
        public virtual void OnColor(Color c)
        {
            renderer.material.color = c;
            Color = c;
        }

        void onmasktype(int t)
        {
            OnMaskType(t);
        }
        public virtual void OnMaskType(int t)
        {
            renderer.material.SetInt("masktype", t);
            MaskType = t;
        }

    }
}