/*
RippleImageLogic.cs is part of the VLAB project.
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
using VLab;
using System.Collections.Generic;
using System.Linq;

public class RippleImageLogic : ExperimentLogic
{
    ParallelPort pport;
    int notifylatency, exlatencyerror, onlinesignallatency, markpulsewidth;
    int startch, stopch, condch;
    float diameter;

    public override void OnStart()
    {
        recordmanager = new RecordManager(RecordSystem.Ripple);
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        startch = (int)config[VLCFG.StartCh];
        stopch = (int)config[VLCFG.StopCh];
        condch = (int)config[VLCFG.ConditionCh];
        notifylatency = (int)config[VLCFG.NotifyLatency];
        exlatencyerror = (int)config[VLCFG.ExLatencyError];
        onlinesignallatency = (int)config[VLCFG.OnlineSignalLatency];
        markpulsewidth = (int)config[VLCFG.MarkPulseWidth];
    }

    public override void PrepareCondition(bool isforceprepare = true)
    {
        if (isforceprepare == false && condmanager.cond != null)
        { }
        else
        {
            var ni = (float)ex.GetParam("NumOfImage");
            var cond = new Dictionary<string, List<object>>();
            cond["Image"] = Enumerable.Range(1, (int)ni).Select(i => (object)i.ToString()).ToList();
            condmanager.TrimCondition(cond);
        }
        if (condmanager.ncond > 0)
        {
            ex.Cond = condmanager.cond;
            condmanager.UpdateSampleSpace(ex.CondSampling, ex.BlockParam, ex.BlockSampling);
            OnConditionPrepared(false);
        }
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        var mt = (MaskType)GetEnvActiveParam("MaskType");
        if (mt == MaskType.DiskFade || mt == MaskType.Disk)
        {
            diameter = (float)GetEnvActiveParam("Diameter");
            var mrr = (float)GetEnvActiveParam("MaskRadius") / 0.5f;
            SetEnvActiveParam("Diameter", diameter / mrr);
        }
        envmanager.Invoke("RpcPreLoadImage", new object[] { condmanager.cond["Image"].Select(i => (string)i).ToArray() });
        recordmanager.recorder.RecordPath = ex.GetDataPath(ext: "");
        timer.Timeout(notifylatency);
        pport.BitPulse(bit: startch, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("Mark", OnOff.Off);
        pport.SetBit(bit: condch, value: false);
        base.StopExperiment();
        var mt = (MaskType)GetEnvActiveParam("MaskType");
        if (mt == MaskType.DiskFade || mt == MaskType.Disk)
        {
            SetEnvActiveParam("Diameter", diameter);
        }
        timer.Timeout(ex.Latency + exlatencyerror + onlinesignallatency);
        pport.BitPulse(bit: stopch, duration_ms: 5);
        timer.Stop();
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                SetEnvActiveParam("Visible", false);
                SetEnvActiveParam("Mark", OnOff.Off);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    SetEnvActiveParam("Visible", true);
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                        // The marker pulse width should be > 2 frame(60Hz==16.7ms) to make sure
                        // marker params will take effect on screen.
                        SetEnvActiveParamTwice("Mark", OnOff.On, markpulsewidth, OnOff.Off);
                        pport.ConcurrentBitPulse(bit: condch, duration_ms: markpulsewidth);
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Mark", OnOff.On);
                        pport.SetBit(bit: condch, value: true);
                    }
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    CondState = CONDSTATE.SUFICI;
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Visible", false);
                        SetEnvActiveParam("Mark", OnOff.Off);
                        pport.SetBit(bit: condch, value: false);
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
