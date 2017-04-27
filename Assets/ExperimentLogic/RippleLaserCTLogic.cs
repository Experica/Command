/*
RippleLaserCTLogic.cs is part of the VLAB project.
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


public class RippleLaserCTLogic : ExperimentLogic
{
    ParallelPort pport;
    ParallelPortSquareWave ppsw;
    int notifylatency, exlatencyerror, onlinesignallatency,markpulsewidth;
    int startbit, stopbit, condbit, signalbit;

    Omicron luxx473;
    Cobolt mambo594;
    float power;
    int ICIFactor = 5;
    List<string> condpushexcept = new List<string>() { "LaserPower", "LaserFreq" };

    public override void OnStart()
    {
        recordmanager = new RecordManager(VLRecordSystem.Ripple);
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
        ppsw = new ParallelPortSquareWave(pport);
        startbit = (int)config[VLCFG.StartBit];
        stopbit = (int)config[VLCFG.StopBit];
        condbit = (int)config[VLCFG.ConditionBit];
        signalbit = (int)config[VLCFG.Signal1Bit];
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
            // get laser conditions
            var lcond = new Dictionary<string, List<object>>()
            {
                {"LaserPower", ((List<float>)ex.GetParam("LaserPower")).Where(i => i > 0).Select(i => (object)i).ToList()},
                {"LaserFreq",((List<float>)ex.GetParam("LaserFreq")).Select(i=>(object)i).ToList() }
            };
            lcond = lcond.OrthoCondOfFactorLevel();
            lcond["LaserPower"].Add(0f);
            lcond["LaserFreq"].Add(0.1f);

            // get base conditions
            var bcond = condmanager.ReadCondition(ex.CondPath);
            if (bcond != null)
            {
                bcond = bcond.ResolveConditionReference(ex.Param).FactorLevelOfDesign();
                if (bcond.ContainsKey("factorlevel") && bcond["factorlevel"].Count == 0)
                {
                    bcond = bcond.OrthoCondOfFactorLevel();
                }
                condmanager.TrimCondition(bcond);
            }

            // get final conditions
            var fcond = new Dictionary<string, List<object>>()
        {
            {"l",Enumerable.Range(0,lcond.First().Value.Count).Select(i=>(object)i).ToList() },
            {"b",Enumerable.Range(0,bcond.First().Value.Count).Select(i=>(object)i).ToList() }
        };
            fcond = fcond.OrthoCondOfFactorLevel();
            foreach (var bf in bcond.Keys)
            {
                fcond[bf] = new List<object>();
            }
            foreach (var lf in lcond.Keys)
            {
                fcond[lf] = new List<object>();
            }
            for (var i = 0; i < fcond["l"].Count; i++)
            {
                var bci = (int)fcond["b"][i];
                var lci = (int)fcond["l"][i];
                foreach (var bf in bcond.Keys)
                {
                    fcond[bf].Add(bcond[bf][bci]);
                }
                foreach (var lf in lcond.Keys)
                {
                    fcond[lf].Add(lcond[lf][lci]);
                }
            }
            fcond.Remove("b"); fcond.Remove("l");

            condmanager.TrimCondition(fcond);
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
        luxx473 = new Omicron((string)config[VLCFG.COMPort1]);
        mambo594 = new Cobolt((string)config[VLCFG.COMPort2]);
        luxx473.LaserOn();
        timer.Countdown(3000);

        base.StartExperiment();
        recordmanager.recorder.SetRecordPath(ex.GetDataPath(""));
        timer.Countdown(notifylatency);
        pport.BitPulse(bit: startbit, duration_ms: 5);
        timer.Restart();
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("Mark", OnOff.Off);
        ppsw.Stop(signalbit);
        base.StopExperiment();

        luxx473.LaserOff();
        luxx473.Dispose();
        mambo594.Dispose();
        timer.Countdown(ex.Latency + exlatencyerror + onlinesignallatency);
        pport.BitPulse(bit: stopbit, duration_ms: 5);
        timer.Stop();
    }

    public override void SamplePushCondition(bool isautosampleblock = true)
    {
        condmanager.PushCondition(condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, isautosampleblock),
            envmanager, condpushexcept);
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
                SetEnvActiveParam("Mark", OnOff.Off);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    CondState = CONDSTATE.COND;
                    SetEnvActiveParam("Visible", true);
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                        // The marker pulse width should be > 2 frame(60Hz==16.7ms) to make sure
                        // marker params will take effect on screen.
                        SetEnvActiveParamTwice("Mark", OnOff.On, markpulsewidth, OnOff.Off);
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Mark", OnOff.On);
                    }
                    if (power > 0)
                    {
                        ppsw.bitlatency_ms[signalbit] = ex.Latency;
                        ppsw.SetBitFreq(signalbit, condmanager.cond["LaserFreq"][condmanager.condidx].Convert<float>());
                        ppsw.Start(signalbit);
                    }
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    CondState = CONDSTATE.SUFICI;
                    // None ICI Mode
                    if (ex.PreICI == 0 && ex.SufICI == 0)
                    {
                    }
                    else // ICI Mode
                    {
                        SetEnvActiveParam("Visible", false);
                        SetEnvActiveParam("Mark", OnOff.Off);
                    }
                    if (power > 0)
                    {
                        ppsw.Stop(signalbit);
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
