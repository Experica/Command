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

public class RippleCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    int notifylatency = 200;
    int exlatencyerror = 20;
    int onlinesignallatency = 50;

    public override void OnStart()
    {
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(ext: ""));
        /* 
        Ripple recorder set path through UDP network and Trellis receive
        message and change file path, all of which need time to complete.
        Issue record triggering TTL before file path change completion will
        not successfully start recording.

        VLab online analysis also need time to complete signal clear buffer,
        otherwise the delayed action may clear up the start TTL pluse which is
        needed to mark the start time of VLab.
        */
        timer.Countdown(notifylatency);
        pport.BitPulse(bit: 2, duration_ms: 5);
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
        pport.SetBit(bit: 0, value: false);
        base.StopExperiment();
        // Tail period to make sure lagged effect data is recorded before stop recording
        timer.Countdown(ex.Latency + exlatencyerror + onlinesignallatency);
        pport.BitPulse(bit: 3, duration_ms: 5);
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
                        SetEnvActiveParamTwice("Mark", OnOff.On, 35, OnOff.Off);
                        pport.ThreadBitPulse(bit: 0, duration_ms: 35);
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Mark", OnOff.On);
                        pport.SetBit(bit: 0, value: true);
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
                        pport.SetBit(bit: 0, value: false);
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
