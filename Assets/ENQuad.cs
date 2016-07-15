// --------------------------------------------------------------
// ENQuad.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

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