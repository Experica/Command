// --------------------------------------------------------------
// VLApplicationManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace VLab
{
    public class VLApplicationManager : MonoBehaviour
    {
        public VLUIController uicontroller;
        public Dictionary<string, object> config;
        public readonly string configpath = "VLabConfig.yaml";

        void Awake()
        {
            if (File.Exists(configpath))
            {
                config = Yaml.ReadYaml<Dictionary<string, object>>(configpath);
            }
            else
            {
                config = new Dictionary<string, object>();
            }
            ValidateConfig();
        }

        void ValidateConfig()
        {
            if (!config.ContainsKey("issaveexonquit"))
            {
                config["issaveexonquit"] = true;
            }
            if (!config.ContainsKey("exdefdir"))
            {
                config["exdefdir"] = "Experiment";
            }
            if (!config.ContainsKey("condtestdir"))
            {
                config["condtestdir"] = "ConditionTest";
            }
            if (!config.ContainsKey("defaultexperimentlogic"))
            {
                config["defaultexperimentlogic"] = "ConditionTestLogic";
            }
            if (!config.ContainsKey("antialiasing"))
            {
                config["antialiasing"] = 2;
            }
            if (!config.ContainsKey("anisotropicfilterlevel"))
            {
                config["anisotropicfilterlevel"] = 5;
            }
            if (!config.ContainsKey("logictick"))
            {
                config["logictick"] = 0.0001f;
            }
            if (!config.ContainsKey("isshowinactiveenvparam"))
            {
                config["isshowinactiveenvparam"] = false;
            }
            if (!config.ContainsKey("isshowenvparamfullname"))
            {
                config["isshowenvparamfullname"] = false;
            }
            if (!config.ContainsKey("maxlogentry"))
            {
                config["maxlogentry"] = 999;
            }
        }

        void OnApplicationQuit()
        {
            if (uicontroller.netmanager.isNetworkActive)
            {
                uicontroller.netmanager.StopHost();
            }
            if ((bool)VLConvert.Convert(config["issaveexonquit"], typeof(bool)))
            {
                uicontroller.exmanager.SaveAllExDef();
            }
            Yaml.WriteYaml(configpath, config);
        }

    }
}