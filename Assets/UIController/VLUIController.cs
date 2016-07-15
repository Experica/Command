// --------------------------------------------------------------
// VLUIController.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MsgPack;
using MsgPack.Serialization;

namespace VLab
{
    public class VLUIController : MonoBehaviour
    {
        public VLApplicationManager appmanager;
        public Toggle host, server;
        public VLNetManager netmanager;
        public ExperimentManager exmanager;
        public VLAnalysisManager alsmanager;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ControlPanel controlpanel;
        public ConsolePanel consolepanel;

        void UpdateExDropdown()
        {
            exmanager.GetExFiles();
            if (exmanager.exids.Count > 0)
            {
                controlpanel.exdropdown.ClearOptions();
                controlpanel.exdropdown.AddOptions(exmanager.exids);
                OnExDropdownValueChange(0);
            }
        }

        public void OnExDropdownValueChange(int i)
        {
            exmanager.LoadEL(exmanager.exfiles[i]);
            expanel.UpdateEx(exmanager.el.ex);
            if (NetworkServer.active)
            {
                ChangeScene();
            }
        }

        public void OnNotifyCondTest(CONDTESTPARAM name, List<object> value)
        {
            if (alsmanager != null)
            {
                var stream = new MemoryStream();
                switch (name)
                {
                    case CONDTESTPARAM.CondIndex:
                        MsgPackSerializer.ListIntSerializer.Pack(stream, value.ConvertAll(i => (int)i), PackerCompatibilityOptions.None);
                        break;
                    case CONDTESTPARAM.CONDSTATE:
                        MsgPackSerializer.CONDSTATESerializer.Pack(stream, value.ConvertAll(i => (List<Dictionary<string, double>>)i), PackerCompatibilityOptions.None);
                        break;
                    default:
                        MsgPackSerializer.ListObjectSerializer.Pack(stream, value, PackerCompatibilityOptions.None);
                        break;
                }
                alsmanager.RpcNotifyCondTestData(name, stream.ToArray());
            }
        }

        public void OnNotifyEnd(double time)
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyAnalysis(time);
            }
        }

        public void OnNotifyExperiment(Experiment ex)
        {
            if (alsmanager != null)
            {
                var stream = new MemoryStream();
                MsgPackSerializer.ExSerializer.Pack(stream, ex, PackerCompatibilityOptions.None);
                alsmanager.RpcNotifyExperiment(stream.ToArray());
            }
        }

        public void OnNotifyStartExperiment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyStartExperiment();
            }
        }

        public void OnNotifyStopExperiment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyStopExperiment();
            }
        }

        public void OnBeginStartExperiment()
        {
            controlpanel.startstoptext.text = "Stop";
            controlpanel.pauseresume.interactable = true;
            consolepanel.LogError("Experiment Started.");

            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 0;

            Time.fixedDeltaTime = (float)appmanager.config[VLCFG.LogicTick];
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            OnNotifyStartExperiment();
        }

        public void OnEndStartExperiment()
        {
            OnNotifyExperiment(exmanager.el.ex);
        }

        public void OnBeginStopExperiment()
        {
            controlpanel.startstoptext.text = "Start";
            if (controlpanel.pauseresume.isOn)
            {
                controlpanel.pauseresume.isOn = false;
                TogglePauseResumeExperiment(false);
            }
            controlpanel.pauseresume.interactable = false;
            consolepanel.LogError("Experiment Stoped.");

            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;

            Time.fixedDeltaTime = 0.02f;
            Process.GetCurrentProcess().PriorityBoostEnabled = false;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }

        public void OnEndStopExperiment()
        {
            OnNotifyStopExperiment();
            exmanager.el.SaveData();
        }

        public void ToggleStartStopExperiment(bool isstart)
        {
            if(exmanager.el!=null)
            {
                exmanager.el.StartStopExperiment(isstart);
            }
        }

        public void OnNotifyPauseExperiment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyPauseExperiment();
            }
        }

        public void OnNotifyResumeExperiment()
        {
            if (alsmanager != null)
            {
                alsmanager.RpcNotifyResumeExperiment();
            }
        }

        public void OnBeginPauseExperiment()
        {
            controlpanel.pauseresumetext.text = "Resume";
            consolepanel.LogWarn("Experiment Paused.");
        }

        public void OnEndPauseExperiment()
        {
            OnNotifyPauseExperiment();
        }

        public void OnBeginResumeExperiment()
        {
            controlpanel.pauseresumetext.text = "Pause";
            consolepanel.LogWarn("Experiment Resumed.");
        }

        public void OnEndResumeExpeirment()
        {
            OnNotifyResumeExperiment();
        }

        public void TogglePauseResumeExperiment(bool ispause)
        {
            if(exmanager.el!=null)
            {
                exmanager.el.PauseResumeExperiment(ispause);
            }
        }

        public void UpdateEnv()
        {
            envpanel.UpdateEnv(exmanager.el.envmanager);
        }

        public void UpdateView()
        {
            viewpanel.UpdateView();
        }

        public void ToggleExInheritParam(string name, bool isinherit)
        {
            var ps = exmanager.el.ex.ExInheritParam;
            if (ps.Contains(name))
            {
                if (!isinherit)
                {
                    ps.Remove(name);
                }
            }
            else
            {
                if (isinherit)
                {
                    ps.Add(name);
                    exmanager.InheritExParam(name);
                    expanel.UpdateParamUI(name, exmanager.el.ex.GetValue(name));
                }
            }
        }

        public void ToggleEnvInheritParam(string name, bool isinherit)
        {
            var ps = exmanager.el.ex.EnvInheritParam;
            if (ps.Contains(name))
            {
                if (!isinherit)
                {
                    ps.Remove(name);
                }
            }
            else
            {
                if (isinherit)
                {
                    ps.Add(name);
                    exmanager.InheritEnvParam(name);
                    envpanel.UpdateParamUI(name, exmanager.el.envmanager.GetParam(name));
                }
            }
        }

        public void SetExParam(string name, object value)
        {
            if (Experiment.Properties.ContainsKey(name))
            {
                exmanager.el.ex.SetValue(name, value);
            }
            else
            {
                exmanager.el.ex.Param[name] = value;
            }
        }

        public void SetEnvParam(string name, object value)
        {
            exmanager.el.envmanager.SetParam(name, value);
        }

        public void SaveEx()
        {
            exmanager.SaveEx(controlpanel.exdropdown.captionText.text);
        }

        public void DeleteEx()
        {
            var i = exmanager.DeleteEx(controlpanel.exdropdown.captionText.text);
            if (i >= 0)
            {
                controlpanel.exdropdown.options.RemoveAt(i);
                if (i < exmanager.exids.Count)
                {
                    controlpanel.exdropdown.value = i;
                    controlpanel.exdropdown.captionText.text = controlpanel.exdropdown.options[i].text;
                    OnExDropdownValueChange(i);
                }
                else
                {
                    if (exmanager.exids.Count > 0)
                    {
                        i = exmanager.exids.Count - 1;
                        controlpanel.exdropdown.value = i;
                        controlpanel.exdropdown.captionText.text = controlpanel.exdropdown.options[i].text;
                        OnExDropdownValueChange(i);
                    }
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
                netmanager.StopHost();
            }
            server.interactable = !ison;
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
                netmanager.StopServer();
            }
            host.interactable = !ison;
        }

        void ChangeScene()
        {
            if (exmanager.el != null)
            {
                var scene = exmanager.el.ex.EnvPath;
                if (string.IsNullOrEmpty(scene))
                {
                    scene = "Showroom";
                }
                if (NetworkServer.active)
                {
                    netmanager.ServerChangeScene(scene);
                }
            }
        }

        void Start()
        {
            UpdateExDropdown();
        }

    }
}