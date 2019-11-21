/*
RippleLaserLogic.cs is part of the Experica.
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
    public class RippleLaserLogic : RippleCTLogic
    {
        protected ParallelPort pport2;
        protected GPIOWave ppw;
        protected ILaser laser, laser2;
        protected int? lasersignalch = null, laser2signalch = null;
        protected float power, power2;

        protected override void OnStart()
        {
            base.OnStart();
            pport2 = new ParallelPort(config.ParallelPort2);
            ppw = new GPIOWave(pport2);
        }

        protected override void GenerateFinalCondition()
        {
            pushexcludefactors = new List<string>() { "LaserPower", "LaserFreq", "LaserPower2", "LaserFreq2" };

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

            condmanager.FinalizeCondition(lcond);
        }

        protected override void StartExperimentTimeSync()
        {
            laser?.LaserOn();
            laser2?.LaserOn();
            timer.Timeout(ex.GetParam("LaserOnLatency").Convert<int>());
            base.StartExperimentTimeSync();
        }

        protected override void OnStopExperiment()
        {
            ppw.StopAll();
            base.OnStopExperiment();
        }

        protected override void OnExperimentStopped()
        {
            base.OnExperimentStopped();
            laser?.LaserOff();
            laser?.Dispose();
            lasersignalch = null;
            laser2?.LaserOff();
            laser2?.Dispose();
            laser2signalch = null;
        }

        protected override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
        {
            base.SamplePushCondition(manualcondidx, manualblockidx, istrysampleblock);
            // Push laser conditions
            if (condmanager.finalcond.ContainsKey("LaserPower"))
            {
                power = (float)condmanager.finalcond["LaserPower"][condmanager.condidx];
                if (lasersignalch != null)
                {
                    laser.PowerRatio = power;
                    if (power > 0 && condmanager.finalcond.ContainsKey("LaserFreq"))
                    {
                        var freq = (Vector4)condmanager.finalcond["LaserFreq"][condmanager.condidx];
                        if (freq.y > 0 && freq.z <= 0 && freq.w <= 0)
                        {
                            ppw.SetBitWave(lasersignalch.Value, freq.y, ex.Display_ID.DisplayLatency(config.Display), freq.x);
                        }
                        else if (freq.y > 0 && freq.z > 0 && freq.w <= 0)
                        {
                            ppw.SetBitWave(lasersignalch.Value, freq.y, freq.z, ex.Display_ID.DisplayLatency(config.Display), freq.x);
                        }
                        else if (freq.y > 0 && freq.z > 0 && freq.w > 0)
                        {
                            ppw.SetBitWave(lasersignalch.Value, freq.y, freq.z, freq.w, ex.Display_ID.DisplayLatency(config.Display), freq.x);
                        }
                    }
                }
            }
            if (condmanager.finalcond.ContainsKey("LaserPower2"))
            {
                power2 = (float)condmanager.finalcond["LaserPower2"][condmanager.condidx];
                if (laser2signalch != null)
                {
                    laser2.PowerRatio = power2;
                    if (power2 > 0 && condmanager.finalcond.ContainsKey("LaserFreq2"))
                    {
                        var freq2 = (Vector4)condmanager.finalcond["LaserFreq2"][condmanager.condidx];
                        if (freq2.y > 0 && freq2.z <= 0 && freq2.w <= 0)
                        {
                            ppw.SetBitWave(laser2signalch.Value, freq2.y, ex.Display_ID.DisplayLatency(config.Display), freq2.x);
                        }
                        else if (freq2.y > 0 && freq2.z > 0 && freq2.w <= 0)
                        {
                            ppw.SetBitWave(laser2signalch.Value, freq2.y, freq2.z, ex.Display_ID.DisplayLatency(config.Display), freq2.x);
                        }
                        else if (freq2.y > 0 && freq2.z > 0 && freq2.w > 0)
                        {
                            ppw.SetBitWave(laser2signalch.Value, freq2.y, freq2.z, freq2.w, ex.Display_ID.DisplayLatency(config.Display), freq2.x);
                        }
                    }
                }
            }
        }

        protected override void Logic()
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
                        SyncEvent(CONDSTATE.COND.ToString());
                        if (ex.GetParam("WithVisible").Convert<bool>())
                        {
                            SetEnvActiveParam("Visible", true);
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
                        ppw.Start(lsc.ToArray());
                    }
                    break;
                case CONDSTATE.COND:
                    if (CondHold >= ex.CondDur)
                    {
                        CondState = CONDSTATE.SUFICI;
                        if (ex.PreICI != 0 || ex.SufICI != 0)
                        {
                            SyncEvent(CONDSTATE.SUFICI.ToString());
                            if (ex.GetParam("WithVisible").Convert<bool>())
                            {
                                SetEnvActiveParam("Visible", false);
                            }
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
                    break;
                case CONDSTATE.SUFICI:
                    if (SufICIHold >= ex.SufICI + power * ex.CondDur * ex.GetParam("ICIFactor").Convert<float>())
                    {
                        CondState = CONDSTATE.NONE;
                    }
                    break;
            }
        }
    }
}