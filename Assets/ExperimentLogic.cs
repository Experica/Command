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
using System.Text;
using System.Reflection;
using MathNet.Numerics;

namespace VLab
{
    public class ExperimentLogic : MonoBehaviour
    {
        public Experiment ex = new Experiment();
        public VLTimer timer = new VLTimer();

        public EnvironmentManager envmanager = new EnvironmentManager();
        public ConditionManager condmanager = new ConditionManager();
        public CondTestManager condtestmanager = new CondTestManager();
        public RecordManager recordmanager;

        public bool islogicactive = false;
        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime, PreIBIOnTime, BlockOnTime, SufIBIOnTime;

        public double PreICIHold()
        {
            return timer.ElapsedMS - PreICIOnTime;
        }
        public double CondHold()
        {
            return timer.ElapsedMS - CondOnTime;
        }
        public double SufICIHold()
        {
            return timer.ElapsedMS - SufICIOnTime;
        }
        public double PreITIHold()
        {
            return timer.ElapsedMS - PreITIOnTime;
        }
        public double TrialHold()
        {
            return timer.ElapsedMS - TrialOnTime;
        }
        public double SufITIHold()
        {
            return timer.ElapsedMS - SufITIOnTime;
        }
        public double PreIBIHold()
        {
            return timer.ElapsedMS - PreIBIOnTime;
        }
        public double BlockHold()
        {
            return timer.ElapsedMS - BlockOnTime;
        }
        public double SufIBIHold()
        {
            return timer.ElapsedMS - SufIBIOnTime;
        }

        private CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState
        {
            get { return condstate; }
            set
            {
                switch (value)
                {
                    case CONDSTATE.PREICI:
                        PreICIOnTime = timer.ElapsedMS;
                        if (ex.condtestatstate == CONDTESTATSTATE.PREICI)
                        {
                            condtestmanager.NewCondTest(PreICIOnTime,ex.condtestnotifyparams, ex.analysispercondtest);
                        }
                        if (ex.pushcondatstate == PUSHCONDATSTATE.PREICI)
                        {
                            if (condmanager.IsFinishRepeat(ex.condrepeat))
                            {
                                StopExperiment();
                                return;
                            }
                            else
                            {
                                condmanager.SamplePushCondition();
                            }
                            if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("CONDSTATE", value.ToString(), PreICIOnTime);
                        }
                        break;
                    case CONDSTATE.COND:
                        CondOnTime = timer.ElapsedMS;
                        if (ex.pushcondatstate == PUSHCONDATSTATE.COND)
                        {
                            if (condmanager.IsFinishRepeat(ex.condrepeat))
                            {
                                StopExperiment();
                                return;
                            }
                            else
                            {
                                condmanager.SamplePushCondition();
                            }
                            if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("CONDSTATE", value.ToString(), CondOnTime);
                        }
                        break;
                    case CONDSTATE.SUFICI:
                        SufICIOnTime = timer.ElapsedMS;
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("CONDSTATE", value.ToString(), SufICIOnTime);
                        }
                        break;
                }
                condstate = value;
            }
        }

        private TRIALSTATE trialstate = TRIALSTATE.NONE;
        public TRIALSTATE TrialState
        {
            get { return trialstate; }
            set
            {
                switch (value)
                {
                    case TRIALSTATE.PREITI:
                        PreITIOnTime = timer.ElapsedMS;
                        if (ex.condtestatstate == CONDTESTATSTATE.PREITI)
                        {
                            condtestmanager.NewCondTest(PreITIOnTime,ex.condtestnotifyparams, ex.analysispercondtest);
                        }
                        if (ex.pushcondatstate == PUSHCONDATSTATE.TRIAL)
                        {
                            if (condmanager.IsFinishRepeat(ex.condrepeat))
                            {
                                StopExperiment();
                                return;
                            }
                            else
                            {
                                condmanager.SamplePushCondition();
                            }
                            if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("TRIALSTATE", value.ToString(), PreITIOnTime);
                        }
                        break;
                    case TRIALSTATE.TRIAL:
                        TrialOnTime = timer.ElapsedMS;
                        if (ex.pushcondatstate == PUSHCONDATSTATE.TRIAL)
                        {
                            if (condmanager.IsFinishRepeat(ex.condrepeat))
                            {
                                StopExperiment();
                                return;
                            }
                            else
                            {
                                condmanager.SamplePushCondition();
                            }
                            if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("TRIALSTATE", value.ToString(), TrialOnTime);
                        }
                        break;
                    case TRIALSTATE.SUFITI:
                        SufITIOnTime = timer.ElapsedMS;
                        if (ex.condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("TRIALSTATE", value.ToString(), SufITIOnTime);
                        }
                        break;
                }
                trialstate = value;
            }
        }

        private BLOCKSTATE blockstate = BLOCKSTATE.NONE;
        public BLOCKSTATE BlockState
        {
            get { return blockstate; }
            set
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

        private EXPERIMENTSTATE experimentstate = EXPERIMENTSTATE.NONE;
        public EXPERIMENTSTATE ExperimentState
        {
            get { return experimentstate; }
            set
            {
                switch (value)
                {
                    case EXPERIMENTSTATE.NONE:
                        switch (experimentstate)
                        {
                            case EXPERIMENTSTATE.EXPERIMENT:
                                condtestmanager.NotifyCondTestAnalysis(condtestmanager.notifyidx, ex.condtestnotifyparams, timer.ElapsedMS);
                                SaveExperiment();
                                timer.Stop();
                                break;
                        }
                        break;
                    case EXPERIMENTSTATE.EXPERIMENT:
                        switch (experimentstate)
                        {
                            case EXPERIMENTSTATE.NONE:
                                PrepareCondition();
                                condtestmanager.Clear();
                                timer.ReStart();
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
                var cond = condmanager.ReadCondition(ex.condpath);

                foreach (var f in cond.Keys)
                {
                    if (cond[f].Count == 0)
                    {
                        if (f != "factorlevels")
                        {
                            cond.Remove(f);
                        }
                    }
                    else
                    {
                        if (((string)cond[f][0]).Contains("$"))
                        {
                            var p = ((string)cond[f][0]).Substring(1);
                            if (ex.param.ContainsKey(p) && ex.param[p] == null)
                            {
                                cond[f] = ex.param[p] as List<object>;
                            }
                            else
                            {
                                cond.Remove(f);
                            }
                        }
                        if ((string)cond[f][0] == "conditiondesign" && cond[f].Count > 1)
                        {
                            var d = Yaml.Deserialize<Dictionary<string, object>>((string)cond[f][1]);
                            var fl = new FactorLevel(f, d["start"], d["end"], d["step"], (DesignMethod)d["method"]);
                            cond[f] = fl.GenerateFactorLevels().Value;
                        }
                    }
                }

                if (cond.ContainsKey("factorlevels"))
                {
                    cond.Remove("factorlevels");
                    cond = ConditionDesigner.GenerateCondition(cond);
                }
                condmanager.TrimCondition(cond);
                ex.cond = cond;
            }
            condmanager.UpdateCondPopulation(ex.condsampling);
        }

        public virtual string CondTestPath()
        {
            return ex.CondTestPath();
        }

        public virtual void SaveExperiment()
        {
            ex.cond = condmanager.cond;
            ex.condtest = condtestmanager.condtest;
            ex.envparam = envmanager.GetParams();

            Yaml.WriteYaml(CondTestPath(), ex);
        }

        public virtual void PauseExperiment()
        {
            Time.timeScale = 0;
            islogicactive = false;
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            BlockState = BLOCKSTATE.NONE;
        }

        public virtual void ResumeExperiment()
        {
            Time.timeScale = 1;
            islogicactive = true;
        }

        public virtual void StartExperiment()
        {
            ExperimentState = EXPERIMENTSTATE.EXPERIMENT;
            islogicactive = true;
        }

        public virtual void StopExperiment()
        {
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            BlockState = BLOCKSTATE.NONE;
            ExperimentState = EXPERIMENTSTATE.NONE;
             islogicactive = false;
        }

        void Awake()
        {
            if (!VLTimer.IsHighResolution)
            {
                UnityEngine.Debug.LogWarning("This Machine Doesn't Have High Resolution Timer.");
            }
            condmanager.envmanager = envmanager;
            OnAwake();
        }
        public virtual void OnAwake()
        {
        }

        public virtual void OnServerSceneChanged(string scenename)
        {
            envmanager.AddScene(scenename);
            envmanager.SetParams(ex.envparam);
        }

        void Update()
        {
            OnUpdate();
        }
        public virtual void OnUpdate()
        {
            if (ex.input > 0 && Input.GetJoystickNames().Count() > 0)
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
                        if(envmanager.maincamera!=null)
                        {
                            var po = envmanager.GetParam("position");
                            if(po!=null)
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
                    if(jxr!=0||jyr!=0)
                    {
                        var wo = envmanager.GetParam("width");
                        var lo = envmanager.GetParam("length");
                        if(wo!=null)
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
                    if(jz!=0)
                    {
                        var oo = envmanager.GetParam("ori");
                        if(oo!=null)
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