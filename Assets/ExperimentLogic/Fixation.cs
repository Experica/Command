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
using System.Collections.Generic;

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
    public IEyeTracker EyeTracker;
    public Vector2 FixPosition;
    protected NetworkVariable<Vector3> fixdotposition;
    protected List<ScaleGrid> scalegrid = new();
    protected bool updatefixtrail, recordgaze;
    Tag tag;
    DotTrail fixtrail;
    Circle fixcircle;


    protected override void Enable()
    {
        MoveAction = InputSystem.actions.FindActionMap("Logic").FindAction("Move");

        EyeTracker = PupilLabsCore.TryGetPupilLabsCore();
    }

    protected override void Disable()
    {
        if (EyeTracker != null)
        {
            EyeTracker.Dispose();
            EyeTracker = null;
        }
    }

    public override void OnSceneReady(List<ulong> clientids)
    {
        fixdotposition = envmgr.GetNetworkVariable<Vector3>("FixDotPosition");
        scalegrid.Clear();
        if (clientids.Count == 0) { return; }
        for (var i = 0; i < clientids.Count; i++)
        {
            var cname = $"OrthoCamera{(i == 0 ? "" : i)}";
            var oc = envmgr.SpawnTagMarkerOrthoCamera(cname, clientid: clientids[i]);
            oc.OnCameraChange += _ => appmgr.ui.UpdateView();
            // we want scalegrid to center on FixDot, so here spawn as a child of FixDot
            var sg = envmgr.SpawnScaleGrid(oc, clientid: clientids[i], parse: false, parent: fixdotposition.GetBehaviour().transform);
            scalegrid.Add(sg);
        }
        tag = envmgr.GetNetworkVariableByGameObject<float>("TagMargin", "OrthoCamera/Tag0").GetBehaviour() as Tag;
    }

    /// <summary>
    /// add helpful visual guides
    /// </summary>
    public override void OnPlayerReady()
    {
        //Action<NetEnvVisual, Vector3> upxy = (o, p) => o.Position.Value = new(p.x, p.y, o.Position.Value.z);

        var fixradius = (float)ex.ExtendParam["FixRadius"];
        // here also spawn as a child of FixDot, so the circle would center FixDot
        fixcircle = envmgr.SpawnCircle(color: new(0.1f, 0.8f, 0.1f), size: new(2 * fixradius, 2 * fixradius, 1), parse: false, parent: fixdotposition.GetBehaviour().transform);
        // hook a ExtendParam to a NetworkVariable
        ex.extendproperties["FixRadius"].propertyChanged += (o, e) => fixcircle.Size.Value = new(2 * (float)ex.ExtendParam["FixRadius"], 2 * (float)ex.ExtendParam["FixRadius"], 1);
        // tracing fixation position
        fixtrail = envmgr.SpawnDotTrail(position: Vector3.back, size: new(0.25f, 0.25f, 1), color: new(1, 0.1f, 0.1f), parse: false);
    }

    protected override void PrepareCondition()
    {
        var pos = GetExParam<List<Vector3>>("FixDotPosition");
        if (pos == null || pos.Count == 0)
        {
            pos = new List<Vector3>() { Vector3.zero };
        }
        var cond = new Dictionary<string, List<object>>()
        {
            ["FixDotPosition"] = pos.Cast<object>().ToList(),
        };
        condmgr.PrepareCondition(cond);
    }

    public override bool Guide
    {
        get
        {
            if (scalegrid.Count == 0) { return false; }
            return scalegrid.First().Visible.Value;
        }
        set
        {
            foreach (var sg in scalegrid) { sg.Visible.Value = value; }
            if (fixcircle != null) { fixcircle.Visible.Value = value; }
            if (fixtrail != null) { fixtrail.Visible.Value = value; }
        }
    }

    public override bool NetVisible
    {
        get
        {
            if (scalegrid.Count == 0) { return false; }
            var sg = scalegrid.First();
            return !sg.NetworkObject.IsNetworkHideFromAll();
        }
        set
        {
            foreach (var sg in scalegrid)
            {
                if (value) { sg.NetworkObject.NetworkShowOnlyTo(sg.ClientID); }
                else { sg.NetworkObject.NetworkHideFromAll(); }
            }
            if (fixcircle != null) { fixcircle.NetworkObject.NetworkShowHideAll(value); }
            if (fixtrail != null) { fixtrail.NetworkObject.NetworkShowHideAll(value); }
        }
    }

    protected override void OnUpdate()
    {
        if (ex.Input && envmgr.MainCamera.Count > 0 && MoveAction.phase == InputActionPhase.Started)
        {
            FixPosition += MoveAction.ReadValue<Vector2>();
            clampMove(ref FixPosition);
            updatefixtrail = true;
        }

        if (EyeTracker != null && envmgr.MainCamera.Count > 0)
        {
            FixPosition = surfacegaze2cameragaze(EyeTracker.Gaze2D, envmgr.MainCamera.First(), tag.TagSurfaceMargin);
            if (recordgaze && ex.HasCondTestState())
            {
                condtestmgr.AddInList(nameof(CONDTESTPARAM.Gaze), TimeMS, FixPosition);
            }
            updatefixtrail = true;
        }

        if (updatefixtrail && fixtrail != null && fixtrail.Visible.Value)
        {
            fixtrail.Position.Value = FixPosition;
            updatefixtrail = false;
        }
    }

    Vector2 surfacegaze2cameragaze(Vector2 sg, INetEnvCamera camera, float surfacemargin)
    {
        sg.x = sg.x - 0.5f;
        sg.y = sg.y - 0.5f;
        return new Vector2(sg.x * (camera.Width - 2 * surfacemargin), sg.y * (camera.Height - 2 * surfacemargin));
    }

    void clampMove(ref Vector2 pos)
    {
        var r = GetExParam<float>("MoveRadius");
        if (r == 0) { r = 50; }
        pos.x = Mathf.Clamp(pos.x, -r, r);
        pos.y = Mathf.Clamp(pos.y, -r, r);
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
        }
        TaskState = value;
        if (sync) { SyncEvent(value.ToString()); }
        return EnterStateCode.Success;
    }

    protected bool IsTargetOn { get; set; }

    protected virtual void TurnOnTarget()
    {
        IsTargetOn = true;
    }

    protected virtual void TurnOffTarget()
    {
        IsTargetOn = false;
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

    protected virtual void OnMiss()
    {
        if (ex.HasCondTestState())
        {
            condtestmgr.Add(nameof(CONDTESTPARAM.TaskResult), nameof(TASKRESULT.MISS));
            condtestmgr.Add("FixHold", FixHold);
        }
        ex.SufITI = RandSufITIDur;
        Debug.LogError("Miss");
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
                            EnterTaskState(TASKSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI); // long SUFITI as punishment
                        }
                        else if (FixHold >= FixDur)
                        {
                            // Successfully hold fixation in required period
                            OnHit();
                            SetEnvActiveParam("FixDotVisible", false);
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