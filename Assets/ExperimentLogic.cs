// --------------------------------------------------------------
// ExperimentLogic.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
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
        public string experimentlogic { get; set; }

        public string recordsession { get; set; }
        public string recordsite { get; set; }
        public string condtestdir { get; set; }
        public string condtestpath { get; set; }
        public Dictionary<string, List<object>> condtest { get; set; }
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

        public virtual Experiment DeepCopy()
        {
            var ex = new Experiment();
            foreach (var pn in properties.Keys)
            {
                ex.SetValue(pn, GetValue(pn));
            }
            return ex;
        }

        public virtual string CondTestPath()
        {
            var filename = subject_id + "_" + recordsession + "_" + recordsite + "_" + id + "_";
            var fs = Directory.GetFiles(condtestdir, filename + "*.yaml", SearchOption.AllDirectories);
            if (fs.Length == 0)
            {
                filename = filename + "1.yaml";
            }
            else
            {
                var ns = new List<int>();
                foreach (var p in fs)
                {
                    var s = p.LastIndexOf('_') + 1;
                    var e = p.LastIndexOf('.') - 1;
                    ns.Add(int.Parse(p.Substring(s, e - s + 1)));
                }
                filename = filename + (ns.Max() + 1).ToString() + ".yaml";
            }
            var dir = Path.Combine(condtestdir, subject_id);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            condtestpath = Path.Combine(dir, filename);
            return condtestpath;
        }
    }




    public class CondTestManager
    {
        public Dictionary<string, List<object>> condtest = new Dictionary<string, List<object>>();
        public int condtestidx = -1;

        public void NewCondTest()
        {
            condtestidx++;
        }

        public void Clear()
        {
            condtest.Clear();
            condtestidx = -1;
        }

        public void Add(string key, object value)
        {
            if (condtest.ContainsKey(key))
            {
                var vs = condtest[key];
                for (var i = vs.Count; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                vs.Add(value);
            }
            else
            {
                var vs = new List<object>();
                for (var i = 0; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                vs.Add(value);
                condtest[key] = vs;
            }
        }

        public void AddEvent(string key, string eventname, double timestamp)
        {
            if (condtest.ContainsKey(key))
            {
                var vs = condtest[key];
                for (var i = vs.Count; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                if (vs.Count < (condtestidx + 1))
                {
                    var es = new List<Dictionary<string, double>>();
                    var e = new Dictionary<string, double>();
                    e[eventname] = timestamp;
                    es.Add(e);
                    vs.Add(es);
                }
                else
                {
                    var es = (List<Dictionary<string, double>>)vs[condtestidx];
                    var e = new Dictionary<string, double>();
                    e[eventname] = timestamp;
                    es.Add(e);
                }
            }
            else
            {
                var vs = new List<object>();
                for (var i = 0; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                var es = new List<Dictionary<string, double>>();
                var e = new Dictionary<string, double>();
                e[eventname] = timestamp;
                es.Add(e);
                vs.Add(es);
                condtest[key] = vs;
            }
        }
    }


    public class RecordManager
    {
        XippmexDotNet.XippmexDotNet xippmex;
        public RecordManager(string recordsystem)
        {
            switch (recordsystem)
            {
                case "ripple":
                    xippmex = new XippmexDotNet.XippmexDotNet();
                    break;
            }
        }

        public void Help()
        {
            //MWArray[] output = new MWArray[1];
            //var output = xippmex.xippmex(1,new MWCharArray( "time"));
        }

        public void RecordPath()
        {
        }

    }

    public enum CONDSTATE
    {
        NONE = 0,
        PREICI = 1,
        COND = 2,
        SUFICI = 3
    }

    public enum TRIALSTATE
    {
        NONE = 1000,
        PREITI = 1001,
        TRIAL = 1002,
        SUFITI = 1003
    }

    public enum BLOCKSTATE
    {
        NONE = 2000,
        PREIBI = 2001,
        BLOCK = 2002,
        SUFIBI = 2003
    }

    public enum EXPERIMENTSTATE
    {
        NONE = 3000,
        PREIEI = 3001,
        EXPERIMENT = 3002,
        SUFIEI = 3003
    }

    public enum PUSHCONDATSTATE
    {
        NONE = -1,
        PREICI = CONDSTATE.PREICI,
        COND = CONDSTATE.COND,
        TRIAL = TRIALSTATE.TRIAL
    }

    public enum CONDTESTATSTATE
    {
        NONE = -1,
        PREICI = CONDSTATE.PREICI,
        PREITI = TRIALSTATE.PREITI
    }


    public class ExperimentLogic : MonoBehaviour
    {
        public Experiment ex = new Experiment();
        public Timer timer = new Timer();

        public EnvironmentManager envmanager = new EnvironmentManager();
        public ConditionManager condmanager = new ConditionManager();
        public CondTestManager condtestmanager = new CondTestManager();
        public RecordManager recordmanager;

        public bool islogicactive = false;

        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime;

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

        private PUSHCONDATSTATE pushcondatstate = PUSHCONDATSTATE.NONE;
        public PUSHCONDATSTATE PushCondAtState = PUSHCONDATSTATE.COND;
        private CONDTESTATSTATE condtestatstate = CONDTESTATSTATE.NONE;
        public CONDTESTATSTATE CondTestAtState = CONDTESTATSTATE.PREICI;

        private CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState
        {
            get { return condstate; }
            set
            {
                switch (value)
                {
                    case CONDSTATE.PREICI:
                        if (condtestatstate == CONDTESTATSTATE.PREICI)
                        {
                            condtestmanager.NewCondTest();
                        }
                        if (pushcondatstate == PUSHCONDATSTATE.PREICI)
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
                            if (condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        PreICIOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("CONDSTATE", value.ToString(), PreICIOnTime);
                        }
                        break;
                    case CONDSTATE.COND:
                        if (pushcondatstate == PUSHCONDATSTATE.COND)
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
                            if (condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        CondOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("CONDSTATE", value.ToString(), CondOnTime);
                        }
                        break;
                    case CONDSTATE.SUFICI:
                        SufICIOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
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
                        if (condtestatstate == CONDTESTATSTATE.PREITI)
                        {
                            condtestmanager.NewCondTest();
                        }
                        PreITIOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("TRIALSTATE", value.ToString(), PreITIOnTime);
                        }
                        break;
                    case TRIALSTATE.TRIAL:
                        if (pushcondatstate == PUSHCONDATSTATE.TRIAL)
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
                            if (condtestatstate != CONDTESTATSTATE.NONE)
                            {
                                condtestmanager.Add("CondIndex", condmanager.condidx);
                                condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                            }
                        }
                        TrialOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.AddEvent("TRIALSTATE", value.ToString(), TrialOnTime);
                        }
                        break;
                    case TRIALSTATE.SUFITI:
                        SufITIOnTime = timer.ElapsedMS;
                        if (condtestatstate != CONDTESTATSTATE.NONE)
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
                                ex.cond = condmanager.cond;
                                ex.condtest = condtestmanager.condtest;
                                ex.envparam = envmanager.GetEnvParam();

                                Yaml.WriteYaml(CondTestPath(), ex);
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

                foreach(var f in cond.Keys)
                {
                    if(cond[f].Count==0)
                    {
                        if (f != "factorlevels")
                        {
                            cond.Remove(f);
                        }
                    }
                    else
                    {
                        if(((string)cond[f][0]).Contains("$"))
                        {
                            var p = ((string)cond[f][0]).Substring(1);
                            if(ex.param.ContainsKey(p)&&ex.param[p]==null)
                            {
                                cond[f] = ex.param[p] as List<object>;
                            }
                            else
                            {
                                cond.Remove(f);
                            }
                        }
                        if((string)cond[f][0]=="conditiondesign"&&cond[f].Count>1)
                        {
                            var d = Yaml.Deserialize<Dictionary<string, object>>((string)cond[f][1]);
                            var fl = new FactorLevel(f, d["start"], d["end"], d["step"], (DesignMethod)d["method"]);
                            cond[f] = fl.GenerateFactorLevels().Value;
                        }
                    }
                }

                if(cond.ContainsKey("factorlevels"))
                {
                    cond.Remove("factorlevels");
                    cond = ConditionDesigner.GenerateCondition(cond);
                }
                condmanager.TrimCondition(cond);
                ex.cond = cond;
            }
            condmanager.UpdateCondPopulation();
        }

        public virtual string CondTestPath()
        {
            return ex.CondTestPath();
        }

        public virtual void StartExperiment()
        {
            ExperimentState = EXPERIMENTSTATE.EXPERIMENT;
            pushcondatstate = PushCondAtState;
            condtestatstate = CondTestAtState;


            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 2;
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 0;

            Time.fixedDeltaTime = 0.0001f;
            Time.maximumDeltaTime = 0.33f;
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            islogicactive = true;
        }

        public virtual void StopExperiment()
        {
            //envmanager.activenetbehavior.visible = false;
            CondState = CONDSTATE.NONE;
            TrialState = TRIALSTATE.NONE;
            ExperimentState = EXPERIMENTSTATE.NONE;
            pushcondatstate = PUSHCONDATSTATE.NONE;
            condtestatstate = CONDTESTATSTATE.NONE;


            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;

            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.33f;
            Process.GetCurrentProcess().PriorityBoostEnabled = false;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            islogicactive = false;
        }

        void Awake()
        {
            if (!Timer.IsHighResolution)
            {
                UnityEngine.Debug.Log("This Machine Doesn't Have High Resolution Timer.");
            }
            Init();
        }

        public virtual void Init()
        {

        }

        public virtual void UpdateScene(string scenename)
        {
            envmanager.AddScene(scenename);
            envmanager.SetEnvParam(ex.envparam);
            condmanager.envmanager = envmanager;
        }

        public bool isplayercontrol;
        public void StartPlayer()
        {
            isplayercontrol = !isplayercontrol;
            //Cursor.visible = false;
        }


        void Update()
        {
            if(ex.input>0&&Input.GetJoystickNames().Count()>0)
            {
                if(envmanager.activesync.Count>0)
                {
                    if (Input.GetAxis("JX")!=0 ||Input.GetAxis("JY")!=0)
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
            //UnityEngine.Debug.Log(timer.ElapsedMS);
        }

        public virtual void Logic()
        {

        }
    }
}