using UnityEngine;
using System.Collections;

public class conditiontest : ExperimentLogic
{
    public override void Init()
    {
        ex.conddur = 0.5;
        ex.preICI = 0.1;
        ex.sufICI = 0.1;
        ex.condrepeat = 2;
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
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    envmanager.activenetbehavior.visible = false;
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
