/*
VLUIController.cs is part of the VLAB project.
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
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System;
using System.Runtime;
using MsgPack;
using MsgPack.Serialization;

namespace VLab
{
    public class VLUIController : MonoBehaviour
    {
        public VLApplicationManager appmanager;
        public Toggle host, server, start, pause;
        public Dropdown exs;
        public Button savedata, newex, saveex, deleteex;
        public Text startstoptext, pauseresumetext, version;
        public VLNetManager netmanager;
        public ExperimentManager exmanager;
        public VLAnalysisManager alsmanager;
        public VLControlManager ctrlmanager;
        public ControlPanel controlpanel;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ConsolePanel consolepanel;
        public ConditionPanel condpanel;
        public ConditionTestPanel ctpanel;


        void Start()
        {
            UpdateExDropdown();
            savedata.interactable = !appmanager.config.AutoSaveData;
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
            if (appmanager.config.IsSaveExOnQuit && exmanager.el != null)
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

        public void OnNotifyCondTest(CONDTESTPARAM name, List<object> value)
        {
            if (alsmanager != null)
            {
                using (var stream = new MemoryStream())
                {
                    switch (name)
                    {
                        case CONDTESTPARAM.BlockRepeat:
                        case CONDTESTPARAM.BlockIndex:
                        case CONDTESTPARAM.CondRepeat:
                        case CONDTESTPARAM.CondIndex:
                            VLMsgPack.ListIntSerializer.Pack(stream, value.ConvertAll(i => (int)i), PackerCompatibilityOptions.None);
                            break;
                        case CONDTESTPARAM.TASKSTATE:
                        case CONDTESTPARAM.BLOCKSTATE:
                        case CONDTESTPARAM.TRIALSTATE:
                        case CONDTESTPARAM.CONDSTATE:
                            VLMsgPack.ListCONDSTATESerializer.Pack(stream, value.ConvertAll(i => (List<Dictionary<string, double>>)i), PackerCompatibilityOptions.None);
                            break;
                    }
                    if (stream.Length > 0)
                    {
                        alsmanager.RpcNotifyCondTest(name, stream.ToArray());
                    }
                }
            }
        }

        public void OnNotifyCondTestEnd(double time)
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyCondTestEnd(time);
            }
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
            Time.fixedDeltaTime = appmanager.config.FixedDeltaTime;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

            if (alsmanager != null && exmanager.el.ex.NotifyPerCondTest > 0)
            {
                alsmanager.RpcNotifyStartExperiment();
            }
        }

        public void OnEndStartExperiment()
        {
            if (alsmanager != null && exmanager.el.ex.NotifyPerCondTest > 0)
            {
                var stream = new MemoryStream();
                exmanager.el.ex.EnvParam = exmanager.el.envmanager.GetActiveParams(true);
                VLMsgPack.ExSerializer.Pack(stream, exmanager.el.ex, PackerCompatibilityOptions.None);
                alsmanager.RpcNotifyExperiment(stream.ToArray());
            }

            // Get Lowest GC Intrusiveness
            GC.Collect();
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        }

        public void ToggleStartStopExperiment(bool isstart)
        {
            if (exmanager.el != null)
            {
                exmanager.el.StartStopExperiment(isstart);
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

            // Return Normal Performance
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 1;
            Time.fixedDeltaTime = 0.02f;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        }

        public void OnEndStopExperiment()
        {
            if (alsmanager != null && exmanager.el.ex.NotifyPerCondTest > 0)
            {
                alsmanager.RpcNotifyStopExperiment();
            }
            if (appmanager.config.AutoSaveData)
            {
                exmanager.el.SaveData();
            }

            if (exmanager.el.ex.SendMail)
            {
                var subject = "Experiment Stopped";
                var body = $"{exmanager.el.ex.ID} finished in {exmanager.el.timer.Elapsed.ToString("g")}";
                exmanager.el.ex.Experimenter.GetAddresses(appmanager.config).Mail(subject, body);
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
            var i = exmanager.DeleteEx(exs.captionText.text);
            if (i >= 0)
            {
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
                ChangeScene();
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
                ChangeScene();
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
                    netmanager.ServerChangeScene(scene);
                }
            }
        }

        public void UpdateSystemInformation()
        {
            version.text = $"Version {Application.version}\nUnity {Application.unityVersion}";
        }
    }
}