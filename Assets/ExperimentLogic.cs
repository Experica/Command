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
    public string species="";
    public string name="";
    public string id="";
    public string gender="";
    public float age;
    //public Vector3 size=new Vector3();
    public float weight;
}

public class Experiment
{
    public string name;
    public string id;
    public string experimenter;
    public Subject subject = new Subject();
    public Dictionary<string, List<object>> cond;
    public Dictionary<string, List<object>> condtest;

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

public enum EXSTATE
{
    Stop = 0,
    Start = 1
}

public enum CONDSTATE
{
    CONDNONE = 0,
    PREICI = 1,
    COND = 2,
    SUFICI = 3    
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
    public int sampleidx=-1;
    public int condidx=-1;
    public SampleMethod samplemethod = SampleMethod.Ascending;
    public int scendingstep = 1;
    public EnvironmentManager envmanager = new EnvironmentManager();
    

    public Dictionary<string, List<object>> ReadCondition(string path)
    {
        cond = VLIO.ReadYaml<Dictionary<string, List<object>>>(path);
        return cond;
    }

    public List<int> UpdateCondPopulation()
    {
        var nc = cond.Values.First().Count;
        
        popcondidx = Enumerable.Range(0, nc).ToList();
        sampleidx = -1;
        return popcondidx;
    }



    public int SampleCondIdx()
    {
            switch (samplemethod)
            {
                case SampleMethod.Ascending:
                    sampleidx += scendingstep;
                    if (sampleidx > popcondidx.Count-1)
                        sampleidx -=popcondidx.Count;
                    condidx = popcondidx[sampleidx];
                return condidx;
                default:
                    return -1;
            }
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
    public Dictionary<string, GameObject> sceneobjects=new Dictionary<string, GameObject>();
    public Camera maincamera;
    public NetSyncBase target;
    public NetSyncBase figure;
    public Dictionary<string, NetSyncBase> netobjects = new Dictionary<string, NetSyncBase>();
    public Dictionary<string, Dictionary<string,object>> noparams=new Dictionary<string, Dictionary<string, object>>();

    public void AddScene(string scenename)
    {
        SceneManager.GetActiveScene();
        scene = SceneManager.GetSceneByName(scenename);
        UpdateEnvironment();
    }

    public void UpdateEnvironment()
    {
        sceneobjects.Clear();
        netobjects.Clear();
        noparams.Clear();
        foreach (var o in scene.GetRootGameObjects())
        {
            sceneobjects[o.name] = o;
            if (o.tag == "MainCamera")
            {
                maincamera = o.GetComponent<Camera>();
            }
            var nsb = o.GetComponent<NetSyncBase>();
            if (nsb)
            {
                netobjects[o.name] = nsb;
                var ps = nsb.GetType().GetFields();
                foreach(var p in ps)
                {
                    var ttt = p.IsDefined(typeof(SyncVarAttribute),true);
                    var atts = p.GetCustomAttributes(true);
                    foreach(var a in atts)
                    {
                        if (a.GetType() == typeof(SyncVarAttribute))
                        {
                            var aa = (SyncVarAttribute)a;
                            //aa.
                            if (noparams.ContainsKey(o.name))
                            {
                                noparams[o.name][p.Name] = p;
                            }
                            else
                            {
                                var pv = new Dictionary<string, object>();
                                pv[p.Name] = p;
                                noparams[o.name] = pv;

                            }
                        }
                    }
                }
            }
        }
        figure = netobjects.Values.First();
    }

    public void SetParam(string param, object value)
    {
        var nsb = netobjects.Values.First();
        var f = (FieldInfo)noparams[nsb.name][param];
        object v=null;
        if (f.FieldType == typeof(float))
        {
            v = float.Parse((string)value);
        }
        //UnityEngine.Debug.Log("--------before-----------");
        //UnityEngine.Debug.Log(nsb.ori);
        //f.SetValue(nsb, v);
        //nsb.ori = (float)v;
        var a= (SyncVarAttribute)f.GetCustomAttributes(typeof(SyncVarAttribute), true)[0];
        nsb.GetType().GetMethod(a.hook).Invoke(nsb, new object[] { v});

        //UnityEngine.Debug.Log("--------after-----------");
        //UnityEngine.Debug.Log(nsb.ori);
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
    public EXSTATE exstate;
    public bool isplayercontrol;

    public EnvironmentManager envmanager = new EnvironmentManager();
    public ConditionManager condmanager = new ConditionManager();
    public CondTestManager condtestmanager = new CondTestManager();
    
    public double PreICIOnTime, CondOnTime, SufICIOnTime;

    private CONDSTATE condstate;
    public CONDSTATE CondState
    {
        set
        {
            condstate = value;
            switch(condstate)
            {
                case CONDSTATE.PREICI:
                    condtestmanager.NewCondTest();
                    condmanager.SamplePushCondition();
                    PreICIOnTime = timer.ElapsedSeconds;
                    condtestmanager.Append("CondIdx", condmanager.condidx);
                    condtestmanager.AddEvent(typeof(CONDSTATE).ToString(),condstate.ToString(), PreICIOnTime);
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
        }
        get { return condstate; }
    }

    // Use this for initialization
    void Start()
    {
        Init();
        //StartPlayer();
        
        ex.cond = condmanager.ReadCondition("cond.yaml");
        ex.condtest = condtestmanager.condtest;
        UnityEngine.Debug.Log(Timer.IsHighResolution);
    }

    

    public virtual void OnSceneChange(string sceneName)
    {
        envmanager.AddScene(sceneName);
        condmanager.envmanager = envmanager;
    }
    public virtual void Init()
    {

    }
    public virtual void Logic()
    {

    }

    public void StartLogic()
    {
        UnityEngine.Debug.Log(exstate);
        if (exstate == EXSTATE.Start)
        {
            exstate = EXSTATE.Stop;
            VLIO.WriteYaml("condtest.yaml", ex.condtest);
        }
        else
        {
            exstate = EXSTATE.Start;
            condmanager.UpdateCondPopulation();
        }
        
    }
    public void StartPlayer()
    {
        UnityEngine.Debug.Log(isplayercontrol);
        isplayercontrol = !isplayercontrol;
        Cursor.visible = false;

    }
    // Update is called once per frame
    void Update()
    {
        
        if (exstate == EXSTATE.Start)
        {
            //UnityEngine.Debug.Log("uu");
            Logic();
        }

        if (isplayercontrol)
        {
            UnityEngine.Debug.Log(ex.cond["ori"][0]);
            if (envmanager.figure)
            {
                float r = System.Convert.ToSingle(Input.GetButton("Fire1"));
                float r1 = System.Convert.ToSingle(Input.GetButton("Fire2"));
                envmanager.figure.ori += r - r1;
                envmanager.figure.length += 0.1f * Input.GetAxis("Horizontal");
                envmanager.figure.width += 0.1f * Input.GetAxis("Vertical");
                var p = envmanager.maincamera.ScreenToWorldPoint(Input.mousePosition);
                p.z = envmanager.figure.transform.position.z;
                envmanager.figure.position = p;
            }
        }

    }
}
