using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
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

    public int nblock, ntrial, ncondtest,blockn,trialn,condtestn;
    public double preIBI, blockdur, sufIBI, preITI, trialdur, sufITI, preICI, conddur, sufICI;
}

public class Timer : Stopwatch
{
    public double ElapsedSeconds
    {
        get { return Elapsed.TotalSeconds; }
    }

    public void ReStart()
    {
        Reset();
        Start();
    }
}


public enum CONDSTATE
{
    CONDNONE = 0,
    PREICI=1,
    COND=2,
    SUFICI=3
}

public enum TRIALSTATE
{
    TRIALNONE=1000,
    PREITI=1001,
    TRIAL=1002,
    SUFITI=1003
}

public enum BLOCKSTATE
{
    BLOCKNONE=2000,
    PREIBI=2001,
    BLOCK=2002,
    SUFIBI=2003
}

public enum EXPERIMENTSTATE
{
    EXPERIMENTNONE=3000,
    PREIEI=3001,
    EXPERIMENT=3002,
    SUFIEI=3003
}

public enum PUSHCONDATSTATE
{
    COND=2,
    TRIAL=1002
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
    public List<int> popcondidx;
    public int sampleidx = -1;
    public SampleMethod samplemethod = SampleMethod.Ascending;
    public int scendingstep = 1;
    public int condidx = -1;
    public EnvironmentManager envmanager = new EnvironmentManager();
    

    public Dictionary<string, List<object>> ReadCondition(string path)
    {
        cond = VLIO.ReadYaml<Dictionary<string, List<object>>>(path);
        return cond;
    }

    public List<int> UpdateCondPopulation()
    {
        switch(samplemethod)
        {
            case SampleMethod.Ascending:
                var nc = cond.Values.First().Count;

                popcondidx = Enumerable.Range(0, nc).ToList();
                sampleidx = -1;
                break;
            case SampleMethod.Descending:
                break;
        }
        return popcondidx;
    }

    public int SampleCondIdx()
    {
        switch (samplemethod)
        {
            case SampleMethod.Ascending:
                sampleidx += scendingstep;
                if (sampleidx > popcondidx.Count - 1)
                    sampleidx -= popcondidx.Count;
                condidx = popcondidx[sampleidx];
                break;
        }
        return condidx;
    }

    public void PushCondition(int idx)
    {
        foreach(var kv in cond)
        {
            envmanager.SetParam(kv.Key, kv.Value[idx]);
        }
    }

    public void SamplePushCondition()
    {
        PushCondition(SampleCondIdx());
    }
}

public class ConditionDesigner
{

}

public class CondTestManager
{
    public Dictionary<string, List<object>> condtest = new Dictionary<string, List<object>>();
    public int condtestidx=-1;

    public void NewCondTest()
    {
        condtestidx++;
    }

    public void Append(string valuetype, object value)
    {
        if (condtest.ContainsKey(valuetype))
        {
            condtest[valuetype].Add(value);
        }
        else
        {
            var vs = new List<object>();
            vs.Add(value);
            condtest.Add(valuetype, vs);
        }
    }

    public void AddEvent(string eventtype, string eventname, double timestamp)
    {
        if (condtest.ContainsKey(eventtype))
        {
            var vs = condtest[eventtype];
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
            var es = new List<Dictionary<string,double>>();
            var e = new Dictionary<string, double>();
            e[eventname] = timestamp;
            es.Add(e);
            vs.Add(es);
            condtest.Add(eventtype, vs);
        }
    }
}

public class EnvironmentManager
{
    public Scene scene;
    public Dictionary<string, GameObject> sceneobject = new Dictionary<string, GameObject>();
    public Dictionary<string, NetBehaviorBase> netbehavior = new Dictionary<string, NetBehaviorBase>();
    public Dictionary<string, Dictionary<string, PropertyInfo>> syncvar = new Dictionary<string, Dictionary<string, PropertyInfo>>();

    public Camera maincamera;
    public string activenetbehavior;
    public NetBehaviorBase ActiveNetBehavior
    {
        get { return netbehavior[activenetbehavior]; }
    }


    public void AddScene(string scenename)
    {
        scene = SceneManager.GetSceneByName(scenename);
        UpdateEnvironment();
    }

    public void UpdateEnvironment()
    {
        sceneobject.Clear();
        netbehavior.Clear();
        syncvar.Clear();
        foreach (var go in scene.GetRootGameObjects())
        {
            sceneobject[go.name] = go;
            var ismaincamera = false;
            if (go.tag == "MainCamera")
            {
                maincamera = go.GetComponent<Camera>();
                ismaincamera = true;
            }
            var nb = go.GetComponent<NetBehaviorBase>();
            if (nb)
            {
                netbehavior[go.name] = nb;
                var fs = nb.GetType().GetFields();
                foreach(var f in fs)
                {
                    if (f.IsDefined(typeof(SyncVarAttribute), true))
                    {
                        if (syncvar.ContainsKey(go.name))
                        {
                            syncvar[go.name][f.Name] = nb.GetType().GetProperty("Network" + f.Name);
                        }
                        else
                        {
                            var pv = new Dictionary<string, PropertyInfo>();
                            pv[f.Name] = nb.GetType().GetProperty("Network" + f.Name);
                            syncvar[go.name] = pv;
                        }
                    }
                }
                if (!ismaincamera && nb.isActiveAndEnabled)
                {
                    activenetbehavior = nb.name;
                }
            }
        }
    }

    public void SetParam(string param, object value)
    {
        NetBehaviorBase nb;
        if(activenetbehavior!=null)
        {
            nb = netbehavior[activenetbehavior];
        }
        else
        {
            nb = netbehavior.Values.First();
        }
        var p = syncvar[nb.name][param];

        object v=null;
        if (p.PropertyType == typeof(float))
        {
            v = float.Parse((string)value);
        }
        p.SetValue(nb, v, null);
    }

    public void SetMainCameraOrthoSize(float screenhalfheight, float screentoeye)
    {
        maincamera.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(screenhalfheight, screentoeye);
    }
}


public class ExperimentLogic : MonoBehaviour
{
    public Experiment ex = new Experiment();
    public Timer timer = new Timer();

    public EnvironmentManager envmanager = new EnvironmentManager();
    public ConditionManager condmanager = new ConditionManager();
    public CondTestManager condtestmanager = new CondTestManager();

    public bool islogicactive = false;
    public bool usecondition = false;
    public bool samplecondition = false;
    public PUSHCONDATSTATE pushcondatstate = PUSHCONDATSTATE.COND;

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

    private CONDSTATE condstate;
    public CONDSTATE CondState
    {
        get { return condstate; }
        set
        {
            switch (value)
            {
                case CONDSTATE.PREICI:
                    condtestmanager.NewCondTest();
                    condmanager.SamplePushCondition();
                    PreICIOnTime = timer.ElapsedSeconds;
                    condtestmanager.Append("CondIdx", condmanager.condidx);
                    condtestmanager.AddEvent(typeof(CONDSTATE).ToString(), condstate.ToString(), PreICIOnTime);
                    break;
                case CONDSTATE.COND:
                    CondOnTime = timer.ElapsedSeconds;
                    condtestmanager.AddEvent(typeof(CONDSTATE).ToString(), condstate.ToString(), CondOnTime);
                    break;
                case CONDSTATE.SUFICI:
                    SufICIOnTime = timer.ElapsedSeconds;
                    condtestmanager.AddEvent(typeof(CONDSTATE).ToString(), condstate.ToString(), SufICIOnTime);
                    break;
            }
            condstate = value;
        }
    }

    private TRIALSTATE trialstate;
    public TRIALSTATE TrialState
    {
        get { return trialstate; }
        set
        {
            trialstate = value;
        }
    }

    private BLOCKSTATE blockstate;
    public BLOCKSTATE BlockState
    {
        get { return blockstate; }
        set
        {
            blockstate = value;
        }
    }

    private EXPERIMENTSTATE experimentstate = EXPERIMENTSTATE.EXPERIMENTNONE;
    public EXPERIMENTSTATE ExperimentState
    {
        get { return experimentstate; }
        set
        {
            switch (value)
            {
                case EXPERIMENTSTATE.EXPERIMENTNONE:
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
                        case EXPERIMENTSTATE.EXPERIMENTNONE:
                            condmanager.UpdateCondPopulation();
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
        islogicactive = true;
    }

    public void StopExperiment()
    {
        ExperimentState = EXPERIMENTSTATE.EXPERIMENTNONE;
        islogicactive = false;
    }


    void Start()
    {

        //StartPlayer();

        ex.cond = condmanager.ReadCondition("cond.yaml");
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
            UnityEngine.Debug.Log(ex.cond["ori"][0]);
            if (envmanager.ActiveNetBehavior)
            {
                float r = System.Convert.ToSingle(Input.GetButton("Fire1"));
                float r1 = System.Convert.ToSingle(Input.GetButton("Fire2"));
                envmanager.ActiveNetBehavior.ori += r - r1;
                envmanager.ActiveNetBehavior.length += 0.1f * Input.GetAxis("Horizontal");
                envmanager.ActiveNetBehavior.width += 0.1f * Input.GetAxis("Vertical");
                var p = envmanager.maincamera.ScreenToWorldPoint(Input.mousePosition);
                p.z = envmanager.ActiveNetBehavior.transform.position.z;
                envmanager.ActiveNetBehavior.position = p;
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