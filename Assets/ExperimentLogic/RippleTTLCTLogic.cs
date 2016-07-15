// --------------------------------------------------------------
// ttlrecordconditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using VLab;

public class RippleTTLCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

    protected override void StartExperiment()
    {
        base.StartExperiment();
        pport.BitPulse(0, 0.1);
        timer.ReStart();
    }

    protected override void StopExperiment()
    {
        base.StopExperiment();
        pport.BitPulse(0, 0.1);
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.ActiveSyncSetParam("visible", false);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    envmanager.ActiveSyncSetParam("visible", true);
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    envmanager.ActiveSyncSetParam("visible", false);
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
