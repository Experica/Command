/*
VLCFG.cs is part of the VLAB project.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VLab
{
    public class VLCFG
    {
        public bool IsSaveExOnQuit { get; set; } = true;
        public bool AutoSaveData { get; set; } = true;
        public DataFormat SaveDataFormat { get; set; } = DataFormat.YAML;
        public string ExDir { get; set; } = "Experiment";
        public string DataDir { get; set; } = "Data";
        public string ExLogic { get; set; } = "ConditionTestLogic";
        public string EnvCrossInheritRulePath { get; set; } = "EnvCrossInheritRule.yaml";
        public List<CONDTESTPARAM> NotifyParams { get; set; } = new List<CONDTESTPARAM> { CONDTESTPARAM.CondIndex, CONDTESTPARAM.CONDSTATE };
        public int AntiAliasing { get; set; } = 2;
        public int AnisotropicFilterLevel { get; set; } = 5;
        public float FixedDeltaTime { get; set; } = 1000000f;
        public bool IsShowInactiveEnvParam { get; set; } = false;
        public bool IsShowEnvParamFullName { get; set; } = false;
        public int MaxLogEntry { get; set; } = 999;
        public List<string> ExHideParams { get; set; } = new List<string> { "Cond", "CondTest", "EnvParam", "Param", "Log", "Subject_Log", "DataPath", "ExInheritParam", "EnvInheritParam" };
        public int NotifyLatency { get; set; } = 200;
        public int ExLatencyError { get; set; } = 20;
        public int OnlineSignalLatency { get; set; } = 50;
        public int ParallelPort1 { get; set; } = 45072;
        public int ParallelPort2 { get; set; } = 53264;
        public int ParallelPort3 { get; set; } = 53264;
        public int StartCh { get; set; } = 2;
        public int StopCh { get; set; } = 3;
        public int ConditionCh { get; set; } = 0;
        public int SignalCh1 { get; set; } = 1;
        public int SignalCh2 { get; set; } = 4;
        public int SignalCh3 { get; set; } = 5;
        public int OICh { get; set; } = 0;
        public string SerialPort1 { get; set; } = "COM3";
        public string SerialPort2 { get; set; } = "COM4";
        public string SerialPort3 { get; set; } = "COM5";
        public int MarkPulseWidth { get; set; } = 21;
        public int WaveHighDur { get; set; } = 5;
        public int WaveLowDur { get; set; } = 20;
        public string FirstTestID { get; set; } = "ConditionTest";
        public Dictionary<string, string> ExperimenterAddress { get; set; } = new Dictionary<string, string> { { "Alex", "4109829463@mms.att.net" } };
    }
}