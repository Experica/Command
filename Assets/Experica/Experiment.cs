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
using MessagePack;

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
            // comment/uncomment the following line to trigger unity compile will solve the issuse.
            var t = reflectedtype.GetProperty(propertyname);
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
        public Gender Subject_Gender { get; set; } = Gender.None;
        public string Subject_Birth { get; set; } = "";
        public Vector3 Subject_Size { get; set; } = Vector3.zero;
        public float Subject_Weight { get; set; } = 0;
        public string Subject_Log { get; set; } = "";

        public string EnvPath { get; set; } = "";
        public Dictionary<string, object> EnvParam { get; set; } = new Dictionary<string, object>();
        public string CondPath { get; set; } = "";
        public Dictionary<string, IList> Cond { get; set; } = new Dictionary<string, IList>();
        public string LogicPath { get; set; } = "";

        public Hemisphere Hemisphere { get; set; } = Hemisphere.None;
        public Eye Eye { get; set; } = Eye.None;
        public string RecordSession { get; set; } = "";
        public string RecordSite { get; set; } = "";
        public string DataDir { get; set; } = "";
        public string DataPath { get; set; } = "";
        public bool Input { get; set; } = false;

        public SampleMethod CondSampling { get; set; } = SampleMethod.UniformWithoutReplacement;
        public SampleMethod BlockSampling { get; set; } = SampleMethod.UniformWithoutReplacement;
        public int CondRepeat { get; set; } = 1;
        public int BlockRepeat { get; set; } = 1;
        public List<string> BlockParam { get; set; } = new List<string>();

        public double PreICI { get; set; } = 0;
        public double CondDur { get; set; } = 1000;
        public double SufICI { get; set; } = 0;
        public double PreITI { get; set; } = 0;
        public double TrialDur { get; set; } = 0;
        public double SufITI { get; set; } = 0;
        public double PreIBI { get; set; } = 0;
        public double BlockDur { get; set; } = 0;
        public double SufIBI { get; set; } = 0;

        public PUSHCONDATSTATE PushCondAtState { get; set; } = PUSHCONDATSTATE.COND;
        public CONDTESTATSTATE CondTestAtState { get; set; } = CONDTESTATSTATE.PREICI;
        public int NotifyPerCondTest { get; set; } = 0;
        public List<CONDTESTPARAM> NotifyParam { get; set; } = new List<CONDTESTPARAM>();
        public List<string> InheritParam { get; set; } = new List<string>();
        public List<string> EnvInheritParam { get; set; } = new List<string>();
        public Dictionary<string, object> Param { get; set; } = new Dictionary<string, object>();
        public double TimerDriftSpeed { get; set; } = 6e-5;
        public EventSyncProtocol EventSyncProtocol { get; set; } = new EventSyncProtocol();
        public string Display_ID { get; set; } = "";
        public CONDTESTSHOWLEVEL CondTestShowLevel { get; set; } = CONDTESTSHOWLEVEL.FULL;
        public bool NotifyExperimenter { get; set; } = false;
        public uint Version { get; set; } = Extension.ExperimentDataVersion;


        [IgnoreMember]
        public CommandConfig Config { get; set; }
        [IgnoreMember]
        public Dictionary<CONDTESTPARAM, List<object>> CondTest { get; set; }
        [IgnoreMember]
        public static readonly Dictionary<string, PropertyAccess> Properties;
        [IgnoreMember]
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
                    if (notifyui)
                    {
                        OnNotifyUI?.Invoke(name, Param[name]);
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

        bool SetProperty(Experiment ex, PropertyAccess p, object value, bool notifyui = false)
        {
            object v = value.Convert(p.Type);
            p.Setter(ex, v);
            if (notifyui)
            {
                OnNotifyUI?.Invoke(p.Name, v);
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

        object GetProperty(Experiment ex, PropertyAccess p)
        {
            return p.Getter(ex);
        }

        /// <summary>
        /// Prepare data path if `DataPath` is valid, otherwise create a new unique data path based on experiment parameters.
        /// </summary>
        /// <param name="ext">Data file extension</param>
        /// <param name="searchext">File extension with which the files were searched to get unique index of the new data name</param>
        /// <param name="addfiledir">Whether add a same name dir in data path</param>
        /// <returns></returns>
        public string GetDataPath(string ext = "", string searchext = ".yaml", bool addfiledir = false)
        {
            // make sure if ext and/or searchext, then it is in .* format
            if (!string.IsNullOrEmpty(ext) && !ext.StartsWith("."))
            {
                ext = "." + ext;
            }
            if (!string.IsNullOrEmpty(searchext) && !searchext.StartsWith("."))
            {
                searchext = "." + searchext;
            }
            if (string.IsNullOrEmpty(DataPath))
            {
                var subjectsessionsite = string.Join("_", new[] { Subject_ID, RecordSession, RecordSite }.Where(i => !string.IsNullOrEmpty(i)));
                var dataname = string.Join("_", new[] { subjectsessionsite, ID }.Where(i => !string.IsNullOrEmpty(i)));
                if (string.IsNullOrEmpty(dataname)) { return DataPath; }
                // Prepare Data Root Dir
                if (string.IsNullOrEmpty(DataDir))
                {
                    DataDir = Directory.GetCurrentDirectory();
                }
                var subjectdir = Path.Combine(DataDir, Subject_ID);
                var subjectsessionsitedir = Path.Combine(subjectdir, subjectsessionsite);
                // Prepare a new unique data file name
                var newindex = $"{dataname}_*{searchext}".SearchIndexForNewFile(subjectsessionsitedir, SearchOption.AllDirectories);
                var datafilename = $"{dataname}_{newindex}";
                var datadir = addfiledir ? Path.Combine(subjectsessionsitedir, datafilename) : subjectsessionsitedir;
                Directory.CreateDirectory(datadir);
                DataPath = Path.Combine(datadir, datafilename + (string.IsNullOrEmpty(ext) ? "" : ext));
            }
            else
            {
                var datadir = Path.GetDirectoryName(DataPath);
                var datafilename = Path.GetFileNameWithoutExtension(DataPath);
                Directory.CreateDirectory(datadir);
                DataPath = Path.Combine(datadir, datafilename + (string.IsNullOrEmpty(ext) ? "" : ext));
            }
            return DataPath;
        }

    }

    public enum Gender
    {
        None,
        Male,
        Female,
        Others
    }

    public enum Eye
    {
        None,
        Left,
        Right,
        Both
    }

    public enum Hemisphere
    {
        None,
        Left,
        Right,
        Both
    }

    public enum DisplayType
    {
        CRT,
        LCD,
        LED,
        Projector
    }

    public class Display
    {
        public string ID { get; set; } = "";
        public DisplayType Type { get; set; } = DisplayType.LCD;
        public double Latency { get; set; } = 0;
        public double RiseLag { get; set; } = 0;
        public double FallLag { get; set; } = 0;
        public int CLUTSize { get; set; } = 32;
        public DisplayFitType FitType { get; set; } = DisplayFitType.LinearSpline;
        public Dictionary<string, List<object>> IntensityMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Dictionary<string, List<object>> SpectralMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Texture3D CLUT;
    }

    public enum DisplayFitType
    {
        Gamma,
        LinearSpline,
        CubicSpline
    }

    public enum ColorSpace
    {
        RGB,
        HSL,
        XYZ,
        LMS,
        DKL,
        CAM
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
        public List<SyncMethod> SyncMethods { get; set; } = new List<SyncMethod>() { SyncMethod.GPIO, SyncMethod.Display };
        public uint nSyncChannel { get; set; } = 1;
        public uint nSyncpEvent { get; set; } = 1;
    }

    public enum SyncMethod
    {
        GPIO,
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

    public enum EnterCode
    {
        Success = 0,
        Failure,
        AlreadyIn,
        NoNeed
    }

    public enum CONDTESTSHOWLEVEL
    {
        NONE,
        SHORT,
        FULL
    }

    public enum EXPERIMENTSTATUS
    {
        NONE,
        STARTING,
        RUNNING,
        STOPPING,
        STOPPED
    }
}
