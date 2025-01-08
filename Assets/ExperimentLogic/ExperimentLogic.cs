/*
ExperimentLogic.cs is part of the Experica.
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
using System;
using System.IO;
using System.Threading;
using System.Linq;
using Experica.NetEnv;
using MathNet.Numerics.Random;
using Unity.Netcode;

namespace Experica.Command
{
    public class ExperimentLogic : MonoBehaviour, IDisposable
    {
        #region Disposable
        int disposecount = 0;

        ~ExperimentLogic()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (1 == Interlocked.Exchange(ref disposecount, 1))
            {
                return;
            }
            if (disposing)
            {
                recorder?.Dispose();
            }
        }
        #endregion

        public AppManager appmgr;
        public Experiment ex;
        public Timer timer = new();
        public NetEnvManager envmgr = new();
        public ConditionManager condmgr = new();
        public ConditionTestManager condtestmgr = new();
        public IRecorder recorder;
        public System.Random RNG = new MersenneTwister();
        public bool forcepreparecond = true;
        public CondPushTarget condpushtarget = CondPushTarget.NetEnvManager;
        public List<string> pushexcludefactor;

        protected IGPIO gpio;
        protected bool syncstate;

        public Action
            /* called anywhere in update will guarantee:
            1. all updates will be received and processed in a whole in all Environment connected to Command,
               so that rendered frame is the same as the Command.
            2. logic will not resume until last frame has been fully processed by all Environment,
               so that no frame will ever dropped/missed by any Environment.
            */
            SyncFrame;

        bool issyncingframe = false, islogicactive = false;
        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime, PreIBIOnTime, BlockOnTime, SufIBIOnTime, SyncFrameOnTime;
        public double PreICIHold => timer.ElapsedMillisecond - PreICIOnTime;
        public double CondHold => timer.ElapsedMillisecond - CondOnTime;
        public double SufICIHold => timer.ElapsedMillisecond - SufICIOnTime;
        public double PreITIHold => timer.ElapsedMillisecond - PreITIOnTime;
        public double TrialHold => timer.ElapsedMillisecond - TrialOnTime;
        public double SufITIHold => timer.ElapsedMillisecond - SufITIOnTime;
        public double PreIBIHold => timer.ElapsedMillisecond - PreIBIOnTime;
        public double BlockHold => timer.ElapsedMillisecond - BlockOnTime;
        public double SufIBIHold => timer.ElapsedMillisecond - SufIBIOnTime;
        public double TimeMS => timer.ElapsedMillisecond;
        public CommandConfig Config => ex.Config;


        #region State Management
        CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState => condstate;
        protected virtual EnterStateCode EnterCondState(CONDSTATE value, bool sync = false)
        {
            if (value == condstate) { return EnterStateCode.AlreadyIn; }
            switch (value)
            {
                case CONDSTATE.PREICI:
                    PreICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREICI) // usually the start state of a condition test
                    {
                        if (condmgr.IsCondOfAndBlockRepeated(ex.CondRepeat, ex.BlockRepeat))
                        {
                            StartStopExperiment(false);
                            return EnterStateCode.ExFinish;
                        }
                        condtestmgr.NewCondTest();
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), PreICIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREICI)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondIndex), condmgr.CondIndex);
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondRepeat), condmgr.CurrentCondRepeat);
                            if (condmgr.NBlock > 1)
                            {
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockIndex), condmgr.BlockIndex);
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockRepeat), condmgr.CurrentBlockRepeat);
                            }
                        }
                    }
                    break;
                case CONDSTATE.COND:
                    CondOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), CondOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.COND)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondIndex), condmgr.CondIndex);
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondRepeat), condmgr.CurrentCondRepeat);
                            if (condmgr.NBlock > 1)
                            {
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockIndex), condmgr.BlockIndex);
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockRepeat), condmgr.CurrentBlockRepeat);
                            }
                        }
                    }
                    break;
                case CONDSTATE.SUFICI:
                    SufICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), SufICIOnTime);
                    }
                    break;
            }
            condstate = value;
            if (sync) { SyncEvent(value.ToString()); }
            return EnterStateCode.Success;
        }

        TRIALSTATE trialstate = TRIALSTATE.NONE;
        public TRIALSTATE TrialState => trialstate;
        protected virtual EnterStateCode EnterTrialState(TRIALSTATE value, bool sync = false)
        {
            if (value == trialstate) { return EnterStateCode.AlreadyIn; }
            switch (value)
            {
                case TRIALSTATE.PREITI:
                    PreITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREITI)
                    {
                        if (condmgr.IsCondOfAndBlockRepeated(ex.CondRepeat, ex.BlockRepeat))
                        {
                            StartStopExperiment(false);
                            return EnterStateCode.ExFinish;
                        }
                        condtestmgr.NewCondTest();
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), PreITIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREITI)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondIndex), condmgr.CondIndex);
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondRepeat), condmgr.CurrentCondRepeat);
                            if (condmgr.NBlock > 1)
                            {
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockIndex), condmgr.BlockIndex);
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockRepeat), condmgr.CurrentBlockRepeat);
                            }
                        }
                    }
                    break;
                case TRIALSTATE.TRIAL:
                    TrialOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), TrialOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.TRIAL)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondIndex), condmgr.CondIndex);
                            condtestmgr.Add(nameof(CONDTESTPARAM.CondRepeat), condmgr.CurrentCondRepeat);
                            if (condmgr.NBlock > 1)
                            {
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockIndex), condmgr.BlockIndex);
                                condtestmgr.Add(nameof(CONDTESTPARAM.BlockRepeat), condmgr.CurrentBlockRepeat);
                            }
                        }
                    }
                    break;
                case TRIALSTATE.SUFITI:
                    SufITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), SufITIOnTime);
                    }
                    break;
            }
            trialstate = value;
            if (sync) { SyncEvent(value.ToString()); }
            return EnterStateCode.Success;
        }

        BLOCKSTATE blockstate = BLOCKSTATE.NONE;
        public BLOCKSTATE BlockState => blockstate;
        protected virtual EnterStateCode EnterBlockState(BLOCKSTATE value, bool sync = false)
        {
            if (value == blockstate) { return EnterStateCode.AlreadyIn; }
            switch (value)
            {
                case BLOCKSTATE.PREIBI:
                    PreIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), PreIBIOnTime);
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    BlockOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), BlockOnTime);
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    SufIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), SufIBIOnTime);
                    }
                    break;
            }
            blockstate = value;
            if (sync) { SyncEvent(value.ToString()); }
            return EnterStateCode.Success;
        }
        #endregion


        #region Condition
        /// <summary>
        /// prepare condition based on `ex.CondPath` file
        /// </summary>
        protected virtual void PrepareCondition()
        {
            condmgr.PrepareCondition(ex.CondPath);
        }

        public void InitializeCondSampling(bool forceprepare = false)
        {
            if (forceprepare || condmgr.NCond == 0)
            {
                PrepareCondition();
            }
            ex.Cond = condmgr.Cond;
            condmgr.InitializeSampling(ex.CondSampling, ex.BlockSampling, ex.BlockFactor);
        }

        /// <summary>
        /// sample condition and block, then push all factors/value of the condition(except `pushexcludefactor`) to pushtargets
        /// </summary>
        /// <param name="manualcondindex"></param>
        /// <param name="manualblockindex"></param>
        /// <param name="autosampleblock">whether automatically sample block when condofblock finished repeating certain times, and push all factors/value instead of only none block factors</param>
        protected virtual void SamplePushCondition(int manualcondindex = 0, int manualblockindex = 0, bool autosampleblock = true, CondPushTarget pushtarget = CondPushTarget.NetEnvManager)
        {
            var ci = condmgr.SampleCondition(ex.CondRepeat, manualcondindex, manualblockindex, autosampleblock);
            PushCondition(ci, pushtarget, autosampleblock, pushexcludefactor);
        }

        protected virtual void SamplePushBlock(int manualblockindex = 0, CondPushTarget pushtarget = CondPushTarget.NetEnvManager)
        {
            var bi = condmgr.SampleBlockSpace(manualblockindex);
            PushBlock(bi, pushtarget, pushexcludefactor);
        }

        public void PushCondition(int ci, CondPushTarget pushtarget, bool includeblockfactor = false, List<string> excludefactor = null)
        {
            switch (pushtarget)
            {
                case CondPushTarget.NetEnvManager:
                    condmgr.PushCondition(ci, envmgr, includeblockfactor, pushexcludefactor);
                    break;
                case CondPushTarget.Experiment:
                    condmgr.PushCondition(ci, ex, includeblockfactor, pushexcludefactor);
                    break;
                case CondPushTarget.All:
                    var factors = condmgr.ConditionPushFactor(ci, includeblockfactor, pushexcludefactor);
                    if (factors == null) { break; }
                    foreach (var f in factors)
                    {
                        var v = condmgr.Cond[f][ci];
                        if (!envmgr.SetParam(f, v)) { ex.SetParam(f, v); }
                    }
                    break;
            }
        }

        public void PushBlock(int bi, CondPushTarget pushtarget, List<string> excludefactor = null)
        {
            switch (pushtarget)
            {
                case CondPushTarget.NetEnvManager:
                    condmgr.PushBlock(bi, envmgr, pushexcludefactor);
                    break;
                case CondPushTarget.Experiment:
                    condmgr.PushBlock(bi, ex, pushexcludefactor);
                    break;
                case CondPushTarget.All:
                    var factors = condmgr.BlockPushFactor(bi, pushexcludefactor);
                    if (factors == null) { break; }
                    foreach (var f in factors)
                    {
                        var v = condmgr.BlockCond[f][bi];
                        if (!envmgr.SetParam(f, v)) { ex.SetParam(f, v); }
                    }
                    break;
            }
        }
        #endregion


        #region Data
        /// <summary>
        /// user function for preparing `DataPath` for saving experiment data
        /// </summary>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        protected virtual string GetDataPath(DataFormat dataFormat)
        {
            var extension = dataFormat.ToString().ToLower();
            return ex.GetDataPath(ext: extension, searchext: extension);
        }

        public void SaveData(bool force = false)
        {
            if (!force)
            {
                if (ex.Config.AutoSaveData)
                {
                    if (ex.CondTestAtState == CONDTESTATSTATE.NONE) { return; }
                }
                else { return; }
            }

            ex.CondTest = condtestmgr.CondTest;
            ex.EnvParam = envmgr.GetParams();
            ex.Version = Base.ExperimentVersion;
            // Hold references to data that may not need to save
            Dictionary<string, Dictionary<string, List<object>>[]> m = null;
            CommandConfig cfg = ex.Config;
            if (!cfg.SaveConfigInData)
            {
                ex.Config = null;
            }
            else
            {
                if (!cfg.SaveConfigDisplayMeasurementInData)
                {
                    m = cfg.Display.ToDictionary(kv => kv.Key, kv => new[] { kv.Value.IntensityMeasurement, kv.Value.SpectralMeasurement });
                    foreach (var d in cfg.Display.Values)
                    {
                        d.IntensityMeasurement = null;
                        d.SpectralMeasurement = null;
                    }
                }
            }

            GetDataPath(cfg.SaveDataFormat).Save(ex);

            ex.CondTest = null;
            // Restore data that may not be saved
            if (!cfg.SaveConfigInData)
            {
                ex.Config = cfg;
            }
            else
            {
                if (!cfg.SaveConfigDisplayMeasurementInData)
                {
                    foreach (var d in cfg.Display.Keys)
                    {
                        cfg.Display[d].IntensityMeasurement = m[d][0];
                        cfg.Display[d].SpectralMeasurement = m[d][1];
                    }
                }
            }
        }
        #endregion


        #region Access Experiment and NetEnv
        public T GetExParam<T>(string name) => ex.GetParam<T>(name);

        public object GetExParam(string name) => ex.GetParam(name);

        public void SetExParam(string name, object value) => ex.SetParam(name, value);


        public T GetEnvActiveParam<T>(string name) => envmgr.GetActiveParam<T>(name);

        public T GetEnvParam<T>(string name) => envmgr.GetParam<T>(name);

        public object GetEnvActiveParam(string name) => envmgr.GetActiveParam(name);

        public object GetEnvParam(string name) => envmgr.GetParam(name);

        public T GetEnvActiveParamByGameObject<T>(string nvName, string goName) => envmgr.GetActiveParamByGameObject<T>(nvName, goName);

        public T GetEnvParamByGameObject<T>(string nvName, string goName) => envmgr.GetParamByGameObject<T>(nvName, goName);

        public object GetEnvActiveParamByGameObject(string nvName, string goName) => envmgr.GetActiveParamByGameObject(nvName, goName);

        public object GetEnvParamByGameObject(string nvName, string goName) => envmgr.GetParamByGameObject(nvName, goName);

        public void SetEnvActiveParam(string name, object value) => envmgr.SetActiveParam(name, value);

        public bool SetEnvParam(string name, object value) => envmgr.SetParam(name, value);

        public void SetEnvActiveParamByGameObject(string nvName, string goName, object value) => envmgr.SetActiveParamByGameObject(nvName, goName, value);

        public void SetEnvParamByGameObject(string nvName, string goName, object value) => envmgr.SetParamByGameObject(nvName, goName, value);


        public void WaitSetEnvActiveParam(float waittime_ms, string name, object value, bool notifyui = true)
        {
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(waittime_ms, name, value, notifyui));
        }

        IEnumerator WaitSetEnvActiveParam_Coroutine(float waittime_ms, string name, object value, bool notifyui = true)
        {
            var settime = Time.realtimeSinceStartup + waittime_ms / 1000;
            while (Time.realtimeSinceStartup < settime)
            {
                yield return null;
            }
            envmgr.SetActiveParam(name, value);
        }

        public void SetEnvActiveParamTwice(string name, object value1, float interval_ms, object value2, bool notifyui = false)
        {
            SetEnvActiveParamTwice(name, value1, interval_ms, name, value2, notifyui);
        }

        public void SetEnvActiveParamTwice(string name1, object value1, float interval_ms, string name2, object value2, bool notifyui = true)
        {
            envmgr.SetActiveParam(name1, value1);
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(interval_ms, name2, value2, notifyui));
        }
        #endregion


        #region Experiment Control
        public void PauseResumeExperiment(bool ispause)
        {
            if (ispause)
            {
                appmgr.OnBeginPauseExperiment();
                PauseExperiment();
                appmgr.OnEndPauseExperiment();
            }
            else
            {
                appmgr.OnBeginResumeExperiment();
                ResumeExperiment();
                appmgr.OnEndResumeExpeirment();
            }
        }

        protected virtual void PauseExperiment()
        {
            islogicactive = false;
            timer.Stop();
            Time.timeScale = 0;
        }

        protected virtual void ResumeExperiment()
        {
            Time.timeScale = 1;
            timer.Start();
            islogicactive = true;
        }

        /// <summary>
        /// main function to control experiment start/stop, following various steps in the starting/stopping process.
        /// </summary>
        /// <param name="isstart"></param>
        public void StartStopExperiment(bool isstart)
        {
            if (isstart == islogicactive) { return; }
            if (isstart)
            {
                appmgr.OnBeginStartExperiment();
                // clean for new experiment
                condstate = CONDSTATE.NONE;
                trialstate = TRIALSTATE.NONE;
                blockstate = BLOCKSTATE.NONE;
                condtestmgr.Clear();
                /* 
                 * clear `DataPath` for new experiment, 
                 * so that new `DataPath` could be generated later, 
                 * preventing overwriting existing files.
                 */
                ex.DataPath = null;

                OnStartExperiment();
                InitializeCondSampling(forcepreparecond);
                StartCoroutine(ExperimentStartSequence());
            }
            else
            {
                appmgr.OnBeginStopExperiment();
                OnStopExperiment();
                islogicactive = false;

                // Push any condtest left
                condtestmgr.PushCondTest(timer.ElapsedMillisecond, ex.NotifyParam, ex.NotifyPerCondTest, true, true);
                StartCoroutine(ExperimentStopSequence());
            }
        }

        /// <summary>
        /// user function for clean/init of new experiment,
        /// here init gpio and inactive sync state.
        /// </summary>
        protected virtual void OnStartExperiment()
        {
            if (ex.EventSyncProtocol.Routes.Contains(EventSyncRoute.DigitalOut))
            {
                gpio = new ParallelPort(dataaddress: Config.ParallelPort0);
                //if (!gpio.Found)
                //{
                //    gpio = new FTDIGPIO();
                //}
                if (!gpio.Found)
                {
                    // gpio = new MCCDevice(config.MCCDevice, config.MCCDPort);
                }
                if (!gpio.Found)
                {
                    Debug.LogWarning("No GPIO for DigitalOut EventSyncRoute.");
                }
            }
            SyncEvent();
        }

        protected IEnumerator ExperimentStartSequence()
        {
            // sync several frames to make sure Command and all connected Environment have been initialized to the same start frame.
            var n = 4 + QualitySettings.maxQueuedFrames;
            for (var i = 0; i < n; i++)
            {
                SyncFrame?.Invoke();
                yield return null;
                while (issyncingframe)
                {
                    yield return null;
                }
            }
            // wait until the synced same start frame have been presented on display.
            var dur = n * ex.Display_ID.DisplayLatencyPlusResponseTime(Config.Display) ?? Config.NotifyLatency;
            yield return new WaitForSecondsRealtime((float)dur / 1000f);

            StartExperimentTimeSync();
            // wait for all timelines have been started and synced.
            yield return new WaitForSecondsRealtime(Config.NotifyLatency / 1000f);
            OnExperimentStarted();
            appmgr.OnEndStartExperiment();
            islogicactive = true;
        }

        /// <summary>
        /// user function for starting timeline sync of all hardware/software systems involved in experiment
        /// </summary>
        protected virtual void StartExperimentTimeSync()
        {
            timer.Restart();
        }

        /// <summary>
        /// empty user function when experiment started(time synced)
        /// </summary>
        protected virtual void OnExperimentStarted()
        {
        }

        /// <summary>
        /// empty user function before experiment stop
        /// </summary>
        protected virtual void OnStopExperiment()
        {
        }

        protected IEnumerator ExperimentStopSequence()
        {
            // sync several frames to make sure Command and all connected Environment have been set to the same stop frame.
            var n = 4 + QualitySettings.maxQueuedFrames;
            for (var i = 0; i < n; i++)
            {
                SyncFrame?.Invoke();
                yield return null;
                while (issyncingframe)
                {
                    yield return null;
                }
            }
            // wait until the synced same stop frame have been presented on display.
            var dur = n * ex.Display_ID.DisplayLatencyPlusResponseTime(Config.Display) ?? Config.NotifyLatency;
            yield return new WaitForSecondsRealtime((float)dur / 1000f);

            StopExperimentTimeSync();
            // wait for all timelines have been synced and stopped.
            yield return new WaitForSecondsRealtime(Config.NotifyLatency / 1000f);
            OnExperimentStopped();
            appmgr.OnEndStopExperiment();
        }

        /// <summary>
        /// user function for stopping timeline sync of all hardware/software systems involved in experiment
        /// </summary>
        protected virtual void StopExperimentTimeSync()
        {
            timer.Stop();
        }

        /// <summary>
        /// user function after experiment stopped(time synced),
        /// here return to inactive sync state and release gpio
        /// </summary>
        protected virtual void OnExperimentStopped()
        {
            SyncEvent();
            if (gpio != null) { gpio.Dispose(); gpio = null; }
        }
        #endregion


        #region Unity Event Callback
        void Awake()
        {
            OnAwake();
        }
        /// <summary>
        /// empty user virtual "Awake"
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        void OnEnable()
        {
            Enable();
        }
        /// <summary>
        /// empty user virtual "OnEnable"
        /// </summary>
        protected virtual void Enable()
        {
        }

        void Start()
        {
            OnStart();
        }
        /// <summary>
        /// empty user virtual "Start"
        /// </summary>
        protected virtual void OnStart()
        {
        }

        void Update()
        {
            OnUpdate();
            if (issyncingframe)
            {
                if (Time.realtimeSinceStartupAsDouble - SyncFrameOnTime >= Config.SyncFrameTimeOut)
                {
                    Debug.Log($"SyncFrame Timeout({Config.SyncFrameTimeOut}s), Stop Waiting.");
                    issyncingframe = false;
                }
            }
            else
            {
                if (islogicactive) { Logic(); }
            }
        }
        /// <summary>
        /// empty user virtual "Update"
        /// </summary>
        protected virtual void OnUpdate()
        {
        }
        /// <summary>
        /// empty user virtual Logic of experiment
        /// </summary>
        protected virtual void Logic()
        {
        }

        void OnDisable()
        {
            Disable();
        }
        /// <summary>
        /// empty user virtual "OnDisable"
        /// </summary>
        protected virtual void Disable()
        {
        }


        /// <summary>
        /// empty user virtual function called when server and connected clients have loaded and parsed scene,
        /// here we do player spawning for each connected clients
        /// </summary>
        public virtual void OnSceneReady(List<ulong> clientids)
        {
        }

        /// <summary>
        /// empty user virtual function called when scene and players all loaded/parsed, 
        /// here we do scene decoration, such as guide spawning
        /// </summary>
        public virtual void OnPlayerReady()
        {
        }

        /// <summary>
        /// empty user virtual property for toggling logic specific visual guides
        /// </summary>
        public virtual bool Guide { get; set; }

        /// <summary>
        /// empty user virtual property for toggling logic specific visual guides Network Visibility(show/hide from connected clients)
        /// </summary>
        public virtual bool NetVisible { get; set; }
        #endregion


        #region IO Function
        /// <summary>
        /// Sync to External Device and Register Event Name/Time/Value according to EventSyncProtocol
        /// </summary>
        /// <param name="name">Event Name, NullorEmpty will Reset Sync Channel to inactive state without register</param>
        /// <param name="time">Event Time, Non-NaN value will register in `Event`</param>
        /// <param name="value">Event Value, Non-Null value will register for event name</param>
        protected virtual void SyncEvent(string name = null, double time = double.NaN, object value = null)
        {
            var esp = ex.EventSyncProtocol;
            if (esp.Routes == null || esp.Routes.Length == 0)
            {
                Debug.LogWarning("No SyncRoute in EventSyncProtocol, Skip SyncEvent ...");
                return;
            }
            bool addtosynclist = false;
            bool syncreset = string.IsNullOrEmpty(name);

            // binary flip signal in one channel
            if (esp.NChannel == 1 && esp.NEdgePEvent == 1)
            {
                addtosynclist = !syncreset;
                syncstate = addtosynclist && !syncstate;

                for (var i = 0; i < esp.Routes.Length; i++)
                {
                    switch (esp.Routes[i])
                    {
                        case EventSyncRoute.Display:
                            foreach (var c in envmgr.MainCamera)
                            {
                                SetEnvActiveParamByGameObject("Mark", NetEnvManager.GetGameObjectFullName(c.gameObject) + '/' + "Marker", syncstate);
                            }
                            break;
                        case EventSyncRoute.DigitalOut:
                            gpio?.BitOut(bit: Config.EventSyncCh, value: syncstate);
                            break;
                    }
                }
            }
            if (addtosynclist && ex.CondTestAtState != CONDTESTATSTATE.NONE)
            {
                if (!double.IsNaN(time))
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), name, time);
                }
                condtestmgr.AddInList(nameof(CONDTESTPARAM.SyncEvent), name);
                if (value != null)
                {
                    condtestmgr.AddInList(name, value);
                }
            }
        }
        #endregion

    }
}