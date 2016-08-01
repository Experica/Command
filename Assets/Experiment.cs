// -----------------------------------------------------------------------------
// Experiment.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System;
using Fasterflect;

namespace VLab
{
    public class PropertyAccess
    {
        public MemberGetter getter;
        public MemberSetter setter;
        public Type type;
        public string name;

        public PropertyAccess(Type t,string n ,MemberGetter g,MemberSetter s)
        {
            type = t;
            name = n;
            getter = g;
            setter = s;
        }

        public PropertyAccess(Type reflectedtype,string propertyname):
            this( reflectedtype.GetProperty(propertyname).PropertyType, propertyname,
                reflectedtype.DelegateForGetPropertyValue(propertyname),
                reflectedtype.DelegateForSetPropertyValue(propertyname))
        {
        }
    }

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
        public GENDER Subject_Gender { get; set; }
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

        public static readonly Dictionary<string, PropertyAccess> Properties;
        public Action<string, object> OnNotifyUI;

        static Experiment()
        {
            var T = typeof(Experiment);
            Properties = new Dictionary<string, PropertyAccess>();
            foreach (var p in T.GetProperties())
            {
                var n = p.Name;
                Properties[n] = new PropertyAccess(p.PropertyType,n,
                    T.DelegateForGetPropertyValue(n),T.DelegateForSetPropertyValue(n));
            }
        }

        public void SetParam(string name, object value,bool notifyui=false)
        {
            SetValue(name, value);
            if (Param.ContainsKey(name))
            {
                Param[name] = value;
                if (OnNotifyUI != null)
                {
                    OnNotifyUI(name, value);
                }
            }
        }

        public void SetValue(string name, object value,bool notifyui=false)
        {
            if (Properties.ContainsKey(name))
            {
                SetValue(this, Properties[name], value,notifyui);
            }
        }

        public void SetValue(Experiment ex, PropertyAccess p, object value,bool notifyui=false)
        {
            object v = value.Convert(p.type);
            p.setter(ex, v);
            if(OnNotifyUI!=null)
            {
                OnNotifyUI(p.name, v);
            }
        }

        public object GetParam(string name)
        {
            var v = GetValue(name);
            if(v==null)
            {
                if(Param.ContainsKey(name))
                {
                    v = Param[name];
                }
            }
            return v;
        }

        public object GetValue(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return GetValue(this, Properties[name]);
            }
            return null;
        }

        public static object GetValue(Experiment ex, PropertyAccess p)
        {
            return p.getter(ex);
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

    public enum GENDER
    {
        Male,
        Female,
        Others
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

    public enum TASKSTATE
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
