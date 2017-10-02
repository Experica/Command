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
    ParallelPort pport1, pport2;
    ParallelPortWave ppw;
    int notifylatency, exlatencyerror, onlinesignallatency, markpulsewidth;
    int startch, stopch, condch, signalch1, signalch2;

    Omicron luxx473;
    Cobolt mambo594;
    float power;

    public override void OnStart()
    {
        recordmanager = new RecordManager(RecordSystem.Ripple);
        pport1 = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        pport2 = new ParallelPort((int)config[VLCFG.ParallelPort2]);
        ppw = new ParallelPortWave(pport2);
        startch = (int)config[VLCFG.StartCh];
        stopch = (int)config[VLCFG.StopCh];
        condch = (int)config[VLCFG.ConditionCh];
        signalch1 = (int)config[VLCFG.SignalCh1];
        signalch2 = (int)config[VLCFG.SignalCh2];
        notifylatency = (int)config[VLCFG.NotifyLatency];
        exlatencyerror = (int)config[VLCFG.ExLatencyError];
        onlinesignallatency = (int)config[VLCFG.OnlineSignalLatency];
        markpulsewidth = (int)config[VLCFG.MarkPulseWidth];
    }

    public override void PrepareCondition(bool isforceprepare = true)
    {
        if (isforceprepare == false && condmanager.cond != null)
        { }
        else
        {
            var cond = new Dictionary<string, List<object>>()
            {
                {"LaserPower", ((List<float>)ex.GetParam("LaserPower")).Where(i => i > 0).Select(i => (object)i).ToList()},
                {"LaserFreq",((List<float>)ex.GetParam("LaserFreq")).Select(i=>(object)i).ToList() }
            };
            cond = cond.OrthoCondOfFactorLevel();
            cond["LaserPower"].Add(0f);
            cond["LaserFreq"].Add(0f);

            condmanager.TrimCondition(cond);
        }
        if (condmanager.ncond > 0)
        {
            ex.Cond = condmanager.cond;
            condmanager.UpdateSampleSpace(ex.CondSampling, ex.BlockParam, ex.BlockSampling);
            OnConditionPrepared(true);
        }
    }

    protected override void StartExperiment()
    {
        luxx473 = new Omicron((string)config[VLCFG.SerialPort1]);
        mambo594 = new Cobolt((string)config[VLCFG.SerialPort2]);
        luxx473.LaserOn();
        timer.Timeout(3000);

        base.StartExperiment();
        recordmanager.recorder.RecordPath = ex.GetDataPath("");
        timer.Timeout(notifylatency);
        pport1.BitPulse(bit: startch, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        ppw.Stop(signalch1);
        ppw.Stop(signalch2);
        pport1.SetBit(bit: condch, value: false);
        base.StopExperiment();

        luxx473.LaserOff();
        luxx473.Dispose();
        mambo594.Dispose();
        timer.Timeout(ex.Latency + exlatencyerror + onlinesignallatency);
        pport1.BitPulse(bit: stopch, duration_ms: 5);
        timer.Stop();
    }

    public override void SamplePushCondition(bool isautosampleblock = true, int manualblockidx = 0, int manualcondidx = 0)
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
                        ppw.bitlatency_ms[signalch1] = ex.Latency;
                        ppw.SetBitFreq(signalch1, condmanager.cond["LaserFreq"][condmanager.condidx].Convert<float>());
                        ppw.bitlatency_ms[signalch2] = ex.Latency;
                        ppw.SetBitFreq(signalch2, condmanager.cond["LaserFreq"][condmanager.condidx].Convert<float>());
                        ppw.Start(signalch1);
                        ppw.Start(signalch2);
                    }
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                        // The marker pulse width should be > 2 frame(60Hz==16.7ms) to make sure
                        // marker on/off will take effect on screen.
                        pport1.ConcurrentBitPulse(bit: condch, duration_ms: markpulsewidth);
                    }
                    else // ICI Mode
                    {
                        pport1.SetBit(bit: condch, value: true);
                    }
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    CondState = CONDSTATE.SUFICI;
                    if (power > 0)
                    {
                        ppw.Stop(signalch1);
                        ppw.Stop(signalch2);
                    }
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                    }
                    else // ICI Mode
                    {
                        pport1.SetBit(bit: condch, value: false);
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (ex.SufICI == 0)
                {
                    CondState = CONDSTATE.PREICI;
                }
                if (SufICIHold >= ex.SufICI + power * ex.CondDur * (float)ex.GetParam("ICIFactor"))
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
