/*
Grating.cs is part of the Experica.
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
    public class GratingQuad : EnvNet
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
        [SyncVar(hook = "onmasktype")]
        public MaskType MaskType = MaskType.None;
        [SyncVar(hook = "onmaskradius")]
        public float MaskRadius = 0.5f;
        [SyncVar(hook = "onsigma")]
        public float Sigma = 0.15f;
        [SyncVar(hook = "onoripositionoffset")]
        public bool OriPositionOffset = false;
        [SyncVar(hook = "onluminance")]
        public float Luminance = 0.5f;
        [SyncVar(hook = "oncontrast")]
        public float Contrast = 1f;
        [SyncVar(hook = "onspatialfreq")]
        public float SpatialFreq = 0.2f;
        [SyncVar(hook = "ontemporalfreq")]
        public float TemporalFreq = 1f;
        [SyncVar(hook = "onmodulatefreq")]
        public float ModulateFreq = 0.2f;
        [SyncVar(hook = "onspatialphase")]
        public float SpatialPhase = 0;
        [SyncVar(hook = "onmincolor")]
        public Color MinColor = Color.black;
        [SyncVar(hook = "onmaxcolor")]
        public Color MaxColor = Color.white;
        [SyncVar(hook = "onisdrifting")]
        public bool Drifting = true;
        [SyncVar(hook = "onismodulating")]
        public bool Modulating = false;
        [SyncVar(hook = "ongratingtype")]
        public WaveType GratingType = WaveType.Square;
        [SyncVar(hook = "onmodulatetype")]
        public WaveType ModulateType = WaveType.Square;
        [SyncVar(hook = "onisreversetime")]
        public bool ReverseTime = false;
        [SyncVar(hook = "onduty")]
        public float Duty = 0.5f;
        [SyncVar(hook = "onmodulateduty")]
        public float ModulateDuty = 0.5f;

        double reversetime;
        bool isblank;
        Timer timer = new Timer();

        public override void OnAwake()
        {
            base.OnAwake();
            reversetime = 0;
            timer.Start();
        }

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
            renderer.material.SetVector("_size", new Vector2(s.x, s.y));
            Size = s;
        }

        void ondiameter(float d)
        {
            onsize(new Vector3(d, d, Size.z));
            Diameter = d;
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

        void onori(float o)
        {
            if (float.IsNaN(o))
            {
                renderer.material.SetColor("_maxcolor", new Color(1, 1, 1, 0));
                renderer.material.SetColor("_mincolor", new Color(0, 0, 0, 0));
                isblank = true;
                return;
            }
            if (isblank)
            {
                renderer.material.SetColor("_maxcolor", MaxColor);
                renderer.material.SetColor("_mincolor", MinColor);
                isblank = false;
            }
            var theta = o + OriOffset;
            renderer.material.SetFloat("_ori", Mathf.Deg2Rad * theta);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(theta);
            }
            Ori = o;
        }

        void onorioffset(float ooffset)
        {
            var theta = ooffset + Ori;
            renderer.material.SetFloat("_ori", Mathf.Deg2Rad * theta);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(theta);
            }
            OriOffset = ooffset;
        }

        public override void OnVisible(bool v)
        {
            if (!Visible && v)
            {
                reversetime = 0;
                renderer.material.SetFloat("_t", 0);
                renderer.material.SetFloat("_mt", 0);
                timer.Restart();
            }
            base.OnVisible(v);
        }

        void onluminance(float l)
        {
            Color _mincolor, _maxcolor;
            Extension.GetColorScale(l, Contrast).GetColor(MinColor, MaxColor, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_mincolor", _mincolor);
            renderer.material.SetColor("_maxcolor", _maxcolor);
            Luminance = l;
        }

        void oncontrast(float ct)
        {
            Color _mincolor, _maxcolor;
            Extension.GetColorScale(Luminance, ct).GetColor(MinColor, MaxColor, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_mincolor", _mincolor);
            renderer.material.SetColor("_maxcolor", _maxcolor);
            Contrast = ct;
        }

        void onspatialfreq(float sf)
        {
            if (!float.IsNaN(sf))
            {
                renderer.material.SetFloat("_sf", sf);
                SpatialFreq = sf;
            }
        }

        void ontemporalfreq(float tf)
        {
            renderer.material.SetFloat("_tf", tf);
            TemporalFreq = tf;
        }

        void onmodulatefreq(float mf)
        {
            renderer.material.SetFloat("_mf", mf);
            ModulateFreq = mf;
        }

        void onspatialphase(float p)
        {
            renderer.material.SetFloat("_phase", p);
            SpatialPhase = p;
        }

        void onmincolor(Color c)
        {
            renderer.material.SetColor("_mincolor", c);
            MinColor = c;
        }

        void onmaxcolor(Color c)
        {
            renderer.material.SetColor("_maxcolor", c);
            MaxColor = c;
        }

        void onisdrifting(bool i)
        {
            Drifting = i;
        }

        void onismodulating(bool i)
        {
            Modulating = i;
        }

        void ongratingtype(WaveType t)
        {
            renderer.material.SetInt("_gratingtype", (int)t);
            GratingType = t;
        }

        void onmodulatetype(WaveType t)
        {
            renderer.material.SetInt("_modulatetype", (int)t);
            ModulateType = t;
        }

        void onduty(float d)
        {
            renderer.material.SetFloat("_duty", d);
            Duty = d;
        }

        void onmodulateduty(float d)
        {
            renderer.material.SetFloat("_modulateduty", d);
            ModulateDuty = d;
        }

        void onisreversetime(bool r)
        {
            reversetime = ReverseTime ? reversetime - timer.ElapsedSecond : reversetime + timer.ElapsedSecond;
            timer.Restart();
            ReverseTime = r;
        }

        void LateUpdate()
        {
            if (Drifting)
            {
                renderer.material.SetFloat("_t", (float)(ReverseTime ? reversetime - timer.ElapsedSecond : reversetime + timer.ElapsedSecond));
            }
            if (Modulating)
            {
                renderer.material.SetFloat("_mt", (float)(ReverseTime ? reversetime - timer.ElapsedSecond : reversetime + timer.ElapsedSecond));
            }
        }
    }
}