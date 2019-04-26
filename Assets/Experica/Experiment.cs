/*
Experiment.cs is part of the Experica.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System;
using Fasterflect;
using MsgPack.Serialization;

namespace Experica
{
    public class PropertyAccess
    {
        public MemberGetter Getter { get; }
        public MemberSetter Setter { get; }
        public Type Type { get; }
        public string Name { get; }

        public PropertyAccess(Type t, string n, MemberGetter g, MemberSetter s)
        {
            Type = t;
            Name = n;
            Getter = g;
            Setter = s;
        }

        public PropertyAccess(Type reflectedtype, string propertyname)
        {
            //var t = reflectedtype.GetProperty(propertyname);
            Type = reflectedtype.GetProperty(propertyname).PropertyType;
            Name = propertyname;
            Getter = reflectedtype.DelegateForGetPropertyValue(propertyname);
            Setter = reflectedtype.DelegateForSetPropertyValue(propertyname);
        }
    }

    public class MethodAccess
    {
        public MethodInvoker Call { get; }
        public string Name { get; }

        public MethodAccess(string n, MethodInvoker m)
        {
            Name = n;
            Call = m;
        }

        public MethodAccess(Type reflectedtype, string methodname)
        {
            Name = methodname;
            var minfo = reflectedtype.GetMethod(methodname);
            Call = reflectedtype.DelegateForCallMethod(methodname, minfo.GetParameters().Select(i => i.ParameterType).ToArray());
        }
    }

    /// <summary>
    /// Holds all information that define an experiment
    /// </summary>
    public class Experiment
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Designer { get; set; } = "";
        public string Experimenter { get; set; } = "";
        public string Log { get; set; } = "";

        public string Subject_ID { get; set; } = "";
        public string Subject_Name { get; set; } = "";
        public string Subject_Species { get; set; } = "";
        public Gender Subject_Gender { get; set; }
        public float Subject_Age { get; set; }
        public Vector3 Subject_Size { get; set; }
        public float Subject_Weight { get; set; }
        public string Subject_Log { get; set; } = "";

        public string EnvPath { get; set; } = "";
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<string, object> EnvParam { get; set; } = new Dictionary<string, object>();
        public string CondPath { get; set; } = "";
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<string, IList> Cond { get; set; }
        public string ExLogicPath { get; set; } = "";

        public string RecordSession { get; set; } = "";
        public string RecordSite { get; set; } = "";
        public string DataDir { get; set; } = "";
        public string DataPath { get; set; } = "";
        public SampleMethod CondSampling { get; set; }
        public SampleMethod BlockSampling { get; set; }
        public int CondRepeat { get; set; }
        public int BlockRepeat { get; set; }
        public List<string> BlockParam { get; set; } = new List<string>();
        public InputMethod Input { get; set; }

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
        public List<CONDTESTPARAM> NotifyParam { get; set; }
        public List<string> ExInheritParam { get; set; } = new List<string>();
        public List<string> EnvInheritParam { get; set; } = new List<string>();
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<string, object> Param { get; set; } = new Dictionary<string, object>();
        public double TimerDriftSpeed { get; set; }
        public EventSyncProtocol EventSyncProtocol { get; set; } = new EventSyncProtocol();
        public string Display_ID { get; set; } = "";
        public double ResponseDelay { get; set; }
        public uint Version { get; set; } = 2;
        public CommandConfig Config { get; set; }

        [MessagePackIgnore]
        public Dictionary<CONDTESTPARAM, List<object>> CondTest { get; set; }
        [MessagePackIgnore]
        public CONDTESTSHOWLEVEL CondTestShowLevel { get; set; }
        [MessagePackIgnore]
        public bool SendMail { get; set; } = false;
        [MessagePackIgnore]
        public static readonly Dictionary<string, PropertyAccess> Properties;
        [MessagePackIgnore]
        public Action<string, object> OnNotifyUI;

        static Experiment()
        {
            var T = typeof(Experiment);
            Properties = new Dictionary<string, PropertyAccess>();
            foreach (var p in T.GetProperties())
            {
                var n = p.Name;
                Properties[n] = new PropertyAccess(p.PropertyType, n,
                    T.DelegateForGetPropertyValue(n), T.DelegateForSetPropertyValue(n));
            }
        }

        public bool SetParam(string name, object value, bool notifyui = false)
        {
            if (!SetProperty(name, value, notifyui))
            {
                if (Param.ContainsKey(name))
                {
                    Param[name] = value;
                    if (notifyui && OnNotifyUI != null)
                    {
                        OnNotifyUI(name, Param[name]);
                    }
                    return true;
                }
                return false;
            }
            return true;
        }

        public bool SetProperty(string name, object value, bool notifyui = false)
        {
            if (Properties.ContainsKey(name))
            {
                return SetProperty(this, Properties[name], value, notifyui);
            }
            return false;
        }

        public bool SetProperty(Experiment ex, PropertyAccess p, object value, bool notifyui = false)
        {
            object v = value.Convert(p.Type);
            p.Setter(ex, v);
            if (notifyui && OnNotifyUI != null)
            {
                OnNotifyUI(p.Name, v);
            }
            return true;
        }

        public object GetParam(string name)
        {
            var v = GetProperty(name);
            if (v == null)
            {
                if (Param.ContainsKey(name))
                {
                    v = Param[name];
                }
            }
            return v;
        }

        public object GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return GetProperty(this, Properties[name]);
            }
            return null;
        }

        public object GetProperty(Experiment ex, PropertyAccess p)
        {
            return p.Getter(ex);
        }

        public virtual string GetDataPath(string ext = "", string searchext = "yaml")
        {
            if (string.IsNullOrEmpty(DataPath))
            {
                var sessionsite = string.Join("_", new[] { RecordSession, RecordSite }.Where(i => !string.IsNullOrEmpty(i)).ToArray());
                var filename = string.Join("_", new[] { Subject_ID, RecordSession, RecordSite, ID }.Where(i => !string.IsNullOrEmpty(i)).ToArray());
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
                var sessionsitedir = Path.Combine(subjectdir, sessionsite);
                if (!Directory.Exists(sessionsitedir))
                {
                    Directory.CreateDirectory(sessionsitedir);
                }
                var fs = Directory.GetFiles(sessionsitedir, $"{filename}*.{ searchext}", SearchOption.TopDirectoryOnly);
                if (fs.Length == 0)
                {
                    filename = filename + "_1" + (string.IsNullOrEmpty(ext) ? "" : $".{ext}");
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
                    filename = $"{filename}_{ns.Max() + 1}" + (string.IsNullOrEmpty(ext) ? "" : $".{ext}");
                }
                DataPath = Path.Combine(sessionsitedir, filename);
            }
            else
            {
                var ddir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(ddir))
                {
                    Directory.CreateDirectory(ddir);
                }
                var fname = Path.GetFileNameWithoutExtension(DataPath) + (string.IsNullOrEmpty(ext) ? "" : $".{ext}");
                DataPath = Path.Combine(ddir, fname);
            }
            return DataPath;
        }
    }

    public enum Gender
    {
        Male,
        Female,
        Others
    }

    public enum DisplayType
    {
        CRT,
        LCD,
        Projector
    }

    public class Display
    {
        public string ID { get; set; } = "";
        public DisplayType Type { get; set; } = DisplayType.CRT;
        public double Latency { get; set; } = 0;
        public int CLUTSize { get; set; } = 16;
        public DisplayFitType FitType { get; set; } = DisplayFitType.Gamma;
        public Dictionary<string, List<object>> IntensityMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Dictionary<string, List<object>> SpectralMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Texture2D CLUT;
    }

    public enum DisplayFitType
    {
        Gamma,
        LinearSpline,
        CubicSpline
    }

    public enum InputMethod
    {
        None,
        Joystick
    }

    public enum SampleMethod
    {
        Manual,
        Ascending,
        Descending,
        UniformWithReplacement,
        UniformWithoutReplacement
    }

    public class EventSyncProtocol
    {
        public List<SyncMethod> SyncMethods { get; set; } = new List<SyncMethod>() { SyncMethod.ParallelPort, SyncMethod.Display };
        public uint nSyncChannel { get; set; } = 1;
        public uint nSyncpEvent { get; set; } = 1;
    }

    public enum SyncMethod
    {
        ParallelPort,
        Display
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
        NONE = 101,
        PREITI,
        TRIAL,
        SUFITI
    }

    public enum BLOCKSTATE
    {
        NONE = 201,
        PREIBI,
        BLOCK,
        SUFIBI
    }

    public enum TASKSTATE
    {
        NONE = 301,
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

    public enum CONDTESTSHOWLEVEL
    {
        NONE,
        SHORT,
        FULL
    }
}
