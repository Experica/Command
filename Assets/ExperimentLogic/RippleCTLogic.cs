/*
RippleCTLogic.cs is part of the VLAB project.
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

public class RippleCTLogic : ExperimentLogic
{
    ParallelPort pport;
    int notifylatency, exlatencyerror, onlinesignallatency, markpulsewidth;
    int startch, stopch, condch;

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

    protected override void StartExperiment()
    {
        base.StartExperiment();
        recordmanager.recorder.RecordPath = ex.GetDataPath(ext: "");
        /* 
        Ripple recorder set path through UDP network and Trellis receive
        message and change file path, all of which need time to complete.
        Trigger record TTL before file path change completion will
        not successfully start recording.

        VLab online analysis also need time to clear signal buffer,
        otherwise the delayed action may clear the start TTL pluse which is
        needed to mark the start time of VLab.
        */
        timer.Timeout(notifylatency);
        pport.BitPulse(bit: startch, duration_ms: 5);
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
        pport.SetBit(bit: condch, value: false);
        base.StopExperiment();
        // Tail period to make sure lagged effect data is recorded before stop recording
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
                        // marker on/off will take effect on screen.
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
