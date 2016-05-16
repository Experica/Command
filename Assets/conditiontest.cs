// --------------------------------------------------------------
// ConditionTest.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using VLab;

public class ConditionTest : ExperimentLogic
{
    public override void Init()
    {
        //ex.conddur = 500;
        //ex.preICI = 100;
        //ex.sufICI = 100;
        //ex.condrepeat = 3;
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.ActiveSyncVisible(false);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    envmanager.ActiveSyncVisible(true);
                    CondState = CONDSTATE.COND;
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    envmanager.ActiveSyncVisible(false);
                    CondState = CONDSTATE.SUFICI;
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
