
using VLab;

public class OITTLCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    bool isreverse = false;

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.ActiveSyncSetParam("visible", false);
                CondState = CONDSTATE.PREICI;
                envmanager.ActiveSyncSetParam("isdrifting", false);
                envmanager.ActiveSyncSetParam("visible", true);
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    envmanager.ActiveSyncSetParam("isdrifting", true);
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (!isreverse && CondHold() >= ex.conddur)
                {
                    envmanager.ActiveSyncSetParam("isreversetime", true);
                    //var curori = (float)envmanager.GetParam("ori");
                    //envmanager.ActiveSyncSetParam("ori", curori + 180);
                    isreverse = true;
                }
                if (isreverse && CondHold() >= 2 * ex.conddur)
                {
                    envmanager.ActiveSyncSetParam("visible", false);
                    isreverse = false;
                    envmanager.ActiveSyncSetParam("isreversetime", false);
                    CondState = CONDSTATE.SUFICI;
                    pport.SetBit(bit: 0, value: false);
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold() >= ex.sufICI)
                {
                    CondState = CONDSTATE.PREICI;
                    envmanager.ActiveSyncSetParam("isdrifting", false);
                    envmanager.ActiveSyncSetParam("visible", true);
                }
                break;
        }
    }
}
