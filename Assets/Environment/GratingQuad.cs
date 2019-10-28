/*
GratingQuad.cs is part of the Experica.
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
    public class GratingQuad : Quad
    {
        [SyncVar(hook = "onluminance")]
        public float Luminance = 0.5f;
        [SyncVar(hook = "oncontrast")]
        public float Contrast = 1f;
        [SyncVar(hook = "onspatialfreq")]
        public float SpatialFreq = 0.2f;
        [SyncVar(hook = "ontemporalfreq")]
        public float TemporalFreq = 2f;
        [SyncVar(hook = "onspatialphase")]
        public float SpatialPhase = 0;
        [SyncVar(hook = "onmincolor")]
        public Color MinColor = Color.black;
        [SyncVar(hook = "onmaxcolor")]
        public Color MaxColor = Color.white;
        [SyncVar(hook = "onisdrifting")]
        public bool Drifting = true;
        [SyncVar(hook = "ongratingtype")]
        public GratingType GratingType = GratingType.Square;
        [SyncVar(hook = "onisreversetime")]
        public bool ReverseTime = false;

        double reversetime;
        bool isblank;

        public override void OnAwake()
        {
            base.OnAwake();
            reversetime = 0;
            timer.Start();
        }

        public override void OnOri(float o)
        {
            if (float.IsNaN(o))
            {
                renderer.material.SetColor("maxc", new Color(1, 1, 1, 0));
                renderer.material.SetColor("minc", new Color(0, 0, 0, 0));
                isblank = true;
                return;
            }
            if (isblank)
            {
                renderer.material.SetColor("maxc", MaxColor);
                renderer.material.SetColor("minc", MinColor);
                isblank = false;
            }
            renderer.material.SetFloat("ori", o);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(OriOffset + o);
            }
            Ori = o;
        }

        public override void OnOriOffset(float ooffset)
        {
            renderer.material.SetFloat("orioffset", ooffset);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(Ori + ooffset);
            }
            OriOffset = ooffset;
        }

        public override void OnVisible(bool v)
        {
            if (!Visible && v)
            {
                reversetime = 0;
                timer.Restart();
            }
            base.OnVisible(v);
        }

        void onluminance(float l)
        {
            OnLuminance(l);
        }
        public virtual void OnLuminance(float l)
        {
            Color minc, maxc;
            Extension.GetColorScale(l, Contrast).GetColor(MinColor, MaxColor, out minc, out maxc);

            renderer.material.SetColor("minc", minc);
            renderer.material.SetColor("maxc", maxc);
            renderer.material.SetColor("cdiff", maxc - minc);
            Luminance = l;
        }

        void oncontrast(float ct)
        {
            OnContrast(ct);
        }
        public virtual void OnContrast(float ct)
        {
            Color minc, maxc;
            Extension.GetColorScale(Luminance, ct).GetColor(MinColor, MaxColor, out minc, out maxc);

            renderer.material.SetColor("minc", minc);
            renderer.material.SetColor("maxc", maxc);
            renderer.material.SetColor("cdiff", maxc - minc);
            Contrast = ct;
        }

        void onspatialfreq(float sf)
        {
            OnSpatialFreq(sf);
        }
        public virtual void OnSpatialFreq(float sf)
        {
            if (!float.IsNaN(sf))
            {
                renderer.material.SetFloat("sf", sf);
                SpatialFreq = sf;
            }
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

        void onmincolor(Color c)
        {
            OnMinColor(c);
        }
        public virtual void OnMinColor(Color c)
        {
            renderer.material.SetColor("minc", c);
            renderer.material.SetColor("cdiff", MaxColor - c);
            MinColor = c;
        }

        void onmaxcolor(Color c)
        {
            OnMaxColor(c);
        }
        public virtual void OnMaxColor(Color c)
        {
            renderer.material.SetColor("maxc", c);
            renderer.material.SetColor("cdiff", c - MinColor);
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

        void ongratingtype(GratingType t)
        {
            OnGratingType(t);
        }
        public virtual void OnGratingType(GratingType t)
        {
            renderer.material.SetInt("gratingtype", (int)t);
            GratingType = t;
        }

        void onisreversetime(bool r)
        {
            OnIsReverseTime(r);
        }
        public virtual void OnIsReverseTime(bool r)
        {
            reversetime = ReverseTime ? reversetime - timer.ElapsedSecond : reversetime + timer.ElapsedSecond;
            timer.Restart();
            ReverseTime = r;
        }

        void LateUpdate()
        {
            if (Drifting)
            {
                renderer.material.SetFloat("t", (float)(ReverseTime ? reversetime - timer.ElapsedSecond : reversetime + timer.ElapsedSecond));
            }
        }
    }
}