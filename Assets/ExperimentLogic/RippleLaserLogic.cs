/*
RippleLaserLogic.cs is part of the VLAB project.
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
using System.Collections.Generic;
using System.Linq;


public class RippleLaserLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    ParallelPortSquareWave ppsw;
    int notifylatency = 200;
    int exlatencyerror = 20;
    int onlinesignallatency = 50;

    int ppbit = 0;
    Omicron luxx473;
    Cobolt mambo594;
    float power;
    int ICIFactor = 5;

    public override void OnStart()
    {
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
        ppsw = new ParallelPortSquareWave(pport);
    }

    public override void PrepareCondition()
    {
        var cond = new Dictionary<string, List<object>>()
            {
                {"LaserPower", ((List<float>)ex.GetParam("LaserPower")).Where(i => i > 0).Select(i => (object)i).ToList()},
                {"LaserFreq",((List<float>)ex.GetParam("LaserFreq")).Select(i=>(object)i).ToList() }
            };
        cond = cond.OrthoCondOfFactorLevel();
        cond["LaserPower"].Add(0f);
        cond["LaserFreq"].Add(0.1f);

        condmanager.TrimCondition(cond);
        ex.Cond = condmanager.cond;
        condmanager.UpdateSampleSpace(ex.CondSampling,ex.BlockParam,ex.BlockSampling);
        OnConditionPrepared(true);
    }

    protected override void StartExperiment()
    {
        luxx473 = new Omicron("COM5");
        mambo594 = new Cobolt("COM6");
        luxx473.LaserOn();
        timer.Countdown(3000);

        base.StartExperiment();
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(""));
        timer.Countdown(notifylatency);
        pport.BitPulse(bit: 2, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        ppsw.Stop(ppbit);
        base.StopExperiment();

        luxx473.LaserOff();
        luxx473.Dispose();
        mambo594.Dispose();
        timer.Countdown(ex.Latency + exlatencyerror + onlinesignallatency);
        pport.BitPulse(bit: 3, duration_ms: 5);
        timer.Stop();
    }

    public override void SamplePushCondition(bool isautosampleblock = true)
    {
        condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, isautosampleblock);
        power = condmanager.cond["LaserPower"][condmanager.condidx].Convert<float>();
        luxx473.PowerRatio = power;
        mambo594.PowerRatio = power;
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                SetEnvActiveParam("Visible", false);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    if (power > 0)
                    {
                        ppsw.bitlatency[ppbit] = ex.Latency;
                        ppsw.bitfreq[ppbit] = condmanager.cond["LaserFreq"][condmanager.condidx].Convert<float>();
                        ppsw.Start(ppbit);
                    }
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    CondState = CONDSTATE.SUFICI;
                    if (power > 0)
                    {
                        ppsw.Stop(ppbit);
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI * (1 + power * ICIFactor))
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
