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
    public class GratingQuad : EnvNetVisual
    {
        [SyncVar(hook = "onrotation")]
        public Vector3 Rotation = Vector3.zero;
        [SyncVar(hook = "onrotationoffset")]
        public Vector3 RotationOffset = Vector3.zero;
        /// <summary>
        /// Orientation of the grating in degree
        /// </summary>
        [SyncVar(hook = "onori")]
        public float Ori = 0;
        [SyncVar(hook = "onorioffset")]
        public float OriOffset = 0;
        /// <summary>
        /// Diameter of the grating in visual field degree
        /// </summary>
        [SyncVar(hook = "ondiameter")]
        public float Diameter = 10;
        [SyncVar(hook = "onsize")]
        public Vector3 Size = new Vector3(10, 10, 1);
        [SyncVar(hook = "onmasktype")]
        public MaskType MaskType = MaskType.None;
        /// <summary>
        /// Mask radius in uv coordinates
        /// </summary>
        [SyncVar(hook = "onmaskradius")]
        public float MaskRadius = 0.5f;
        /// <summary>
        /// Sigma parameter of the mask
        /// </summary>
        [SyncVar(hook = "onmasksigma")]
        public float MaskSigma = 0.15f;
        /// <summary>
        /// Orient the position offset by `Ori`
        /// </summary>
        [SyncVar(hook = "onoripositionoffset")]
        public bool OriPositionOffset = false;
        /// <summary>
        /// Mean luminance of grating in [0, 1] scale
        /// </summary>
        [SyncVar(hook = "onluminance")]
        public float Luminance = 0.5f;
        /// <summary>
        /// Michelson contrast of grating in [0, 1]
        /// </summary>
        [SyncVar(hook = "oncontrast")]
        public float Contrast = 1f;
        /// <summary>
        /// Spatial Frequency in cycle/degree
        /// </summary>
        [SyncVar(hook = "onspatialfreq")]
        public float SpatialFreq = 0.2f;
        /// <summary>
        /// Temporal Frequency in cycle/second
        /// </summary>
        [SyncVar(hook = "ontemporalfreq")]
        public float TemporalFreq = 1f;
        [SyncVar(hook = "onmodulatetemporalfreq")]
        public float ModulateTemporalFreq = 0.2f;
        [SyncVar(hook ="onmodulatetemporalphase")]
        public float ModulateTemporalPhase = 0f;
        /// <summary>
        /// Spatial Phase in [0, 1] scale
        /// </summary>
        [SyncVar(hook = "onspatialphase")]
        public float SpatialPhase = 0;
        /// <summary>
        /// minimum color of the grating
        /// </summary>
        [SyncVar(hook = "onmincolor")]
        public Color MinColor = Color.black;
        /// <summary>
        /// maximum color of the grating
        /// </summary>
        [SyncVar(hook = "onmaxcolor")]
        public Color MaxColor = Color.white;
        [SyncVar(hook = "onpausetime")]
        public bool PauseTime = false;
        [SyncVar(hook = "onpausemodulatetime")]
        public bool PauseModulateTime = true;
        [SyncVar(hook = "ongratingtype")]
        public WaveType GratingType = WaveType.Square;
        [SyncVar(hook = "onmodulatetype")]
        public WaveType ModulateType = WaveType.Square;
        [SyncVar(hook = "onreversetime")]
        public bool ReverseTime = false;
        [SyncVar(hook = "onduty")]
        public float Duty = 0.5f;
        [SyncVar(hook = "onmodulateduty")]
        public float ModulateDuty = 0.5f;
        [SyncVar(hook = "ontimesecond")]
        public float TimeSecond = 0;
        [SyncVar(hook = "onmodulatetimesecond")]
        public float ModulateTimeSecond = 0;

        double timeatreverse = 0;
        // timers are used to calculate grating visuals, so here use frame time
        Timer timer = new Timer(true);
        Timer mtimer = new Timer(true);

        protected override void OnStart()
        {
            base.OnStart();
            timer.Start();
            mtimer.Start();
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

        protected override void OnPosition(Vector3 p)
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

        protected override void OnPositionOffset(Vector3 poffset)
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

        void onmasksigma(float s)
        {
            renderer.material.SetFloat("_masksigma", s);
            MaskSigma = s;
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

        protected override void OnVisible(bool v)
        {
            // reset time when reappear
            if (v)
            {
                timeatreverse = 0;
                ontimesecond(0);
                onmodulatetimesecond(0);
                if (PauseTime) { timer.Reset(); }
                else { timer.Restart(); }
            }
            base.OnVisible(v);
        }

        void onluminance(float l)
        {
            Color _mincolor, _maxcolor;
            Extension.LuminanceSpan(l, Contrast).ScaleColor(MinColor, MaxColor, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_mincolor", _mincolor);
            renderer.material.SetColor("_maxcolor", _maxcolor);
            Luminance = l;
        }

        void oncontrast(float c)
        {
            Color _mincolor, _maxcolor;
            Extension.LuminanceSpan(Luminance, c).ScaleColor(MinColor, MaxColor, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_mincolor", _mincolor);
            renderer.material.SetColor("_maxcolor", _maxcolor);
            Contrast = c;
        }

        void onspatialfreq(float sf)
        {
            renderer.material.SetFloat("_sf", sf);
            SpatialFreq = sf;
        }

        void ontemporalfreq(float tf)
        {
            renderer.material.SetFloat("_tf", tf);
            TemporalFreq = tf;
        }

        void onmodulatetemporalfreq(float mtf)
        {
            renderer.material.SetFloat("_mtf", mtf);
            ModulateTemporalFreq = mtf;
        }

        void onmodulatetemporalphase(float mphase)
        {
            renderer.material.SetFloat("_mphase", mphase);
            ModulateTemporalPhase = mphase;
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

        void onpausetime(bool i)
        {
            if (i) { timer.Stop(); }
            else { timer.Start(); }
            PauseTime = i;
        }

        void onpausemodulatetime(bool i)
        {
            if (i) { mtimer.Stop(); }
            else { mtimer.Start(); }
            PauseModulateTime = i;
        }

        void ongratingtype(WaveType t)
        {
            renderer.material.SetInt("_gratingtype", (int)t);
            GratingType = t;
        }

        void onmodulatetype(WaveType t)
        {
            renderer.material.SetInt("_mgratingtype", (int)t);
            ModulateType = t;
        }

        void onduty(float d)
        {
            renderer.material.SetFloat("_duty", d);
            Duty = d;
        }

        void onmodulateduty(float d)
        {
            renderer.material.SetFloat("_mduty", d);
            ModulateDuty = d;
        }

        void onreversetime(bool r)
        {
            timeatreverse = GetTimeSecond;
            if (PauseTime) { timer.Reset(); }
            else { timer.Restart(); }
            ReverseTime = r;
        }

        double GetTimeSecond
        {
            get { return ReverseTime ? timeatreverse - timer.ElapsedSecond : timeatreverse + timer.ElapsedSecond; }
        }

        void ontimesecond(float t)
        {
            renderer.material.SetFloat("_t", t);
        }

        void onmodulatetimesecond(float t)
        {
            renderer.material.SetFloat("_mt", t);
        }

        /// <summary>
        /// automatically update grating time every frame
        /// </summary>
        void LateUpdate()
        {
            if (!PauseTime)
            {
                ontimesecond((float)GetTimeSecond);
            }
            if (!PauseModulateTime)
            {
                onmodulatetimesecond((float)mtimer.ElapsedSecond);
            }
        }
    }
}