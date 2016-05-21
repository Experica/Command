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
using XippmexDotNet;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET;
using MathWorks.MATLAB.NET.Utility;
using MathNet.Numerics;

namespace VLab
{
    public class Experiment
    {
        public string name { get; set; }
        public string id { get; set; }
        public string designer { get; set; }
        public string experimenter { get; set; }
        public string log { get; set; }

        public string subject_species { get; set; }
        public string subject_name { get; set; }
        public string subject_id { get; set; }
        public string subject_gender { get; set; }
        public float subject_age { get; set; }
        public Vector3 subject_size { get; set; }
        public float subject_weight { get; set; }
        public string subject_log { get; set; }

        public string environmentpath { get; set; }
        public Dictionary<string, object> envparam { get; set; }
        public string condpath { get; set; }
        public Dictionary<string, List<object>> cond { get; set; }
        public string experimentlogicpath { get; set; }

        public string recordsession { get; set; }
        public string recordsite { get; set; }
        public string condtestdir { get; set; }
        public string condtestpath { get; set; }
        public Dictionary<string, List<object>> condtest { get; set; }
        public SampleMethod condsampling { get; set; }
        public int condrepeat { get; set; }
        public int input { get; set; }

        public double preICI { get; set; }
        public double conddur { get; set; }
        public double sufICI { get; set; }
        public double preITI { get; set; }
        public double trialdur { get; set; }
        public double sufITI { get; set; }
        public double preIBI { get; set; }
        public double blockdur { get; set; }
        public double sufIBI { get; set; }
        public PUSHCONDATSTATE pushcondatstate { get; set; }
        public CONDTESTATSTATE condtestatstate { get; set; }
        public Dictionary<string, object> param { get; set; }
        public List<string> exinheritparams { get; set; }
        public List<string> envinheritparams { get; set; }


        public static readonly Dictionary<string, PropertyInfo> properties;
        static Experiment()
        {
            properties = new Dictionary<string, PropertyInfo>();
            foreach (var p in typeof(Experiment).GetProperties())
            {
                properties[p.Name] = p;
            }
        }

        public void SetValue(string name, object value)
        {
            if (properties.ContainsKey(name))
            {
                SetValue(this, properties[name], value);
            }
        }

        public static void SetValue(Experiment ex, PropertyInfo p, object value)
        {
            p.SetValue(ex, VLConvert.Convert(value, p.PropertyType), null);
        }

        public object GetValue(string name)
        {
            object v = null;
            if (properties.ContainsKey(name))
            {
                v = GetValue(this, properties[name]);
            }
            return v;
        }

        public static object GetValue(Experiment ex, PropertyInfo p)
        {
            return p.GetValue(ex, null);
        }

        public virtual string CondTestPath()
        {
            var filename = subject_id + "_" + recordsession + "_" + recordsite + "_" + id + "_";
            if (string.IsNullOrEmpty(condtestdir))
            {
                condtestdir = Directory.GetCurrentDirectory();
            }
            else
            {
                if (!Directory.Exists(condtestdir))
                {
                    Directory.CreateDirectory(condtestdir);
                }
            }
            var fs = Directory.GetFiles(condtestdir, filename + "*.yaml", SearchOption.AllDirectories);
            if (fs.Length == 0)
            {
                filename = filename + "1.yaml";
            }
            else
            {
                var ns = new List<int>();
                foreach (var f in fs)
                {
                    var s = f.LastIndexOf('_') + 1;
                    var e = f.LastIndexOf('.') - 1;
                    ns.Add(int.Parse(f.Substring(s, e - s + 1)));
                }
                filename = filename + (ns.Max() + 1).ToString() + ".yaml";
            }
            var subjectdir = Path.Combine(condtestdir, subject_id);
            if (!Directory.Exists(subjectdir))
            {
                Directory.CreateDirectory(subjectdir);
            }
            condtestpath = Path.Combine(subjectdir, filename);
            return condtestpath;
        }
    }

    public enum CONDSTATE
    {
        NONE = 1,
        PREICI = 2,
        COND = 3,
        SUFICI = 4
    }

    public enum TRIALSTATE
    {
        NONE = 1001,
        PREITI = 1002,
        TRIAL = 1003,
        SUFITI = 1004
    }

    public enum BLOCKSTATE
    {
        NONE = 2001,
        PREIBI = 2002,
        BLOCK = 2003,
        SUFIBI = 2004
    }

    public enum EXPERIMENTSTATE
    {
        NONE = 3001,
        PREIEI = 3002,
        EXPERIMENT = 3003,
        SUFIEI = 3004
    }

    public enum PUSHCONDATSTATE
    {
        NONE = 0,
        PREICI = CONDSTATE.PREICI,
        COND = CONDSTATE.COND,
        PREITI = TRIALSTATE.PREITI,
        TRIAL = TRIALSTATE.TRIAL
    }

    public enum CONDTESTATSTATE
    {
        NONE = 0,
        PREICI = CONDSTATE.PREICI,
        PREITI = TRIALSTATE.PREITI,
    }


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
                            condtestmanager.NewCondTest();
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
                            condtestmanager.NewCondTest();
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
            timer.Stop();
        }

        public virtual void ResumeExperiment()
        {
            Time.timeScale = 1;
            timer.Start();
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
                    if (Input.GetAxis("JX") != 0 || Input.GetAxis("JY") != 0)
                    {
                        var p = (Vector3)envmanager.GetParam("position");


                        var hh = envmanager.maincamera.orthographicSize;
                        var hw = hh * envmanager.maincamera.aspect;

                        envmanager.SetParam("position", new Vector3(
                        Mathf.Clamp(p.x + Input.GetAxis("JX"), -hw, hw),
                        Mathf.Clamp(p.y + Input.GetAxis("JY"), -hh, hh),
                        p.z));

                        //var pmin = envmanager.maincamera.ScreenToWorldPoint(new Vector3(envmanager.maincamera.v));
                        //        p.z = envmanager.activenetbehavior.transform.position.z;
                        //        envmanager.activenetbehavior.position = p;
                    }
                }
            }
            //if (isplayercontrol)
            //{
            //    if (envmanager.activenetbehavior)
            //    {
            //        float r = Convert.ToSingle(Input.GetButton("Fire1"));
            //        float r1 = Convert.ToSingle(Input.GetButton("Fire2"));
            //        envmanager.activenetbehavior.ori += r - r1;
            //        envmanager.activenetbehavior.length += 0.1f * Input.GetAxis("Horizontal");
            //        envmanager.activenetbehavior.width += 0.1f * Input.GetAxis("Vertical");
            //        var p = envmanager.maincamera.ScreenToWorldPoint(Input.mousePosition);
            //        p.z = envmanager.activenetbehavior.transform.position.z;
            //        envmanager.activenetbehavior.position = p;
            //    }
            //}
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