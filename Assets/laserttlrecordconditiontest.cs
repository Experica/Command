// --------------------------------------------------------------
// laserttlrecordconditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using VLab;

public class laserttlrecordconditiontest : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    Omicron luxx473 = new Omicron("COM5");
    Cobolt mambo594 = new Cobolt("COM6");

    public override void OnAwake()
    {
        ex.pushcondatstate = PUSHCONDATSTATE.PREICI;
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
        //recordmanager.recorder.SetRecordPath();
        luxx473.LaserOn();
    }

    public override void StartExperiment()
    {
        base.StartExperiment();
        pport.BitPulse(2, 0.002);
        timer.ReStart();
    }

    public override void StopExperiment()
    {
        base.StopExperiment();
        pport.BitPulse(3, 0.002);
        timer.Stop();
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                //envmanager.activenetbehavior.visible = false;
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    //envmanager.activenetbehavior.visible = true;
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    //envmanager.activenetbehavior.visible = false;
                    CondState = CONDSTATE.SUFICI;
                    pport.SetBit(bit: 0, value: false);
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold() >= ex.sufICI)
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
