// -----------------------------------------------------------------------------
// LaserRippleTTLCTLogic.cs is part of the VLAB project.
// Copyright (c) 2016 Li Alex Zhang and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

using VLab;

public class LaserRippleTTLCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    Omicron luxx473 = new Omicron("COM5");
    Cobolt mambo594 = new Cobolt("COM6");

    public override void OnAwake()
    {
        luxx473.LaserOn();
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
    }

    protected override void StartExperiment()
    {
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(""));
        base.StartExperiment();
        pport.BitPulse(2, 0.1);
        timer.ReStart();
    }

    protected override void StopExperiment()
    {
        base.StopExperiment();
        pport.BitPulse(3, 0.1);
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.SetActiveParam("visible", false);
                CondState = CONDSTATE.PREICI;
                if (condmanager.cond.ContainsKey("laserpower%"))
                {
                    var v = condmanager.cond["laserpower%"][condmanager.condidx].Convert<double>();
                    luxx473.PowerRatio = v;
                    mambo594.PowerRatio = v;
                }
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    envmanager.SetActiveParam("visible", true);
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    envmanager.SetActiveParam("visible", false);
                    CondState = CONDSTATE.SUFICI;
                    pport.SetBit(bit: 0, value: false);
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    CondState = CONDSTATE.NONE;
                }
                break;
        }
    }
}
