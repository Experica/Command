/*
SpikeGLXOpto.cs is part of the Experica.
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
using Experica;
using System.Collections.Generic;
using System.Linq;
using ColorSpace = Experica.NetEnv.ColorSpace;

/// <summary>
/// Condition Test using SpikeGLX Data Acquisition System, combined with optogenetic manipulation
/// </summary>
public class SpikeGLXOpto : SpikeGLXCondTest
{
    protected PWMWave pwmwave;

    protected override void OnStartExperiment()
    {
        base.OnStartExperiment();
        var ch = GetExParam<int>("OptoCh");
        pwmwave = new PWMWave
        {
            gpio = gpio,
            bit = ch,
            duration_ms = ex.CondDur,
            startdelay_ms = ex.PreICI
        };
    }

    protected override void OnExperimentStopped()
    {
        pwmwave?.Stop();
        base.OnExperimentStopped();
    }

    protected override void PrepareCondition()
    {
        // get predefined colors for current display
        var colorspace = GetExParam<ColorSpace>("ColorSpace");
        var colorvar = GetExParam<string>("Color");
        var colorname = colorspace + "_" + colorvar;

        List<Color> color = null;
        List<Color> wp = null;
        List<float> angle = null;
        var data = ex.Display_ID.GetColorData();
        if (data != null)
        {
            if (data.ContainsKey(colorname))
            {
                color = data[colorname].Convert<List<Color>>();

                var wpname = colorname + "_WP";
                if (data.ContainsKey(wpname))
                {
                    wp = data[wpname].Convert<List<Color>>();
                }
                var anglename = colorname + "_Angle";
                if (data.ContainsKey(anglename))
                {
                    angle = data[anglename].Convert<List<float>>();
                }
            }
            else
            {
                Debug.LogWarning($"{colorname} is not found in colordata of display: {ex.Display_ID}.");
            }
        }

        if (color != null)
        {
            SetEnvActiveParam("MinColor", color[0]);
            SetEnvActiveParam("MaxColor", color[1]);
            if (wp != null)
            {
                SetEnvActiveParam("BGColor", wp[0]);
            }
        }

        // get optogenetics conditions
        var optofreq = GetExParam<List<double>>("OptoFreq");
        var optoduty = GetExParam<List<double>>("OptoDuty");
        var nopto = GetExParam<bool>("AddNoOpto");
        var ocond = new Dictionary<string, List<object>>()
        {
            ["Opto"] = new List<object> { true }
        };
        if (optofreq != null) { ocond["OptoFreq"] = optofreq.Cast<object>().ToList(); }
        if (optoduty != null) { ocond["OptoDuty"] = optoduty.Cast<object>().ToList(); }
        ocond = ocond.OrthoCombineFactor();
        if (nopto)
        {
            ocond["Opto"].Insert(0, false);
            ocond["OptoFreq"].Insert(0, 0.0);
            ocond["OptoDuty"].Insert(0, 0.0);
        }

        // get base conditions from condition file
        var bcond = ConditionManager.ProcessCondition(ex.CondPath);

        // combine optogenetics conditions with base conditions
        var cond = ocond.OrthoCombineCondition(bcond);
        condmgr.PrepareCondition(cond);

        // prepare factor push target
        PrepareFactorPushTarget();
        foreach (var f in new[] { "Opto", "OptoFreq", "OptoDuty" })
        {
            factorpushtarget[f] = pwmwave;
        }

    }
}
