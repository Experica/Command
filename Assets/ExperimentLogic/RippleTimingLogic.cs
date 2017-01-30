using UnityEngine;
using VLab;

public class RippleTimingLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);

    public override void OnStart()
    {
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(ext: ""));
        pport.BitPulse(bit: 2, duration_ms: 100);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Mark", OnOff.Off);
        pport.SetBit(bit: 0, value: false);

        base.StopExperiment();
        pport.BitPulse(bit: 3, duration_ms: 1);
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                SetEnvActiveParam("Mark", OnOff.Off);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
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