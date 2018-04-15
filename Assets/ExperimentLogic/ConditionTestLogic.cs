/*
ConditionTestLogic.cs is part of the VLAB project.
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
using VLab;

public class ConditionTestLogic : ExperimentLogic
{
    protected override void StopExperiment()
    {
        SetEnvActiveParam("Visible", true);
        SetEnvActiveParam("Mark", OnOff.Off);
        base.StopExperiment();
    }

    public override void Logic()
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
                switch (TrialState)
                {
                    case TRIALSTATE.NONE:
                        TrialState = TRIALSTATE.PREITI;
                        break;
                    case TRIALSTATE.PREITI:
                        if (PreITIHold >= ex.PreITI)
                        {
                            TrialState = TRIALSTATE.TRIAL;
                        }
                        break;
                    case TRIALSTATE.TRIAL:
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
                                    if (ex.PreICI == 0 && ex.SufICI == 0) // None ICI Mode
                                    {
                                        // The marker pulse width should be > 2 frames(60Hz==16.7ms) to make sure marker on_off will take effect on screen.
                                        SetEnvActiveParamTwice("Mark", OnOff.On, config.MarkPulseWidth, OnOff.Off);
                                    }
                                    else // ICI Mode
                                    {
                                        SetEnvActiveParam("Mark", OnOff.On);
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
                                        SetEnvActiveParam("Mark", OnOff.Off);
                                    }
                                }
                                break;
                            case CONDSTATE.SUFICI:
                                if (SufICIHold >= ex.SufICI)
                                {
                                    if (TrialHold >= ex.TrialDur)
                                    {
                                        CondState = CONDSTATE.NONE;
                                        TrialState = TRIALSTATE.SUFITI;
                                    }
                                    else
                                    {
                                        CondState = CONDSTATE.PREICI;
                                    }
                                }
                                break;
                        }
                        break;
                    case TRIALSTATE.SUFITI:
                        if (SufITIHold >= ex.SufITI)
                        {
                            if (BlockHold >= ex.BlockDur)
                            {
                                TrialState = TRIALSTATE.NONE;
                                BlockState = BLOCKSTATE.SUFIBI;
                            }
                            else
                            {
                                TrialState = TRIALSTATE.PREITI;
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