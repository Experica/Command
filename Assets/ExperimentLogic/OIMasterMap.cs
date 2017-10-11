/*
OIMasterMap.cs is part of the VLAB project.
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

public class OIMasterMap : ExperimentLogic
{
    int condidx;
    bool start, go;
    double reversetime;
    bool reverse;

    public override void OnStart()
    {
        recordmanager = new RecordManager(RecordSystem.Ripple);
    }

    protected override void StartExperiment()
    {
        base.StartExperiment();
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("ReverseTime", false);
    }

    protected override void StopExperiment()
    {
        SetEnvActiveParam("Visible", false);
        SetEnvActiveParam("ReverseTime", false);
        base.StopExperiment();
    }

    /// <summary>
    /// Optical Imaging VDAQ output a byte, of which bit 7 is the GO bit,
    /// and bit 0-6 can represent StimulusID:0-127. In order to send StimulusID
    /// before GO bit, we use ID:0 as blank stimulus, and all real stimulus
    /// start from 1 and map to VLab condidx 0.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="condidx"></param>
    /// <param name="go"></param>
    void ParseOIMessage(ref bool start, ref int condidx, ref bool go)
    {
        List<double>[] dt; List<int>[] dv;
        var isdin = recordmanager.recorder.din(out dt, out dv);
        if (isdin && dt[0] != null)
        {
            int msg = dv[0].Last();
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
                condidx = -1;
            }
        }
    }

    public override void SamplePushCondition(bool istrysampleblock = true, int manualblockidx = 0, int manualcondidx = 0)
    {
        // Manually sample and push condition index received from OI message
        base.SamplePushCondition(manualcondidx: condidx);
    }

    public override void Logic()
    {
        ParseOIMessage(ref start, ref condidx, ref go);
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
                    reverse = GetEnvActiveParam("ReverseTime").Convert<bool>();
                }
                break;
            case CONDSTATE.COND:
                if (go)
                {
                    var now = timer.ElapsedMillisecond;
                    if (now - reversetime >= ex.GetParam("ReverseDur").Convert<double>())
                    {
                        reverse = !reverse;
                        SetEnvActiveParam("ReverseTime", reverse);
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
