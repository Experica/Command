/*
OIMaster.cs is part of the Experica.
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
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Experica
{
    public class OIMaster : ExperimentLogic
    {
        protected int condidx;
        protected bool start, go, issizeadjusted;
        protected double reversetime;
        protected Vector3 sizebeforeadjust;

        protected override void OnStart()
        {
            recorder = new RippleRecorder();
        }

        protected override void OnStartExperiment()
        {
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("ReverseTime", false);
            var fss = ex.GetParam("FullScreenSize");
            if (fss != null && fss.Convert<bool>())
            {
                issizeadjusted = true;
                sizebeforeadjust = (Vector3)GetEnvActiveParam("Size");
                var hh = envmanager.maincamera_scene.orthographicSize;
                var hw = hh * envmanager.maincamera_scene.aspect;
                SetEnvActiveParam("Size", new Vector3(2.1f * hw, 2.1f * hh, 1));
            }
        }

        protected override void OnExperimentStopped()
        {
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("ReverseTime", false);
            if (issizeadjusted)
            {
                SetEnvActiveParam("Size", sizebeforeadjust);
                issizeadjusted = false;
            }
        }

        /// <summary>
        /// Optical Imaging VDAQ output a byte, of which bit 7 is the GO bit,
        /// and bit 0-6 can represent StimulusID:0-127. In order to send StimulusID
        /// before GO bit, we use ID:0 as blank stimulus, and all real stimulus
        /// start from 1 and map to condidx 0.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="go"></param>
        /// <param name="condidx"></param>
        void ParseOIMessage(ref bool start, ref bool go, ref int condidx)
        {
            Dictionary<int, List<double>> dt; Dictionary<int, List<int>> dv;
            var isdin = recorder.ReadDigitalInput(out dt, out dv);
            if (isdin && dt.ContainsKey(config.Bits16Ch) && dt[config.Bits16Ch] != null && dt[config.Bits16Ch].Count > 0)
            {
                int msg = dv[config.Bits16Ch].Last();
                if (msg > 127)
                {
                    go = true;
                    msg -= 128;
                }
                else
                {
                    go = false;
                }
                if (msg > 0)
                {
                    start = true;
                }
                else
                {
                    start = false;
                }
                condidx = msg - 1;
                // Any condidx out of condition design is treated as blank
                if (condidx >= condmanager.ncond)
                {
                    start = false;
                    go = false;
                    condidx = -1;
                }
            }
        }

        protected override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
        {
            // Manually sample and push condition index parsed from OI Message
            base.SamplePushCondition(manualcondidx: condidx);
        }

        protected override void Logic()
        {
            ParseOIMessage(ref start, ref go, ref condidx);
            switch (CondState)
            {
                case CONDSTATE.NONE:
                    if (start)
                    {
                        CondState = CONDSTATE.PREICI;
                        SetEnvActiveParam("Drifting", false);
                        SetEnvActiveParam("Visible", true);
                    }
                    break;
                case CONDSTATE.PREICI:
                    if (go)
                    {
                        CondState = CONDSTATE.COND;
                        SetEnvActiveParam("Drifting", true);
                        reversetime = CondOnTime;
                    }
                    break;
                case CONDSTATE.COND:
                    if (go)
                    {
                        var now = timer.ElapsedMillisecond;
                        if (now - reversetime >= ex.GetParam("ReverseDur").Convert<double>())
                        {
                            SetEnvActiveParam("ReverseTime", !GetEnvActiveParam("ReverseTime").Convert<bool>());
                            reversetime = now;
                        }
                    }
                    else
                    {
                        CondState = CONDSTATE.SUFICI;
                        SetEnvActiveParam("Visible", false);
                        SetEnvActiveParam("ReverseTime", false);
                    }
                    break;
                case CONDSTATE.SUFICI:
                    if (SufICIHold >= ex.SufICI)
                    {
                        CondState = CONDSTATE.NONE;
                    }
                    break;
            }
        }
    }
}