using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class ENGratingQuad : ENQuad
    {
        [SyncVar(hook = "onspatialfreq")]
        public float spatialfreq;
        [SyncVar(hook = "ontemporalfreq")]
        public float temporalfreq;
        [SyncVar(hook = "onspatialphase")]
        public float spatialphase;
        [SyncVar(hook = "onsigma")]
        public float sigma;
        [SyncVar(hook = "onmincolor")]
        public Color mincolor;
        [SyncVar(hook = "onmaxcolor")]
        public Color maxcolor;
        [SyncVar(hook = "onisdrifting")]
        public bool isdrifting = true;

        public override void OnVisible(bool v)
        {
            base.OnVisible(v);
            if (v)
            {
                t.ReStart();
            }
        }

        void onspatialfreq(float sf)
        {
            OnSpatialFreq(sf);
        }
        public virtual void OnSpatialFreq(float sf)
        {
            renderer.material.SetFloat("sf", sf);
            spatialfreq = sf;
        }

        void ontemporalfreq(float tf)
        {
            OnTemporalFreq(tf);
        }
        public virtual void OnTemporalFreq(float tf)
        {
            renderer.material.SetFloat("tf", tf);
            temporalfreq = tf;
        }

        void onspatialphase(float p)
        {
            OnSpatialPhase(p);
        }
        public virtual void OnSpatialPhase(float p)
        {
            renderer.material.SetFloat("phase", p);
            spatialphase = p;
        }

        void onsigma(float s)
        {
            OnSigma(s);
        }
        public virtual void OnSigma(float s)
        {
            renderer.material.SetFloat("sigma", s);
            sigma = s;
        }

        void onmincolor(Color c)
        {
            OnMinColor(c);
        }
        public virtual void OnMinColor(Color c)
        {
            renderer.material.SetColor("mincolor", c);
            mincolor = c;
        }

        void onmaxcolor(Color c)
        {
            OnMaxColor(c);
        }
        public virtual void OnMaxColor(Color c)
        {
            renderer.material.SetColor("maxcolor", c);
            maxcolor = c;
        }

        void onisdrifting(bool i)
        {
            OnIsDrifting(i);
        }
        public virtual void OnIsDrifting(bool i)
        {
            isdrifting = i;
        }

        void Update()
        {
            if (isdrifting)
            {
                renderer.material.SetFloat("t", (float)t.ElapsedS);
            }
        }
    }
}