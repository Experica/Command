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
using System.IO;
using System.Linq;
using System;
using MessagePack;
using System.Runtime.CompilerServices;

namespace Experica.Command
{
    /// <summary>
    /// Holds all information that define an experiment,
    /// with reflection for properties warpped for UI DataSource Binding
    /// </summary>
    public class Experiment : DataClass
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
        public Dictionary<string, object> EnvParam { get; set; } = new();
        public string CondPath { get; set; } = "";
        public Dictionary<string, IList> Cond { get; set; } = new();
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
        public List<string> BlockFactor { get; set; } = new();

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
        public List<string> EnvInheritParam { get; set; } = new();
        public double TimerDriftSpeed { get; set; } = 6e-5;
        public EventSyncProtocol EventSyncProtocol { get; set; } = new();
        public string Display_ID { get; set; } = "";
        public CONDTESTSHOW CondTestShow { get; set; } = CONDTESTSHOW.ALL;
        public bool NotifyExperimenter { get; set; } = false;
        public uint Version { get; set; } = Experica.ExperimentVersion;

        [IgnoreMember]
        public CommandConfig Config { get; set; }
        [IgnoreMember]
        public Dictionary<string, IList> CondTest { get; set; }


        public bool HasCondTestState() => CondTestAtState != CONDTESTATSTATE.NONE;

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
        /// Exclude config and data, only save experiment definition
        /// </summary>
        /// <param name="filepath"></param>
        public void SaveDefinition(string filepath)
        {
            Version = Experica.ExperimentVersion;
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

        /// <summary>
        /// Exclude data, validation and update extendproperties for experiment ready to run
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public Experiment PrepareDefinition(CommandConfig cfg)
        {
            Cond = null;
            CondTest = null;
            if (string.IsNullOrEmpty(Name))
            {
                Name = ID;
            }
            if (string.IsNullOrEmpty(Subject_Name))
            {
                Subject_Name = Subject_ID;
            }

            if (cfg != null)
            {
                Config = cfg;
                if (string.IsNullOrEmpty(DataDir))
                {
                    DataDir = Config.DataDir;
                    if (!Directory.Exists(DataDir))
                    {
                        Directory.CreateDirectory(DataDir);
                        Debug.Log($"Create Data Directory \"{DataDir}\".");
                    }
                }
                NotifyParam ??= Config.NotifyParams;
            }

            RefreshExtendProperties();
            return this;
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
        public EventSyncRoute[] Routes { get; set; } = new[] { EventSyncRoute.DigitalOut, EventSyncRoute.Display };
        public uint NChannel { get; set; } = 1;
        public uint NEdgePEvent { get; set; } = 1;
    }

    public enum EventSyncRoute
    {
        DigitalOut,
        Display
    }

    public enum CONDSTATE
    {
        NONE = 1,
        PREICI,
        COND,
        SUFICI,
        ICI
    }

    public enum TRIALSTATE
    {
        NONE = 101,
        PREITI,
        TRIAL,
        SUFITI,
        ITI
    }

    public enum BLOCKSTATE
    {
        NONE = 201,
        PREIBI,
        BLOCK,
        SUFIBI,
        IBI
    }

    public enum TASKRESULT
    {
        NONE = 301,
        TIMEOUT,
        EARLY,
        HIT,
        MISS
    }

    //public enum TASKSTATE
    //{
    //    NONE = 301,
    //    FIXTARGET_ON,
    //    FIX_ACQUIRED,
    //    TARGET_ON,
    //    TARGET_CHANGE,
    //    AXISFORCED,
    //    REACTIONALLOWED,
    //    FIGARRAY_ON,
    //    FIGFIX_ACQUIRED,
    //    FIGFIX_LOST
    //}

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

    public enum EnterStateCode
    {
        Success = 0,
        Failure,
        AlreadyIn,
        ExFinish
    }

    public enum CONDTESTSHOW
    {
        NONE,
        ONE_SHORT,
        ONE,
        ALL_SHORT,
        ALL
    }

    public enum EXPERIMENTSTATUS
    {
        NONE,
        RUNNING,
        PAUSED,
        STOPPED
    }
}