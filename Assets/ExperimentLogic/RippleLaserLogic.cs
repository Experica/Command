/*
RippleLaserLogic.cs is part of the VLAB project.
Copyright (c) 2016 Li Alex Zhang and Contributors

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
    ParallelPort pport1, pport2;
    ParallelPortWave ppw;
    Omicron luxx473;
    Cobolt mambo594;
    float power;

    public override void OnStart()
    {
        recorder = new RippleRecorder();
        pport1 = new ParallelPort(config.ParallelPort1);
        pport2 = new ParallelPort(config.ParallelPort2);
        ppw = new ParallelPortWave(pport2);
    }

    public override void GenerateFinalCondition()
    {
        var t = ex.GetParam("LaserPower").Convert<List<float>>();
        var cond = new Dictionary<string, List<object>>()
            {
                {"LaserPower", (ex.GetParam("LaserPower").Convert<List<float>>()).Where(i => i > 0).Select(i => (object)i).ToList()},
                {"LaserFreq",(ex.GetParam("LaserFreq").Convert<List<float>>()).Where(i=>i>0).Select(i=>(object)i).ToList() }
            };
        cond = cond.OrthoCondOfFactorLevel();
        cond["LaserPower"].Insert(0,0f);
        cond["LaserFreq"].Insert(0,0f);

        condmanager.FinalizeCondition(cond);
    }

    protected override void StartExperiment()
    {
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("Mark", OnOff.Off);
        pport1.SetBit(bit: config.ConditionCh, value: false);
        luxx473 = new Omicron(config.SerialPort1);
        mambo594 = new Cobolt(config.SerialPort2);
        luxx473.LaserOn();
        timer.Timeout(ex.GetParam("LaserOnLatency").Convert<int>());

        base.StartExperiment();
        recorder.RecordPath = ex.GetDataPath();
        timer.Timeout(config.NotifyLatency);
        pport1.BitPulse(bit: config.StartCh, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        ppw.Stop(config.SignalCh1, config.SignalCh2);
        pport1.SetBit(bit: config.ConditionCh, value: false);
        base.StopExperiment();

        luxx473.LaserOff();
        luxx473.Dispose();
        mambo594.Dispose();
        timer.Timeout(ex.Latency + config.ExLatencyError + config.OnlineSignalLatency);
        pport1.BitPulse(bit: config.StopCh, duration_ms: 5);
        timer.Stop();
    }

    public override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
    {
        condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, manualcondidx, manualblockidx, istrysampleblock);
        power = (float)condmanager.finalcond["LaserPower"][condmanager.condidx];
        luxx473.PowerRatio = power;
        mambo594.PowerRatio = power;
        if (power > 0)
        {
            var freq = (float)condmanager.finalcond["LaserFreq"][condmanager.condidx];
            ppw.bitlatency_ms[config.SignalCh1] = ex.Latency;
            ppw.SetBitFreq(config.SignalCh1, freq);
            ppw.bitlatency_ms[config.SignalCh2] = ex.Latency;
            ppw.SetBitFreq(config.SignalCh2, freq);
        }
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    if (power > 0)
                    {
                        ppw.Start(config.SignalCh1, config.SignalCh2);
                    }
                    if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                    {
                        // The marker pulse width should be > 2 frames(60Hz==16.7ms) to make sure marker on_off will take effect on screen.
                        pport1.ConcurrentBitPulse(bit: config.ConditionCh, duration_ms: config.MarkPulseWidth);
                    }
                    else // ICI Mode
                    {
                        pport1.SetBit(bit: config.ConditionCh, value: true);
                    }
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    CondState = CONDSTATE.SUFICI;
                    if (power > 0)
                    {
                        ppw.Stop(config.SignalCh1, config.SignalCh2);
                    }
                    if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                    {
                    }
                    else // ICI Mode
                    {
                        pport1.SetBit(bit: config.ConditionCh, value: false);
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI + power * ex.CondDur * ex.GetParam("ICIFactor").Convert<float>())
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
