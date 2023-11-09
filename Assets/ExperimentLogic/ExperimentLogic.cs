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

        public Experiment ex;
        public Experica.Timer timer = new();
        public NetEnvManager envmanager = new();
        public ConditionManager condmanager = new();
        public ConditionTestManager condtestmanager = new();
        public IRecorder recorder;
        public List<string> pushexcludefactors;

        public Action OnBeginStartExperiment, OnEndStartExperiment,
            OnBeginStopExperiment, OnEndStopExperiment,
            OnBeginPauseExperiment, OnEndPauseExperiment,
            OnBeginResumeExperiment, OnEndResumeExpeirment,
            OnConditionPrepared,
            /* called anywhere in update will guarantee:
            1. all updates will be received and processed in a whole in all Environment connected to Command,
               so that rendered frame is the same as the Command.
            2. logic will not resume until last frame has been fully processed by all Environment,
               so that no frame will ever dropped/missed by any Environment.
            */
            SyncFrame;

        public bool issyncingframe = false, islogicactive = false, regeneratecond = true;
        public double PreICIOnTime, CondOnTime, SufICIOnTime, PreITIOnTime,
            TrialOnTime, SufITIOnTime, PreIBIOnTime, BlockOnTime, SufIBIOnTime, SyncFrameOnTime;
        public double PreICIHold { get { return timer.ElapsedMillisecond - PreICIOnTime; } }
        public double CondHold { get { return timer.ElapsedMillisecond - CondOnTime; } }
        public double SufICIHold { get { return timer.ElapsedMillisecond - SufICIOnTime; } }
        public double PreITIHold { get { return timer.ElapsedMillisecond - PreITIOnTime; } }
        public double TrialHold { get { return timer.ElapsedMillisecond - TrialOnTime; } }
        public double SufITIHold { get { return timer.ElapsedMillisecond - SufITIOnTime; } }
        public double PreIBIHold { get { return timer.ElapsedMillisecond - PreIBIOnTime; } }
        public double BlockHold { get { return timer.ElapsedMillisecond - BlockOnTime; } }
        public double SufIBIHold { get { return timer.ElapsedMillisecond - SufIBIOnTime; } }
        /// <summary>
        /// Get the CommandConfig in the Experiment of the ExperimentLogic
        /// </summary>
        public CommandConfig Config { get { return ex.Config; } }

        CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState
        {
            get { return condstate; }
        }
        protected virtual EnterCode EnterCondState(CONDSTATE value)
        {
            if (value == condstate) { return EnterCode.AlreadyIn; }
            switch (value)
            {
                case CONDSTATE.NONE:
                    break;
                case CONDSTATE.PREICI:
                    PreICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREICI)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StartStopExperiment(false);
                            return EnterCode.NoNeed;
                        }
                        condtestmanager.NewCondTest(PreICIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), PreICIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREICI)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.CondIndex);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.CondIndex]);
                            if (condmanager.nblock > 1)
                            {
                                condtestmanager.Add(CONDTESTPARAM.BlockIndex, condmanager.blockidx);
                                condtestmanager.Add(CONDTESTPARAM.BlockRepeat, condmanager.blockrepeat[condmanager.blockidx]);
                            }
                        }
                    }
                    break;
                case CONDSTATE.COND:
                    CondOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), CondOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.COND)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.CondIndex);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.CondIndex]);
                            if (condmanager.nblock > 1)
                            {
                                condtestmanager.Add(CONDTESTPARAM.BlockIndex, condmanager.blockidx);
                                condtestmanager.Add(CONDTESTPARAM.BlockRepeat, condmanager.blockrepeat[condmanager.blockidx]);
                            }
                        }
                    }
                    break;
                case CONDSTATE.SUFICI:
                    SufICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), SufICIOnTime);
                    }
                    break;
            }
            condstate = value;
            return EnterCode.Success;
        }

        TRIALSTATE trialstate = TRIALSTATE.NONE;
        public TRIALSTATE TrialState
        {
            get { return trialstate; }
        }
        protected virtual EnterCode EnterTrialState(TRIALSTATE value)
        {
            if (value == trialstate) { return EnterCode.AlreadyIn; }
            switch (value)
            {
                case TRIALSTATE.NONE:
                    break;
                case TRIALSTATE.PREITI:
                    PreITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREITI)
                    {
                        if (condmanager.IsCondRepeat(ex.CondRepeat))
                        {
                            StartStopExperiment(false);
                            return EnterCode.NoNeed;
                        }
                        condtestmanager.NewCondTest(PreITIOnTime, ex.NotifyParam, ex.NotifyPerCondTest);
                    }
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), PreITIOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.PREITI)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.CondIndex);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.CondIndex]);
                            if (condmanager.nblock > 1)
                            {
                                condtestmanager.Add(CONDTESTPARAM.BlockIndex, condmanager.blockidx);
                                condtestmanager.Add(CONDTESTPARAM.BlockRepeat, condmanager.blockrepeat[condmanager.blockidx]);
                            }
                        }
                    }
                    break;
                case TRIALSTATE.TRIAL:
                    TrialOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), TrialOnTime);
                    }
                    if (ex.PushCondAtState == PUSHCONDATSTATE.TRIAL)
                    {
                        SamplePushCondition();
                        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                        {
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.CondIndex);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.CondIndex]);
                            if (condmanager.nblock > 1)
                            {
                                condtestmanager.Add(CONDTESTPARAM.BlockIndex, condmanager.blockidx);
                                condtestmanager.Add(CONDTESTPARAM.BlockRepeat, condmanager.blockrepeat[condmanager.blockidx]);
                            }
                        }
                    }
                    break;
                case TRIALSTATE.SUFITI:
                    SufITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), SufITIOnTime);
                    }
                    break;
            }
            trialstate = value;
            return EnterCode.Success;
        }

        BLOCKSTATE blockstate = BLOCKSTATE.NONE;
        public BLOCKSTATE BlockState
        {
            get { return blockstate; }
        }
        protected virtual EnterCode EnterBlockState(BLOCKSTATE value)
        {
            if (value == blockstate) { return EnterCode.AlreadyIn; }
            switch (value)
            {
                case BLOCKSTATE.NONE:
                    break;
                case BLOCKSTATE.PREIBI:
                    PreIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), PreIBIOnTime);
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    BlockOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), BlockOnTime);
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    SufIBIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
                    {
                        condtestmanager.AddInList(CONDTESTPARAM.Event, value.ToString(), SufIBIOnTime);
                    }
                    break;
            }
            blockstate = value;
            return EnterCode.Success;
        }

        protected virtual void GenerateFinalCondition()
        {
            condmanager.FinalizeCondition(ex.CondPath);
        }

        public void PrepareCondition(bool forceprepare = true)
        {
            if (forceprepare == true || condmanager.FinalCond == null)
            {
                GenerateFinalCondition();
            }
            ex.Cond = condmanager.FinalCond;
            condmanager.InitializeSampleSpaces(ex.CondSampling, ex.BlockParam, ex.BlockSampling);
            OnConditionPrepared?.Invoke();
        }

        protected virtual void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
        {
            condmanager.PushCondition(condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, manualcondidx, manualblockidx, istrysampleblock), envmanager, pushexcludefactors);
        }

        protected virtual void SamplePushBlock(int manualblockidx = 0)
        {
            condmanager.PushBlock(condmanager.SampleBlockSpace(manualblockidx), envmanager, pushexcludefactors);
        }

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

        public void AutoSaveData(bool force = false)
        {
            if (!force)
            {
                if (ex.Config.AutoSaveData)
                {
                    if (ex.CondTestAtState == CONDTESTATSTATE.NONE) { return; }
                }
                else { return; }
            }

            ex.CondTest = condtestmanager.CondTest;
            ex.EnvParam = envmanager.GetActiveParams();
            ex.Version = ExpericaExtension.ExperimentDataVersion;
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

        public T GetExParam<T>(string name)
        {
            return ex.GetParam(name).Convert<T>();
        }

        public object GetExParam(string name)
        {
            return ex.GetParam(name);
        }

        public T GetEnvActiveParam<T>(string name)
        {
            return envmanager.GetActiveParam(name).Convert<T>();
        }

        public object GetEnvActiveParam(string name)
        {
            return envmanager.GetActiveParam(name);
        }

        public void SetExParam(string name, object value, bool notifyui = true)
        {
            ex.SetParam(name, value);
        }

        public void SetEnvActiveParam(string name, object value, bool notifyui = true)
        {
            envmanager.SetActiveParam(name, value);
        }

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
            envmanager.SetActiveParam(name, value);
        }

        public void SetEnvActiveParamTwice(string name, object value1, float interval_ms, object value2, bool notifyui = false)
        {
            SetEnvActiveParamTwice(name, value1, interval_ms, name, value2, notifyui);
        }

        public void SetEnvActiveParamTwice(string name1, object value1, float interval_ms, string name2, object value2, bool notifyui = true)
        {
            envmanager.SetActiveParam(name1, value1);
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(interval_ms, name2, value2, notifyui));
        }

        #region Experiment Control
        public void PauseResumeExperiment(bool ispause)
        {
            if (ispause)
            {
                OnBeginPauseExperiment?.Invoke();
                PauseExperiment();
                OnEndPauseExperiment?.Invoke();
            }
            else
            {
                OnBeginResumeExperiment?.Invoke();
                ResumeExperiment();
                OnEndResumeExpeirment?.Invoke();
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
                OnBeginStartExperiment?.Invoke();

                // clean for new experiment
                condstate = CONDSTATE.NONE;
                trialstate = TRIALSTATE.NONE;
                blockstate = BLOCKSTATE.NONE;
                condtestmanager.Clear();
                // clear `DataPath` for new experiment, 
                // so that new `DataPath` could be generated later, 
                // preventing overwriting existing files.
                ex.DataPath = null;

                OnStartExperiment();
                PrepareCondition(regeneratecond);
                StartCoroutine(ExperimentStartSequence());
            }
            else
            {
                OnBeginStopExperiment?.Invoke();

                OnStopExperiment();
                islogicactive = false;

                // Push any condtest left
                condtestmanager.PushCondTest(timer.ElapsedMillisecond, ex.NotifyParam, ex.NotifyPerCondTest, true, true);
                StartCoroutine(ExperimentStopSequence());
            }
        }

        /// <summary>
        /// empty user function for clean/init of new experiment
        /// </summary>
        protected virtual void OnStartExperiment()
        {
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
            OnEndStartExperiment?.Invoke();
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
        /// empty user function when experiment started
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
            OnEndStopExperiment?.Invoke();
        }

        /// <summary>
        /// user function for stopping timeline sync of all hardware/software systems involved in experiment
        /// </summary>
        protected virtual void StopExperimentTimeSync()
        {
            timer.Stop();
        }

        /// <summary>
        /// empty user function after experiment stopped
        /// </summary>
        protected virtual void OnExperimentStopped()
        {
        }
        #endregion

        void Awake()
        {
            OnAwake();
        }
        protected virtual void OnAwake()
        {
        }

        void Start()
        {
            OnStart();
        }
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
                if (islogicactive)
                {
                    Logic();
                }
            }
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void Logic()
        {
        }

        public virtual void OnPositionAction(Vector2 position)
        {
            if (ex.Input && envmanager.MainCamera.Count > 0)
            {
                var po = envmanager.GetActiveParam("Position");
                if (po != null)
                {
                    var so = envmanager.GetActiveParam("Size");
                    var p = (Vector3)po;
                    var s = so == null ? Vector3.zero : (Vector3)so;
                    var hh = (envmanager.MainCamera.First().Height + s.y) / 2;
                    var hw = (envmanager.MainCamera.First().Width + s.x) / 2;
                    envmanager.SetActiveParam("Position", new Vector3(
                    Mathf.Clamp(p.x + position.x * hw * Time.deltaTime, -hw, hw),
                    Mathf.Clamp(p.y + position.y * hh * Time.deltaTime, -hh, hh),
                    p.z));
                }
            }
        }

        public virtual void OnSizeAction(Vector2 size)
        {
            if (ex.Input)
            {
                var so = envmanager.GetActiveParam("Size");
                if (so != null)
                {
                    var s = (Vector3)so;
                    envmanager.SetActiveParam("Size", new Vector3(
                        Mathf.Max(0, s.x + size.x * s.x * Time.deltaTime),
                        Mathf.Max(0, s.y + size.y * s.y * Time.deltaTime),
                        s.z));
                }
            }
        }

        public virtual void OnVisibleAction(float v)
        {
            if (ex.Input)
            {
                envmanager.SetActiveParam("Visible", v > 0);
            }
        }

        public virtual void OnOriAction(float v)
        {
            if (ex.Input)
            {
                var oo = envmanager.GetActiveParam("Ori");
                if (oo != null)
                {
                    var o = (float)oo;
                    o = (o + v * 180 * Time.deltaTime) % 360f;
                    envmanager.SetActiveParam("Ori", o < 0 ? 360f - o : o);
                }
            }
        }

        public virtual void OnDiameterAction(float diameter)
        {
            if (ex.Input)
            {
                var dio = envmanager.GetActiveParam("Diameter");
                if (dio != null)
                {
                    var d = (float)dio;
                    envmanager.SetActiveParam("Diameter", Mathf.Max(0, d + Mathf.Pow(diameter * d * Time.deltaTime, 1)));
                }
            }
        }

        public virtual void OnSpatialFreqAction(float sf)
        {
            if (ex.Input)
            {
                var sfo = envmanager.GetActiveParam("SpatialFreq");
                if (sfo != null)
                {
                    var s = (float)sfo;
                    envmanager.SetActiveParam("SpatialFreq", Mathf.Clamp(s + sf * s * Time.deltaTime, 0, 20f));
                }
            }
        }

        public virtual void OnTemporalFreqAction(float tf)
        {
            if (ex.Input)
            {
                var tfo = envmanager.GetActiveParam("TemporalFreq");
                if (tfo != null)
                {
                    var t = (float)tfo;
                    envmanager.SetActiveParam("TemporalFreq", Mathf.Clamp(t + tf * t * Time.deltaTime, 0, 20f));
                }
            }
        }

        public virtual void OnFunction1Action()
        { }

        public virtual void OnFunction2Action()
        { }

        public virtual void OnFunction3Action()
        { }

        public virtual void OnFunction4Action()
        { }

    }
}