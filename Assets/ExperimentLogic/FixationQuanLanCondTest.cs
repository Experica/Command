/*
FixationCondTest.cs is part of the Experica.
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
using UnityEngine.InputSystem;
using System;
using Experica;
using Experica.Command;
using System.Linq;
using Experica.NetEnv;

/// <summary>
/// Condition Test while eyes fixing on a target, with User Input Action mimicking eye movement, and helpful visual guides.
/// </summary>
public class FixationQuanLanCondTest : FixationCondTest
{
    protected QuanLan_RS ql;

    protected override void OnExperimentStarted()
    {
        ql = new();
        var id = GetExParam<string>("QuanLanDeviceID");
        ql.Connect(id.Trim('"'));
    }
    protected override void OnExperimentStopped()
    {
        base.OnExperimentStopped();
        ql.Dispose();
        ql = null;
    }

    protected override void StartExperimentTimeSync()
    {
        if (ex.HasCondTestState())
        {
            if (ql != null)
            {
                ql.RecordPath = ex.GetDataPath();
                /* 
                QuanLan command server receive network message and change file path, all of which need time to complete.
                Start recording before file path change completion may not save to correct file path.
                */
                timer.WaitMillisecond(Config.NotifyLatency);

                ql.StartRecordAndAcquisite();
                /* 
                QuanLan command server receive network message and change record state, all of which need time to complete.
                Begin experiment before record started may lose information.
                */
                timer.WaitMillisecond(Config.NotifyLatency);
            }
        }
        base.StartExperimentTimeSync();
    }

    protected override void StopExperimentTimeSync()
    {
        if (ql != null)
        {
            ql.StopAcquisiteAndRecord();
            /*
            QuanLan command server receive network message and change record state, all of which need time to complete.
            Here wait recording ended before further processing.
            */
            timer.WaitMillisecond(Config.NotifyLatency);
        }
        base.StopExperimentTimeSync();
    }
}