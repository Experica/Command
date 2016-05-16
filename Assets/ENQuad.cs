using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class ENQuad : EnvNet
    {
        [SyncVar(hook = "onori")]
        public float ori = 0;
        [SyncVar(hook = "onlength")]
        public float length = 1;
        [SyncVar(hook = "onwidth")]
        public float width = 1;
        [SyncVar(hook = "onheight")]
        public float height = 1;
        [SyncVar(hook = "oncolor")]
        public Color color = new Color();
        [SyncVar(hook = "onmasktype")]
        public int masktype = 0;

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
            transform.eulerAngles = new Vector3(0, 0, o);
            ori = o;
        }

        void onlength(float l)
        {
            OnLength(l);
        }
        public virtual void OnLength(float l)
        {
            transform.localScale = new Vector3(l, width, height);
            renderer.material.SetFloat("length", l);
            length = l;
        }

        void onwidth(float w)
        {
            OnWidth(w);
        }
        public virtual void OnWidth(float w)
        {
            transform.localScale = new Vector3(length, w, height);
            renderer.material.SetFloat("width", w);
            width = w;
        }

        void onheight(float h)
        {
            OnHeight(h);
        }
        public virtual void OnHeight(float h)
        {
            transform.localScale = new Vector3(length, width, h);
            height = h;
        }

        void oncolor(Color c)
        {
            OnColor(c);
        }
        public virtual void OnColor(Color c)
        {
            renderer.material.color = c;
            color = c;
        }

        void onmasktype(int t)
        {
            OnMaskType(t);
        }
        public virtual void OnMaskType(int t)
        {
            renderer.material.SetInt("masktype", t);
            masktype = t;
        }

    }
}