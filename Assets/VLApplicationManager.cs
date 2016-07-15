// --------------------------------------------------------------
// VLApplicationManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace VLab
{
    public enum VLCFG
    {
        IsSaveExOnQuit,
        ExDir,
        DataDir,
        ExLogic,
        NotifyParams,
        AntiAliasing,
        AnisotropicFilterLevel,
        LogicTick,
        IsShowInactiveEnvParam,
        IsShowEnvParamFullname,
        MaxLogEntry
    }

    public class VLApplicationManager : MonoBehaviour
    {
        public VLUIController uicontroller;
        public Dictionary<VLCFG, object> config;
        public readonly string configpath = "VLabConfig.yaml";

        void Awake()
        {
            if (File.Exists(configpath))
            {
                config = Yaml.ReadYaml<Dictionary<VLCFG, object>>(configpath);
            }
            if (config == null)
            {
                config = new Dictionary<VLCFG, object>();
            }
            ValidateConfig();
        }

        // because Yaml deserialize text string stops working on object type, we need to 
        // make sure config contain valid type value
        void ValidateConfig()
        {
            if (!config.ContainsKey(VLCFG.IsSaveExOnQuit))
            {
                config[VLCFG.IsSaveExOnQuit] = true;
            }
            else
            {
                config[VLCFG.IsSaveExOnQuit] = config[VLCFG.IsSaveExOnQuit].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.ExDir))
            {
                config[VLCFG.ExDir] = "Experiment";
            }
            if (!config.ContainsKey(VLCFG.DataDir))
            {
                config[VLCFG.DataDir] = "Data";
            }
            if (!config.ContainsKey(VLCFG.ExLogic))
            {
                config[VLCFG.ExLogic] = "ConditionTestLogic";
            }
            if (!config.ContainsKey(VLCFG.NotifyParams))
            {
                config[VLCFG.NotifyParams] = new List<CONDTESTPARAM> { CONDTESTPARAM.CondIndex, CONDTESTPARAM.CONDSTATE};
            }
            else
            {
                var o = config[VLCFG.NotifyParams] as List<object>;
                config[VLCFG.NotifyParams] =o.Select(i => i.Convert<CONDTESTPARAM>()).ToList();
            }
            if (!config.ContainsKey(VLCFG.AntiAliasing))
            {
                config[VLCFG.AntiAliasing] = 2;
            }
            else
            {
                config[VLCFG.AntiAliasing] = config[VLCFG.AntiAliasing].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.AnisotropicFilterLevel))
            {
                config[VLCFG.AnisotropicFilterLevel] = 5;
            }
            else
            {
                config[VLCFG.AnisotropicFilterLevel] = config[VLCFG.AnisotropicFilterLevel].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.LogicTick))
            {
                config[VLCFG.LogicTick] = 0.0001f;
            }
            else
            {
                config[VLCFG.LogicTick] = config[VLCFG.LogicTick].Convert<float>();
            }
            if (!config.ContainsKey(VLCFG.IsShowInactiveEnvParam))
            {
                config[VLCFG.IsShowInactiveEnvParam] = false;
            }
            else
            {
                config[VLCFG.IsShowInactiveEnvParam] = config[VLCFG.IsShowInactiveEnvParam].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.IsShowEnvParamFullname))
            {
                config[VLCFG.IsShowEnvParamFullname] = false;
            }
            else
            {
                config[VLCFG.IsShowEnvParamFullname] = config[VLCFG.IsShowEnvParamFullname].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.MaxLogEntry))
            {
                config[VLCFG.MaxLogEntry] = 999;
            }
            else
            {
                config[VLCFG.MaxLogEntry] = config[VLCFG.MaxLogEntry].Convert<int>();
            }

            if (!VLTimer.IsHighResolution)
            {
                MessageBox.Show("This Machine Doesn't Have High Resolution Timer.", "Warning");
            }
        }

        void OnApplicationQuit()
        {
            if (uicontroller.netmanager.isNetworkActive)
            {
                uicontroller.netmanager.StopHost();
            }
            if ((bool)config[VLCFG.IsSaveExOnQuit])
            {
                uicontroller.exmanager.SaveAllEx();
            }
            Yaml.WriteYaml(configpath, config);
        }

    }
}