/*
OITTLCTLogic.cs is part of the VLAB project.
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

public class OIMasterMapOri8 : ExperimentLogic
{
    ParallelPort pport;
    double reverseontime;
    bool isreverse;

    public override void OnStart()
    {
        pport = new ParallelPort((int)config[VLCFG.ParallelPort1]);
    }

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE: 
               SetEnvActiveParam("Drifting", false);
                SetEnvActiveParam("Visible", true);
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    SetEnvActiveParam("Drifting", true);
                    CondState = CONDSTATE.COND;
                    reverseontime = CondOnTime;
                    isreverse = envmanager.GetActiveParam("ReverseTime").Convert<bool>();
                }
                break;
            case CONDSTATE.COND:
                if(CondHold>=ex.CondDur)
                {
                    SetEnvActiveParam("Visible", false);
                    SetEnvActiveParam("ReverseTime", false);
                    CondState = CONDSTATE.SUFICI;
                }
                if(timer.ElapsedMillisecond-reverseontime>=2000)
                {
                    isreverse = !isreverse;
                    SetEnvActiveParam("ReverseTime", isreverse);
                    reverseontime = timer.ElapsedMillisecond;
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    envmanager.SetActiveParam("Drifting", false);
                    envmanager.SetActiveParam("Visible", true);
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }
    }
}
