// --------------------------------------------------------------
// laserttlconditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

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
                envmanager.ActiveSyncSetParam("visible", false);
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
                    CondState = CONDSTATE.NONE;
                }
                break;
        }
    }
}
