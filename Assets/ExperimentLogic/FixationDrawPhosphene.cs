/*
FixationDrawBorder.cs is part of the Experica.
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
using System.Collections.Generic;
using Experica;
using Experica.Command;
using System.Linq;
using Experica.NetEnv;

/// <summary>
/// Ask user to draw border of the phosphene while eyes fixing on a target, with User Input Action mimicking eye movement, and helpful visual guides.
/// </summary>
public class FixationDrawPhosphene : FixationDrawBorder
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
    }

    protected override void PrepareCondition()
    {
        var ch = GetExParam<List<int>>("Channel");
        var amp = GetExParam<List<float>>("Amplitude");
        var pw = GetExParam<List<float>>("PulseWidth");
        var n = GetExParam<List<int>>("NumOfPulse");
        var pi = GetExParam<List<float>>("PulseInterval");
        var freq = GetExParam<List<float>>("PulseFrequency");

        var cond = new Dictionary<string, List<object>>
        {
            ["Channel"] = ch.Cast<object>().ToList(),
            ["Amplitude"] = amp.Cast<object>().ToList(),
            ["PulseWidth"] = pw.Cast<object>().ToList(),
            ["NumOfPulse"] = n.Cast<object>().ToList(),
            ["PulseInterval"] = pi.Cast<object>().ToList(),
            ["PulseFrequency"] = freq.Cast<object>().ToList(),
        };

        condmgr.PrepareCondition(cond.OrthoCombineFactor());
    }

    protected override void PushCondition(int ci, bool includeblockfactor = false, Dictionary<string, IFactorPushTarget> factorpushtarget = null, List<string> pushexcludefactor = null)
    {
        var s = new BiPhasicPulse
        {
            Amplitude = (float)condmgr.Cond["Amplitude"][ci],
            PulseWidth = (float)condmgr.Cond["PulseWidth"][ci],
            NumOfPulse = (int)condmgr.Cond["NumOfPulse"][ci],
            InterPhaseInterval = (float)condmgr.Cond["PulseInterval"][ci],
            Frequency = (float)condmgr.Cond["PulseFrequency"][ci]
        };
        ql.SetBiPhasicPulse((int)condmgr.Cond["Channel"][ci], s);
        ql.StartStimulation();
        // make sure in hit trial, fixation holds until stimulation ends
        FixDur = Math.Max(FixDur, s.Duration);
    }
}