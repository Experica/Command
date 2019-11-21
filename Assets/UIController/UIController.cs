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
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System;
using System.Runtime;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MsgPack;
using MsgPack.Serialization;

namespace Experica.Command
{
    public class UIController : MonoBehaviour
    {
        public CommandConfigManager configmanager;
        public CommandConfig config;
        public PostProcessVolume postprocessvolume;
        readonly string configmanagerpath = "CommandConfigManager.yaml";    // The file storing serialized CommandConfigManager object

        public Toggle host, server, start, pause;
        public Dropdown exs;
        public Button savedata, newex, saveex, deleteex;
        public Text startstoptext, pauseresumetext, version;

        // The managers for the panels on the Scene
        public NetManager netmanager;
        public ExperimentManager exmanager;
        public AnalysisManager alsmanager;
        public ControlManager ctrlmanager;

        // The Panels Physically created on the scene
        public ControlPanel controlpanel;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ConsolePanel consolepanel;
        public ConditionPanel condpanel;
        public ConditionTestPanel ctpanel;

        /// <summary>
        /// Awake is called when the script instance is being loaded. Awake is called only
        /// a single time.See the following URL for more information: 
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        /// Loads a configmanger object from the configmanager path variable, then loads the config object.
        /// </summary>
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

        /// <summary>
        /// Loads in and returns a CommandConfig object from configfilepath. Creates A new
        /// config object if it can't be loaded
        /// </summary>
        /// <param name="configfilepath">The config file path to the CommandConfig object</param>
        /// <param name="otherwisedefault">create a new CommandConfig object to use instead.</param>
        /// <returns>The loaded/deault CommandConfig object</returns>
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

        /// <summary>
        /// Sets the version number and starts the application.
        /// Start is called on the frame when a script is enabled just before any of the
        /// Update methods are called the first time.See the URL below for more information:
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
        /// </summary>
        void Start()
        {
            version.text = $"Version {Application.version}\nUnity {Application.unityVersion}";
            PushConfig();
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

            foreach(var el in exmanager.elhistory)
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
                ChangeScene();
            }
        }

        public void ToggleColorGrading(bool ison)
        {
            ColorGrading colorgrading;
            if (postprocessvolume.profile.TryGetSettings(out colorgrading))
            {
                colorgrading.enabled.value = ison;
            }
        }

        public void SetCLUT(Texture2D clut)
        {
            ColorGrading colorgrading;
            if (postprocessvolume.profile.TryGetSettings(out colorgrading))
            {
                colorgrading.ldrLut.value = clut;
            }
        }

        public bool PrepareDisplayCLUT(string displayid, bool forceprepare = false)
        {
            if (!config.Display.ContainsKey(displayid)) { return false; }
            var display = config.Display[displayid];
            var m = display.IntensityMeasurement;
            if (m == null || m.Count == 0) { return false; }
            if (display.CLUT == null || forceprepare)
            {
                Dictionary<string, double[]> x, y;
                switch (display.FitType)
                {
                    case DisplayFitType.Gamma:
                        m.GetRGBIntensityMeasurement(out x, out y, false, true);
                        double rgamma, ra, rc, ggamma, ga, gc, bgamma, ba, bc;
                        Extension.GammaFit(x["R"], y["R"], out rgamma, out ra, out rc);
                        Extension.GammaFit(x["G"], y["G"], out ggamma, out ga, out gc);
                        Extension.GammaFit(x["B"], y["B"], out bgamma, out ba, out bc);
                        display.CLUT = Extension.GenerateRGBGammaCLUT(rgamma, ggamma, bgamma, display.CLUTSize);
                        break;
                    case DisplayFitType.LinearSpline:
                    case DisplayFitType.CubicSpline:
                        m.GetRGBIntensityMeasurement(out x, out y, true, true);
                        IInterpolation rii, gii, bii;
                        Extension.SplineFit(y["R"], x["R"], out rii, display.FitType);
                        Extension.SplineFit(y["G"], x["G"], out gii, display.FitType);
                        Extension.SplineFit(y["B"], x["B"], out bii, display.FitType);
                        if (rii != null && gii != null && bii != null)
                        {
                            display.CLUT = Extension.GenerateRGBSplineCLUT(rii, gii, bii, display.CLUTSize);
                        }
                        break;
                }
            }
            if (display.CLUT == null) { return false; }
            SetCLUT(display.CLUT);
            return true;
        }

        public byte[] SerializeCLUT(out int width, out int height)
        {
            width = 0; height = 0;
            var displayid = exmanager.el.ex.Display_ID;
            if (!config.Display.ContainsKey(displayid) || config.Display[displayid].CLUT == null) { return null; }
            var clut = config.Display[displayid].CLUT;
            width = clut.width;
            height = clut.height;
            return clut.GetRawTextureData();
        }

        public void OnAspectRatioMessage(float ratio)
        {
            exmanager.el.envmanager.SetParam("ScreenAspect", ratio, true);
        }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was 
        /// initiated by the server with ServerChangeScene().
        /// </summary>
        /// <param name="sceneName">The scene that changed</param>
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
                using (var stream = new MemoryStream())
                {
                    switch (name)
                    {
                        case CONDTESTPARAM.BlockRepeat:
                        case CONDTESTPARAM.BlockIndex:
                        case CONDTESTPARAM.TrialRepeat:
                        case CONDTESTPARAM.TrialIndex:
                        case CONDTESTPARAM.CondRepeat:
                        case CONDTESTPARAM.CondIndex:
                            MsgPack.ListIntSerializer.Pack(stream, value.ConvertAll(i => (int)i), PackerCompatibilityOptions.None);
                            break;
                        case CONDTESTPARAM.SyncEvent:
                            MsgPack.ListListStringSerializer.Pack(stream, value.ConvertAll(i => (List<string>)i), PackerCompatibilityOptions.None);
                            break;
                        case CONDTESTPARAM.Event:
                        case CONDTESTPARAM.TASKSTATE:
                        case CONDTESTPARAM.BLOCKSTATE:
                        case CONDTESTPARAM.TRIALSTATE:
                        case CONDTESTPARAM.CONDSTATE:
                            MsgPack.ListListEventSerializer.Pack(stream, value.ConvertAll(i => (List<Dictionary<string, double>>)i), PackerCompatibilityOptions.None);
                            break;
                    }
                    if (stream.Length > 0)
                    {
                        alsmanager.RpcNotifyCondTest(name, stream.ToArray());
                        hr = true;
                    }
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

            // Get Highest Performance
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 0;
            Time.fixedDeltaTime = config.FixedDeltaTime;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

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
                    MsgPack.ExSerializer.Pack(stream, exmanager.el.ex, PackerCompatibilityOptions.None);
                    alsmanager.RpcNotifyExperiment(stream.ToArray());
                }
            }

            // Get Lowest GC Intrusiveness
            GC.Collect();
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        }

        public void ToggleStartStopExperiment(bool isstart)
        {
            exmanager.el?.StartStopExperiment(isstart);
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

            // Return Normal Performance
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 1;
            Time.fixedDeltaTime = 0.02f;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
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
                var body = $"{exmanager.el.ex.ID} finished in {exmanager.el.timer.Elapsed.ToString("g")}";
                exmanager.el.ex.Experimenter.GetAddresses(config).Mail(subject, body);
            }
            // Return Normal GC
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
            GC.Collect();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="paramname"></param>
        /// <param name="isinherit"></param>
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

        /// <summary>
        /// Function called for when the 'Save' button in the Control panel is pressed.
        /// </summary>
        public void SaveEx()
        {
            exmanager.SaveEx(exs.captionText.text);
        }

        /// <summary>
        /// Script for when the 'X' button in the Control panel is clicked to delete an experiment
        /// </summary>
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

        /// <summary>
        /// This function is called when 'Host' button in Control Panel is pressed. Signals
        /// to the network manager to start hosting.
        /// </summary>
        /// <param name="ison">True when the toggle is now down, else false</param>
        public void ToggleHost(bool ison)
        {
            // Toggle is now down / on
            if (ison)
            {
                // Start hosting. Host is both a server and client in one.
                netmanager.StartHost();
                ChangeScene();
            }
            // Toggle is now unpressed / off
            else
            {
                // When there was an experiment running, stop the experiment
                if (start.isOn)
                {
                    ToggleStartStopExperiment(false);
                    start.isOn = false;
                }
                // Quit hosting
                netmanager.StopHost();
            }
            // Turn server button active, and start button inactive
            server.interactable = !ison;
            start.interactable = ison;
        }

        /// <summary>
        /// This function is called when 'Server' button in Control Panel is pressed. Signals
        /// to the network manager to start up a server.
        /// </summary>
        /// <param name="ison">True when the toggle is now down, else false</param>
        public void ToggleServer(bool ison)
        {
            // Toggle is now down / on
            if (ison)
            {
                netmanager.StartServer();
                ChangeScene();
            }
            // Toggle is now up / off
            else
            {
                // Stop an experiment if there is one, and stop the server
                if (start.isOn)
                {
                    ToggleStartStopExperiment(false);
                    start.isOn = false;
                }
                netmanager.StopServer();
            }
            // host button is interactable, start button isn't
            host.interactable = !ison;
            start.interactable = ison;
        }


        public void ChangeScene()
        {
            if (exmanager.el != null)
            {
                var scene = exmanager.el.ex.EnvPath;
                if (string.IsNullOrEmpty(scene))
                {
                    scene = "Showroom";
                    exmanager.el.ex.EnvPath = scene;
                }
                if (NetworkServer.active)
                {
                    // causes the server to switch scenes to the specified scene
                    netmanager.ServerChangeScene(scene);
                }
            }
        }
    }
}