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
using Experica.NetEnv;
using UnityEditor;
using IceInternal;
using Unity.Properties;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;

namespace Experica.Command
{
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
        public Dictionary<string, object> EnvParam { get; set; } = new  ();
        public string CondPath { get; set; } = "";
        public Dictionary<string, IList> Cond { get; set; } = new ();
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
        public List<string> BlockParam { get; set; } = new();

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
        public List<CONDTESTPARAM> NotifyParam { get; set; } = new();
        public List<string> InheritParam { get; set; } = new();
        public List<string> EnvInheritParam { get; set; } = new ();
        public Dictionary<string, object> ExtendParam { get; set; } = new();
        public double TimerDriftSpeed { get; set; } = 6e-5;
        public EventSyncProtocol EventSyncProtocol { get; set; } = new();
        public string Display_ID { get; set; } = "";
        public CONDTESTSHOWLEVEL CondTestShowLevel { get; set; } = CONDTESTSHOWLEVEL.FULL;
        public bool NotifyExperimenter { get; set; } = false;
        public uint Version { get; set; } = ExpericaExtension.ExperimentDataVersion;

        [IgnoreMember]
        public CommandConfig Config { get; set; }
        [IgnoreMember]
        public Dictionary<CONDTESTPARAM, IList> CondTest { get; set; }

        [IgnoreMember]
        public Dictionary<string, PropertySource<Experiment>> Properties  = new();
        [IgnoreMember]
        public Dictionary<string, DictSource<object>> ExtendProperties= new();

        static Experiment()
        {
            var extype = typeof(Experiment);
            var extypename = extype.ToString();
            foreach (var p in extype.GetProperties())
            {
                extypename.StoreProperty(p.Name, new Property(p));             
            }
        }

        public void InitializeDataSource() 
        {
            var extype = typeof(Experiment);
            var extypename = extype.ToString();
            if(extypename.QueryProperties(out var properties))
            {
                Properties = properties.ToDictionary(kv => kv.Key, kv =>new PropertySource<Experiment>(this,kv.Value));
            }
            else { Debug.LogError($"Property Reflections of {extype} Not Initialized") ; }
            ExtendProperties = ExtendParam.ToDictionary(kv => kv.Key, kv => new DictSource<object>(ExtendParam, kv.Key));
        }

        public bool SetParam(string name, object value)
        {
            if (Properties.ContainsKey(name))
            {
                var p = Properties[name];
                p.Value = value.Convert(p.Type);
                return true;
            }
            if (ExtendProperties.ContainsKey(name))
            {
                ExtendProperties[name].Value = value;
                return true;
            }
            Debug.LogError($"Param: {name} not found in Experiment or Experiment.ExtendParam");
            return false;
        }

        public bool SetExtendProperty(string name, object value)
        {
            if (ExtendProperties.ContainsKey(name))
            {
                ExtendProperties[name].Value = value;
                return true;
            }
            Debug.LogError($"ExtendProperty: {name} not exist in Experiment.ExtendParam");
            return false;
        }

        public bool SetProperty(string name, object value)
        {
            if (Properties.ContainsKey(name))
            {
                var p = Properties[name];
                p.Value=value.Convert(p.Type);
                return true;
            }
            Debug.LogError($"Property: {name} not defined in Experiment");
            return false;
        }

        public T GetParam<T>(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return Properties[name].GetValue<T>();
            }
            if (ExtendProperties.ContainsKey(name))
            {
                return ExtendProperties[name].GetValue<T>();
            }
            Debug.LogError($"Param: {name} not found in Experiment or Experiment.ExtendParam, return default value of {typeof(T)} : {default}.");
            return default;
        }

        public object GetParam(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return Properties[name].Value;
            }
            if (ExtendProperties.ContainsKey(name))
            {
                return ExtendProperties[name].Value;
            }
            Debug.LogError($"Param: {name} not found in Experiment or Experiment.ExtendParam");
            return null;
        }

        public T GetExtendProperty<T>(string name)
        {
            if (ExtendProperties.ContainsKey(name))
            {
                return ExtendProperties[name].GetValue<T>();
            }
            Debug.LogError($"ExtendProperty: {name} not exist in Experiment.ExtendParam, return default value of {typeof(T)} : {default}.");
            return default;
        }

        public object GetExtendProperty(string name)
        {
            if (ExtendProperties.ContainsKey(name))
            {
                return ExtendProperties[name].Value;
            }
            Debug.LogError($"ExtendProperty: {name} not exist in Experiment.ExtendParam");
            return null;
        }

        public T GetProperty<T>(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return Properties[name].GetValue<T>();
            }
            Debug.LogError($"Property: {name} not defined in Experiment, return default value of {typeof(T)} : {default}.");
            return default;
        }

        public object GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return Properties[name].Value;
            }
            Debug.LogError($"Property: {name} not defined in Experiment");
            return null;
        }

        public bool ContainsParam(string name) {  return Properties.ContainsKey(name) || ExtendProperties.ContainsKey(name); }
        public bool ContainsExtendProperty(string name) {  return ExtendProperties.ContainsKey(name); }
        public bool ContainsProperty(string name)        {            return  Properties.ContainsKey(name);        }

        public void RemoveExtendProperty(string name)
        {
            ExtendProperties.Remove(name);
            ExtendParam.Remove(name);
            InheritParam.Remove(name);
        }

        public DictSource<object> AddExtendProperty(string name, object value)
        {
            ExtendParam[name] = value;
            DictSource<object> source = new(ExtendParam, name);
            ExtendProperties[name] = source;
            return source;
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

        /// <summary>
        /// Exclude config and data, only saving experiment definition
        /// </summary>
        /// <param name="filepath"></param>
        public void SaveDefinition(string filepath)
        {
            var config = Config;
            var cond = Cond;
            var condtest = CondTest;
            var datapath = DataPath;
            Config = null;
            Cond = null;
            CondTest = null;
            DataPath = null;
            try { filepath.WriteYamlFile(this); }
            catch (Exception ex) { Debug.LogException(ex); }
            finally
            {
                Config = config;
                Cond = cond;
                CondTest = condtest;
                DataPath = datapath;
            }
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
