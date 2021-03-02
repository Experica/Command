/*
RippleLaserImageLogic.cs is part of the Experica.
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
using UnityEngine;

namespace Experica
{
    public class RippleLaserImageLogic : RippleLaserCTLogic
    {
        protected float diameterbeforeadjust;
        protected bool isdiameteradjusted;

        protected override void OnStartExperiment()
        {
            base.OnStartExperiment();
            var mt = (MaskType)GetEnvActiveParam("MaskType");
            if (mt == MaskType.DiskFade || mt == MaskType.Disk)
            {
                isdiameteradjusted = true;
                diameterbeforeadjust = (float)GetEnvActiveParam("Diameter");
                var mrr = (float)GetEnvActiveParam("MaskRadius") / 0.5f;
                SetEnvActiveParam("Diameter", diameterbeforeadjust / mrr);
            }
        }

        protected override void OnExperimentStopped()
        {
            base.OnExperimentStopped();
            if (isdiameteradjusted)
            {
                SetEnvActiveParam("Diameter", diameterbeforeadjust);
                isdiameteradjusted = false;
            }
        }

        protected override void GenerateFinalCondition()
        {
            pushexcludefactors = new List<string>() { "LaserPower", "LaserFreq", "LaserPower2", "LaserFreq2" };

            // get laser conditions
            laser = ex.GetParam("Laser").Convert<string>().GetLaser(config);
            switch (laser?.Type)
            {
                case Laser.Omicron:
                    lasersignalch = config.SignalCh1;
                    break;
                case Laser.Cobolt:
                    lasersignalch = config.SignalCh2;
                    break;
            }
            laser2 = ex.GetParam("Laser2").Convert<string>().GetLaser(config);
            switch (laser2?.Type)
            {
                case Laser.Omicron:
                    laser2signalch = config.SignalCh1;
                    break;
                case Laser.Cobolt:
                    laser2signalch = config.SignalCh2;
                    break;
            }

            var p = ex.GetParam("LaserPower").Convert<List<float>>();
            var f = ex.GetParam("LaserFreq").Convert<List<Vector4>>();
            var p2 = ex.GetParam("LaserPower2").Convert<List<float>>();
            var f2 = ex.GetParam("LaserFreq2").Convert<List<Vector4>>();
            var lcond = new Dictionary<string, List<object>>();
            if (lasersignalch != null)
            {
                if (p != null)
                {
                    lcond["LaserPower"] = p.Where(i => i > 0).Select(i => (object)i).ToList();
                }
                if (f != null)
                {
                    lcond["LaserFreq"] = f.Where(i => i != Vector4.zero).Select(i => (object)i).ToList();
                }
            }
            if (laser2signalch != null)
            {
                if (p2 != null)
                {
                    lcond["LaserPower2"] = p2.Where(i => i > 0).Select(i => (object)i).ToList();
                }
                if (f2 != null)
                {
                    lcond["LaserFreq2"] = f2.Where(i => i != Vector4.zero).Select(i => (object)i).ToList();
                }
            }
            lcond = lcond.OrthoCondOfFactorLevel();

            var addzero = ex.GetParam("AddZeroCond").Convert<bool>();
            if (lasersignalch != null)
            {
                if (p != null && addzero)
                {
                    lcond["LaserPower"].Insert(0, 0f);
                }
                if (f != null && addzero)
                {
                    lcond["LaserFreq"].Insert(0, Vector4.zero);
                }
            }
            if (laser2signalch != null)
            {
                if (p2 != null && addzero)
                {
                    lcond["LaserPower2"].Insert(0, 0f);
                }
                if (f2 != null && addzero)
                {
                    lcond["LaserFreq2"].Insert(0, Vector4.zero);
                }
            }

            // get image conditions
            var bcond = new Dictionary<string, List<object>>
            {
                ["Image"] = Enumerable.Range((int)GetEnvActiveParam("StartIndex"), (int)GetEnvActiveParam("NumOfImage")).Select(i => (object)i).ToList()
            };

            // combine laser and image conditions
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

        protected override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
        {
            // Laser manual sampling and pushing defered into logic BlockState, while Laser on/off into logic TrialState,
            // so that Laser been pushed only once at the beginning of each Block and turned on in each Trial.
            base.SamplePushCondition(manualcondidx, manualblockidx, false);
        }

        protected override void Logic()
        {
            switch (BlockState)
            {
                case BLOCKSTATE.NONE:
                    EnterBlockState(BLOCKSTATE.PREIBI);
                    if (condmanager.blockidx < 0)
                    {
                        condmanager.SampleBlockSpace();
                        if (condmanager.finalblockcond.ContainsKey("LaserPower"))
                        {
                            power = (float)condmanager.finalblockcond["LaserPower"][condmanager.blockidx];
                            if (lasersignalch != null)
                            {
                                laser.PowerRatio = power;
                                if (power > 0 && condmanager.finalblockcond.ContainsKey("LaserFreq"))
                                {
                                    var freq = (Vector4)condmanager.finalblockcond["LaserFreq"][condmanager.blockidx];
                                    if (freq.y > 0 && freq.z <= 0 && freq.w <= 0)
                                    {
                                        ppw.SetBitWave(lasersignalch.Value, freq.y, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq.x);
                                    }
                                    else if (freq.y > 0 && freq.z > 0 && freq.w <= 0)
                                    {
                                        ppw.SetBitWave(lasersignalch.Value, freq.y, freq.z, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq.x);
                                    }
                                    else if (freq.y > 0 && freq.z > 0 && freq.w > 0)
                                    {
                                        ppw.SetBitWave(lasersignalch.Value, freq.y, freq.z, freq.w, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq.x);
                                    }
                                }
                            }
                        }
                        if (condmanager.finalblockcond.ContainsKey("LaserPower2"))
                        {
                            power2 = (float)condmanager.finalblockcond["LaserPower2"][condmanager.blockidx];
                            if (laser2signalch != null)
                            {
                                laser2.PowerRatio = power2;
                                if (power2 > 0 && condmanager.finalblockcond.ContainsKey("LaserFreq2"))
                                {
                                    var freq2 = (Vector4)condmanager.finalblockcond["LaserFreq2"][condmanager.blockidx];
                                    if (freq2.y > 0 && freq2.z <= 0 && freq2.w <= 0)
                                    {
                                        ppw.SetBitWave(laser2signalch.Value, freq2.y, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq2.x);
                                    }
                                    else if (freq2.y > 0 && freq2.z > 0 && freq2.w <= 0)
                                    {
                                        ppw.SetBitWave(laser2signalch.Value, freq2.y, freq2.z, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq2.x);
                                    }
                                    else if (freq2.y > 0 && freq2.z > 0 && freq2.w > 0)
                                    {
                                        ppw.SetBitWave(laser2signalch.Value, freq2.y, freq2.z, freq2.w, ex.Display_ID.DisplayLatency(config.Display) ?? 0, freq2.x);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case BLOCKSTATE.PREIBI:
                    if (PreIBIHold >= ex.PreIBI)
                    {
                        EnterBlockState(BLOCKSTATE.BLOCK);
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    switch (TrialState)
                    {
                        case TRIALSTATE.NONE:
                            if (EnterTrialState(TRIALSTATE.PREITI) == EnterCode.NoNeed) { return; }
                            break;
                        case TRIALSTATE.PREITI:
                            if (PreITIHold >= ex.PreITI)
                            {
                                EnterTrialState(TRIALSTATE.TRIAL);
                                var lsc = new List<int>();
                                if (power > 0 && lasersignalch != null)
                                {
                                    lsc.Add(lasersignalch.Value);
                                }
                                if (power2 > 0 && laser2signalch != null)
                                {
                                    lsc.Add(laser2signalch.Value);
                                }
                                ppw.Start(lsc.ToArray());
                            }
                            break;
                        case TRIALSTATE.TRIAL:
                            switch (CondState)
                            {
                                case CONDSTATE.NONE:
                                    if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                                    break;
                                case CONDSTATE.PREICI:
                                    if (PreICIHold >= ex.PreICI)
                                    {
                                        EnterCondState(CONDSTATE.COND);
                                        SyncEvent(CONDSTATE.COND.ToString());
                                        if (ex.GetParam("WithVisible").Convert<bool>())
                                        {
                                            SetEnvActiveParam("Visible", true);
                                        }
                                    }
                                    break;
                                case CONDSTATE.COND:
                                    if (CondHold >= ex.CondDur)
                                    {
                                        EnterCondState(CONDSTATE.SUFICI);
                                        if (ex.PreICI != 0 || ex.SufICI != 0)
                                        {
                                            SyncEvent(CONDSTATE.SUFICI.ToString());
                                            if (ex.GetParam("WithVisible").Convert<bool>())
                                            {
                                                SetEnvActiveParam("Visible", false);
                                            }
                                        }
                                    }
                                    break;
                                case CONDSTATE.SUFICI:
                                    if (SufICIHold >= ex.SufICI)
                                    {
                                        if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                                        if (TrialHold >= ex.TrialDur || condmanager.IsCondRepeatInBlock(ex.CondRepeat, ex.BlockRepeat))
                                        {
                                            EnterTrialState(TRIALSTATE.SUFITI);
                                            if (ex.GetParam("WithVisible").Convert<bool>())
                                            {
                                                SetEnvActiveParam("Visible", false);
                                            }
                                            var lsc = new List<int>();
                                            if (power > 0 && lasersignalch != null)
                                            {
                                                lsc.Add(lasersignalch.Value);
                                            }
                                            if (power2 > 0 && laser2signalch != null)
                                            {
                                                lsc.Add(laser2signalch.Value);
                                            }
                                            ppw.Stop(lsc.ToArray());
                                        }
                                    }
                                    break;
                            }
                            break;
                        case TRIALSTATE.SUFITI:
                            if (SufITIHold >= ex.SufITI + power * ex.TrialDur * ex.GetParam("ITIFactor").Convert<float>())
                            {
                                if (EnterTrialState(TRIALSTATE.PREITI) == EnterCode.NoNeed) { return; }
                                if (condmanager.IsCondRepeatInBlock(ex.CondRepeat, ex.BlockRepeat))
                                {
                                    EnterBlockState(BLOCKSTATE.SUFIBI);
                                }
                            }
                            break;
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    if (SufIBIHold >= ex.SufIBI)
                    {
                        condmanager.blockidx = -1;
                        EnterBlockState(BLOCKSTATE.NONE);
                    }
                    break;
            }
        }
    }
}