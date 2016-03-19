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

public class Subject
{
    public string species;
    public string name;
    public string id;
    public string log;
    public string gender;
    public float age;
    public Vector3 size=new Vector3();
    public float weight;
}

public class Experiment
{
    public string name;
    public string id;
    public string experimenter;
    public string log;
    public Subject subject = new Subject();
    public string condpath;
    public Dictionary<string, List<object>> cond;
    public string condtestpath;
    public Dictionary<string, List<object>> condtest;
    public string environmentpath;
    public List<object> environment;
    public string experimentlogicpath;
    public string experimentlogic;

    public int condrepeat;
    public double preIBI, blockdur, sufIBI, preITI, trialdur, sufITI, preICI, conddur, sufICI;
}



public enum SampleMethod
{
    Ascending = 0,
    Descending = 1,
    UniformWithReplacement = 2,
    UniformWithoutReplacement = 3
}

public class ConditionManager
{
    public Dictionary<string, List<object>> cond;
    public int nfactor;
    public int ncond;
    public List<int> popcondidx;
    public Dictionary<int,int> condrepeat;
    public int sampleidx = -1;
    public SampleMethod samplemethod = SampleMethod.Ascending;
    public int scendingstep = 1;
    public int condidx = -1;
    public int sampleignores = 0;
    public System.Random rng = new System.Random();
    public EnvironmentManager envmanager = new EnvironmentManager();


    public Dictionary<string, List<object>> ReadCondition(string path)
    {
        cond = VLIO.ReadYaml<Dictionary<string, List<object>>>(path);
        nfactor = cond.Keys.Count;
        if (nfactor == 0)
        {
            UnityEngine.Debug.Log("Condition Empty.");
        }
        else
        {
            var fvn = new int[nfactor];
            for (var i = 0; i < nfactor; i++)
            {
                fvn[i] = cond.Values.ElementAt(i).Count;
            }
            var minfvn = fvn.Min();
            var maxfvn = fvn.Max();
            if (minfvn != maxfvn)
            {
                foreach (var k in cond.Keys)
                {
                    cond[k] = cond[k].GetRange(0, minfvn);
                }
                UnityEngine.Debug.Log("Cut Condition to Minimum Length.");
            }
            ncond = minfvn;
        }
        return cond;
    }

    public List<int> UpdateCondPopulation(bool resetcondrepeat=true)
    {
        switch (samplemethod)
        {
            case SampleMethod.Ascending:
                popcondidx = Enumerable.Range(0, ncond).ToList();
                sampleidx = -1;
                break;
            case SampleMethod.Descending:
                popcondidx = Enumerable.Range(0, ncond).Reverse().ToList();
                sampleidx = -1;
                break;
            case SampleMethod.UniformWithReplacement:
                popcondidx = Enumerable.Range(0, ncond).ToList();
                sampleidx = -1;
                break;
            case SampleMethod.UniformWithoutReplacement:
                popcondidx = Enumerable.Range(0, ncond).ToList();
                sampleidx = -1;
                break;
        }
        if (resetcondrepeat)
        {
            condrepeat = new Dictionary<int, int>();
            foreach(var i in popcondidx)
            {
                condrepeat[i] = 0;
            }
        }
        return popcondidx;
    }

    public int SampleCondIdx()
    {
        if (sampleignores == 0)
        {
            switch (samplemethod)
            {
                case SampleMethod.Ascending:
                    sampleidx += scendingstep;
                    if (sampleidx > popcondidx.Count - 1)
                    {
                        sampleidx -= popcondidx.Count;
                    }
                    condidx = popcondidx[sampleidx];
                    break;
                case SampleMethod.Descending:
                    sampleidx += scendingstep;
                    if (sampleidx > popcondidx.Count - 1)
                    {
                        sampleidx -= popcondidx.Count;
                    }
                    condidx = popcondidx[sampleidx];
                    break;
                case SampleMethod.UniformWithReplacement:
                    sampleidx = rng.Next(popcondidx.Count);
                    condidx = popcondidx[sampleidx];
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    if (popcondidx.Count == 0)
                    {
                        UpdateCondPopulation(false);
                    }
                    sampleidx = rng.Next(popcondidx.Count);
                    condidx = popcondidx[sampleidx];
                    popcondidx.RemoveAt(sampleidx);
                    break;
            }
            condrepeat[condidx] += 1;
        }
        else
        {
            sampleignores--;
        }
        return condidx;
    }

    public void PushCondition(int idx)
    {
        foreach (var kv in cond)
        {
            envmanager.SetParam(kv.Key, kv.Value[idx]);
        }
    }

    public void SamplePushCondition()
    {
        PushCondition(SampleCondIdx());
    }

    public bool IsFinishRepeat(int n)
    {
        foreach(var i in condrepeat.Values)
        {
            if(i<n)
            {
                return false;
            }
        }
        return true;
    }
}

public class ConditionDesigner
{

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
            for(var i=vs.Count;i<condtestidx;i++)
            {
                vs.Add(null);
            }
            vs.Add(value);
        }
        else
        {
            var vs = new List<object>();
            for(var i=0;i<condtestidx;i++)
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

public class EnvironmentManager
{
    public Scene scene;
    public Dictionary<string, GameObject> sceneobject = new Dictionary<string, GameObject>();
    public Dictionary<string, NetBehaviorBase> netbehavior = new Dictionary<string, NetBehaviorBase>();
    public Dictionary<string, Dictionary<string, PropertyInfo>> param = new Dictionary<string, Dictionary<string, PropertyInfo>>();
    public Camera maincamera;
    public NetBehaviorBase activenetbehavior;
    public string activenetbehaviorname;


    public void AddScene(string scenename)
    {
        scene = SceneManager.GetSceneByName(scenename);
        UpdateEnvironment();
    }

    public void UpdateEnvironment()
    {
        sceneobject.Clear();
        netbehavior.Clear();
        param.Clear();
        maincamera = null;
        activenetbehavior = null;
        activenetbehaviorname = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            sceneobject["@" + go.name] = go;
            ParseSceneObjectInfo(go);
        }
    }

    public void ParseSceneObjectInfo(GameObject go)
    {
        var ismaincamera = false;
        if (go.tag == "MainCamera")
        {
            maincamera = go.GetComponent<Camera>();
            ismaincamera = true;
        }
        var nbs = go.GetComponents<NetBehaviorBase>();
        foreach (var nb in nbs)
        {
            netbehavior["@" + nb.name + "@" + go.name] = nb;
            param["@" + nb.name + "@" + go.name] = ParseNetBehaviorInfo(nb);
            if (!ismaincamera && nb.isActiveAndEnabled)
            {
                activenetbehavior = nb;
                activenetbehaviorname = "@" + nb.name + "@" + go.name;
            }
        }
        for (var i = 0; i < go.transform.childCount; i++)
        {
            ParseSceneObjectInfo(go.transform.GetChild(i).gameObject);
        }
    }

    public Dictionary<string, PropertyInfo> ParseNetBehaviorInfo(NetBehaviorBase nb)
    {
        var fs = nb.GetType().GetFields();
        var ps = new Dictionary<string, PropertyInfo>();
        foreach (var f in fs)
        {
            if (f.IsDefined(typeof(SyncVarAttribute), true))
            {
                ps[f.Name] = nb.GetType().GetProperty("Network" + f.Name);
            }
        }
        return ps;
    }

    public void SetParam(string key, object value)
    {
        NetBehaviorBase nb;
        string nbkey;
        string paramname;
        var i = key.IndexOf("@");
        if (i == -1)
        {
            if (activenetbehavior != null && activenetbehaviorname != null)
            {
                paramname = key;
                nbkey = activenetbehaviorname;
                nb = activenetbehavior;
            }
            else
            {
                paramname = key;
                nbkey = netbehavior.Last().Key;
                nb = netbehavior.Last().Value;
            }
        }
        else
        {
            paramname = key.Substring(0, i);
            nbkey = key.Substring(i);
            nb = netbehavior[nbkey];
        }

        if (!param.ContainsKey(nbkey))
        {
            UnityEngine.Debug.Log("Invalid Object Path: " + nbkey);
            return;
        }
        if (!param[nbkey].ContainsKey(paramname))
        {
            UnityEngine.Debug.Log("Invalid Param Name: " + paramname);
            return;
        }
        var p = param[nbkey][paramname];
        object v = null;
        if (p.PropertyType == typeof(float))
        {
            v = float.Parse((string)value);
        }
        else if (p.PropertyType == typeof(bool))
        {
            v = bool.Parse((string)value);
        }
        else if (p.PropertyType == typeof(int))
        {
            v = int.Parse((string)value);
        }
        p.SetValue(nb, v, null);
    }

    public void SetMainCameraOrthoSize(float screenhalfheight, float screentoeye)
    {
        if (maincamera != null)
        {
            maincamera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(screenhalfheight, screentoeye);
        }
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
    COND = CONDSTATE.COND,
    TRIAL = TRIALSTATE.TRIAL
}

public enum CONDTESTATSTATE
{
    NONE=-1,
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

    public bool islogicactive = false;

    public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
        TrialOnTime, SufITIOnTime;

    public double PreICIHold()
    {
        return timer.ElapsedSeconds - PreICIOnTime;
    }

    public double CondHold()
    {
        return timer.ElapsedSeconds - CondOnTime;
    }

    public double SufICIHold()
    {
        return timer.ElapsedSeconds - SufICIOnTime;
    }

    public double PreITIHold()
    {
        return timer.ElapsedSeconds - PreITIOnTime;
    }

    public double TrialHold()
    {
        return timer.ElapsedSeconds - TrialOnTime;
    }

    public double SufITIHold()
    {
        return timer.ElapsedSeconds - SufITIOnTime;
    }

    public PUSHCONDATSTATE PushCondAtState = PUSHCONDATSTATE.NONE;
    public CONDTESTATSTATE CondTestAtState = CONDTESTATSTATE.NONE;

    private CONDSTATE condstate = CONDSTATE.NONE;
    public CONDSTATE CondState
    {
        get { return condstate; }
        set
        {
            switch (value)
            {
                case CONDSTATE.PREICI:
                    if(CondTestAtState==CONDTESTATSTATE.PREICI)
                    {
                        condtestmanager.NewCondTest();
                    }
                    PreICIOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEvent("CONDSTATE", value.ToString(), PreICIOnTime);
                    }
                    break;
                case CONDSTATE.COND:
                    if(PushCondAtState==PUSHCONDATSTATE.COND)
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
                        if (CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add("CondIndex", condmanager.condidx);
                            condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    CondOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEvent("CONDSTATE", value.ToString(), CondOnTime);
                    }
                    break;
                case CONDSTATE.SUFICI:
                    SufICIOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
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
            switch(value)
            {
                case TRIALSTATE.PREITI:
                    if (CondTestAtState == CONDTESTATSTATE.PREITI)
                    {
                        condtestmanager.NewCondTest();
                    }
                    PreITIOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEvent("TRIALSTATE", value.ToString(), PreITIOnTime);
                    }
                    break;
                case TRIALSTATE.TRIAL:
                    if (PushCondAtState == PUSHCONDATSTATE.TRIAL)
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
                        if (CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add("CondIndex", condmanager.condidx);
                            condtestmanager.Add("CondRepeat", condmanager.condrepeat[condmanager.condidx]);
                        }
                    }
                    TrialOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEvent("TRIALSTATE", value.ToString(), TrialOnTime);
                    }
                    break;
                case TRIALSTATE.SUFITI:
                    SufITIOnTime = timer.ElapsedSeconds;
                    if (CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddEvent("TRIALSTATE", value.ToString(), SufITIOnTime);
                    }
                    break;
            }
            trialstate = value;
        }
    }

    private BLOCKSTATE blockstate= BLOCKSTATE.NONE;
    public BLOCKSTATE BlockState
    {
        get { return blockstate; }
        set
        {
            switch(value)
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
                            VLIO.WriteYaml("condtest.yaml", ex.condtest);
                            break;
                    }
                    break;
                case EXPERIMENTSTATE.EXPERIMENT:
                    switch (experimentstate)
                    {
                        case EXPERIMENTSTATE.NONE:
                            ex.cond = condmanager.ReadCondition("cond.yaml");
                            condmanager.UpdateCondPopulation();
                            timer.ReStart();
                            break;
                    }
                    break;
            }
            experimentstate = value;
        }
    }

    public void StartExperiment()
    {
        ExperimentState = EXPERIMENTSTATE.EXPERIMENT;
        Process.GetCurrentProcess().PriorityBoostEnabled = true;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        QualitySettings.maxQueuedFrames = 2;
        PushCondAtState = PUSHCONDATSTATE.COND;
        CondTestAtState = CONDTESTATSTATE.PREICI;
        islogicactive = true;
    }

    public void StopExperiment()
    {
        ExperimentState = EXPERIMENTSTATE.NONE;
        Application.targetFrameRate = 1000;
        QualitySettings.vSyncCount = 1;
        QualitySettings.maxQueuedFrames = 3;
        PushCondAtState = PUSHCONDATSTATE.NONE;
        CondTestAtState = CONDTESTATSTATE.NONE;
        islogicactive = false;
    }


    void Start()
    {        
        ex.condtest = condtestmanager.condtest;
        UnityEngine.Debug.Log(Timer.IsHighResolution);

        Init();
    }

    public virtual void Init()
    {

    }

    public virtual void OnSceneChange(string scenename)
    {
        envmanager.AddScene(scenename);
        condmanager.envmanager = envmanager;
    }

    public bool isplayercontrol;
    public void StartPlayer()
    {
        UnityEngine.Debug.Log(isplayercontrol);
        isplayercontrol = !isplayercontrol;
        //Cursor.visible = false;
    }


    void Update()
    {
        if (isplayercontrol)
        {
            if (envmanager.activenetbehavior)
            {
                float r = Convert.ToSingle(Input.GetButton("Fire1"));
                float r1 = Convert.ToSingle(Input.GetButton("Fire2"));
                envmanager.activenetbehavior.ori += r - r1;
                envmanager.activenetbehavior.length += 0.1f * Input.GetAxis("Horizontal");
                envmanager.activenetbehavior.width += 0.1f * Input.GetAxis("Vertical");
                var p = envmanager.maincamera.ScreenToWorldPoint(Input.mousePosition);
                p.z = envmanager.activenetbehavior.transform.position.z;
                envmanager.activenetbehavior.position = p;
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