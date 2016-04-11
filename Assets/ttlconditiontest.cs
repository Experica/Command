using UnityEngine;
using System;
using System.Linq;
using System.Collections;

public class ttlconditiontest : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

    public override void Init()
    {
        PushCondAtState = PUSHCONDATSTATE.PREICI;
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.activenetbehavior.visible = false;
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    envmanager.activenetbehavior.visible = true;
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    envmanager.activenetbehavior.visible = false;
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
