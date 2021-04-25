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
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System;
using System.Runtime;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MessagePack;
using UnityEngine.InputSystem;

namespace Experica.Command
{
    public class UIController : MonoBehaviour
    {
        public CommandConfigManager configmanager;
        public CommandConfig config;
        /// <summary>
        /// The file storing serialized CommandConfigManager object
        /// </summary>
        readonly string configmanagerpath = "CommandConfigManager.yaml";

        public Toggle host, server, start, pause;
        public Dropdown exs;
        public Button savedata, newex, saveex, deleteex;
        public Text startstoptext, pauseresumetext, version;
        public Volume postprocessing;

        // The managers for the panels on the Scene
        public NetManager netmanager;
        public SyncFrameManager syncmanager;
        public ExperimentManager exmanager;
        public AnalysisManager alsmanager;
        public ControlManager ctrlmanager;

        public GameObject canvas;
        public ControlPanel controlpanel;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ConsolePanel consolepanel;
        public ConditionPanel condpanel;
        public ConditionTestPanel ctpanel;
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

            // Load the config file if there is a path in configmanager
            if (configmanager.AutoLoadSaveLastConfig)
            {
                config = LoadConfig(configmanager.LastConfigFilePath);
            }
        }

        public CommandConfig LoadConfig(string configfilepath, bool otherwisedefault = true)
        {
            CommandConfig cfg = null;

            // Check if the file exists at the specified path, if so, load it.
            if (File.Exists(configfilepath))
            {
                // Deserialize the text using extension method.
                cfg = configfilepath.ReadYamlFile<CommandConfig>();
            }

            // Use default config settings
            if (cfg == null)
            {
                configmanager.LastConfigFilePath = null;
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
                cfg.EnvCrossInheritRule = ValidateEnvCrossInheritRule(cfg.EnvCrossInheritRule);
            }
            return cfg;
        }

        public static Dictionary<string, Dictionary<string, List<string>>> ValidateEnvCrossInheritRule(Dictionary<string, Dictionary<string, List<string>>> rule)
        {
            if (!rule.ContainsKey(EnvironmentObject.GratingQuad.ToString()))
            {
                var gratingquadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.Quad.ToString()] = new List<string> { "Ori", "Position" },
                    [EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position", "Diameter" }
                };
                rule[EnvironmentObject.GratingQuad.ToString()] = gratingquadinheritfrom;
            }
            if (!rule.ContainsKey(EnvironmentObject.Quad.ToString()))
            {
                var quadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Ori", "Position" },
                    [EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position" }
                };
                rule[EnvironmentObject.Quad.ToString()] = quadinheritfrom;
            }
            if (!rule.ContainsKey(EnvironmentObject.ImageQuad.ToString()))
            {
                var imagequadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Position", "Diameter" },
                    [EnvironmentObject.Quad.ToString()] = new List<string> { "Position" }
                };
                rule[EnvironmentObject.ImageQuad.ToString()] = imagequadinheritfrom;
            }
            return rule;
        }

        void Start()
        {
            version.text = $"Version {Application.version}\nUnity {Application.unityVersion}";
            PushConfig();
        }

        public void OnToggleViewAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (canvas.activeSelf)
                {
                    canvas.SetActive(false);
                    var maincamera = exmanager.el.envmanager.maincamera_scene;
                    if (maincamera != null)
                    {
                        exmanager.el.envmanager.SetActiveParam("ScreenAspect", (float)Screen.width / Screen.height);
                        maincamera.targetTexture = null;
                    }
                }
                else
                {
                    canvas.SetActive(true);
                    viewpanel.UpdateViewport();
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

        public void OnToggleFullScreenAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (Screen.fullScreen)
                {
                    Screen.SetResolution(lastwindowwidth, lastwindowheight, false);
                }
                else
                {
                    lastwindowwidth = Math.Max(1024, Screen.width);
                    lastwindowheight = Math.Max(768, Screen.height);
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, config.FullScreenMode);
                }
            }
        }

        public void OnQuitAction(InputAction.CallbackContext context)
        {
            if (context.performed) { Application.Quit(); }
        }

        public void OnPositionAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnPositionAction(context.ReadValue<Vector2>()); }
        }

        public void OnSizeAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnSizeAction(context.ReadValue<Vector2>()); }
        }

        public void OnDiameterAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnDiameterAction(context.ReadValue<float>()); }
        }

        public void OnVisibleAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnVisibleAction(context.ReadValue<float>()); }
        }

        public void OnOriAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnOriAction(context.ReadValue<float>()); }
        }

        public void OnSpatialFreqAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnSpatialFreqAction(context.ReadValue<float>()); }
        }

        public void OnTemporalFreqAction(InputAction.CallbackContext context)
        {
            if (context.performed && exmanager.el != null)
            { exmanager.el.OnTemporalFreqAction(context.ReadValue<float>()); }
        }

        public void PushConfig()
        {
            // Grab settings from Experiement Yaml files, and update the scene
            exmanager.GetExFiles();
            UpdateExDropdown();
            savedata.interactable = !config.AutoSaveData;
            consolepanel.maxentry = config.MaxLogEntry;
        }

        void OnApplicationQuit()
        {
            ToggleHost(false);

            if (configmanager.AutoLoadSaveLastConfig)
            {
                SaveConfig();
            }
            configmanagerpath.WriteYamlFile(configmanager);

            foreach (var el in exmanager.elhistory)
            {
                el?.Dispose();
            }
        }

        public void SaveConfig()
        {
            if (config.IsSaveExOnQuit)
            {
                exmanager.SaveAllEx();
            }

            if (string.IsNullOrEmpty(configmanager.LastConfigFilePath))
            {
                configmanager.LastConfigFilePath = Extension.SaveFile("Save Config File");
            }
            if (!string.IsNullOrEmpty(configmanager.LastConfigFilePath))
            {
                configmanager.LastConfigFilePath.WriteYamlFile(config);
            }
        }

        public void UpdateExDropdown()
        {
            var exn = exmanager.exids.Count;
            if (exn > 0)
            {
                exs.ClearOptions();
                exs.AddOptions(exmanager.exids);
                OnExDropdownValueChange(Mathf.Clamp(exs.value, 0, exn - 1));
            }
        }

        public void OnExDropdownValueChange(int i)
        {
            if (config.IsSaveExOnQuit && exmanager.el != null)
            {
                exmanager.SaveEx(exmanager.el.ex.ID);
            }
            var idx = exmanager.exids.FindIndex(0, id => id == exs.captionText.text);
            if (idx >= 0)
            {
                exmanager.LoadEL(exmanager.exfiles[idx]);
                expanel.UpdateEx(exmanager.el.ex);
                ServerChangeScene();
            }
        }

        public void SyncCurrentDisplayCLUT(List<NetworkConnection> targetconns = null)
        {
            if (postprocessing.profile.TryGet(out Tonemapping tonemapping))
            {
                var cdclut = CurrentDisplayCLUT;
                if (cdclut != null)
                {
                    tonemapping.lutTexture.value = cdclut;

                    var envpeerconn = targetconns ?? netmanager.GetPeerTypeConnection(PeerType.Environment);
                    if (envpeerconn.Count > 0)
                    {
                        var clutmsg = new CLUTMessage
                        {
                            clut = cdclut.GetPixelData<byte>(0).ToArray().Compress(),
                            size = cdclut.width
                        };
                        foreach (var conn in envpeerconn)
                        {
                            conn.Send(MsgType.CLUT, clutmsg);
                        }
                    }
                }
            }
        }

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

        public void OnServerSceneChanged(string sceneName)
        {
            exmanager.PrepareEnv(sceneName);
            envpanel.UpdateEnv(exmanager.el.envmanager);
            viewpanel.UpdateViewport();
        }

        public bool OnNotifyCondTest(CONDTESTPARAM name, List<object> value)
        {
            var hr = false;
            if (alsmanager != null)
            {
                switch (name)
                {
                    case CONDTESTPARAM.BlockRepeat:
                    case CONDTESTPARAM.BlockIndex:
                    case CONDTESTPARAM.TrialRepeat:
                    case CONDTESTPARAM.TrialIndex:
                    case CONDTESTPARAM.CondRepeat:
                    case CONDTESTPARAM.CondIndex:
                        //MsgPack.ListIntSerializer.Pack(stream, value.ConvertAll(i => (int)i), PackerCompatibilityOptions.None);
                        break;
                    case CONDTESTPARAM.SyncEvent:
                        //MsgPack.ListListStringSerializer.Pack(stream, value.ConvertAll(i => (List<string>)i), PackerCompatibilityOptions.None);
                        break;
                    case CONDTESTPARAM.Event:
                    case CONDTESTPARAM.TASKSTATE:
                    case CONDTESTPARAM.BLOCKSTATE:
                    case CONDTESTPARAM.TRIALSTATE:
                    case CONDTESTPARAM.CONDSTATE:
                        //MsgPack.ListListEventSerializer.Pack(stream, value.ConvertAll(i => (List<Dictionary<string, double>>)i), PackerCompatibilityOptions.None);
                        break;
                }
                var data = value.SerializeMsgPack();
                if (data.Length > 0)
                {
                    alsmanager.RpcNotifyCondTest(name, data);
                    hr = true;
                }
            }
            return hr;
        }

        public bool OnNotifyCondTestEnd(double time)
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyCondTestEnd(time);
                return true;
            }
            return false;
        }

        public void OnBeginStartExperiment()
        {
            exs.interactable = false;
            newex.interactable = false;
            saveex.interactable = false;
            deleteex.interactable = false;
            startstoptext.text = "Stop";
            pause.interactable = true;
            consolepanel.Log("Experiment Started.");

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

            alsmanager?.RpcNotifyStartExperiment();
        }

        public void OnEndStartExperiment()
        {
            if (alsmanager != null)
            {
                using (var stream = new MemoryStream())
                {
                    exmanager.el.ex.Config = config;
                    exmanager.el.ex.EnvParam = exmanager.el.envmanager.GetActiveParams(true);
                    //MsgPack.ExSerializer.Pack(stream, exmanager.el.ex, PackerCompatibilityOptions.None);
                    //alsmanager.RpcNotifyExperiment(stream.ToArray());
                }
            }
        }

        public void ToggleStartStopExperiment(bool isstart)
        {
            var el = exmanager?.el;
            if (el != null)
            {
                el.StartStopExperiment(isstart);
            }
            else
            {
                UnityEngine.Debug.LogError("No Current ExperimentLogic to Start/Stop.");
            }
        }

        public void OnBeginStopExperiment()
        {
            exs.interactable = true;
            newex.interactable = true;
            saveex.interactable = true;
            deleteex.interactable = true;
            if (pause.isOn)
            {
                var eh = pause.onValueChanged;
                pause.onValueChanged = new Toggle.ToggleEvent();
                pause.isOn = false;
                pause.onValueChanged = eh;
            }
            if (start.isOn)
            {
                var eh = start.onValueChanged;
                start.onValueChanged = new Toggle.ToggleEvent();
                start.isOn = false;
                start.onValueChanged = eh;
            }
            startstoptext.text = "Start";
            pause.interactable = false;
            consolepanel.Log("Experiment Stoped.");
        }

        public void OnEndStopExperiment()
        {
            alsmanager?.RpcNotifyStopExperiment();
            if (config.AutoSaveData)
            {
                exmanager.el.SaveData();
            }
            if (exmanager.el.ex.SendMail)
            {
                var subject = "Experiment Stopped";
                var body = $"{exmanager.el.ex.Subject_ID} finished \"{exmanager.el.ex.ID}\" in {Math.Round(exmanager.el.timer.ElapsedMinute, 2):g}min.";
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(subject, body);
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
        }

        public void SaveData()
        {
            if (exmanager.el != null)
            {
                exmanager.el.SaveData();
            }
        }

        public void OnBeginPauseExperiment()
        {
            pauseresumetext.text = "Resume";
            consolepanel.LogWarn("Experiment Paused.");
        }

        public void OnEndPauseExperiment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyPauseExperiment();
            }
        }

        public void TogglePauseResumeExperiment(bool ispause)
        {
            if (exmanager.el != null)
            {
                exmanager.el.PauseResumeExperiment(ispause);
            }
        }

        public void OnBeginResumeExperiment()
        {
            pauseresumetext.text = "Pause";
            consolepanel.LogWarn("Experiment Resumed.");
        }

        public void OnEndResumeExpeirment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyResumeExperiment();
            }
        }

        public void ToggleExInherit(string name, bool isinherit)
        {
            var ip = exmanager.el.ex.ExInheritParam;
            if (isinherit)
            {
                if (!ip.Contains(name))
                {
                    ip.Add(name);
                    if (exmanager.el.ex.OnNotifyUI != null)
                    {
                        exmanager.el.ex.OnNotifyUI("ExInheritParam", ip);
                    }
                }
                exmanager.InheritExParam(name);
                expanel.UpdateParamUI(name, exmanager.el.ex.GetParam(name));
            }
            else
            {
                if (ip.Contains(name))
                {
                    ip.Remove(name);
                    if (exmanager.el.ex.OnNotifyUI != null)
                    {
                        exmanager.el.ex.OnNotifyUI("ExInheritParam", ip);
                    }
                }
            }
        }

        public void ForcePushEnv()
        {
            exmanager.el?.envmanager.ForcePushParams();
        }

        public void ViewportSize()
        {
            if (exmanager.el != null)
            {
                var so = exmanager.el.GetEnvActiveParam("Size");
                if (so != null)
                {
                    var s = so.Convert<Vector3>();
                    var w = exmanager.el.envmanager.MainViewportWidth;
                    if (w.HasValue) { s.x = w.Value; }
                    var h = exmanager.el.envmanager.MainViewportHeight;
                    if (h.HasValue) { s.y = h.Value; }
                    exmanager.el.SetEnvActiveParam("Size", s);
                }
            }
        }

        public void ToggleEnvInherit(string fullname, string paramname, bool isinherit)
        {
            var ip = exmanager.el.ex.EnvInheritParam;
            if (isinherit)
            {
                if (!ip.Contains(fullname))
                {
                    ip.Add(fullname);
                    if (exmanager.el.ex.OnNotifyUI != null)
                    {
                        exmanager.el.ex.OnNotifyUI("EnvInheritParam", ip);
                    }
                }
                exmanager.InheritEnvParam(fullname);
                envpanel.UpdateParamUI(fullname, exmanager.el.envmanager.GetParam(fullname));
            }
            else
            {
                if (ip.Contains(fullname))
                {
                    ip.Remove(fullname);
                    if (exmanager.el.ex.OnNotifyUI != null)
                    {
                        exmanager.el.ex.OnNotifyUI("EnvInheritParam", ip);
                    }
                }
            }
        }

        public void SaveEx()
        {
            exmanager.SaveEx(exs.captionText.text);
        }

        public void DeleteEx()
        {
            // Delete the file
            var i = exmanager.DeleteEx(exs.captionText.text);

            // If sucessfully deleted
            if (i >= 0)
            {
                // Remove option from dropdown and update dropdown
                exs.options.RemoveAt(i);
                var exn = exs.options.Count;
                if (exn > 0)
                {
                    i = Mathf.Clamp(i, 0, exn - 1);
                    exs.value = i;
                    exs.captionText.text = exs.options[i].text;
                    OnExDropdownValueChange(i);
                }
                else
                {
                    exs.value = 0;
                    exs.captionText.text = "";
                }
            }
        }

        public void ToggleHost(bool ison)
        {
            if (ison)
            {
                netmanager.StartHost();
                ServerChangeScene();
            }
            else
            {
                if (start.isOn)
                {
                    ToggleStartStopExperiment(false);
                    start.isOn = false;
                }
                netmanager.StopHost();
            }
            server.interactable = !ison;
            start.interactable = ison;
        }

        public void ToggleServer(bool ison)
        {
            if (ison)
            {
                netmanager.StartServer();
                ServerChangeScene();
            }
            else
            {
                if (start.isOn)
                {
                    ToggleStartStopExperiment(false);
                    start.isOn = false;
                }
                netmanager.StopServer();
            }
            host.interactable = !ison;
            start.interactable = ison;
        }

        public void ServerChangeScene()
        {
            var scene = "Showroom";
            if (exmanager.el != null)
            {
                scene = exmanager.el.ex.EnvPath;
            }
            ServerChangeScene(scene);
        }

        public void ServerChangeScene(string scene)
        {
            if (NetworkServer.active)
            {
                netmanager.ServerChangeScene(scene);
            }
        }
    }
}