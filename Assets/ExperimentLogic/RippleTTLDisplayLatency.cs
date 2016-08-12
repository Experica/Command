using UnityEngine;
using VLab;

public class RippleTTLDisplayLatency : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

    public override void OnAwake()
    {
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(""));
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
                envmanager.SetActiveParam("BGColor", Color.black, true);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    envmanager.SetActiveParam("BGColor", Color.white, true);
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    envmanager.SetActiveParam("BGColor", Color.black, true);
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
