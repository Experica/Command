/*
ExperimentLogic.cs is part of the VLAB project.
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
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace VLab
{
    public class ExperimentLogic : MonoBehaviour
    {
        public Dictionary<VLCFG, object> config;
        public Experiment ex = new Experiment();
        public VLTimer timer = new VLTimer();

        public Action OnBeginStartExperiment, OnEndStartExperiment,
            OnBeginStopExperiment, OnEndStopExperiment,
            OnBeginPauseExperiment, OnEndPauseExperiment,
            OnBeginResumeExperiment, OnEndResumeExpeirment;
        public Action<bool> OnConditionPrepared;

        public EnvironmentManager envmanager = new EnvironmentManager();
        public ConditionManager condmanager = new ConditionManager();
        public CondTestManager condtestmanager = new CondTestManager();
        public RecordManager recordmanager = new RecordManager();

        public bool islogicactive = false;
        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime, PreIBIOnTime, BlockOnTime, SufIBIOnTime;
        public double PreICIHold { get { return timer.ElapsedMillisecond - PreICIOnTime; } }
        public double CondHold { get { return timer.ElapsedMillisecond - CondOnTime; } }
        public double SufICIHold { get { return timer.ElapsedMillisecond - SufICIOnTime; } }
        public double PreITIHold { get { return timer.ElapsedMillisecond - PreITIOnTime; } }
        public double TrialHold { get { return timer.ElapsedMillisecond - TrialOnTime; } }
        public double SufITIHold { get { return timer.ElapsedMillisecond - SufITIOnTime; } }
        public double PreIBIHold { get { return timer.ElapsedMillisecond - PreIBIOnTime; } }
        public double BlockHold { get { return timer.ElapsedMillisecond - BlockOnTime; } }
        public double SufIBIHold { get { return timer.ElapsedMillisecond - SufIBIOnTime; } }

        private CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState
        {
            get { return condstate; }
            set
            {
                if (value != condstate)
                {
                    OnEnterCondState(value);
                    condstate = value;
                }
            }
        }
        public virtual void OnEnterCondState(CONDSTATE value)
        {
            switch (value)
            {
                case CONDSTATE.PREICI:
                    PreICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREICI)
                    {
                        condtestmanager.NewCondTest(PreICIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.CONDSTATE, value.ToString(), PreICIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREICI)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StartStopExperiment(false);
                            return;
                        }
                        else
                        {
                            SamplePushCondition();
                        }
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    break;
                case CONDSTATE.COND:
                    CondOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.CONDSTATE, value.ToString(), CondOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.COND)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StartStopExperiment(false);
                            return;
                        }
                        else
                        {
                            SamplePushCondition();
                        }
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    break;
                case CONDSTATE.SUFICI:
                    SufICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.CONDSTATE, value.ToString(), SufICIOnTime);
                    }
                    break;
            }
        }

        private TRIALSTATE trialstate = TRIALSTATE.NONE;
        public TRIALSTATE TrialState
        {
            get { return trialstate; }
            set
            {
                if (value != trialstate)
                {
                    OnEnterTrialState(value);
                    trialstate = value;
                }
            }
        }
        public virtual void OnEnterTrialState(TRIALSTATE value)
        {
            switch (value)
            {
                case TRIALSTATE.PREITI:
                    PreITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREITI)
                    {
                        condtestmanager.NewCondTest(PreITIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.TRIALSTATE, value.ToString(), PreITIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREITI)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StartStopExperiment(false);
                            return;
                        }
                        else
                        {
                            SamplePushCondition();
                        }
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    break;
                case TRIALSTATE.TRIAL:
                    TrialOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.TRIALSTATE, value.ToString(), TrialOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.TRIAL)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StopExperiment();
                            return;
                        }
                        else
                        {
                            SamplePushCondition();
                        }
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.AddToCondTest(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    break;
                case TRIALSTATE.SUFITI:
                    SufITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.TRIALSTATE, value.ToString(), SufITIOnTime);
                    }
                    break;
            }
        }

        private BLOCKSTATE blockstate = BLOCKSTATE.NONE;
        public virtual BLOCKSTATE BlockState
        {
            get { return blockstate; }
            set
            {
                if (value != blockstate)
                {
                    OnEnterBlockState(value);
                    blockstate = value;
                }
            }
        }
        public virtual void OnEnterBlockState(BLOCKSTATE value)
        {
            switch (value)
            {
                case BLOCKSTATE.PREIBI:
                    PreIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.BLOCKSTATE, value.ToString(), PreIBIOnTime);
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    BlockOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.BLOCKSTATE, value.ToString(), BlockOnTime);
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    SufIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEventToCondTest(CONDTESTPARAM.BLOCKSTATE, value.ToString(), SufIBIOnTime);
                    }
                    break;
            }
        }

        private EXPERIMENTSTATE experimentstate = EXPERIMENTSTATE.NONE;
        public virtual EXPERIMENTSTATE ExperimentState
        {
            get { return experimentstate; }
            set
            {
                switch (experimentstate)
                {
                    // Previous State
                    case EXPERIMENTSTATE.NONE:
                        switch (value)
                        {
                            // Enter State
                            case EXPERIMENTSTATE.EXPERIMENT:
                                break;
                        }
                        break;
                    // Previous State
                    case EXPERIMENTSTATE.EXPERIMENT:
                        switch (value)
                        {
                            // Enter State
                            case EXPERIMENTSTATE.NONE:
                                break;
                        }
                        break;
                }
                experimentstate = value;
            }
        }

        public virtual void PrepareCondition()
        {
            if (condmanager.cond == null)
            {
                var cond = condmanager.ReadCondition(ex.CondPath);
                if (cond != null)
                {
                    cond = cond.ResolveConditionReference(ex.Param).FactorLevelOfDesign();

                    if (cond.ContainsKey("factorlevel") && cond["factorlevel"].Count == 0)
                    {
                        cond = cond.OrthoCondOfFactorLevel();
                    }
                    condmanager.TrimCondition(cond);
                }
            }
            if (condmanager.ncond > 0)
            {
                ex.Cond = condmanager.cond;
                condmanager.UpdateSampleSpace(ex.CondSampling, ex.BlockParam, ex.BlockSampling);
                OnConditionPrepared(true);
            }
        }

        public virtual void SamplePushCondition(bool isautosampleblock = true)
        {
            condmanager.PushCondition(condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, isautosampleblock), envmanager);
        }

        public virtual void SamplePushBlock()
        {
            condmanager.PushBlock(condmanager.SampleBlockSpace(), envmanager);
        }

        public virtual string DataPath()
        {
            return ex.GetDataPath();
        }

        public virtual void SaveData()
        {
            var ct = condtestmanager.condtest;
            if (ct.Count > 0)
            {
                ex.CondTest = ct;
                ex.EnvParam = envmanager.GetParams();

                Yaml.WriteYaml(DataPath(), ex, false);
                ex.DataPath = null;
            }
        }

        public virtual object GetEnvActiveParam(string name)
        {
            return envmanager.GetActiveParam(name);
        }

        public virtual void SetEnvActiveParam(string name, object value, bool notifyui = true)
        {
            envmanager.SetActiveParam(name, value, notifyui);
        }

        public virtual void WaitSetEnvActiveParam(float waittime_ms, string name, object value, bool notifyui = false)
        {
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(waittime_ms, name, value, notifyui));
        }

        IEnumerator WaitSetEnvActiveParam_Coroutine(float waittime_ms, string name, object value, bool notifyui = false)
        {
            waittime_ms /= 1000;
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < waittime_ms)
            {
                yield return null;
            }
            envmanager.SetActiveParam(name, value, notifyui);
        }

        public virtual void SetEnvActiveParamTwice(string name, object value1, float interval_ms, object value2, bool notifyui = false)
        {
            SetEnvActiveParamTwice(name, value1, interval_ms, name, value2, notifyui);
        }

        public virtual void SetEnvActiveParamTwice(string name1, object value1, float interval_ms, string name2, object value2, bool notifyui = false)
        {
            envmanager.SetActiveParam(name1, value1, notifyui);
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(interval_ms, name2, value2, notifyui));
        }

        public virtual void PauseResumeExperiment(bool ispause)
        {
            if (ispause)
            {
                OnBeginPauseExperiment();
                PauseExperiment();
                OnEndPauseExperiment();
            }
            else
            {
                OnBeginResumeExperiment();
                ResumeExperiment();
                OnEndResumeExpeirment();
            }
        }

        protected virtual void PauseExperiment()
        {
            islogicactive = false;
            Time.timeScale = 0;
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            BlockState = BLOCKSTATE.NONE;
        }

        protected virtual void ResumeExperiment()
        {
            Time.timeScale = 1;
            islogicactive = true;
        }

        public virtual void StartStopExperiment(bool isstart)
        {
            if (isstart)
            {
                OnBeginStartExperiment();
                StartExperiment();
                OnEndStartExperiment();
            }
            else
            {
                OnBeginStopExperiment();
                StopExperiment();
                OnEndStopExperiment();
            }
        }

        protected virtual void StartExperiment()
        {
            ExperimentState = EXPERIMENTSTATE.EXPERIMENT;
            condtestmanager.Clear();
            PrepareCondition();
            timer.Restart();
            islogicactive = true;
        }

        protected virtual void StopExperiment()
        {
            islogicactive = false;
            // Nofity any condtest left
            if (ex.NotifyPerCondTest > 0 && condtestmanager.condtestidx > 0)
            {
                condtestmanager.NotifyCondTestAndEnd(condtestmanager.notifyidx, ex.NotifyParam, timer.ElapsedMillisecond);
            }
            timer.Stop();
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            BlockState = BLOCKSTATE.NONE;
            ExperimentState = EXPERIMENTSTATE.NONE;
        }

        void Awake()
        {
            OnAwake();
        }
        public virtual void OnAwake()
        {
        }

        void Start()
        {
            OnStart();
        }
        public virtual void OnStart()
        {

        }

        void Update()
        {
            OnUpdate();
        }
        public virtual void OnUpdate()
        {
            if (ex.Input == InputMethod.Joystick && Input.GetJoystickNames().Count() > 0 && envmanager.activenet.Count > 0)
            {
                var jx = Input.GetAxis("JX");
                var jy = Input.GetAxis("JY");
                var jz = Input.GetAxis("JZ");
                var jxr = Input.GetAxis("JXR");
                var jyr = Input.GetAxis("JYR");
                var jrb = Input.GetAxis("JRB");
                if (jx != 0 || jy != 0)
                {
                    if (envmanager.maincamera != null)
                    {
                        var po = envmanager.GetParam("Position");
                        if (po != null)
                        {
                            var p = (Vector3)po;
                            var hh = envmanager.maincamera.orthographicSize;
                            var hw = hh * envmanager.maincamera.aspect;
                            envmanager.SetParam("Position", new Vector3(
                            Mathf.Clamp(p.x + Mathf.Pow(jx, 3), -hw, hw),
                            Mathf.Clamp(p.y + Mathf.Pow(jy, 3), -hh, hh),
                            p.z), true);
                        }
                    }
                }
                if (jz != 0)
                {
                    var oo = envmanager.GetParam("Ori");
                    if (oo != null)
                    {
                        var no = ((float)oo + Mathf.Pow(jz, 3) * 4) % 360f;
                        envmanager.SetParam("Ori", no < 0 ? 360f - no : no, true);
                    }
                }
                if (jxr != 0 || jyr != 0)
                {
                    var so = envmanager.GetParam("Size");
                    var dio = envmanager.GetParam("Diameter");

                    if (jrb > 0.5)
                    {
                        if (dio != null)
                        {
                            var d = (float)dio;
                            envmanager.SetParam("Diameter", Mathf.Max(0, d + Mathf.Pow(jxr, 3)), true);
                        }
                    }
                    else
                    {
                        if (so != null)
                        {
                            var s = (Vector3)so;
                            envmanager.SetParam("Size", new Vector3(
                                Mathf.Max(0, s.x + Mathf.Pow(jxr, 3)),
                                Mathf.Max(0, s.y + Mathf.Pow(jyr, 3)),
                                s.z), true);
                        }
                    }
                }
            }

            if (islogicactive)
            {
                Logic();
            }
        }

        public virtual void Logic()
        {
        }

    }
}