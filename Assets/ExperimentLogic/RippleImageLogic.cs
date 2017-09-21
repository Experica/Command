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
    int startbit, stopbit, condbit;
    float diameter;

    public override void OnStart()
    {
        recordmanager = new RecordManager(RecordSystem.Ripple);
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        startbit = (int)config[VLCFG.StartCh];
        stopbit = (int)config[VLCFG.StopCh];
        condbit = (int)config[VLCFG.ConditionCh];
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
        if ((MaskType)GetEnvActiveParam("MaskType") == MaskType.DiskFade)
        {
            diameter = (float)GetEnvActiveParam("Diameter");
            var mrr = (float)GetEnvActiveParam("MaskRadius") / 0.5f;
            SetEnvActiveParam("Diameter", diameter / mrr);
        }
        envmanager.Invoke("RpcPreLoadImage",new object[] { condmanager.cond["Image"].Select(i => (string)i).ToArray() });
        recordmanager.recorder.RecordPath=ex.GetDataPath(ext: "");
        /* 
        Ripple recorder set path through UDP network and Trellis receive
        message and change file path, all of which need time to complete.
        Issue record triggering TTL before file path change completion will
        not successfully start recording.

        VLab online analysis also need time to complete signal clear buffer,
        otherwise the delayed action may clear up the start TTL pluse which is
        needed to mark the start time of VLab.
        */
        timer.Timeout(notifylatency);
        pport.BitPulse(bit: startbit, duration_ms: 5);
        /*
        Immediately after the TTL falling edge triggering ripple recording, we reset timer
        in VLab, so we can align VLab time zero with the ripple time of the triggering TTL falling edge. 
        */
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("Mark", OnOff.Off);
        pport.SetBit(bit: condbit, value: false);
        base.StopExperiment();
        if ((MaskType)GetEnvActiveParam("MaskType") == MaskType.DiskFade)
        {
            SetEnvActiveParam("Diameter", diameter);
        }
        // Tail period to make sure lagged effect data is recorded before stop recording
        timer.Timeout(ex.Latency + exlatencyerror + onlinesignallatency);
        pport.BitPulse(bit: stopbit, duration_ms: 5);
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
                        pport.ConcurrentBitPulse(bit: condbit, duration_ms: markpulsewidth);
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Mark", OnOff.On);
                        pport.SetBit(bit: condbit, value: true);
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
                        pport.SetBit(bit: condbit, value: false);
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
