/*
UIController.cs is part of the Experica.
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
using Unity.Netcode;
//using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System;
using System.Linq;
using System.Runtime;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MessagePack;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Experica.NetEnv;

namespace Experica.Command
{
    public class UIController : MonoBehaviour
    {
        public CommandConfigManager configmanager;
        public CommandConfig config;

        //public VisualElement ui;
        public UI ui;

        public Toggle host, server, start, startsession, pause;
        public Dropdown exs, exss;
        public Button savedata, newex, saveex, deleteex;
        public Text startstoptext, startstopsessiontext, pauseresumetext, version;
        public Volume postprocessing;

        // The managers for the panels on the Scene
        public NetworkController networkcontroller;
        // public SyncFrameManager syncmanager;
        public ExperimentManager exmanager;
        public ExperimentSessionManager exsmanager;
        // public AnalysisManager alsmanager;
        // public ControlManager ctrlmanager;

        public GameObject canvas;
        public ControlPanel controlpanel;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ConsolePanel consolepanel;
        public ConditionPanel condpanel;
        public ConditionTestPanel ctpanel;

        const string configmanagerpath = "CommandConfigManager.yaml";
        int lastwindowwidth = 1024, lastwindowheight = 768;

        void Awake()
        {
            // Check for CommandConfigManager.yaml existance
            if (File.Exists(configmanagerpath))
            {
                configmanager = configmanagerpath.ReadYamlFile<CommandConfigManager>();
            }
            if (configmanager == null)
            {
                configmanager = new CommandConfigManager();
            }

            // Load the config file according to configmanager
            if (configmanager.AutoLoadSaveLastConfig)
            {
                config = LoadConfig(configmanager.LastConfigFilePath);
            }
        }

        public CommandConfig LoadConfig(string configfilepath, bool otherwisedefault = true)
        {
            CommandConfig cfg = null;
            if (File.Exists(configfilepath))
            {
                cfg = configfilepath.ReadYamlFile<CommandConfig>();
            }
            if (cfg == null)
            {
                configmanager.LastConfigFilePath = null; // forgot why add this line
                if (otherwisedefault)
                {
                    cfg = new CommandConfig();
                }
            }
            if (cfg != null)
            {
                if (cfg.EnvCrossInheritRule == null)
                {
                    cfg.EnvCrossInheritRule = new Dictionary<string, Dictionary<string, List<string>>>();
                }
                cfg.EnvCrossInheritRule = CommandConfig.ValidateEnvCrossInheritRule(cfg.EnvCrossInheritRule);
            }
            return cfg;
        }

        void Start()
        {
            //version.text = $"Version {Application.version}\nUnity {Application.unityVersion}";
            Initialize();
        }

        #region Command Action Callback
        public void OnToggleFullViewportAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                IsFullViewport = !IsFullViewport;
            }
        }

        bool isfullviewport = false;
        public bool IsFullViewport
        {
            get { return isfullviewport; }
            set
            {
                var maincamera = exmanager.el.envmanager.MainCamera.First().Camera;
                if (maincamera != null && isfullviewport != value)
                {
                    isfullviewport = value;
                    if (value)
                    {
                        canvas.SetActive(false);
                        exmanager.el.envmanager.SetActiveParam("ScreenAspect", (float)Screen.width / Screen.height);
                        maincamera.targetTexture = null;
                    }
                    else
                    {
                        canvas.SetActive(true);
                        viewpanel.UpdateViewport();
                    }
                }
            }
        }

        public void OnToggleHostAction(InputAction.CallbackContext context)
        {
            if (context.performed) { controlpanel.startstophost.isOn = !controlpanel.startstophost.isOn; }
        }

        public void OnToggleExperimentAction(InputAction.CallbackContext context)
        {
            if (context.performed) { controlpanel.startstopexperiment.isOn = !controlpanel.startstopexperiment.isOn; }
        }

        public void OnToggleExperimentSessionAction(InputAction.CallbackContext context)
        {
            if (context.performed) { controlpanel.startstopexperimentsession.isOn = !controlpanel.startstopexperimentsession.isOn; }
        }

        public void OnToggleFullScreenAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                FullScreen = !FullScreen;
            }
        }

        public bool FullScreen
        {
            get { return Screen.fullScreen; }
            set
            {
                if (Screen.fullScreen == value) { return; }
                if (value)
                {
                    lastwindowwidth = Math.Max(1024, Screen.width);
                    lastwindowheight = Math.Max(768, Screen.height);
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, config.FullScreenMode);
                    var maincamera = exmanager.el.envmanager.MainCamera.First().Camera;
                    if (maincamera != null && maincamera.targetTexture == null)
                    {
                        exmanager.el.envmanager.SetActiveParam("ScreenAspect", (float)Screen.currentResolution.width / Screen.currentResolution.height);
                        maincamera.targetTexture = null;
                    }
                }
                else
                {
                    Screen.SetResolution(lastwindowwidth, lastwindowheight, false);
                    var maincamera = exmanager.el.envmanager.MainCamera.First().Camera;
                    if (maincamera != null && maincamera.targetTexture == null)
                    {
                        exmanager.el.envmanager.SetActiveParam("ScreenAspect", (float)lastwindowwidth / lastwindowheight);
                        maincamera.targetTexture = null;
                    }
                }
            }
        }

        public void OnToggleGuideAction(InputAction.CallbackContext context)
        {
            if (context.performed) { GuideActive = !GuideActive; }
        }

        bool guideactive = true;
        public bool GuideActive
        {
            get => guideactive;
            set
            {
                if (guideactive == value) { return; }
                guideactive = value;
                viewpanel.togglegrid.isOn = value;
            }
        }

        public void showhidescalegrid(INetEnvCamera camera, bool isshow)
        {
            var sg = camera.gameObject.GetComponentInChildren<ScaleGrid>(true);
            if (isshow)
            {
                if (sg == null)
                {
                }
                else { sg.gameObject.SetActive(true); }
            }
            else
            {
                if (sg != null) { sg.gameObject.SetActive(false); }
            }
        }

        public void OnQuitAction(InputAction.CallbackContext context)
        {
            if (context.performed) { Application.Quit(); }
        }
        #endregion

        public void Initialize()
        {
            exmanager.CollectDefination(config.ExDir);
            exsmanager.CollectDefination(config.ExSessionDir);
            ui.UpdateExperimentList(exmanager.deffile.Keys.ToList(), config.FirstTestID);
            //UpdateExDropdown();
            ui.UpdateExperimentSessionList(exsmanager.deffile.Keys.ToList());
            //UpdateExSessionDropdown();
            //savedata.interactable = !config.AutoSaveData;
            //consolepanel.maxentry = config.MaxLogEntry;
        }

        void OnApplicationQuit()
        {
            ToggleHost(false);

            if (configmanager.AutoLoadSaveLastConfig)
            {
                SaveConfig();
            }
            configmanagerpath.WriteYamlFile(configmanager);
            exmanager.Clear();
        }

        public void SaveConfig()
        {
            if (config.IsSaveExOnQuit)
            {
                exmanager.SaveAllEx();
            }
            if (config.IsSaveExSessionOnQuit)
            {
                exsmanager.SaveExSession();
            }

            if (string.IsNullOrEmpty(configmanager.LastConfigFilePath))
            {
                configmanager.LastConfigFilePath = Experica.SaveFile("Save Config File");
            }
            if (!string.IsNullOrEmpty(configmanager.LastConfigFilePath))
            {
                configmanager.LastConfigFilePath.WriteYamlFile(config);
            }
        }



        public void OnExSessionChoiceChanged(string newValue)
        {
            if (config.IsSaveExSessionOnQuit && exsmanager.esl != null)
            {
                exsmanager.SaveExSession(exsmanager.esl.exsession.ID);
            }
            if (exsmanager.deffile.ContainsKey(newValue))
            {
                exsmanager.LoadESL(exsmanager.deffile[newValue]);
            }
        }



        public void OnExChoiceChanged(string newValue)
        {
            if (config.IsSaveExOnQuit && exmanager.el != null)
            {
                exmanager.SaveEx(exmanager.el.ex.ID);
            }
            if (exmanager.deffile.ContainsKey(newValue))
            {
                exmanager.LoadEL(exmanager.deffile[newValue]);
                ui.UpdateEx(exmanager.el.ex);
                LoadCurrentScene();
            }
        }

        // public void SyncCurrentDisplayCLUT(List<NetworkConnection> targetconns = null)
        // {
        //     if (postprocessing.profile.TryGet(out Tonemapping tonemapping))
        //     {
        //         var cdclut = CurrentDisplayCLUT;
        //         if (cdclut != null)
        //         {
        //             tonemapping.lutTexture.value = cdclut;

        //             var envpeerconn = targetconns ?? netmanager.GetPeerTypeConnection(PeerType.Environment);
        //             if (envpeerconn.Count > 0)
        //             {
        //                 var clutmsg = new CLUTMessage
        //                 {
        //                     clut = cdclut.GetPixelData<byte>(0).ToArray().Compress(),
        //                     size = cdclut.width
        //                 };
        //                 foreach (var conn in envpeerconn)
        //                 {
        //                     conn.Send(MsgType.CLUT, clutmsg);
        //                 }
        //             }
        //         }
        //     }
        // }

        public Texture3D CurrentDisplayCLUT
        {
            get
            {
                Texture3D tex = null;
                var cd = exmanager.el.ex.Display_ID.GetDisplay(config.Display);
                if (cd != null)
                {
                    if (cd.PrepareCLUT())
                    {
                        tex = cd.CLUT;
                    }
                }
                return tex;
            }
        }

        public void OnAspectRatioMessage(float ratio)
        {
            exmanager.el.envmanager.SetParam("ScreenAspect", ratio, true);
        }

        /// <summary>
        /// whenever new scene loaded, we get access to scene parameters, setup inheritance, update UI and get ready for running experiment
        /// </summary>
        /// <param name="scene"></param>
        public void OnSceneLoadEventCompleted(string scene)
        {
            if (scene == Experica.EmptyScene) // just do proper cleaning for the empty scene
            {
                ui.ClearEnv();
                ui.ClearView();
            }
            else
            {
                exmanager.el.envmanager.ParseScene(scene);
                // init user envparam values
                exmanager.el.envmanager.SetParams(exmanager.el.ex.EnvParam);
                // apply user inherit rules
                exmanager.InheritEnv();
                exmanager.el.envmanager.RefreshParams();
                // uicontroller.SyncCurrentDisplayCLUT();


                ui.UpdateEnv();
                //showhidescalegrid(exmanager.el.envmanager.MainCamera[0], true);
                ui.UpdateView();
                exmanager.OnReady();
            }
        }

        public bool OnNotifyCondTest(CONDTESTPARAM name, List<object> value)
        {
            var hr = false;
            // if (alsmanager != null)
            // {
            //     switch (name)
            //     {
            //         case CONDTESTPARAM.BlockRepeat:
            //         case CONDTESTPARAM.BlockIndex:
            //         case CONDTESTPARAM.TrialRepeat:
            //         case CONDTESTPARAM.TrialIndex:
            //         case CONDTESTPARAM.CondRepeat:
            //         case CONDTESTPARAM.CondIndex:
            //             //MsgPack.ListIntSerializer.Pack(stream, value.ConvertAll(i => (int)i), PackerCompatibilityOptions.None);
            //             break;
            //         case CONDTESTPARAM.SyncEvent:
            //             //MsgPack.ListListStringSerializer.Pack(stream, value.ConvertAll(i => (List<string>)i), PackerCompatibilityOptions.None);
            //             break;
            //         case CONDTESTPARAM.Event:
            //         case CONDTESTPARAM.TASKSTATE:
            //         case CONDTESTPARAM.BLOCKSTATE:
            //         case CONDTESTPARAM.TRIALSTATE:
            //         case CONDTESTPARAM.CONDSTATE:
            //             //MsgPack.ListListEventSerializer.Pack(stream, value.ConvertAll(i => (List<Dictionary<string, double>>)i), PackerCompatibilityOptions.None);
            //             break;
            //     }
            //     var data = value.SerializeMsgPack();
            //     if (data.Length > 0)
            //     {
            //         alsmanager.RpcNotifyCondTest(name, data);
            //         hr = true;
            //     }
            // }
            return hr;
        }

        public bool OnNotifyCondTestEnd(double time)
        {
            // if (alsmanager != null)
            // {
            //     alsmanager.RpcNotifyCondTestEnd(time);
            //     return true;
            // }
            return false;
        }

        #region ExperimentSession Control Callback
        public void OnBeginStartExperimentSession()
        {
            exss.interactable = false;

            exs.interactable = false;
            newex.interactable = false;
            saveex.interactable = false;
            deleteex.interactable = false;
            start.interactable = false;

            startstopsessiontext.text = "StopSession";
            var msg = $"Experiment Session \"{exsmanager.esl.exsession.ID}\" Started.";
            consolepanel.Log(msg);
            if (exsmanager.esl.exsession.NotifyExperimenter)
            {
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(body: msg);
            }
        }

        public void OnEndStartExperimentSession() { }

        public void ToggleStartStopExperimentSession(bool isstart)
        {
            var esl = exsmanager?.esl;
            if (esl != null)
            {
                esl.StartStopExperimentSession(isstart);
            }
            else
            {
                UnityEngine.Debug.LogError("No Current ExperimentSessionLogic to Start/Stop.");
            }
        }

        public void OnBeginStopExperimentSession()
        {
            exss.interactable = true;

            exs.interactable = true;
            newex.interactable = true;
            saveex.interactable = true;
            deleteex.interactable = true;
            start.interactable = true;

            if (startsession.isOn)
            {
                var eh = startsession.onValueChanged;
                startsession.onValueChanged = new Toggle.ToggleEvent();
                startsession.isOn = false;
                startsession.onValueChanged = eh;
            }
            startstopsessiontext.text = "StartSession";
        }

        public void OnEndStopExperimentSession()
        {
            consolepanel.Log($"Experiment Session \"{exsmanager.esl.exsession.ID}\" Stoped.");
            if (exsmanager.esl.exsession.NotifyExperimenter)
            {
                var msg = $"{exmanager.el.ex.Subject_ID} finished Experiment Session \"{exsmanager.esl.exsession.ID}\" in {Math.Round(exmanager.timer.ElapsedHour, 2):g}hour.";
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(body: msg);
            }
        }
        #endregion

        #region Experiment Control Callback
        public void OnBeginStartExperiment()
        {
            ui.start.SetValueWithoutNotify(true);
            ui.start.label = "Stop";
            ui.pause.SetEnabled(true);
            ui.experimentlist.SetEnabled(false);
            ui.newex.SetEnabled(false);
            ui.saveex.SetEnabled(false);
            ui.deleteex.SetEnabled(false);

            var msg = $"Experiment \"{exmanager.el.ex.ID}\" Started.";
            //consolepanel.Log(msg);
            if (exmanager.el.ex.NotifyExperimenter)
            {
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(body: msg);
            }

            // By default, Command need to run as fast as possible(no vsync, pipelining, realtimer, etc.), 
            // whereas the connected Environment presenting the final stimuli.
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 2;
            exmanager.el.timer.IsFrameTime = false;
            if (!canvas.activeSelf)
            {
                // FullViewport(No UI), hide cursor
                Cursor.visible = false;
                if (Screen.fullScreen)
                {
                    // FullScreen Viewport can be used to present the final stimuli without any connected Environment.
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.vSyncCount = config.VSyncCount;
                    QualitySettings.maxQueuedFrames = config.MaxQueuedFrames;
                    exmanager.el.timer.IsFrameTime = config.FrameTimer;
                }
            }
            Time.fixedDeltaTime = config.FixedDeltaTime;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            // alsmanager?.RpcNotifyStartExperiment();
        }

        public void OnEndStartExperiment()
        {
            // if (alsmanager != null)
            // {
            //     using (var stream = new MemoryStream())
            //     {
            //         exmanager.el.ex.EnvParam = exmanager.el.envmanager.GetActiveParams(true);
            //         //MsgPack.ExSerializer.Pack(stream, exmanager.el.ex, PackerCompatibilityOptions.None);
            //         //alsmanager.RpcNotifyExperiment(stream.ToArray());
            //     }
            // }

            exmanager.OnStart();
        }

        public void OnBeginStopExperiment() { }

        public void OnEndStopExperiment()
        {
            exmanager.OnStop();
            // alsmanager?.RpcNotifyStopExperiment();
            exmanager.el.SaveData();
            consolepanel.Log($"Experiment \"{exmanager.el.ex.ID}\" Stoped.");
            if (exmanager.el.ex.NotifyExperimenter)
            {
                var msg = $"{exmanager.el.ex.Subject_ID} finished Experiment \"{exmanager.el.ex.ID}\" in {Math.Round(exmanager.el.timer.ElapsedMinute, 2):g}min.";
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(body: msg);
            }

            

            // Return normal when experiment stopped
            Cursor.visible = true;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;
            Time.fixedDeltaTime = 0.016666f;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
            GCSettings.LatencyMode = GCLatencyMode.Interactive;

            ui.deleteex.SetEnabled(true);
            ui.saveex.SetEnabled(true);
            ui.newex.SetEnabled(true);
            ui.experimentlist.SetEnabled(true);
            ui.pause.label = "Pause";
            ui.pause.SetValueWithoutNotify(false);
            ui.pause.SetEnabled(false);
            ui.start.label = "Start";
            ui.start.SetValueWithoutNotify(false);
        }

        public void OnBeginPauseExperiment()
        {
            ui.pause.SetValueWithoutNotify(true);
            ui.pause.label = "Resume";
            consolepanel.LogWarn("Experiment Paused.");
        }

        public void OnEndPauseExperiment()
        {
            // if (alsmanager != null)
            // {
            //     alsmanager.RpcNotifyPauseExperiment();
            // }
        }

        public void OnBeginResumeExperiment()
        {
            consolepanel.LogWarn("Experiment Resumed.");
        }

        public void OnEndResumeExpeirment()
        {
            // if (alsmanager != null)
            // {
            //     alsmanager.RpcNotifyResumeExperiment();
            // }
            ui.pause.label = "Pause";
            ui.pause.SetValueWithoutNotify(false);
        }
        #endregion


        public void ToggleExInherit(string name, bool isinherit)
        {
            var ip = exmanager.el.ex.InheritParam;
            if (isinherit)
            {
                if (!ip.Contains(name))
                {
                    ip.Add(name);
                    exmanager.el.ex.Properties()["ExInheritParam"].NotifyValue();
                }
                exmanager.InheritExParam(name);
            }
            else
            {
                if (ip.Contains(name))
                {
                    ip.Remove(name);
                    exmanager.el.ex.Properties()["ExInheritParam"].NotifyValue();
                }
            }
        }

        public void ViewportSize()
        {
            if (exmanager.el != null)
            {
                var so = exmanager.el.GetEnvActiveParam("Size");
                if (so != null)
                {
                    var s = so.Convert<Vector3>();
                    s.x = exmanager.el.envmanager.MainCamera.First().Width;
                    //if (w.HasValue) { s.x = w.Value; }
                    s.y = exmanager.el.envmanager.MainCamera.First().Height;
                    //if (h.HasValue) { s.y = h.Value; }
                    exmanager.el.SetEnvActiveParam("Size", s);
                }
            }
        }

        public void FullViewportSize()
        {
            if (exmanager.el != null)
            {
                var so = exmanager.el.GetEnvActiveParam("Size");
                if (so != null)
                {
                    var s = so.Convert<Vector3>();
                    s.x = exmanager.el.envmanager.MainCamera.First().Width;
                    //if (w.HasValue) { s.x = w.Value; }
                    s.y = exmanager.el.envmanager.MainCamera.First().Height;
                    //if (h.HasValue) { s.y = h.Value; }

                    var po = exmanager.el.GetEnvActiveParam("Position");
                    var poff = exmanager.el.GetEnvActiveParam("PositionOffset");
                    if (po != null && poff != null)
                    {
                        var p = po.Convert<Vector3>();
                        var pf = poff.Convert<Vector3>();
                        s.x += 2 * Mathf.Abs(p.x + pf.x);
                        s.y += 2 * Mathf.Abs(p.y + pf.y);
                    }
                    exmanager.el.SetEnvActiveParam("Size", s);
                }
            }
        }

        public void ToggleEnvInherit(string fullname, bool isinherit)
        {
            var ip = exmanager.el.ex.EnvInheritParam;
            if (isinherit)
            {
                if (!ip.Contains(fullname))
                {
                    ip.Add(fullname);
                    exmanager.el.ex.Properties()["EnvInheritParam"].NotifyValue();
                }
                exmanager.InheritEnvParam(fullname);
            }
            else
            {
                if (ip.Contains(fullname))
                {
                    ip.Remove(fullname);
                    exmanager.el.ex.Properties()["EnvInheritParam"].NotifyValue();
                }
            }
        }

        public void ToggleHost(bool newValue)
        {
            if (newValue)
            {
                networkcontroller.StartHostServer();
                LoadCurrentScene();
            }
            else
            {
                exmanager.el?.StartStopExperiment(false);
                networkcontroller.Shutdown();
            }
            ui.host.label = newValue ? "Shutdown" : "Host";
            ui.server.SetEnabled(!newValue);
            ui.start.SetEnabled(newValue);
        }

        public void ToggleServer(bool newValue)
        {
            if (newValue)
            {
                networkcontroller.StartHostServer(false);
                LoadCurrentScene();
            }
            else
            {
                exmanager.el?.StartStopExperiment(false);
                networkcontroller.Shutdown();
            }
            ui.server.label = newValue ? "Shutdown" : "Server";
            ui.host.SetEnabled(!newValue);
            ui.start.SetEnabled(newValue);
        }

        public void LoadCurrentScene()
        {
            var scene = exmanager.el?.ex.EnvPath;
            if (!string.IsNullOrEmpty(scene))
            {
                networkcontroller.LoadScene(scene);
            }

        }

    }
}