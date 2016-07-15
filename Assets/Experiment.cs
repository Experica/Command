using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

namespace VLab
{
    public class Experiment
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Designer { get; set; }
        public string Experimenter { get; set; }
        public string Log { get; set; }

        public string Subject_Species { get; set; }
        public string Subject_Name { get; set; }
        public string Subject_ID { get; set; }
        public string Subject_Gender { get; set; }
        public float Subject_Age { get; set; }
        public Vector3 Subject_Size { get; set; }
        public float Subject_Weight { get; set; }
        public string Subject_Log { get; set; }

        public string EnvPath { get; set; }
        public Dictionary<string, object> EnvParam { get; set; }
        public string CondPath { get; set; }
        public Dictionary<string, List<object>> Cond { get; set; }
        public string ExLogicPath { get; set; }

        public string RecordSession { get; set; }
        public string RecordSite { get; set; }
        public string DataDir { get; set; }
        public string DataPath { get; set; }
        public Dictionary<CONDTESTPARAM, List<object>> CondTest { get; set; }
        public SampleMethod CondSampling { get; set; }
        public int CondRepeat { get; set; }
        public int Input { get; set; }
        public bool AutoSaveData { get; set; }

        public double PreICI { get; set; }
        public double CondDur { get; set; }
        public double SufICI { get; set; }
        public double PreITI { get; set; }
        public double TrialDur { get; set; }
        public double SufITI { get; set; }
        public double PreIBI { get; set; }
        public double BlockDur { get; set; }
        public double SufIBI { get; set; }
        public PUSHCONDATSTATE PushCondAtState { get; set; }
        public CONDTESTATSTATE CondTestAtState { get; set; }
        public int NotifyPerCondTest { get; set; }
        public Dictionary<string, object> Param { get; set; }
        public List<string> ExInheritParam { get; set; }
        public List<string> EnvInheritParam { get; set; }
        public List<CONDTESTPARAM> NotifyParam { get; set; }

        public static readonly Dictionary<string, PropertyInfo> Properties;

        static Experiment()
        {
            Properties = new Dictionary<string, PropertyInfo>();
            foreach (var p in typeof(Experiment).GetProperties())
            {
                Properties[p.Name] = p;
            }
        }

        public void SetValue(string name, object value)
        {
            if (Properties.ContainsKey(name))
            {
                SetValue(this, Properties[name], value);
            }
        }

        public static void SetValue(Experiment ex, PropertyInfo p, object value)
        {
            p.SetValue(ex, value.Convert(p.PropertyType), null);
        }

        public object GetValue(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return GetValue(this, Properties[name]);
            }
            return null;
        }

        public static object GetValue(Experiment ex, PropertyInfo p)
        {
            return p.GetValue(ex, null);
        }

        public virtual string GetDataPath(string ext = ".yaml")
        {
            var filename = Subject_ID + "_" + RecordSession + "_" + RecordSite + "_" + ID + "_";
            if (string.IsNullOrEmpty(DataDir))
            {
                DataDir = Directory.GetCurrentDirectory();
            }
            else
            {
                if (!Directory.Exists(DataDir))
                {
                    Directory.CreateDirectory(DataDir);
                }
            }
            var subjectdir = Path.Combine(DataDir, Subject_ID);
            if (!Directory.Exists(subjectdir))
            {
                Directory.CreateDirectory(subjectdir);
            }
            var fs = Directory.GetFiles(subjectdir, filename + "*" + ext, SearchOption.AllDirectories);
            if (fs.Length == 0)
            {
                filename = filename + "1" + ext;
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
                filename = filename + (ns.Max() + 1).ToString() + ext;
            }
            DataPath = Path.Combine(subjectdir, filename);
            return DataPath;
        }
    }

    public enum CONDSTATE
    {
        NONE = 1,
        PREICI,
        COND,
        SUFICI
    }

    public enum TRIALSTATE
    {
        NONE = 1001,
        PREITI,
        TRIAL,
        SUFITI
    }

    public enum BLOCKSTATE
    {
        NONE = 2001,
        PREIBI,
        BLOCK,
        SUFIBI
    }

    public enum EXPERIMENTSTATE
    {
        NONE = 3001,
        PREIEI,
        EXPERIMENT,
        SUFIEI
    }

    public enum TASTSTATE
    {
        NONE = 4001,
        FIXTARGET_ON,
        FIX_ACQUIRED,
        TARGET_ON,
        TARGET_CHANGE,
        AXISFORCED,
        REACTIONALLOWED,
        FIGARRAY_ON,
        FIGFIX_ACQUIRED,
        FIGFIX_LOST
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

    public enum CONDTESTPARAM
    {
        CondIndex,
        CondRepeat,
        CONDSTATE,
        TRIALSTATE,
        BLOCKSTATE,
        TASKSTATE
    }

    public enum SampleMethod
    {
        Ascending,
        Descending,
        UniformWithReplacement,
        UniformWithoutReplacement
    }
}
