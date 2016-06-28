// --------------------------------------------------------------
// conditiontest.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using VLab;

public class ConditionTestLogic : ExperimentLogic
{
    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.ActiveSyncSetParam("visible",false);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    envmanager.ActiveSyncSetParam("visible", true);
                    CondState = CONDSTATE.COND;
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    envmanager.ActiveSyncSetParam("visible", false);
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