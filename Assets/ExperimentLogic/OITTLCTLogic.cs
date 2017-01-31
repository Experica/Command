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

public class OITTLCTLogic : ExperimentLogic
{
    ParallelPort pport = new ParallelPort(0xC010);
    bool isreverse = false;

    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                envmanager.SetActiveParam("visible", false);
                CondState = CONDSTATE.PREICI;
                envmanager.SetActiveParam("isdrifting", false);
                envmanager.SetActiveParam("visible", true);
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    envmanager.SetActiveParam("isdrifting", true);
                    CondState = CONDSTATE.COND;
                    pport.SetBit(bit: 0, value: true);
                }
                break;
            case CONDSTATE.COND:
                if (!isreverse && CondHold >= ex.CondDur)
                {
                    envmanager.SetActiveParam("isreversetime", true);
                    //var curori = (float)envmanager.GetParam("ori");
                    //envmanager.ActiveSyncSetParam("ori", curori + 180);
                    isreverse = true;
                }
                if (isreverse && CondHold >= 2 * ex.CondDur)
                {
                    envmanager.SetActiveParam("visible", false);
                    isreverse = false;
                    envmanager.SetActiveParam("isreversetime", false);
                    CondState = CONDSTATE.SUFICI;
                    pport.SetBit(bit: 0, value: false);
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    CondState = CONDSTATE.PREICI;
                    envmanager.SetActiveParam("isdrifting", false);
                    envmanager.SetActiveParam("visible", true);
                }
                break;
        }
    }
}
