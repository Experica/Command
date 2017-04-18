using VLab;

public class ParallelPortPinSignal : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    ParallelPortSquareWave ppsw;

    public override void OnStart()
    {
        ppsw = new ParallelPortSquareWave(pport);
    }

    public override void PrepareCondition()
    {
        ppsw.bitlatency_ms[0] = 0;
        ppsw.bitlatency_ms[2] = 0;
        ppsw.bitlatency_ms[3] = 0;
        ppsw.SetBitFreq(0, 1);
        ppsw.SetBitFreq(2, 2);
        ppsw.SetBitFreq(3, 4);
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        ppsw.Start(0, 1, 2, 3);
    }

    protected override void StopExperiment()
    {
        ppsw.Stop(0, 1, 2, 3);
        base.StopExperiment();
        timer.Stop();
    }
}
