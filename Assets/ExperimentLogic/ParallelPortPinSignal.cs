/*
ParallelPortPinSignal.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using VLab;

public class ParallelPortPinSignal : ExperimentLogic
{
    ParallelPort pport;
    ParallelPortSquareWave ppsw;

    public override void OnStart()
    {
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        ppsw = new ParallelPortSquareWave(pport);
    }

    public override void PrepareCondition(bool isforceprepare = true)
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
