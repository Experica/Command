// --------------------------------------------------------------
// ENGratingQuad.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class ENGratingQuad : ENQuad
    {
        [SyncVar(hook = "onspatialfreq")]
        public float SpatialFreq;
        [SyncVar(hook = "ontemporalfreq")]
        public float TemporalFreq;
        [SyncVar(hook = "onspatialphase")]
        public float SpatialPhase;
        [SyncVar(hook = "onsigma")]
        public float Sigma;
        [SyncVar(hook = "onmincolor")]
        public Color MinColor;
        [SyncVar(hook = "onmaxcolor")]
        public Color MaxColor;
        [SyncVar(hook = "onisdrifting")]
        public bool Drifting = true;
        [SyncVar(hook ="onisreversetime")]
        public bool ReverseTime = false;

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
            SpatialFreq = sf;
        }

        void ontemporalfreq(float tf)
        {
            OnTemporalFreq(tf);
        }
        public virtual void OnTemporalFreq(float tf)
        {
            renderer.material.SetFloat("tf", tf);
            TemporalFreq = tf;
        }

        void onspatialphase(float p)
        {
            OnSpatialPhase(p);
        }
        public virtual void OnSpatialPhase(float p)
        {
            renderer.material.SetFloat("phase", p);
            SpatialPhase = p;
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

        void onmincolor(Color c)
        {
            OnMinColor(c);
        }
        public virtual void OnMinColor(Color c)
        {
            renderer.material.SetColor("mincolor", c);
            MinColor = c;
        }

        void onmaxcolor(Color c)
        {
            OnMaxColor(c);
        }
        public virtual void OnMaxColor(Color c)
        {
            renderer.material.SetColor("maxcolor", c);
            MaxColor = c;
        }

        void onisdrifting(bool i)
        {
            OnIsDrifting(i);
        }
        public virtual void OnIsDrifting(bool i)
        {
            Drifting = i;
        }

        double reversetime;
        void onisreversetime(bool r)
        {
            OnIsReverseTime(r);
        }
        public virtual void OnIsReverseTime(bool r)
        {
            reversetime = t.ElapsedS;
            ReverseTime = true;
        }
        void Update()
        {
            if (Drifting)
            {
                renderer.material.SetFloat("t", (float)(ReverseTime?2*reversetime-t.ElapsedS: t.ElapsedS));
            }
        }
    }
}