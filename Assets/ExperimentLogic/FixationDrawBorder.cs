/*
FixationDrawBorder.cs is part of the Experica.
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
using System.Collections.Generic;
using Experica;
using Experica.Command;
using System.Linq;
using Experica.NetEnv;

/// <summary>
/// Ask user to draw border of the object or phosephoen while eyes fixing on a target, with User Input Action mimicking eye movement, and helpful visual guides.
/// </summary>
public class FixationDrawBorder : Fixation
{
    public double WaitForDrawOnTime;
    public InputAction VisibleAction;
    DrawLine drawline;

    protected override void Enable()
    {
        base.Enable();
        VisibleAction = InputSystem.actions.FindActionMap("Logic").FindAction("Visible");
    }

    public override void OnPlayerReady()
    {
        base.OnPlayerReady();
        drawline = envmgr.SpawnDrawLine(envmgr.MainCamera.First(),netvis:NetVisibility.All);
        // show scalegrid to help user locate object
        foreach (var sg in scalegrid) { sg.NetworkObject.NetworkShowOnlyTo(sg.ClientID); }
    }

    protected override void PrepareCondition()
    {
        var pos = GetExParam<List<Vector3>>("ObjectPosition");
        if (pos == null || pos.Count == 0)
        {
            pos = new List<Vector3>() { Vector3.right * 10 };
        }
        var diam = GetExParam<List<float>>("ObjectDiameter");
        if (diam == null || diam.Count == 0)
        {
            diam = new List<float>() { 5 };
        }
        var col = GetExParam<List<Color>>("ObjectColor");
        if (col == null || col.Count == 0)
        {
            col = new List<Color>() { Color.blue };
        }

        var cond = new Dictionary<string, List<object>>
        {
            ["Diameter"] = diam.Cast<object>().ToList(),
            ["Position"] = pos.Cast<object>().ToList(),
            ["Color"] = col.Cast<object>().ToList(),
        };

        condmgr.PrepareCondition(cond.OrthoCombineFactor());
    }

    protected override void TurnOnTarget()
    {
        SetEnvActiveParam("Visible", true);
        base.TurnOnTarget();
    }

    protected override void TurnOffTarget()
    {
        SetEnvActiveParam("Visible", false);
        base.TurnOffTarget();
    }

    public new enum TASKSTATE
    {
        NONE = 401,
        FIX_TARGET_ON,
        FIX_ACQUIRED,
        WAIT_DRAW
    }

    public new TASKSTATE TaskState { get; private set; }

    public EnterStateCode EnterTaskState(TASKSTATE value, bool sync = false)
    {
        if (value == TaskState) { return EnterStateCode.AlreadyIn; }
        switch (value)
        {
            case TASKSTATE.NONE:
                recordgaze = false;
                break;
            case TASKSTATE.FIX_TARGET_ON:
                SetEnvActiveParam("FixDotVisible", true);
                WaitForFixTimeOut = GetExParam<double>("WaitForFixTimeOut");
                FixTargetOnTime = TimeMS;
                if (ex.HasCondTestState())
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), FixTargetOnTime);
                }
                recordgaze = true;
                break;
            case TASKSTATE.FIX_ACQUIRED:
                FixDur = RandFixDur;
                FixOnTime = TimeMS;
                if (ex.HasCondTestState())
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), FixOnTime);
                }
                break;
            case TASKSTATE.WAIT_DRAW:
                WaitForDrawOnTime = TimeMS;
                if (ex.HasCondTestState())
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), WaitForDrawOnTime);
                }
                drawline.EnableDraw.Value = true;
                break;
        }
        TaskState = value;
        if (sync) { SyncEvent(value.ToString()); }
        return EnterStateCode.Success;
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
                                        SetEnvActiveParam("FixDotVisible", false);
                                        EnterTaskState(TASKSTATE.WAIT_DRAW);
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
                    case TASKSTATE.WAIT_DRAW:
                        if (VisibleAction.WasPerformedThisFrame())
                        {
                            if (ex.HasCondTestState())
                            {
                                condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), "Confirm", TimeMS);
                            }
                            var isdraw = drawline.TryGetLine(out Vector3[] line);
                            if (ex.HasCondTestState() && isdraw)
                            {
                                condtestmgr.AddInList("DrawLine", line);
                            }
                            drawline.ClearRpc();
                            drawline.EnableDraw.Value = false;
                            if (isdraw) { OnHit(); } else { OnMiss(); }
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.NONE);
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