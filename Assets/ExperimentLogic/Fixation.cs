/*
Fixation.cs is part of the Experica.
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
/// Eye Fixation Task, with User Input Action mimicking eye movement, and helpful visual guides.
/// </summary>
public class Fixation : ExperimentLogic
{
    public double FixOnTime, FixTargetOnTime, FixDur, WaitForFixTimeOut;
    public double FixHold => TimeMS - FixOnTime;
    public double WaitForFix => TimeMS - FixTargetOnTime;
    public double RandPreITIDur => RNG.Next(GetExParam<int>("MinPreITIDur"), GetExParam<int>("MaxPreITIDur"));
    public double RandSufITIDur => RNG.Next(GetExParam<int>("MinSufITIDur"), GetExParam<int>("MaxSufITIDur"));
    public double RandFixDur => RNG.Next(GetExParam<int>("MinFixDur"), GetExParam<int>("MaxFixDur"));

    public InputAction EyeMoveAction;
    public Vector2 FixPosition;
    public float FixDotDiameter;
    DotTrail fixpoint;

    public enum TASKSTATE
    {
        NONE = 301,
        FIX_TARGET_ON,
        FIX_ACQUIRED
    }
    public TASKSTATE TaskState;

    protected virtual EnterStateCode EnterTaskState(TASKSTATE value)
    {
        if (value == TaskState) { return EnterStateCode.AlreadyIn; }
        switch (value)
        {
            case TASKSTATE.FIX_TARGET_ON:
                SetEnvActiveParam("FixDotVisible", true);
                WaitForFixTimeOut = GetExParam<double>("WaitForFixTimeOut");
                FixTargetOnTime = TimeMS;
                break;
            case TASKSTATE.FIX_ACQUIRED:
                FixDur = RandFixDur;
                FixOnTime = TimeMS;
                // scale FixDot when fix acquired
                SetEnvActiveParam("FixDotDiameter", FixDotDiameter * GetExParam<float>("DotScaleOnFix"));
                break;
        }
        TaskState = value;
        return EnterStateCode.Success;
    }

    protected virtual bool FixOnTarget
    {
        get
        {
            var fixradius = GetExParam<float>("FixRadius");
            var targetposition = GetEnvActiveParam<Vector3>("FixDotPosition");
            if (Vector2.Distance(targetposition, FixPosition) < fixradius) { return true; }
            else { return false; }
        }
    }

    protected virtual void OnEarly()
    {
        ex.SufITI = RandSufITIDur;
        Debug.LogError("Early");
    }

    protected virtual void OnTimeOut()
    {
        ex.PreITI = RandPreITIDur;
        Debug.LogWarning("TimeOut");
    }

    protected virtual void OnHit()
    {
        Debug.Log("Hit");
    }

    /// <summary>
    /// add visual guides when sence loaded
    /// </summary>
    public override void OnReady()
    {
        var fixdotposition = envmanager.GetNetworkVariable<Vector3>("FixDotPosition");
        Action<NetEnvVisual, Vector3> upxy = (o, p) => o.Position.Value = new(p.x, p.y, o.Position.Value.z); // hook function to update only x/y positions

        var fixradius = GetExParam<float>("FixRadius");
        var fixcircle = envmanager.SpawnCircle(color: new(0.1f, 0.8f, 0.1f), size: new(2 * fixradius, 2 * fixradius, 1));
        fixdotposition.OnValueChanged += (p, c) => upxy(fixcircle, c);
        upxy(fixcircle, fixdotposition.Value);

        foreach (var c in envmanager.MainCamera)
        {
            var sg = envmanager.SpawnScaleGrid(c);
            fixdotposition.OnValueChanged += (p, c) => upxy(sg, c);
            upxy(sg, fixdotposition.Value);
        }

        fixpoint = envmanager.SpawnDotTrail(position: Vector3.back, size: new(0.25f, 0.25f, 1), color: new(1, 0.1f, 0.1f));
    }

    protected override void Enable()
    {
        EyeMoveAction = InputSystem.actions.FindActionMap("Logic").FindAction("Move");
    }

    protected override void OnUpdate()
    {
        if (EyeMoveAction.WasPerformedThisFrame())
        {
            FixPosition += EyeMoveAction.ReadValue<Vector2>();
            fixpoint.Position.Value = FixPosition;
        }
    }

    protected override void OnStartExperiment()
    {
        base.OnStartExperiment();
        SetEnvActiveParam("FixDotVisible", false);
        FixDotDiameter = GetEnvActiveParam<float>("FixDotDiameter");
        ex.PreITI = RandPreITIDur;
    }

    protected override void OnExperimentStopped()
    {
        base.OnExperimentStopped();
        SetEnvActiveParam("FixDotVisible", false);
        SetEnvActiveParam("FixDotDiameter", FixDotDiameter);
    }

    protected override void Logic()
    {
        switch (TrialState)
        {
            case TRIALSTATE.NONE:
                EnterTrialState(TRIALSTATE.PREITI);
                break;
            case TRIALSTATE.PREITI:
                if (PreITIHold >= ex.PreITI)
                {
                    EnterTrialState(TRIALSTATE.TRIAL);
                    EnterTaskState(TASKSTATE.FIX_TARGET_ON);
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
                            SetEnvActiveParam("FixDotVisible", false);
                            OnTimeOut();
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.PREITI);
                        }
                        break;
                    case TASKSTATE.FIX_ACQUIRED:
                        if (!FixOnTarget)
                        {
                            // Fixation breaks in required period
                            SetEnvActiveParam("FixDotVisible", false);
                            SetEnvActiveParam("FixDotDiameter", FixDotDiameter);
                            OnEarly();
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI);
                        }
                        else if (FixHold >= FixDur)
                        {
                            // Successfully hold fixation in required period
                            SetEnvActiveParam("FixDotVisible", false);
                            SetEnvActiveParam("FixDotDiameter", FixDotDiameter);
                            OnHit();
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.PREITI);
                        }
                        break;
                }
                break;
            case TRIALSTATE.SUFITI:
                if (SufITIHold >= ex.SufITI)
                {
                    EnterTrialState(TRIALSTATE.PREITI);
                }
                break;
        }

    }
}