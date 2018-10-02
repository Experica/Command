/*
RippleLaserCTLogic.cs is part of the Experica.
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
using System.Collections.Generic;
using System.Linq;

namespace Experica
{
    public class RippleLaserCTLogic : ExperimentLogic
    {
        ParallelPort pport1, pport2;
        ParallelPortWave ppw;
        ILaser laser;
        float power;
        List<string> factorpushexcept = new List<string>() { "LaserPower", "LaserFreq" };

        protected override void OnStart()
        {
            recorder = new RippleRecorder();
            pport1 = new ParallelPort(config.ParallelPort1);
            pport2 = new ParallelPort(config.ParallelPort2);
            ppw = new ParallelPortWave(pport2);
        }

        protected override void GenerateFinalCondition()
        {
            // get laser conditions
            var lcond = new Dictionary<string, List<object>>()
            {
                {"LaserPower", (ex.GetParam("LaserPower").Convert<List<float>>()).Where(i => i > 0).Select(i => (object)i).ToList()},
                {"LaserFreq",(ex.GetParam("LaserFreq").Convert<List<float>>()).Where(i=>i>0).Select(i=>(object)i).ToList() }
            };
            lcond = lcond.OrthoCondOfFactorLevel();
            lcond["LaserPower"].Insert(0, 0f);
            lcond["LaserFreq"].Insert(0, 0f);

            // get base conditions
            var bcond = condmanager.GenerateFinalCondition(ex.CondPath);

            // combine laser and base conditions
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

            condmanager.FinalizeCondition(fcond);
        }

        protected override void StartExperiment()
        {
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("Mark", false);
            pport1.SetBit(bit: config.EventSyncCh, value: false);
            laser = ex.GetParam("Laser").Convert<string>().GetLaser(config);
            laser.LaserOn();
            timer.Timeout(ex.GetParam("LaserOnLatency").Convert<int>());

            base.StartExperiment();
            recorder.RecordPath = ex.GetDataPath();
            timer.Timeout(config.NotifyLatency);
            pport1.BitPulse(bit: config.StartSyncCh, duration_ms: 5);
            timer.Restart();
        }

        protected override void StopExperiment()
        {
            ppw.Stop(config.SignalCh1, config.SignalCh2);
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("Mark", false);
            pport1.SetBit(bit: config.EventSyncCh, value: false);
            base.StopExperiment();

            laser.LaserOff();
            laser.Dispose();
            timer.Timeout(ex.DisplayLatency + config.MaxDisplayLatencyError + config.OnlineSignalLatency);
            pport1.BitPulse(bit: config.StopSyncCh, duration_ms: 5);
            timer.Stop();
        }

        protected override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
        {
            condmanager.PushCondition(condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, manualcondidx, manualblockidx, istrysampleblock),
                envmanager, factorpushexcept);
            power = (float)condmanager.finalcond["LaserPower"][condmanager.condidx];
            laser.PowerRatio = power;
            if (power > 0)
            {
                var freq = (float)condmanager.finalcond["LaserFreq"][condmanager.condidx];
                ppw.SetBitWave(config.SignalCh1, freq, ex.DisplayLatency);
                ppw.SetBitWave(config.SignalCh2, freq, ex.DisplayLatency);
            }
        }

        protected override void Logic()
        {
            switch (BlockState)
            {
                case BLOCKSTATE.NONE:
                    BlockState = BLOCKSTATE.PREIBI;
                    break;
                case BLOCKSTATE.PREIBI:
                    if (PreIBIHold >= ex.PreIBI)
                    {
                        BlockState = BLOCKSTATE.BLOCK;
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    switch (CondState)
                    {
                        case CONDSTATE.NONE:
                            SetEnvActiveParam("Visible", false);
                            SetEnvActiveParam("Mark", false);
                            CondState = CONDSTATE.PREICI;
                            break;
                        case CONDSTATE.PREICI:
                            if (PreICIHold >= ex.PreICI)
                            {
                                CondState = CONDSTATE.COND;
                                SetEnvActiveParam("Visible", true);
                                if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                                {
                                    // The marker pulse width should be > 2 frames(60Hz==16.7ms) to make sure marker on_off will take effect on screen.
                                    SetEnvActiveParamTwice("Mark", true, config.MarkPulseWidth, false);
                                    pport1.ConcurrentBitPulse(bit: config.EventSyncCh, duration_ms: config.MarkPulseWidth);
                                }
                                else // ICI Mode
                                {
                                    SetEnvActiveParam("Mark", true);
                                    pport1.SetBit(bit: config.EventSyncCh, value: true);
                                }
                                if (power > 0)
                                {
                                    ppw.Start(config.SignalCh1, config.SignalCh2);
                                }
                            }
                            break;
                        case CONDSTATE.COND:
                            if (CondHold >= ex.CondDur)
                            {
                                CondState = CONDSTATE.SUFICI;
                                if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                                {
                                }
                                else // ICI Mode
                                {
                                    SetEnvActiveParam("Visible", false);
                                    SetEnvActiveParam("Mark", false);
                                    pport1.SetBit(bit: config.EventSyncCh, value: false);
                                }
                                if (power > 0)
                                {
                                    ppw.Stop(config.SignalCh1, config.SignalCh2);
                                }
                            }
                            break;
                        case CONDSTATE.SUFICI:
                            if (SufICIHold >= ex.SufICI + power * ex.CondDur * ex.GetParam("ICIFactor").Convert<float>())
                            {
                                if (condmanager.IsCondRepeatInBlock(ex.CondRepeat, ex.BlockRepeat))
                                {
                                    CondState = CONDSTATE.NONE;
                                    BlockState = BLOCKSTATE.SUFIBI;
                                }
                                else
                                {
                                    CondState = CONDSTATE.PREICI;
                                }
                            }
                            break;
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    if (SufIBIHold >= ex.SufIBI)
                    {
                        BlockState = BLOCKSTATE.PREIBI;
                    }
                    break;
            }
        }
    }
}