/*
ImagerEpochNoBlank.cs is part of the Experica.
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
using Experica;
using System.IO;
using ColorSpace = Experica.ColorSpace;

/// <summary>
/// Episodic Condition Test(PreITI-{PreICI-Cond-SufICI}-SufITI) with Imager Data Acquisition System, and Predefined Colors.
/// Here no black is inserted between Cond, and only `TRIAL` and `COND` events are synced.
/// </summary>
public class ImagerEpochNoBlank : ImagerEpoch
{
    protected override void Logic()
    {
        switch (TrialState)
        {
            case TRIALSTATE.NONE:
                if (EnterTrialState(TRIALSTATE.PREITI) == EnterCode.NoNeed) { return; }
                SyncFrame();
                break;
            case TRIALSTATE.PREITI:
                if (PreITIHold >= ex.PreITI)
                {
                    EnterTrialState(TRIALSTATE.TRIAL, true);
                    SyncFrame();
                }
                break;
            case TRIALSTATE.TRIAL:
                switch (CondState)
                {
                    case CONDSTATE.NONE:
                        StartEpochRecord(condtestmanager.CondTestIndex);
                        EnterCondState(CONDSTATE.PREICI);
                        SyncFrame();
                        break;
                    case CONDSTATE.PREICI:
                        if (PreICIHold >= ex.PreICI)
                        {
                            EnterCondState(CONDSTATE.COND, true);
                            SetEnvActiveParam("Visible", true);
                            SyncFrame();
                        }
                        break;
                    case CONDSTATE.COND:
                        if (CondHold >= ex.CondDur)
                        {
                            EnterCondState(CONDSTATE.SUFICI);
                            SyncFrame();
                        }
                        break;
                    case CONDSTATE.SUFICI:
                        if (SufICIHold >= ex.SufICI)
                        {
                            StopEpochRecord();
                            EnterCondState(CONDSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI);
                            SyncFrame();
                        }
                        break;
                }
                break;
            case TRIALSTATE.SUFITI:
                if (SufITIHold >= ex.SufITI)
                {
                    EnterTrialState(TRIALSTATE.NONE);
                }
                break;
        }
    }
}
