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
using Unity.Netcode;

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

    public InputAction MoveAction;
    public Vector2 FixPosition;
    public float FixDotDiameter;
    NetworkVariable<Vector3> fixdotposition;
    ScaleGrid scalegrid;
    DotTrail fixtrail;
    Circle fixcircle;

    protected override void Enable()
    {
        MoveAction = InputSystem.actions.FindActionMap("Logic").FindAction("Move");
    }

    /// <summary>
    /// add helpful visual guides
    /// </summary>
    public override void OnSceneReady()
    {
        fixdotposition = envmgr.GetNetworkVariable<Vector3>("FixDotPosition");
        // hook function to update only x/y positions
        Action<NetEnvVisual, Vector3> upxy = (o, p) => o.Position.Value = new(p.x, p.y, o.Position.Value.z);

        var fixradius = (float)ex.ExtendParam["FixRadius"];
        fixcircle = envmgr.SpawnCircle(color: new(0.1f, 0.8f, 0.1f), size: new(2 * fixradius, 2 * fixradius, 1), parse: false);
        fixdotposition.OnValueChanged += (p, c) => upxy(fixcircle, c); // center fixcircle with fixdot
        upxy(fixcircle, fixdotposition.Value);
        // hook a ExtendParam to a NetworkVariable
        ex.extendproperties["FixRadius"].propertyChanged += (o, e) => fixcircle.Size.Value = new(2 * (float)ex.ExtendParam["FixRadius"], 2 * (float)ex.ExtendParam["FixRadius"], 1);

        scalegrid = envmgr.SpawnScaleGrid(envmgr.MainCamera.First(), parse: false);
        fixdotposition.OnValueChanged += (p, c) => upxy(scalegrid, c); // center scalegrid with fixdot
        upxy(scalegrid, fixdotposition.Value);

        fixtrail = envmgr.SpawnDotTrail(position: Vector3.back, size: new(0.25f, 0.25f, 1), color: new(1, 0.1f, 0.1f), parse: false);
    }

    public override bool Guide
    {
        get => (scalegrid?.Visible.Value ?? false) || (fixcircle?.Visible.Value ?? false) || (fixtrail?.Visible.Value ?? false);
        set
        {
            if (scalegrid != null) { scalegrid.Visible.Value = value; }
            if (fixcircle != null) { fixcircle.Visible.Value = value; }
            if (fixtrail != null) { fixtrail.Visible.Value = value; }
        }
    }

    protected override void OnUpdate()
    {
        if (ex.Input && MoveAction.WasPerformedThisFrame() && fixtrail != null && fixtrail.Visible.Value)
        {
            FixPosition += MoveAction.ReadValue<Vector2>();
            fixtrail.Position.Value = FixPosition;
        }
    }

    protected virtual bool FixOnTarget => Vector2.Distance(fixdotposition.Value, FixPosition) < (float)ex.ExtendParam["FixRadius"];

    public enum TASKSTATE
    {
        NONE = 401,
        FIX_TARGET_ON,
        FIX_ACQUIRED
    }
    public TASKSTATE TaskState { get; private set; }

    protected virtual EnterStateCode EnterTaskState(TASKSTATE value, bool sync = false)
    {
        if (value == TaskState) { return EnterStateCode.AlreadyIn; }
        switch (value)
        {
            case TASKSTATE.FIX_TARGET_ON:
                SetEnvActiveParam("FixDotVisible", true);
                WaitForFixTimeOut = GetExParam<double>("WaitForFixTimeOut");
                FixTargetOnTime = TimeMS;
                if (ex.HasCondTestState())
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), FixTargetOnTime);
                }
                break;
            case TASKSTATE.FIX_ACQUIRED:
                FixDur = RandFixDur;
                // scale FixDot when fix acquired
                FixDotDiameter = GetEnvActiveParam<float>("FixDotDiameter");
                SetEnvActiveParam("FixDotDiameter", FixDotDiameter * GetExParam<float>("DotScaleOnFix"));
                FixOnTime = TimeMS;
                if (ex.HasCondTestState())
                {
                    condtestmgr.AddInList(nameof(CONDTESTPARAM.Event), value.ToString(), FixOnTime);
                }
                break;
        }
        TaskState = value;
        if (sync) { SyncEvent(value.ToString()); }
        return EnterStateCode.Success;
    }

    protected virtual void OnTimeOut()
    {
        if (ex.HasCondTestState())
        {
            condtestmgr.Add(nameof(CONDTESTPARAM.TaskResult), nameof(TASKRESULT.TIMEOUT));
        }
        // condition not tested, we repeat current condition by ignore condition sampling once
        condmgr.NSampleSkip = 1;
        Debug.LogWarning("TimeOut");
    }

    protected virtual void OnEarly()
    {
        if (ex.HasCondTestState())
        {
            condtestmgr.Add(nameof(CONDTESTPARAM.TaskResult), nameof(TASKRESULT.EARLY));
            condtestmgr.Add("FixHold", FixHold);
        }
        // condition may not completely tested in EARLY trial, so we repeat current condition by ignore condition sampling once
        condmgr.NSampleSkip = 1;
        ex.SufITI = RandSufITIDur;
        Debug.LogError("Early");
    }

    protected virtual void OnHit()
    {
        if (ex.HasCondTestState())
        {
            condtestmgr.Add(nameof(CONDTESTPARAM.TaskResult), nameof(TASKRESULT.HIT));
            condtestmgr.Add("FixHold", FixHold);
        }
        Debug.Log("Hit");
    }

    protected override void OnStartExperiment()
    {
        base.OnStartExperiment();
        SetEnvActiveParam("FixDotVisible", false);
    }

    protected override void OnExperimentStopped()
    {
        base.OnExperimentStopped();
        SetEnvActiveParam("FixDotVisible", false);
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
                            SetEnvActiveParam("FixDotDiameter", FixDotDiameter);
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI); // ITI as punishment
                        }
                        else if (FixHold >= FixDur)
                        {
                            // Successfully hold fixation in required period
                            OnHit();
                            SetEnvActiveParam("FixDotVisible", false);
                            SetEnvActiveParam("FixDotDiameter", FixDotDiameter);
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