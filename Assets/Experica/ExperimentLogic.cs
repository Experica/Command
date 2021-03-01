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

namespace Experica
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
        public CommandConfig config;
        public Experiment ex = new Experiment();
        public Timer timer = new Timer();
        public EnvironmentManager envmanager = new EnvironmentManager();
        public ConditionManager condmanager = new ConditionManager();
        public ConditionTestManager condtestmanager = new ConditionTestManager();
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

        CONDSTATE condstate = CONDSTATE.NONE;
        public CONDSTATE CondState
        {
            get { return condstate; }
            set
            {
                if (value != condstate)
                {
                    OnEnterCondState(value);
                    condstate = value;
                }
            }
        }
        protected virtual void OnEnterCondState(CONDSTATE value)
        {
            switch (value)
            {
                case CONDSTATE.NONE:
                    break;
                case CONDSTATE.PREICI:
                    PreICIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREICI)
                    {
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
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
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
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
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
        }

        TRIALSTATE trialstate = TRIALSTATE.NONE;
        public TRIALSTATE TrialState
        {
            get { return trialstate; }
            set
            {
                if (value != trialstate)
                {
                    OnEnterTrialState(value);
                    trialstate = value;
                }
            }
        }
        protected virtual void OnEnterTrialState(TRIALSTATE value)
        {
            switch (value)
            {
                case TRIALSTATE.NONE:
                    break;
                case TRIALSTATE.PREITI:
                    PreITIOnTime = timer.ElapsedMillisecond;
                    if (ex.CondTestAtState == CONDTESTATSTATE.PREITI)
                    {
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
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
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
                            condtestmanager.Add(CONDTESTPARAM.CondIndex, condmanager.condidx);
                            condtestmanager.Add(CONDTESTPARAM.CondRepeat, condmanager.condrepeat[condmanager.condidx]);
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
        }

        BLOCKSTATE blockstate = BLOCKSTATE.NONE;
        public BLOCKSTATE BlockState
        {
            get { return blockstate; }
            set
            {
                if (value != blockstate)
                {
                    OnEnterBlockState(value);
                    blockstate = value;
                }
            }
        }
        protected virtual void OnEnterBlockState(BLOCKSTATE value)
        {
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
        }

        protected virtual void GenerateFinalCondition()
        {
            condmanager.GenerateFinalCondition(ex.CondPath);
        }

        public void PrepareCondition(bool forceprepare = true)
        {
            if (forceprepare == false && condmanager.finalcond != null)
            { }
            else
            {
                GenerateFinalCondition();
            }
            ex.Cond = condmanager.finalcond;
            condmanager.UpdateSampleSpace(ex.CondSampling, ex.BlockParam, ex.BlockSampling);
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

        public virtual string DataPath(DataFormat dataFormat)
        {
            var extension = dataFormat.ToString().ToLower();
            return ex.GetDataPath(ext: extension, searchext: extension);
        }

        public virtual void SaveData()
        {
            var ct = condtestmanager.condtest;
            if (ct.Count > 0)
            {
                ex.CondTest = ct;
                Dictionary<string, Dictionary<string, List<object>>[]> m = null;
                if (config.SaveConfigInData)
                {
                    if (!config.SaveConfigDisplayMeasurementInData)
                    {
                        m = config.Display.ToDictionary(kv => kv.Key, kv => new[] { kv.Value.IntensityMeasurement, kv.Value.SpectralMeasurement });
                        foreach (var d in config.Display.Values)
                        {
                            d.IntensityMeasurement = null;
                            d.SpectralMeasurement = null;
                        }
                    }
                    ex.Config = config;
                }

                ex.EnvParam = envmanager.GetActiveParams();
                ex.Version = Extension.ExDataVersion;
                switch (config.SaveDataFormat)
                {
                    case DataFormat.EXPERICA:
                        DataPath(DataFormat.EXPERICA).Save(ex);
                        break;
                    default:
                        DataPath(DataFormat.YAML).WriteYamlFile(ex);
                        break;
                }
                ex.DataPath = null;

                if (config.SaveConfigInData)
                {
                    if (!config.SaveConfigDisplayMeasurementInData)
                    {
                        foreach (var d in config.Display.Keys)
                        {
                            config.Display[d].IntensityMeasurement = m[d][0];
                            config.Display[d].SpectralMeasurement = m[d][1];
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No Data to Save.");
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
            ex.SetParam(name, value, notifyui);
        }

        public void SetEnvActiveParam(string name, object value, bool notifyui = true)
        {
            envmanager.SetActiveParam(name, value, notifyui);
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
            envmanager.SetActiveParam(name, value, notifyui);
        }

        public void SetEnvActiveParamTwice(string name, object value1, float interval_ms, object value2, bool notifyui = false)
        {
            SetEnvActiveParamTwice(name, value1, interval_ms, name, value2, notifyui);
        }

        public void SetEnvActiveParamTwice(string name1, object value1, float interval_ms, string name2, object value2, bool notifyui = true)
        {
            envmanager.SetActiveParam(name1, value1, notifyui);
            StartCoroutine(WaitSetEnvActiveParam_Coroutine(interval_ms, name2, value2, notifyui));
        }

        public virtual void PauseResumeExperiment(bool ispause)
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

        public void StartStopExperiment(bool isstart)
        {
            if (isstart)
            {
                OnBeginStartExperiment?.Invoke();
                StartExperiment();
            }
            else
            {
                OnBeginStopExperiment?.Invoke();
                StopExperiment();
            }
        }

        protected void StartExperiment()
        {
            condstate = CONDSTATE.NONE;
            trialstate = TRIALSTATE.NONE;
            blockstate = BLOCKSTATE.NONE;
            condtestmanager.Clear();

            OnStartExperiment();
            PrepareCondition(regeneratecond);
            StartCoroutine(ExperimentStartSequence());
        }

        protected virtual void OnStartExperiment()
        {
        }

        protected IEnumerator ExperimentStartSequence()
        {
            // sync several frames to make sure Command and all connected Environment have been initialized to the same start frame.
            var n = 3;
            for (var i = 0; i < n; i++)
            {
                SyncFrame?.Invoke();
                yield return null;
                while (issyncingframe)
                {
                    yield return null;
                }
            }
            // wait until the synced same start frame have completed being presented on display.
            var dur = n * ex.Display_ID.DisplayLatency(config.Display) ?? config.NotifyLatency;
            yield return new WaitForSecondsRealtime((float)dur / 1000f);
            StartExperimentTimeSync();
            OnExperimentStarted();
            OnEndStartExperiment?.Invoke();
            islogicactive = true;
        }

        protected virtual void StartExperimentTimeSync()
        {
            timer.Restart();
        }

        protected virtual void OnExperimentStarted()
        {
        }

        protected void StopExperiment()
        {
            islogicactive = false;
            OnStopExperiment();

            // Push any condtest left
            condtestmanager.PushCondTest(timer.ElapsedMillisecond, ex.NotifyParam, ex.NotifyPerCondTest, true, true);
            StartCoroutine(ExperimentStopSequence());
        }

        protected virtual void OnStopExperiment()
        {
        }

        protected IEnumerator ExperimentStopSequence()
        {
            // sync several frames to make sure Command and all connected Environment have been set to the same stop frame.
            var n = 3;
            for (var i = 0; i < n; i++)
            {
                SyncFrame?.Invoke();
                yield return null;
                while (issyncingframe)
                {
                    yield return null;
                }
            }
            // wait until the synced same stop frame have completed being presented on display.
            var dur = n * ex.Display_ID.DisplayLatency(config.Display) ?? config.NotifyLatency;
            yield return new WaitForSecondsRealtime((float)dur / 1000f);
            StopExperimentTimeSync();
            OnExperimentStopped();
            OnEndStopExperiment?.Invoke();
        }

        protected virtual void StopExperimentTimeSync()
        {
            timer.Stop();
        }

        protected virtual void OnExperimentStopped()
        {
        }

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
            if (issyncingframe)
            {
                if (Time.realtimeSinceStartupAsDouble - SyncFrameOnTime >= config.SyncFrameTimeOut)
                {
                    Debug.Log($"SyncFrame Timeout({config.SyncFrameTimeOut}s), Stop Waiting.");
                    issyncingframe = false;
                }
            }
            else
            {
                OnUpdate();
                if (islogicactive)
                {
                    if (condmanager.IsCondRepeat(ex.CondRepeat))
                    {
                        StartStopExperiment(false);
                    }
                    else
                    {
                        Logic();
                    }
                }
            }
        }

        protected virtual void OnUpdate()
        {
            if (ex.Input == InputMethod.Joystick && Input.GetJoystickNames().Count() > 0 && envmanager.active_networkbehaviour.Count > 0)
            {
                var jxa = Input.GetAxis("JXA");
                var jya = Input.GetAxis("JYA");
                var jza = Input.GetAxis("JZA");
                var jxra = Input.GetAxis("JXRA");
                var jyra = Input.GetAxis("JYRA");
                var jlb = Input.GetAxis("JLB");
                var jrb = Input.GetAxis("JRB");
                var ja = Input.GetAxis("JA");
                var jb = Input.GetAxis("JB");
                var jx = Input.GetAxis("JX");
                var jy = Input.GetAxis("JY");
                var jxh = Input.GetAxis("JXH");
                var jyh = Input.GetAxis("JYH");
                if (jxa != 0 || jya != 0)
                {
                    if (envmanager.maincamera_scene != null)
                    {
                        var po = envmanager.GetActiveParam("Position");
                        if (po != null)
                        {
                            var so = envmanager.GetActiveParam("Size");
                            var p = (Vector3)po;
                            var s = so == null ? Vector3.zero : (Vector3)so;
                            var hh = envmanager.maincamera_scene.orthographicSize + s.y / 2;
                            var hw = envmanager.maincamera_scene.orthographicSize * envmanager.maincamera_scene.aspect + s.x / 2;
                            envmanager.SetActiveParam("Position", new Vector3(
                            Mathf.Clamp(p.x + Mathf.Pow(jxa * hw / 1135, 1), -hw, hw),
                            Mathf.Clamp(p.y + Mathf.Pow(jya * hh / 1135, 1), -hh, hh),
                            p.z), true);
                        }
                    }
                }
                if (jza != 0)
                {
                    if (ja > 0.5)
                    {
                        var v = jza > 0 ? true : false;
                        envmanager.SetActiveParam("Visible", v, true);
                    }
                    else if (jb > 0.5)
                    {
                        var sfo = envmanager.GetActiveParam("SpatialFreq");
                        if (sfo != null)
                        {
                            envmanager.SetActiveParam("SpatialFreq", Mathf.Clamp((float)sfo + jza / 200, 0.001f, 20f), true);
                        }
                    }
                    else if (jx > 0.5)
                    {
                        var tfo = envmanager.GetActiveParam("TemporalFreq");
                        if (tfo != null)
                        {
                            envmanager.SetActiveParam("TemporalFreq", Mathf.Clamp((float)tfo + jza / 10, 0.001f, 20f), true);
                        }
                    }
                    else
                    {
                        var oo = envmanager.GetActiveParam("Ori");
                        if (oo != null)
                        {
                            var o = ((float)oo + Mathf.Pow(jza, 1) * 0.4) % 360f;
                            envmanager.SetActiveParam("Ori", o < 0 ? 360f - o : o, true);
                        }
                    }
                }
                if (jxra != 0 || jyra != 0)
                {
                    if (jrb > 0.5)
                    {
                        var dio = envmanager.GetActiveParam("Diameter");
                        if (dio != null)
                        {
                            var d = (float)dio;
                            envmanager.SetParam("Diameter", Mathf.Max(0, d + Mathf.Pow(jxra, 1) * 0.05f), true);
                        }
                    }
                    else
                    {
                        var so = envmanager.GetActiveParam("Size");
                        if (so != null)
                        {
                            var s = (Vector3)so;
                            envmanager.SetParam("Size", new Vector3(
                                Mathf.Max(0, s.x + Mathf.Pow(jxra, 1) * 0.05f),
                                Mathf.Max(0, s.y + Mathf.Pow(jyra, 1) * 0.05f),
                                s.z), true);
                        }
                    }
                }
            }
        }

        protected virtual void Logic()
        {
        }

    }
}