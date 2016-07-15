// --------------------------------------------------------------
// ExperimentLogic.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MathNet.Numerics;

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
                // Previous State
                switch (experimentstate)
                {
                    case EXPERIMENTSTATE.NONE:
                        // Enter State
                        switch (value)
                        {
                            case EXPERIMENTSTATE.EXPERIMENT:
                                break;
                        }
                        break;
                    case EXPERIMENTSTATE.EXPERIMENT:
                        // Enter State
                        switch (value)
                        {
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
                cond = cond.ResolveConditionReference(ex.Param).FactorLevelOfDesign();

                if (cond.ContainsKey("factorlevel") && cond["factorlevel"].Count == 0)
                {
                    cond = cond.OrthoCondOfFactorLevel();
                }
                condmanager.TrimCondition(cond);
            }
            condmanager.UpdateSampleSpace(ex.CondSampling, true);
        }

        public virtual string DataPath()
        {
            return ex.GetDataPath();
        }

        public virtual void SaveData()
        {
            ex.Cond = condmanager.cond;
            ex.CondTest = condtestmanager.condtest;
            ex.EnvParam = envmanager.GetParams();

            Yaml.WriteYaml(DataPath(), ex);
        }

        public virtual void PauseResumeExperiment(bool ispause)
        {
            if(ispause)
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
            if(isstart)
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
            condtestmanager.NotifyCondTestEnd(condtestmanager.notifyidx, ex.NotifyParam, timer.ElapsedMS);
            timer.Stop();
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            BlockState = BLOCKSTATE.NONE;
            ExperimentState = EXPERIMENTSTATE.NONE;
        }

        public virtual void OnServerSceneChanged(string scenename)
        {
            envmanager.AddScene(scenename);
            envmanager.SetParams(ex.EnvParam);
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
            if (ex.Input > 0 && Input.GetJoystickNames().Count() > 0)
            {
                if (envmanager.activenet.Count > 0)
                {

                    var jx = Input.GetAxis("JX");
                    var jy = Input.GetAxis("JY");
                    var jz = Input.GetAxis("JZ");
                    var jxr = Input.GetAxis("JXR");
                    var jyr = Input.GetAxis("JYR");
                    if (jx != 0 || jy != 0)
                    {
                        if (envmanager.maincamera != null)
                        {
                            var po = envmanager.GetParam("position");
                            if (po != null)
                            {
                                var p = (Vector3)po;
                                var hh = envmanager.maincamera.orthographicSize;
                                var hw = hh * envmanager.maincamera.aspect;
                                envmanager.SetParam("position", new Vector3(
                                Mathf.Clamp(p.x + jx, -hw, hw),
                                Mathf.Clamp(p.y + jy, -hh, hh),
                                p.z));
                            }
                        }
                    }
                    if (jxr != 0 || jyr != 0)
                    {
                        var wo = envmanager.GetParam("width");
                        var lo = envmanager.GetParam("length");
                        if (wo != null)
                        {
                            var w = (float)wo;
                            envmanager.SetParam("width", w + jyr);
                        }
                        if (lo != null)
                        {
                            var l = (float)lo;
                            envmanager.SetParam("length", l + jxr);
                        }
                    }
                    if (jz != 0)
                    {
                        var oo = envmanager.GetParam("ori");
                        if (oo != null)
                        {
                            var o = (float)oo;
                            envmanager.SetParam("ori", o + jz);
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