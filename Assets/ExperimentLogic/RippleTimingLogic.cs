/*
RippleTimingLogic.cs is part of the VLAB project.
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

public class RippleTimingLogic : ExperimentLogic
{
    ParallelPort pport;
    int startch, stopch, condch;
    float markpulsewidth;

    public override void OnStart()
    {
        recordmanager = new RecordManager(RecordSystem.Ripple);
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        startch = (int)config[VLCFG.StartCh];
        stopch = (int)config[VLCFG.StopCh];
        condch = (int)config[VLCFG.ConditionCh];
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        SetEnvActiveParam("Mark", OnOff.Off);
        pport.SetBit(bit: condch, value: false);
        recordmanager.recorder.RecordPath = ex.GetDataPath(ext: "");
        timer.Timeout((float)ex.GetParam("MaxRippleStartLatency"));
        pport.BitPulse(bit: startch, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Mark", OnOff.Off);
        pport.SetBit(bit: condch, value: false);
        base.StopExperiment();
        timer.Timeout((float)ex.GetParam("MaxVLabLatency"));
        pport.BitPulse(bit: stopch, duration_ms: 5);
        timer.Stop();
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                    {
                        // The mark pulse width should be > 2 frames(60Hz==16.7ms) to make sure mark takes effect on screen.
                        markpulsewidth = (float)ex.GetParam("MarkPulseWidth");
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
                    if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                    {
                    }
                    else // ICI Mode
                    {
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