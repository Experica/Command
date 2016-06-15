using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

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
        public int analysispercondtest { get; set; }
        public Dictionary<string, object> param { get; set; }
        public List<string> exinheritparams { get; set; }
        public List<string> envinheritparams { get; set; }
        public List<string> condtestnotifyparams { get; set; }


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

        public virtual string CondTestPath(string ext = ".yaml")
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
            var subjectdir = Path.Combine(condtestdir, subject_id);
            if (!Directory.Exists(subjectdir))
            {
                Directory.CreateDirectory(subjectdir);
            }
            var fs = Directory.GetFiles(subjectdir, filename + "*.yaml", SearchOption.AllDirectories);
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

    public enum SampleMethod
    {
        Ascending = 0,
        Descending = 1,
        UniformWithReplacement = 2,
        UniformWithoutReplacement = 3
    }
}
