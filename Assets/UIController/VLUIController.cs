// --------------------------------------------------------------
// UIManager.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace VLab
{
    public class VLUIController : MonoBehaviour
    {
        public Dropdown exdropdown;
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
                exdropdown.ClearOptions();
                exdropdown.AddOptions(exmanager.exdefnames);
                OnExDropdownValueChange(0);
            }
        }

        public void OnExDropdownValueChange(int i)
        {
            exmanager.LoadEx(exmanager.exdefs[i]);
            expanel.UpdateEx(exmanager.el.ex);
            if (host.isOn || server.isOn)
            {
                ChangeScene();
            }
        }

        public void OnStartEx()
        {
            exmanager.el.StartExperiment();
        }

        public void OnStopEx()
        {
            exmanager.el.StopExperiment();
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
                    expanel.input[name].text = exmanager.el.ex.GetValue(name).ToString();
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
                    envpanel.input[name].text = exmanager.el.envmanager.GetParam(name).ToString();
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

        public void NewEx()
        {
            controlpanel.NewEx();
        }

        public void SaveEx()
        {
            exmanager.SaveExDef(exdropdown.captionText.text);
        }

        public void DeleteEx()
        {
            exmanager.DeleteExDef(exdropdown.captionText.text);
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
                netmanager.ServerChangeScene(scene);
            }
        }

        void Start()
        {
            UpdateExDropdown();
        }

    }
}