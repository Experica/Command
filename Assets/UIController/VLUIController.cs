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
using System.Collections;
using System.Diagnostics;

namespace VLab
{
    public class VLUIController : MonoBehaviour
    {
        public VLApplicationManager appmanager;
        public Toggle host, server;
        public VLNetManager netmanager;
        public ExperimentManager exmanager;
        public ExperimentPanel expanel;
        public EnvironmentPanel envpanel;
        public ViewPanel viewpanel;
        public ControlPanel controlpanel;
        public ConsolePanel consolepanel;

        void UpdateExDropdown()
        {
            exmanager.UpdateExDef();
            if (exmanager.exdefnames.Count > 0)
            {
                controlpanel. exdropdown.ClearOptions();
                controlpanel.exdropdown.AddOptions(exmanager.exdefnames);
                OnExDropdownValueChange(0);
            }
        }

        public void OnExDropdownValueChange(int i)
        {
            exmanager.LoadEx(exmanager.exdefs[i]);
            expanel.UpdateEx(exmanager.el.ex);
            if (NetworkServer.active)
            {
                ChangeScene();
            }
        }

        public void ToggleStartStopExperiment(bool isstart)
        {
            if (isstart)
            {
                controlpanel.startstoptext.text = "Stop";
                controlpanel.pauseresume.interactable = true;
                consolepanel.LogError("Experiment Started.");

                QualitySettings.vSyncCount = 0;
                QualitySettings.maxQueuedFrames = 0;

                Time.fixedDeltaTime = VLConvert.Convert<float>( appmanager.config["logictick"]);
                Process.GetCurrentProcess().PriorityBoostEnabled = true;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                exmanager.el.StartExperiment();
            }
            else
            {
                controlpanel.startstoptext.text = "Start";
                if(controlpanel.pauseresume.isOn)
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

                exmanager.el.StopExperiment();
            }
        }

        public void TogglePauseResumeExperiment(bool ispause)
        {
            if (ispause)
            {
                controlpanel.pauseresumetext.text = "Resume";
                consolepanel.LogWarn("Experiment Paused.");
                exmanager.el.PauseExperiment();
            }
            else
            {
                controlpanel.pauseresumetext.text = "Pause";
                consolepanel.LogWarn("Experiment Resumed.");
                exmanager.el.ResumeExperiment();
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
            var ps = exmanager.el.ex.exinheritparams;
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
            var ps = exmanager.el.ex.envinheritparams;
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
            if (Experiment.properties.ContainsKey(name))
            {
                exmanager.el.ex.SetValue(name, value);
            }
            else
            {
                exmanager.el.ex.param[name] = value;
            }
        }

        public void SetEnvParam(string name, object value)
        {
            exmanager.el.envmanager.SetParam(name, value);
        }

        public void SaveEx()
        {
            exmanager.SaveExDef(controlpanel.exdropdown.captionText.text);
        }

        public void DeleteEx()
        {
            var i = exmanager.DeleteExDef(controlpanel.exdropdown.captionText.text);
            if (i >= 0)
            {
                controlpanel.exdropdown.options.RemoveAt(i);
                if (i < exmanager.exdefnames.Count)
                {
                    controlpanel. exdropdown.value = i;
                    controlpanel. exdropdown.captionText.text = controlpanel.exdropdown.options[i].text;
                    OnExDropdownValueChange(i);
                }
                else
                {
                    if (exmanager.exdefnames.Count > 0)
                    {
                        i = exmanager.exdefnames.Count - 1;
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
            if(exmanager.el != null)
            {
                var scene = exmanager.el.ex.environmentpath;
                if (string.IsNullOrEmpty( scene))
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