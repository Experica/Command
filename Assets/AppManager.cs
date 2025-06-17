/*
AppManager.cs is part of the Experica.
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
using System.Threading.Tasks;


namespace Experica.Command
{
    public class AppManager : MonoBehaviour
    {
        public ConfigManager<CommandConfig> cfgmgr = ConfigManager<CommandConfig>.Load(Base.CommandConfigManagerPath);
        public UI ui;

        public Toggle host, server, start, startsession, pause;
        public Dropdown exs, exss;
        public Button savedata, newex, saveex, deleteex;
        public Text startstoptext, startstopsessiontext, pauseresumetext;
        public Volume postprocessing;

        public NetworkController networkcontroller;
        public ExperimentManager exmgr;
        public ExperimentSessionManager exsmgr;
        // public AnalysisManager alsmanager;

        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ConsolePanel consolepanel;
        public ConditionPanel condpanel;
        public ConditionTestPanel conditionTestPanel;

        public AgentStub agentstub;
        public TaskScheduler unitymainthreadscheduler;


        void Awake()
        {
            Application.wantsToQuit += Application_wantsToQuit;
            unitymainthreadscheduler = TaskScheduler.FromCurrentSynchronizationContext();
            agentstub = new(this);
            agentstub.StartStopAgentStub(true);
        }

        void Start()
        {
             if (ui == null)
            {
                UnityEngine.Debug.LogError("找不到 UI 组件！");
                return;
            }

            if (exmgr == null)
            {
                UnityEngine.Debug.LogError("找不到 ExperimentManager 组件！");
                return;
            }

            if (cfgmgr == null)
            {
                UnityEngine.Debug.LogError("找不到 ConfigManager 组件！");
                return;
            }

            if (consolepanel == null)
            {
                var consolePanelElement = ui.consolepanel;
                if (consolePanelElement != null)
                {
                    var consolePanelObj = new GameObject("ConsolePanel");
                    consolepanel = consolePanelObj.AddComponent<ConsolePanel>();
                    consolepanel.Initialize(consolePanelElement);
                    UnityEngine.Debug.Log("已创建并初始化 ConsolePanel");
                }
                else
                {
                    UnityEngine.Debug.LogError("在 UI 中找不到 ConsolePanel 元素！");
                }
            }

            if (conditionTestPanel == null)
            {
                var conditionTestPanelElement = ui.conditiontestpanel;
                if (conditionTestPanelElement != null)
                {
                    var conditionTestPanelObj = new GameObject("ConditionTestPanel");
                    conditionTestPanel = conditionTestPanelObj.AddComponent<ConditionTestPanel>();
                    conditionTestPanel.Initialize(conditionTestPanelElement);
                    UnityEngine.Debug.Log("已创建并初始化 ConditionTestPanel");
                }
                else
                {
                    UnityEngine.Debug.LogError("在 UI 中找不到 ConditionTestPanel 元素！");
                }
            }
            
            //todo 文件读取可能出现IO异常，我们需要捕获异常，catch中应该重新加载有效实验路径，后续处理需跟张博沟通
            try
            {
                exmgr.CollectDefination(cfgmgr.config.ExDir);
                exsmgr.CollectDefination(cfgmgr.config.ExSessionDir);
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogWarning($"文件读取异常:{e.Message}");
            }
           
            ui.UpdateExperimentList(exmgr.deffile.Keys.ToList(), cfgmgr.config.FirstTestID);
            ui.UpdateExperimentSessionList(exsmgr.deffile.Keys.ToList());

           

            // 初始化 UI
            ui.UpdateEnv();
            ui.UpdateView();
        }

        bool Application_wantsToQuit()
        {
            exsmgr.esl?.StartStopExperimentSession(false);
            exmgr.el?.StartStopExperiment(false);
            if (cfgmgr.config.IsSaveExOnQuit)
            {
                // during quiting, network would shutdown and NetEnv params will sync to current experiment on PreShutdown,
                // so we shouldn't sync NetEnv params again in SaveEx because now some NetEnv objects are already invalid.
                exmgr.SaveEx(false);
            }
            if (cfgmgr.config.IsSaveExSessionOnQuit)
            {
                exsmgr.SaveExSession();
            }
            exmgr.Clear();
            cfgmgr.Save(Base.CommandConfigManagerPath);
            agentstub.StartStopAgentStub(false);
            return true;
        }

        #region Command Action Callback
        public void OnToggleFullViewportAction(InputAction.CallbackContext context)
        {
            if (context.performed) { FullViewport = !FullViewport; }
        }

        public bool FullViewport
        {
            get => !ui.uidoc.rootVisualElement.visible;
            set
            {
                if (ui.uidoc.rootVisualElement.visible != value) { return; }
                var maincamera = exmgr.el.envmgr.MainCamera.First();
                if (maincamera != null)
                {
                    if (value)
                    {
                        ui.uidoc.rootVisualElement.visible = !value;
                        //exmgr.el.envmgr.SetActiveParam("ScreenAspect", (float)Screen.width / Screen.height);
                        maincamera.Camera.targetTexture = null;
                    }
                    else
                    {
                        ui.uidoc.rootVisualElement.visible = !value;
                        ui.UpdateView();
                    }
                }
            }
        }

        public void OnToggleHostAction(InputAction.CallbackContext context)
        {
            if (context.performed) { ui.host.value = !ui.host.value; }
        }

        public void OnToggleExperimentAction(InputAction.CallbackContext context)
        {
            if (context.performed) { ui.start.value = !ui.start.value; }
        }

        public void OnToggleExperimentSessionAction(InputAction.CallbackContext context)
        {
            if (context.performed) { ui.startsession.value = !ui.startsession.value; }
        }

        public void OnToggleFullScreenAction(InputAction.CallbackContext context)
        {
            if (context.performed) { FullScreen = !FullScreen; }
        }

        int lastwindowwidth = 1024, lastwindowheight = 768;
        public bool FullScreen
        {
            get { return Screen.fullScreen; }
            set
            {
                if (Screen.fullScreen == value) { return; }
                if (value)
                {
                    lastwindowwidth = Math.Max(800, Screen.width);
                    lastwindowheight = Math.Max(600, Screen.height);
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, cfgmgr.config.FullScreenMode);
                }
                else
                {
                    Screen.SetResolution(lastwindowwidth, lastwindowheight, false);
                }
            }
        }

        public void OnToggleGuideAction(InputAction.CallbackContext context)
        {
            if (context.performed) { exmgr.el.Guide = !exmgr.el.Guide; }
        }

        public void OnToggleNetVisibleAction(InputAction.CallbackContext context)
        {
            if (context.performed) { exmgr.el.NetVisible = !exmgr.el.NetVisible; }
        }

        public void OnToggleCursorAction(InputAction.CallbackContext context)
        {
            if (context.performed) { UnityEngine.Cursor.visible = !UnityEngine.Cursor.visible; }
        }

        public void OnQuitAction(InputAction.CallbackContext context)
        {
            if (context.performed) { Application.Quit(); }
        }
        #endregion

        public void OnScreenSizeChanged()
        {
            if (exmgr.el.envmgr.MainCamera.Count == 0) { return; }
            var lmc = exmgr.el.envmgr.MainCamera.Where(i => i.ClientID == NetworkManager.ServerClientId).First();
            lmc?.ReportRpc("ScreenAspect", Base.ScreenAspect);
            if (ui != null)
            {
                ui.UpdateView();
            }
        }

        public void OnExSessionChoiceChanged(string newValue)
        {
            if (cfgmgr.config.IsSaveExSessionOnQuit && exsmgr.esl != null)
            {
                exsmgr.SaveExSession(exsmgr.esl.exsession.ID);
            }
            if (exsmgr.deffile.ContainsKey(newValue))
            {
                exsmgr.LoadESL(exsmgr.deffile[newValue]);
            }
        }

        public void OnExChoiceChanged(string newValue)
        {
            if (cfgmgr.config.IsSaveExOnQuit && exmgr.el != null)
            {
                exmgr.SaveEx();
            }
            if (exmgr.deffile.ContainsKey(newValue))
            {
                exmgr.LoadEL(exmgr.deffile[newValue]);
                ui.UpdateEx(exmgr.el.ex);
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
                var cd = exmgr.el.ex.Display_ID.GetDisplay(cfgmgr.config.Display);
                if (cd != null)
                {
                    //todo 目前没有此方法的引用，但是可以未雨绸缪，优化建议：考虑缓存CLUT，当已经存在cd.CLUT时候，不用重复执行PrepareCLUT()，可以提高性能。
                    if (cd.PrepareCLUT())
                    {
                        tex = cd.CLUT;
                    }
                }
                return tex;
            }
        }

        /// <summary>
        /// when env ready, we apply and inherit params, update UI and get ready for running experiment
        /// </summary>
        public void OnEnvLoadCompleted()
        {
            if (exmgr.el.envmgr.Empty)
            {
                ui.ClearEnv();
                ui.ClearView();
            }
            else
            {
                // init user envparam values
                exmgr.el.envmgr.SetParams(exmgr.el.ex.EnvParam);
                // apply user inherit rules
                exmgr.InheritEnv();
                // we force all NetworkVariables update to trigger OnValueChanged callbacks
                exmgr.el.envmgr.RefreshParams();
                // ask all clients to report their main camera current states
                exmgr.el.envmgr.AskMainCameraReport();
                // uicontroller.SyncCurrentDisplayCLUT();

                ui.UpdateEnv();
                ui.UpdateView();
            }
            exmgr.OnReady();
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
            var msg = $"Experiment Session \"{exsmgr.esl.exsession.ID}\" Started.";
            consolepanel.Log(msg);
            if (exsmgr.esl.exsession.NotifyExperimenter)
            {
                exmgr.el.ex.Experimenter.GetAddresses(cfgmgr.config).Mail(body: msg);
            }
        }

        public void OnEndStartExperimentSession() { }

        public void ToggleStartStopExperimentSession(bool isstart)
        {
            var esl = exsmgr?.esl;
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
            consolepanel.Log($"Experiment Session \"{exsmgr.esl.exsession.ID}\" Stoped.");
            if (exsmgr.esl.exsession.NotifyExperimenter)
            {
                var msg = $"{exmgr.el.ex.Subject_ID} finished Experiment Session \"{exsmgr.esl.exsession.ID}\" in {Math.Round(exmgr.timer.ElapsedHour, 2):g}hour.";
                exmgr.el.ex.Experimenter.GetAddresses(cfgmgr.config).Mail(body: msg);
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

            var msg = $"Experiment \"{exmgr.el.ex.ID}\" Started.";
            //consolepanel.Log(msg);
            if (exmgr.el.ex.NotifyExperimenter)
            {
                exmgr.el.ex.Experimenter.GetAddresses(cfgmgr.config).Mail(body: msg);
            }

            // By default, Command need to run as fast as possible(no vsync, pipelining, realtimer, etc.), 
            // whereas the connected Environment presenting the final stimuli.
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 2;
            exmgr.el.timer.UnityFrameTime = false;
            if (FullViewport)
            {
                // FullViewport(No UI), hide cursor
                Cursor.visible = false;
                if (FullScreen)
                {
                    // FullScreen Viewport can be used to present the final stimuli without any connected Environment.
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.vSyncCount = cfgmgr.config.VSyncCount;
                    QualitySettings.maxQueuedFrames = cfgmgr.config.MaxQueuedFrames;
                    exmgr.el.timer.UnityFrameTime = cfgmgr.config.FrameTimer;
                }
            }
            Time.fixedDeltaTime = cfgmgr.config.FixedDeltaTime;
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

            exmgr.OnStart();
        }

        public void OnBeginStopExperiment() { }

        public void OnEndStopExperiment()
        {
            exmgr.OnStop();
            // alsmanager?.RpcNotifyStopExperiment();
            exmgr.el.SaveData();
            //consolepanel.Log($"Experiment \"{exmgr.el.ex.ID}\" Stoped.");
            if (exmgr.el.ex.NotifyExperimenter)
            {
                var msg = $"{exmgr.el.ex.Subject_ID} finished Experiment \"{exmgr.el.ex.ID}\" in {Math.Round(exmgr.el.timer.ElapsedMinute, 2):g}min.";
                exmgr.el.ex.Experimenter.GetAddresses(cfgmgr.config).Mail(body: msg);
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




        public void ViewportSize()
        {
            if (exmgr.el != null)
            {
                var so = exmgr.el.GetEnvActiveParam("Size");
                if (so != null)
                {
                    //todo 优化建议,空条件检查，添加默认值，并对Convert进行try-catch异常捕获。
                    //var cameras = exmgr.el?.envmgr?.MainCamera;
                    //if (cameras == null || !cameras.Any())
                    //{
                    //    var s = Vector3.zero; // 或其它默认值
                    //}
                    var s = so.Convert<Vector3>();
                    s.x = exmgr.el.envmgr.MainCamera.First().Width;
                    //if (w.HasValue) { s.x = w.Value; }
                    s.y = exmgr.el.envmgr.MainCamera.First().Height;
                    //if (h.HasValue) { s.y = h.Value; }
                    exmgr.el.SetEnvActiveParam("Size", s);
                }
            }
        }

        public void FullViewportSize()
        {
            if (exmgr.el != null)
            {
                var so = exmgr.el.GetEnvActiveParam("Size");
                if (so != null)
                {
                    var s = so.Convert<Vector3>();
                    s.x = exmgr.el.envmgr.MainCamera.First().Width;
                    //if (w.HasValue) { s.x = w.Value; }
                    s.y = exmgr.el.envmgr.MainCamera.First().Height;
                    //if (h.HasValue) { s.y = h.Value; }

                    var po = exmgr.el.GetEnvActiveParam("Position");
                    var poff = exmgr.el.GetEnvActiveParam("PositionOffset");
                    if (po != null && poff != null)
                    {
                        var p = po.Convert<Vector3>();
                        var pf = poff.Convert<Vector3>();
                        s.x += 2 * Mathf.Abs(p.x + pf.x);
                        s.y += 2 * Mathf.Abs(p.y + pf.y);
                    }
                    exmgr.el.SetEnvActiveParam("Size", s);
                }
            }
        }

        public void ToggleExInherit(string name, bool isinherit)
        {
            var ip = exmgr.el.ex.InheritParam;
            if (isinherit)
            {
                if (!ip.Contains(name))
                {
                    ip.Add(name);
                    exmgr.el.ex.properties["InheritParam"].NotifyValue();
                }
                exmgr.InheritExParam(name);
            }
            else
            {
                if (ip.Contains(name))
                {
                    ip.Remove(name);
                    exmgr.el.ex.properties["InheritParam"].NotifyValue();
                }
            }
        }

        public void ToggleEnvInherit(string fullname, bool isinherit)
        {
            var ip = exmgr.el.ex.EnvInheritParam;
            if (isinherit)
            {
                if (!ip.Contains(fullname))
                {
                    ip.Add(fullname);
                    exmgr.el.ex.properties["EnvInheritParam"].NotifyValue();
                }
                exmgr.InheritEnvParam(fullname);
            }
            else
            {
                if (ip.Contains(fullname))
                {
                    ip.Remove(fullname);
                    exmgr.el.ex.properties["EnvInheritParam"].NotifyValue();
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
                exmgr.el?.StartStopExperiment(false);
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
                exmgr.el?.StartStopExperiment(false);
                networkcontroller.Shutdown();
            }
            ui.server.label = newValue ? "Shutdown" : "Server";
            ui.host.SetEnabled(!newValue);
            ui.start.SetEnabled(newValue);
        }

        public void LoadCurrentScene()
        {
            var scene = exmgr.el?.ex.EnvPath;
            if (!string.IsNullOrEmpty(scene))
            {
                networkcontroller.LoadScene(scene);
            }

        }

        //todo 既然在Awake声明周期张注册了退出处理逻辑，那么Destroy声明周期就需要移除退出事件监听
        void OnDestroy()
        {
            Application.wantsToQuit -= Application_wantsToQuit;
        }

    }

}