/*
VLApplicationManager.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace VLab
{
    public enum VLCFG
    {
        IsSaveExOnQuit,
        AutoSaveData,
        ExDir,
        DataDir,
        ExLogic,
        CrossEnvInheritRulePath,
        NotifyParams,
        AntiAliasing,
        AnisotropicFilterLevel,
        FixedDeltaTime,
        IsShowInactiveEnvParam,
        IsShowEnvParamFullName,
        MaxLogEntry,
        ExHideParams,
        NotifyLatency,
        ExLatencyError,
        OnlineSignalLatency,
        ParallelPort1,
        ParallelPort2,
        ParallelPort3,
        StartCh,
        StopCh,
        ConditionCh,
        SignalCh1,
        SignalCh2,
        SignalCh3,
        SerialPort1,
        SerialPort2,
        SerialPort3,
        MarkPulseWidth,
        WaveHighDur,
        WaveLowDur
    }

    public class VLApplicationManager : MonoBehaviour
    {
        public VLUIController uicontroller;
        public Dictionary<VLCFG, object> config;
        public readonly string configpath = "VLabConfig.yaml";
        public Dictionary<string, Dictionary<string, List<string>>> crossenvinheritrule;

        void Awake()
        {
            ValidateConfig();
            ValidateCrossEnvInheritRule();
        }

        // because Yaml deserialize text string stops working on object type, we need to 
        // make sure config contain valid type value
        void ValidateConfig()
        {
            if (File.Exists(configpath))
            {
                config = Yaml.ReadYaml<Dictionary<VLCFG, object>>(configpath);
            }
            if (config == null)
            {
                config = new Dictionary<VLCFG, object>();
            }

            if (!config.ContainsKey(VLCFG.IsSaveExOnQuit))
            {
                config[VLCFG.IsSaveExOnQuit] = true;
            }
            else
            {
                config[VLCFG.IsSaveExOnQuit] = config[VLCFG.IsSaveExOnQuit].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.AutoSaveData))
            {
                config[VLCFG.AutoSaveData] = true;
            }
            else
            {
                config[VLCFG.AutoSaveData] = config[VLCFG.AutoSaveData].Convert<bool>();
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
            if (!config.ContainsKey(VLCFG.CrossEnvInheritRulePath))
            {
                config[VLCFG.CrossEnvInheritRulePath] = "CrossEnvInheritRule.yaml";
            }
            if (!config.ContainsKey(VLCFG.NotifyParams))
            {
                config[VLCFG.NotifyParams] = new List<CONDTESTPARAM> { CONDTESTPARAM.CondIndex, CONDTESTPARAM.CONDSTATE, CONDTESTPARAM.CondRepeat };
            }
            else
            {
                var o = config[VLCFG.NotifyParams] as List<object>;
                config[VLCFG.NotifyParams] = o.Select(i => i.Convert<CONDTESTPARAM>()).ToList();
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
            if (!config.ContainsKey(VLCFG.FixedDeltaTime))
            {
                config[VLCFG.FixedDeltaTime] = 1000000f;
            }
            else
            {
                config[VLCFG.FixedDeltaTime] = config[VLCFG.FixedDeltaTime].Convert<float>();
            }
            if (!config.ContainsKey(VLCFG.IsShowInactiveEnvParam))
            {
                config[VLCFG.IsShowInactiveEnvParam] = false;
            }
            else
            {
                config[VLCFG.IsShowInactiveEnvParam] = config[VLCFG.IsShowInactiveEnvParam].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.IsShowEnvParamFullName))
            {
                config[VLCFG.IsShowEnvParamFullName] = false;
            }
            else
            {
                config[VLCFG.IsShowEnvParamFullName] = config[VLCFG.IsShowEnvParamFullName].Convert<bool>();
            }
            if (!config.ContainsKey(VLCFG.MaxLogEntry))
            {
                config[VLCFG.MaxLogEntry] = 999;
            }
            else
            {
                config[VLCFG.MaxLogEntry] = config[VLCFG.MaxLogEntry].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ExHideParams))
            {
                config[VLCFG.ExHideParams] = new List<string>{
                    "Cond", "CondTest", "EnvParam", "Param", "Log",
                    "Subject_Log", "DataPath", "ExInheritParam", "EnvInheritParam" };
            }
            else
            {
                var o = config[VLCFG.ExHideParams] as List<object>;
                config[VLCFG.ExHideParams] = o.Select(i => i.Convert<string>()).ToList();
            }
            if (!config.ContainsKey(VLCFG.NotifyLatency))
            {
                config[VLCFG.NotifyLatency] = 200;
            }
            else
            {
                config[VLCFG.NotifyLatency] = config[VLCFG.NotifyLatency].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ExLatencyError))
            {
                config[VLCFG.ExLatencyError] = 20;
            }
            else
            {
                config[VLCFG.ExLatencyError] = config[VLCFG.ExLatencyError].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.OnlineSignalLatency))
            {
                config[VLCFG.OnlineSignalLatency] = 50;
            }
            else
            {
                config[VLCFG.OnlineSignalLatency] = config[VLCFG.OnlineSignalLatency].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ParallelPort1))
            {
                config[VLCFG.ParallelPort1] = 0xC010;
            }
            else
            {
                config[VLCFG.ParallelPort1] = config[VLCFG.ParallelPort1].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ParallelPort2))
            {
                config[VLCFG.ParallelPort2] = 0xC010;
            }
            else
            {
                config[VLCFG.ParallelPort2] = config[VLCFG.ParallelPort2].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ParallelPort3))
            {
                config[VLCFG.ParallelPort3] = 0xC010;
            }
            else
            {
                config[VLCFG.ParallelPort3] = config[VLCFG.ParallelPort3].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.StartCh))
            {
                config[VLCFG.StartCh] = 0;
            }
            else
            {
                config[VLCFG.StartCh] = config[VLCFG.StartCh].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.StopCh))
            {
                config[VLCFG.StopCh] = 1;
            }
            else
            {
                config[VLCFG.StopCh] = config[VLCFG.StopCh].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.ConditionCh))
            {
                config[VLCFG.ConditionCh] = 2;
            }
            else
            {
                config[VLCFG.ConditionCh] = config[VLCFG.ConditionCh].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.SignalCh1))
            {
                config[VLCFG.SignalCh1] = 3;
            }
            else
            {
                config[VLCFG.SignalCh1] = config[VLCFG.SignalCh1].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.SignalCh2))
            {
                config[VLCFG.SignalCh2] = 4;
            }
            else
            {
                config[VLCFG.SignalCh2] = config[VLCFG.SignalCh2].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.SignalCh3))
            {
                config[VLCFG.SignalCh3] = 5;
            }
            else
            {
                config[VLCFG.SignalCh3] = config[VLCFG.SignalCh3].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.SerialPort1))
            {
                config[VLCFG.SerialPort1] = "COM1";
            }
            if (!config.ContainsKey(VLCFG.SerialPort2))
            {
                config[VLCFG.SerialPort2] = "COM2";
            }
            if (!config.ContainsKey(VLCFG.SerialPort3))
            {
                config[VLCFG.SerialPort3] = "COM3";
            }
            if (!config.ContainsKey(VLCFG.MarkPulseWidth))
            {
                config[VLCFG.MarkPulseWidth] = 35;
            }
            else
            {
                config[VLCFG.MarkPulseWidth] = config[VLCFG.MarkPulseWidth].Convert<int>();
            }
            if (!config.ContainsKey(VLCFG.WaveHighDur))
            {
                config[VLCFG.WaveHighDur] = 5f;
            }
            else
            {
                config[VLCFG.WaveHighDur] = config[VLCFG.WaveHighDur].Convert<float>();
            }
            if (!config.ContainsKey(VLCFG.WaveLowDur))
            {
                config[VLCFG.WaveLowDur] = 20f;
            }
            else
            {
                config[VLCFG.WaveLowDur] = config[VLCFG.WaveLowDur].Convert<float>();
            }


            if (!VLTimer.IsHighResolution)
            {
                MessageBox.Show("This Machine Doesn't Have High Resolution Timer.", "Warning");
            }
        }

        void ValidateCrossEnvInheritRule()
        {
            var rulefile = (string)config[VLCFG.CrossEnvInheritRulePath];
            if (File.Exists(rulefile))
            {
                crossenvinheritrule = Yaml.ReadYaml<Dictionary<string, Dictionary<string, List<string>>>>(rulefile);
            }
            if (crossenvinheritrule == null)
            {
                crossenvinheritrule = new Dictionary<string, Dictionary<string, List<string>>>();
            }

            if (!crossenvinheritrule.ContainsKey(EnvironmentObject.GratingQuad.ToString()))
            {
                var gratingquadsourcelist = new Dictionary<string, List<string>>();
                gratingquadsourcelist[EnvironmentObject.Quad.ToString()] = new List<string> { "Ori", "Position" };
                gratingquadsourcelist[EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position" };

                crossenvinheritrule[EnvironmentObject.GratingQuad.ToString()] = gratingquadsourcelist;
            }
            if (!crossenvinheritrule.ContainsKey(EnvironmentObject.Quad.ToString()))
            {
                var quadsourcelist = new Dictionary<string, List<string>>();
                quadsourcelist[EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Ori", "Position" };
                quadsourcelist[EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position" };

                crossenvinheritrule[EnvironmentObject.Quad.ToString()] = quadsourcelist;
            }
            if (!crossenvinheritrule.ContainsKey(EnvironmentObject.ImageQuad.ToString()))
            {
                var imagequadsourcelist = new Dictionary<string, List<string>>();
                imagequadsourcelist[EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Position", "Diameter" };
                imagequadsourcelist[EnvironmentObject.Quad.ToString()] = new List<string> { "Position" };

                crossenvinheritrule[EnvironmentObject.ImageQuad.ToString()] = imagequadsourcelist;
            }
        }

        public bool IsFollowCrossEnvInheritRule(string target, string source, string param)
        {
            if (crossenvinheritrule.ContainsKey(target))
            {
                var sl = crossenvinheritrule[target];
                if (sl.ContainsKey(source) && sl[source].Contains(param))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsCrossEnvInheritTarget(string target)
        {
            return crossenvinheritrule.ContainsKey(target);
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
            Yaml.WriteYaml((string)config[VLCFG.CrossEnvInheritRulePath], crossenvinheritrule);
        }

    }
}