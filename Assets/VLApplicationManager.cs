// --------------------------------------------------------------
// ApplicationManager.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
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