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
using Unity.Netcode;

namespace Experica.NetEnv
{
    public class Plaid : NetEnvVisual
    {
        /// <summary>
        /// Rotation of Quad(degree)
        /// </summary>
        public NetworkVariable<Vector3> Rotation = new(Vector3.zero);
        public NetworkVariable<Vector3> RotationOffset = new(Vector3.zero);
        /// <summary>
        /// Orientation of Gratings(degree)
        /// </summary>
        public NetworkVariable<float> Ori0 = new(0f);
        public NetworkVariable<float> OriOffset0 = new(0f);
        public NetworkVariable<float> Ori1 = new(-90f);
        public NetworkVariable<float> OriOffset1 = new(0f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<float> Diameter = new(1f);
        public NetworkVariable<MaskType> MaskType = new(NetEnv.MaskType.None);
        /// <summary>
        /// Mask radius in centered uv coordinates [-0.5, 0.5]
        /// </summary>
        public NetworkVariable<float> MaskRadius = new(0.5f);
        /// <summary>
        /// Sigma parameter of mask
        /// </summary>
        public NetworkVariable<float> MaskSigma = new(0.15f);
        /// <summary>
        /// Luminance of grating in [0, 1]
        /// </summary>
        public NetworkVariable<float> Luminance = new(0.5f);
        /// <summary>
        /// Michelson contrast of grating in [0, 1]
        /// </summary>
        public NetworkVariable<float> Contrast = new(1f);
        /// <summary>
        /// Spatial Frequency in cycle/degree
        /// </summary>
        public NetworkVariable<float> SpatialFreq0 = new(0.2f);
        public NetworkVariable<float> SpatialFreq1 = new(0.2f);
        /// <summary>
        /// Temporal Frequency in cycle/second
        /// </summary>
        public NetworkVariable<float> TemporalFreq0 = new(1f);
        public NetworkVariable<float> TemporalFreq1 = new(1f);
        public NetworkVariable<float> ModulateTemporalFreq0 = new(0.2f);
        public NetworkVariable<float> ModulateTemporalFreq1 = new(0.2f);
        public NetworkVariable<float> ModulateTemporalPhase0 = new(0f);
        public NetworkVariable<float> ModulateTemporalPhase1 = new(0f);
        /// <summary>
        /// Spatial Phase in [0, 1]
        /// </summary>
        public NetworkVariable<float> SpatialPhase0 = new(0f);
        public NetworkVariable<float> SpatialPhase1 = new(0f);
        /// <summary>
        /// minimum color of the grating
        /// </summary>
        public NetworkVariable<Color> MinColor = new(Color.black);
        /// <summary>
        /// maximum color of the grating
        /// </summary>
        public NetworkVariable<Color> MaxColor = new(Color.white);
        public NetworkVariable<bool> PauseTime = new(false);
        public NetworkVariable<bool> PauseModulateTime = new(true);
        public NetworkVariable<WaveType> GratingType0 = new(WaveType.Square);
        public NetworkVariable<WaveType> GratingType1 = new(WaveType.Square);
        public NetworkVariable<WaveType> ModulateType0 = new(WaveType.Square);
        public NetworkVariable<WaveType> ModulateType1 = new(WaveType.Square);
        public NetworkVariable<bool> ReverseTime = new(false);
        public NetworkVariable<float> Duty0 = new(0.5f);
        public NetworkVariable<float> Duty1 = new(0.5f);
        public NetworkVariable<float> ModulateDuty0 = new(0.5f);
        public NetworkVariable<float> ModulateDuty1 = new(0.5f);
        public NetworkVariable<float> TimeSecond = new(0f);
        public NetworkVariable<float> ModulateTimeSecond = new(0f);
        public NetworkVariable<Vector3> PositionOffset = new(Vector3.zero);

        double timeatreverse = 0;
        // timers are used to calculate gratings visuals, so here use frame time
        Timer timer = new(true);
        Timer mtimer = new(true);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Rotation.OnValueChanged += OnRotation;
            RotationOffset.OnValueChanged += OnRotationOffset;
            Ori0.OnValueChanged += OnOri0;
            Ori1.OnValueChanged += OnOri1;
            OriOffset0.OnValueChanged += OnOriOffset0;
            OriOffset1.OnValueChanged += OnOriOffset1;
            Size.OnValueChanged += OnSize;
            Diameter.OnValueChanged += OnDiameter;
            MaskType.OnValueChanged += OnMaskType;
            MaskRadius.OnValueChanged += OnMaskRadius;
            MaskSigma.OnValueChanged += OnMaskSigma;
            Luminance.OnValueChanged += OnLuminance;
            Contrast.OnValueChanged += OnContrast;
            SpatialFreq0.OnValueChanged += OnSpatialFreq0;
            SpatialFreq1.OnValueChanged += OnSpatialFreq1;
            TemporalFreq0.OnValueChanged += OnTemporalFreq0;
            TemporalFreq1.OnValueChanged += OnTemporalFreq1;
            ModulateTemporalFreq0.OnValueChanged += OnModulateTemporalFreq0;
            ModulateTemporalFreq1.OnValueChanged += OnModulateTemporalFreq1;
            ModulateTemporalPhase0.OnValueChanged += OnModulateTemporalPhase0;
            ModulateTemporalPhase1.OnValueChanged += OnModulateTemporalPhase1;
            SpatialPhase0.OnValueChanged += OnSpatialPhase0;
            SpatialPhase1.OnValueChanged += OnSpatialPhase1;
            MinColor.OnValueChanged += OnMinColor;
            MaxColor.OnValueChanged += OnMaxColor;
            PauseTime.OnValueChanged += OnPauseTime;
            PauseModulateTime.OnValueChanged += OnPauseModulateTime;
            GratingType0.OnValueChanged += OnGratingType0;
            GratingType1.OnValueChanged += OnGratingType1;
            ModulateType0.OnValueChanged += OnModulateType0;
            ModulateType1.OnValueChanged += OnModulateType1;
            ReverseTime.OnValueChanged += OnReverseTime;
            Duty0.OnValueChanged += OnDuty0;
            Duty1.OnValueChanged += OnDuty1;
            ModulateDuty0.OnValueChanged += OnModulateDuty0;
            ModulateDuty1.OnValueChanged += OnModulateDuty1;
            TimeSecond.OnValueChanged += OnTimeSecond;
            ModulateTimeSecond.OnValueChanged += OnModulateTimeSecond;
            PositionOffset.OnValueChanged += OnPositionOffset;

            timer.Start();
            mtimer.Start();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PositionOffset.OnValueChanged -= OnPositionOffset;
            Rotation.OnValueChanged -= OnRotation;
            RotationOffset.OnValueChanged -= OnRotationOffset;
            Ori0.OnValueChanged -= OnOri0;
            Ori1.OnValueChanged -= OnOri1;
            OriOffset0.OnValueChanged -= OnOriOffset0;
            OriOffset1.OnValueChanged -= OnOriOffset1;
            Size.OnValueChanged -= OnSize;
            Diameter.OnValueChanged -= OnDiameter;
            MaskType.OnValueChanged -= OnMaskType;
            MaskRadius.OnValueChanged -= OnMaskRadius;
            MaskSigma.OnValueChanged -= OnMaskSigma;
            Luminance.OnValueChanged -= OnLuminance;
            Contrast.OnValueChanged -= OnContrast;
            SpatialFreq0.OnValueChanged -= OnSpatialFreq0;
            SpatialFreq1.OnValueChanged -= OnSpatialFreq1;
            TemporalFreq0.OnValueChanged -= OnTemporalFreq0;
            TemporalFreq1.OnValueChanged -= OnTemporalFreq1;
            ModulateTemporalFreq0.OnValueChanged -= OnModulateTemporalFreq0;
            ModulateTemporalFreq1.OnValueChanged -= OnModulateTemporalFreq1;
            ModulateTemporalPhase0.OnValueChanged -= OnModulateTemporalPhase0;
            ModulateTemporalPhase1.OnValueChanged -= OnModulateTemporalPhase1;
            SpatialPhase0.OnValueChanged -= OnSpatialPhase0;
            SpatialPhase1.OnValueChanged -= OnSpatialPhase1;
            MinColor.OnValueChanged -= OnMinColor;
            MaxColor.OnValueChanged -= OnMaxColor;
            PauseTime.OnValueChanged -= OnPauseTime;
            PauseModulateTime.OnValueChanged -= OnPauseModulateTime;
            GratingType0.OnValueChanged -= OnGratingType0;
            GratingType1.OnValueChanged -= OnGratingType1;
            ModulateType0.OnValueChanged -= OnModulateType0;
            ModulateType1.OnValueChanged -= OnModulateType1;
            ReverseTime.OnValueChanged -= OnReverseTime;
            Duty0.OnValueChanged -= OnDuty0;
            Duty1.OnValueChanged -= OnDuty1;
            ModulateDuty0.OnValueChanged -= OnModulateDuty0;
            ModulateDuty1.OnValueChanged -= OnModulateDuty1;
            TimeSecond.OnValueChanged -= OnTimeSecond;
            ModulateTimeSecond.OnValueChanged -= OnModulateTimeSecond;
            PositionOffset.OnValueChanged -= OnPositionOffset;
        }

        void OnRotation(Vector3 p, Vector3 c)
        {
            transform.localEulerAngles = c + RotationOffset.Value;
        }

        void OnRotationOffset(Vector3 p, Vector3 c)
        {
            transform.localEulerAngles = Rotation.Value + c;
        }

        protected override void OnPosition(Vector3 p, Vector3 c)
        {
            transform.localPosition = c + PositionOffset.Value;
        }

        void OnPositionOffset(Vector3 p, Vector3 c)
        {
            transform.localPosition = Position.Value + c;
        }

        void OnOri0(float p, float c)
        {
            var theta = c + OriOffset0.Value;
            renderer.material.SetFloat("_Ori0", Mathf.Deg2Rad * theta);
        }

        void OnOri1(float p, float c)
        {
            var theta = c + OriOffset1.Value;
            renderer.material.SetFloat("_Ori1", Mathf.Deg2Rad * theta);
        }

        void OnOriOffset0(float p, float c)
        {
            var theta = c + Ori0.Value;
            renderer.material.SetFloat("_Ori0", Mathf.Deg2Rad * theta);
        }

        void OnOriOffset1(float p, float c)
        {
            var theta = c + Ori1.Value;
            renderer.material.SetFloat("_Ori1", Mathf.Deg2Rad * theta);
        }

        void OnSize(Vector3 p, Vector3 c)
        {
            transform.localScale = c;
            renderer.material.SetVector("_Size", (Vector2)c);
        }

        void OnDiameter(float p, float c)
        {
            if (IsServer)
            {
                // Only Server has write permission of NetworkVariable
                Size.Value = new Vector3(c, c, c);
            }
        }

        void OnMaskType(MaskType p, MaskType c)
        {
            renderer.material.SetInt("_MaskType", (int)c);
        }

        void OnMaskRadius(float p, float c)
        {
            renderer.material.SetFloat("_MaskRadius", c);
        }

        void OnMaskSigma(float p, float c)
        {
            renderer.material.SetFloat("_MaskSigma", c);
        }

        protected override void OnVisible(bool p, bool c)
        {
            // reset time when reappear
            if (c)
            {
                timeatreverse = 0;
                OnTimeSecond(0f, 0f);
                OnModulateTimeSecond(0f, 0f);
                if (PauseTime.Value) { timer.Reset(); }
                else { timer.Restart(); }
            }
            base.OnVisible(p, c);
        }

        void OnLuminance(float p, float c)
        {
            Color _mincolor, _maxcolor;
            Base.LuminanceSpan(c, Contrast.Value).ScaleColor(MinColor.Value, MaxColor.Value, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_MinColor", _mincolor);
            renderer.material.SetColor("_MaxColor", _maxcolor);
        }

        void OnContrast(float p, float c)
        {
            Color _mincolor, _maxcolor;
            Base.LuminanceSpan(Luminance.Value, c).ScaleColor(MinColor.Value, MaxColor.Value, out _mincolor, out _maxcolor);

            renderer.material.SetColor("_MinColor", _mincolor);
            renderer.material.SetColor("_MaxColor", _maxcolor);
        }

        void OnSpatialFreq0(float p, float c)
        {
            renderer.material.SetFloat("_SF0", c);
        }

        void OnSpatialFreq1(float p, float c)
        {
            renderer.material.SetFloat("_SF1", c);
        }

        void OnTemporalFreq0(float p, float c)
        {
            renderer.material.SetFloat("_TF0", c);
        }

        void OnTemporalFreq1(float p, float c)
        {
            renderer.material.SetFloat("_TF1", c);
        }

        void OnModulateTemporalFreq0(float p, float c)
        {
            renderer.material.SetFloat("_MTF0", c);
        }

        void OnModulateTemporalFreq1(float p, float c)
        {
            renderer.material.SetFloat("_MTF1", c);
        }

        void OnModulateTemporalPhase0(float p, float c)
        {
            renderer.material.SetFloat("_MPhase0", c);
        }

        void OnModulateTemporalPhase1(float p, float c)
        {
            renderer.material.SetFloat("_MPhase1", c);
        }

        void OnSpatialPhase0(float p, float c)
        {
            renderer.material.SetFloat("_Phase0", c);
        }

        void OnSpatialPhase1(float p, float c)
        {
            renderer.material.SetFloat("_Phase1", c);
        }

        void OnMinColor(Color p, Color c)
        {
            renderer.material.SetColor("_MinColor", c);
        }

        void OnMaxColor(Color p, Color c)
        {
            renderer.material.SetColor("_MaxColor", c);
        }

        void OnPauseTime(bool p, bool c)
        {
            if (c) { timer.Stop(); }
            else { timer.Start(); }
        }

        void OnPauseModulateTime(bool p, bool c)
        {
            if (c) { mtimer.Stop(); }
            else { mtimer.Start(); }
        }

        void OnGratingType0(WaveType p, WaveType c)
        {
            renderer.material.SetInt("_GratingType0", (int)c);
        }

        void OnGratingType1(WaveType p, WaveType c)
        {
            renderer.material.SetInt("_GratingType1", (int)c);
        }

        void OnModulateType0(WaveType p, WaveType c)
        {
            renderer.material.SetInt("_MGratingType0", (int)c);
        }

        void OnModulateType1(WaveType p, WaveType c)
        {
            renderer.material.SetInt("_MGratingType1", (int)c);
        }

        void OnDuty0(float p, float c)
        {
            renderer.material.SetFloat("_Duty0", c);
        }

        void OnDuty1(float p, float c)
        {
            renderer.material.SetFloat("_Duty1", c);
        }

        void OnModulateDuty0(float p, float c)
        {
            renderer.material.SetFloat("_MDuty0", c);
        }

        void OnModulateDuty1(float p, float c)
        {
            renderer.material.SetFloat("_MDuty1", c);
        }

        void OnReverseTime(bool p, bool c)
        {
            timeatreverse = GetTimeSecond;
            if (PauseTime.Value) { timer.Reset(); }
            else { timer.Restart(); }
        }

        double GetTimeSecond
        {
            get { return ReverseTime.Value ? timeatreverse - timer.ElapsedSecond : timeatreverse + timer.ElapsedSecond; }
        }

        void OnTimeSecond(float p, float c)
        {
            renderer.material.SetFloat("_T", c);
        }

        void OnModulateTimeSecond(float p, float c)
        {
            renderer.material.SetFloat("_MT", c);
        }

        /// <summary>
        /// automatically update grating time every frame
        /// </summary>
        void LateUpdate()
        {
            if (!PauseTime.Value)
            {
                OnTimeSecond(0f, (float)GetTimeSecond);
            }
            if (!PauseModulateTime.Value)
            {
                OnModulateTimeSecond(0f, (float)mtimer.ElapsedSecond);
            }
        }
    }
}