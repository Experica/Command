using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Linq;
using System.Text;

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
    Ascending=0,
    Descending=1,
    RandomWithReplacement=2,
    RandomWithoutReplacement=3
}

public class ConditionManager
{
    public Dictionary<string, List<object>> cond;
    public SampleMethod samplemethod = SampleMethod.Ascending;
    public int scendingstep = 1;
    public int idx;
    

    public List<int> SubCondIdx;


    public Dictionary<string, List<object>> ReadCondition(string path)
    {
        cond = VLIO.ReadYaml<Dictionary<string, List<object>>>(path);
        Init();
        return cond;
    }

    private void Init()
    {
        var nc = cond.Values.First().Count;
        
        SubCondIdx = Enumerable.Range(0, nc).ToList();
        idx = -1;
    }



    public int NextCondIdx
    {
        get
        {
            switch (samplemethod)
            {
                case SampleMethod.Ascending:
                    idx += scendingstep;
                    if (idx > SubCondIdx.Count-1)
                        idx -=SubCondIdx.Count;
                    return SubCondIdx[idx];
                default:
                    return -1;
            }
        }
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
    public string name;
    public float time;
}


public class ExperimentLogic : MonoBehaviour
{
    public Experiment ex = new Experiment();
    public Timer timer = new Timer();
    public EXSTATE exstate;
    public bool isplayercontrol;

    public ConditionManager condmanager = new ConditionManager();
    public CondTestManager condtestmanager = new CondTestManager();
    public int condidx;
    
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
                    condidx = condmanager.NextCondIdx;
                    PushCondition(condidx);
                    PreICIOnTime = timer.ElapsedSeconds;
                    condtestmanager.Append("CondIdx", condidx);
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


    public Camera maincamera;
    public GameObject player;
    public NetSyncBase visualobject;


    // Use this for initialization
    void Start()
    {
        Init();
        //StartPlayer();
        ex.cond = condmanager.ReadCondition("cond.yaml");
        ex.condtest = condtestmanager.condtest;
        UnityEngine.Debug.Log(Timer.IsHighResolution);
    }

    
    public void PushCondition(int condidx)
    {

    }

    public void PushValue(string obj,string param, object value)
    {

    }

    

    public virtual void OnSceneChange(string sceneName)
    {
        maincamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        player = GameObject.Find("Player");
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
            var t = new EnvironmentManager
            {
                name = "a",
                time = 0.1f
            };
            VLIO.WriteYaml("condtest.yaml", t);
        }
        else
        {
            exstate = EXSTATE.Start;
        }
        
    }
    public void StartPlayer()
    {
        UnityEngine.Debug.Log(isplayercontrol);
        isplayercontrol = !isplayercontrol;

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
            if (visualobject)
            {
                float r = System.Convert.ToSingle(Input.GetButton("Fire1"));
                float r1 = System.Convert.ToSingle(Input.GetButton("Fire2"));
                visualobject.ori += r - r1;
                visualobject.length += 0.1f * Input.GetAxis("Horizontal");
                visualobject.width += 0.1f * Input.GetAxis("Vertical");
                var p = maincamera.ScreenToWorldPoint(Input.mousePosition);
                p.z = visualobject.transform.position.z;
                visualobject.position = p;
            }
        }

    }
}
