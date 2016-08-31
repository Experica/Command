/*
RippleTTLCTLogic.cs is part of the VLAB project.
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
using VLab;

public class RippleTTLCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

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
        not successfully start recording, so here a long TTL pulse is used to 
        wait Trellis ready, then trigger recording by hi->lo edge of TTL.
        */
        pport.BitPulse(bit: 2, duration_ms: 100);
        /*
        Immediately after the TTL falling edge triggering ripple recording, we reset timer
        in VLab, so we can align VLab time zero with the ripple time of the triggering TTL. 
        */
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        base.StopExperiment();
        pport.BitPulse(bit: 3, duration_ms: 1);
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.SetActiveParam("Visible", false, true);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    envmanager.SetActiveParam("Visible", true, true);
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    envmanager.SetActiveParam("Visible", false, true);
                    CondState = CONDSTATE.SUFICI;
                    pport.SetBit(bit: 0, value: false);
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
