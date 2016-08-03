// -----------------------------------------------------------------------------
// ExperimentLogic.cs is part of the VLAB project.
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
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace VLab
{
    public class ExperimentLogic : MonoBehaviour
    {
        public Experiment ex = new Experiment();
        public VLTimer timer = new VLTimer();

        public Action OnBeginStartExperiment, OnEndStartExperiment,
            OnBeginStopExperiment, OnEndStopExperiment,
            OnBeginPauseExperiment, OnEndPauseExperiment,
            OnBeginResumeExperiment, OnEndResumeExpeirment;

        public EnvironmentManager envmanager = new EnvironmentManager();
        public ConditionManager condmanager = new ConditionManager();
        public CondTestManager condtestmanager = new CondTestManager();
        public RecordManager recordmanager = new RecordManager();

        public bool islogicactive = false;
        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime, PreIBIOnTime, BlockOnTime, SufIBIOnTime;
        public double PreICIHold { get { return timer.ElapsedMS - PreICIOnTime; } }
        public double CondHold { get { return timer.ElapsedMS - CondOnTime; } }
        public double SufICIHold { get { return timer.ElapsedMS - SufICIOnTime; } }
        public double PreITIHold { get { return timer.ElapsedMS - PreITIOnTime; } }
        public double TrialHold { get { return timer.ElapsedMS - TrialOnTime; } }
        public double SufITIHold { get { return timer.ElapsedMS - SufITIOnTime; } }
        public double PreIBIHold { get { return timer.ElapsedMS - PreIBIOnTime; } }
        public double BlockHold { get { return timer.ElapsedMS - BlockOnTime; } }
        public double SufIBIHold { get { return timer.ElapsedMS - SufIBIOnTime; } }

        private CONDSTATE condstate = CONDSTATE.NONE;
        public virtual CONDSTATE CondState
        {
            get { return condstate; }
            set
            {
                if (value != condstate)
                {
                    switch (value)
                    {
                        case CONDSTATE.PREICI:
                            PreICIOnTime = timer.ElapsedMS;
                            if (ex.CondTestAtState == CONDTESTATSTATE.PREICI)
                            {
                                condtestmanager.NewCondTest(PreICIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
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
                                    condmanager.SamplePushCondition(envmanager);
                                }
                                if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                                {
                                    condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                                    condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                                }
                            }
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.CONDSTATE, value.ToString(), PreICIOnTime);
                            }
                            break;
                        case CONDSTATE.COND:
                            CondOnTime = timer.ElapsedMS;
                            if (ex.PushCondAtState == PUSHCONDATSTATE.COND)
                            {
                                if (condmanager.IsCondRepeat(ex.CondRepeat))
                                {
                                    StopExperiment();
                                    return;
                                }
                                else
                                {
                                    condmanager.SamplePushCondition(envmanager);
                                }
                                if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                                {
                                    condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                                    condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                                }
                            }
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.CONDSTATE, value.ToString(), CondOnTime);
                            }
                            break;
                        case CONDSTATE.SUFICI:
                            SufICIOnTime = timer.ElapsedMS;
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.CONDSTATE, value.ToString(), SufICIOnTime);
                            }
                            break;
                    }
                    condstate = value;
                }
            }
        }

        private TRIALSTATE trialstate = TRIALSTATE.NONE;
        public virtual TRIALSTATE TrialState
        {
            get { return trialstate; }
            set
            {
                if (value != trialstate)
                {
                    switch (value)
                    {
                        case TRIALSTATE.PREITI:
                            PreITIOnTime = timer.ElapsedMS;
                            if (ex.CondTestAtState == CONDTESTATSTATE.PREITI)
                            {
                                condtestmanager.NewCondTest(PreITIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
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
                                    condmanager.SamplePushCondition(envmanager);
                                }
                                if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                                {
                                    condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                                    condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                                }
                            }
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.TRIALSTATE, value.ToString(), PreITIOnTime);
                            }
                            break;
                        case TRIALSTATE.TRIAL:
                            TrialOnTime = timer.ElapsedMS;
                            if (ex.PushCondAtState == PUSHCONDATSTATE.TRIAL)
                            {
                                if (condmanager.IsCondRepeat(ex.CondRepeat))
                                {
                                    StopExperiment();
                                    return;
                                }
                                else
                                {
                                    condmanager.SamplePushCondition(envmanager);
                                }
                                if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                                {
                                    condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                                    condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
                                }
                            }
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.TRIALSTATE, value.ToString(), TrialOnTime);
                            }
                            break;
                        case TRIALSTATE.SUFITI:
                            SufITIOnTime = timer.ElapsedMS;
                            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.AddEvent(CONDTESTPARAM.TRIALSTATE, value.ToString(), SufITIOnTime);
                            }
                            break;
                    }
                    trialstate = value;
                }
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
                    switch (value)
                    {
                        case BLOCKSTATE.PREIBI:
                            break;
                        case BLOCKSTATE.BLOCK:
                            break;
                        case BLOCKSTATE.SUFIBI:
                            break;
                    }
                    blockstate = value;
                }
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
            condmanager.UpdateSampleSpace(ex.CondSampling, true);
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
                ex.Cond = condmanager.cond;
                ex.CondTest = ct;
                ex.EnvParam = envmanager.GetParams();

                Yaml.WriteYaml(DataPath(), ex);
            }
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
            timer.ReStart();
            islogicactive = true;
        }

        protected virtual void StopExperiment()
        {
            islogicactive = false;
            // Nofity any condtest left
            condtestmanager.NotifyCondTestAndEnd(condtestmanager.notifyidx, ex.NotifyParam, timer.ElapsedMS);
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
                if (jx !=0 || jy !=0)
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
                            Mathf.Clamp(p.x +Mathf.Pow( jx,3), -hw, hw),
                            Mathf.Clamp(p.y +Mathf.Pow( jy,3), -hh, hh),
                            p.z),true);
                        }
                    }
                }
                if (jz != 0)
                {
                    var oo = envmanager.GetParam("Ori");
                    if (oo != null)
                    {
                        var no = ((float)oo + Mathf.Pow(jz, 3) * 4) % 360f;
                        envmanager.SetParam("Ori",no<0?360f-no:no ,true);
                    }
                }
                if (jxr != 0 || jyr != 0)
                {
                    var so = envmanager.GetParam("Size");
                    var dio = envmanager.GetParam("Diameter");

                    if(jrb>0.5)
                    {
                        if (dio != null)
                        {
                            var d = (float)dio;
                            envmanager.SetParam("Diameter",Mathf.Max(0, d +Mathf.Pow( jxr,3)),true);
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
                                s.z),true);
                        }
                    }
                }
            }
        }

        void FixedUpdate()
        {
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