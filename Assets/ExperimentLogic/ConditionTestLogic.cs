/*
ConditionTestLogic.cs is part of the Experica.
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
using System;
using Experica;
using Experica.Command;
using System.Linq;

/// <summary>
/// Condition Test Logic {PreICI - Cond - SufICI} ...
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected override void OnStartExperiment()
    {
        SetEnvActiveParam("Visible", false);
    }

    protected override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.NoNeed) { return; }
                SyncFrame?.Invoke();
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    EnterCondState(CONDSTATE.COND, true);
                    SetEnvActiveParam("Visible", true);
                    SyncFrame?.Invoke();
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    /*
                    for successive conditions without rest, 
                    make sure no extra logic updates(frames) are inserted.
                    */
                    if (ex.PreICI <= 0 && ex.SufICI <= 0)
                    {
                        // new condtest starts at PreICI
                        if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.NoNeed) { return; }
                        EnterCondState(CONDSTATE.COND, true);
                    }
                    else
                    {
                        EnterCondState(CONDSTATE.SUFICI, true);
                        SetEnvActiveParam("Visible", false);
                    }
                    SyncFrame?.Invoke();
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.NoNeed) { return; }
                    SyncFrame?.Invoke();
                }
                break;
        }
    }
}