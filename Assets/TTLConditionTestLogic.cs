// --------------------------------------------------------------
// ttlrecordconditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using VLab;

public class TTLConditionTestLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

    public override void OnAwake()
    {
        ex.pushcondatstate = PUSHCONDATSTATE.PREICI;
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
