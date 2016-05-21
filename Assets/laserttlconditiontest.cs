// --------------------------------------------------------------
// laserttlconditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using VLab;

public class laserttlconditiontest : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    Omicron luxx473 = new Omicron("COM5");
    Cobolt mambo594 = new Cobolt("COM6");

    public override void OnAwake()
    {
        ex.pushcondatstate = PUSHCONDATSTATE.PREICI;
        luxx473.LaserOn();
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                //envmanager.activenetbehavior.visible = false;
                CondState = CONDSTATE.PREICI;

                var v = double.Parse((string)condmanager.cond["laserpower%"][condmanager.condidx]);
                luxx473.PowerRatio = v;
                mambo594.PowerRatio = v;
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
                    CondState = CONDSTATE.NONE;
                }
                break;
        }
    }
}
