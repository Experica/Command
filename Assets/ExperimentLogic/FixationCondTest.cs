﻿/*
FixationCondTest.cs is part of the Experica.
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
using UnityEngine.InputSystem;
using System;
using Experica;
using Experica.Command;
using System.Linq;
using Experica.NetEnv;

/// <summary>
/// Condition Test while eyes fixing on a target, with User Input Action mimicking eye movement, and helpful visual guides.
/// </summary>
public class FixationCondTest : Fixation
{
    protected override void TurnOnTarget()
    {
        if (ex.ID == "FixationRFMapping")
        {
            SetEnvActiveParamByGameObject("Visible", "FixDot/Plaid", true);
        }
        else
        {
            SetEnvActiveParam("Visible", true);
        }
        base.TurnOnTarget();
    }

    protected override void TurnOffTarget()
    {
        if (ex.ID == "FixationRFMapping")
        {
            SetEnvActiveParamByGameObject("Visible", "FixDot/Plaid", false);
        }
        else
        {
            SetEnvActiveParam("Visible", false);
        }
        base.TurnOffTarget();
    }

    protected override void PrepareCondition()
    {
        condmgr.PrepareCondition(ex.CondPath);
    }

    protected override void Logic()
    {
        switch (TrialState)
        {
            case TRIALSTATE.NONE:
                ex.PreITI = RandPreITIDur;
                EnterTrialState(TRIALSTATE.PREITI);
                break;
            case TRIALSTATE.PREITI:
                if (PreITIHold >= ex.PreITI)
                {
                    EnterTrialState(TRIALSTATE.TRIAL);
                    EnterTaskState(TASKSTATE.FIX_TARGET_ON);
                    EnterCondState(CONDSTATE.NONE);
                }
                break;
            case TRIALSTATE.TRIAL:
                switch (TaskState)
                {
                    case TASKSTATE.FIX_TARGET_ON:
                        if (FixOnTarget)
                        {
                            EnterTaskState(TASKSTATE.FIX_ACQUIRED);
                        }
                        else if (WaitForFix >= WaitForFixTimeOut)
                        {
                            // Failed to acquire fixation
                            OnTimeOut();
                            SetEnvActiveParam("FixDotVisible", false);
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.NONE);
                        }
                        break;
                    case TASKSTATE.FIX_ACQUIRED:
                        if (!FixOnTarget)
                        {
                            // Fixation breaks in required period
                            OnEarly();
                            SetEnvActiveParam("FixDotVisible", false);
                            if (IsTargetOn)
                            {
                                TurnOffTarget();
                                EnterCondState(CONDSTATE.NONE, true);
                            }
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI); // long SUFITI as punishment
                        }
                        else
                        {
                            switch (CondState)
                            {
                                case CONDSTATE.NONE:
                                    if (FixHold >= GetExParam<float>("FixPreDur"))
                                    {
                                        EnterCondState(CONDSTATE.PREICI);
                                    }
                                    break;
                                case CONDSTATE.PREICI:
                                    if (FixHold >= FixDur)
                                    {
                                        // Successfully hold fixation in required period
                                        OnHit();
                                        SetEnvActiveParam("FixDotVisible", false);
                                        EnterTaskState(TASKSTATE.NONE);
                                        EnterTrialState(TRIALSTATE.NONE);
                                    }
                                    else if (PreICIHold >= ex.PreICI)
                                    {
                                        TurnOnTarget();
                                        EnterCondState(CONDSTATE.COND, true);
                                    }
                                    break;
                                case CONDSTATE.COND:
                                    if (CondHold >= ex.CondDur)
                                    {
                                        TurnOffTarget();
                                        EnterCondState(CONDSTATE.SUFICI, true);
                                    }
                                    break;
                                case CONDSTATE.SUFICI:
                                    if (SufICIHold >= ex.SufICI)
                                    {
                                        EnterCondState(CONDSTATE.PREICI);
                                    }
                                    break;
                            }
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