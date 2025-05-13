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
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using Experica;
using Experica.Command;
using System.Linq;
using Experica.NetEnv;

/// <summary>
/// Condition Test Logic {PreICI - Cond - SufICI} ..., with ScaleGrid visual guide and EnvParam manipulation through User Input Action
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected InputAction move_ia, scale_ia, ori_ia, visible_ia;
    protected List<ScaleGrid> scalegrid = new();

    protected override void Enable()
    {
        var actionmap = InputSystem.actions.FindActionMap("Logic");
        move_ia = actionmap.FindAction("Move");
        scale_ia = actionmap.FindAction("Scale");
        ori_ia = actionmap.FindAction("Ori");

        visible_ia = actionmap.FindAction("Visible");
        visible_ia.performed += OnVisibleAction;
    }

    protected override void Disable()
    {
        visible_ia.performed -= OnVisibleAction;
    }

    protected virtual void MoveAction()
    {
        if (move_ia.phase != InputActionPhase.Started) { return; }
        var po = envmgr.GetActiveParam("Position");
        if (po == null) { return; }
        var so = envmgr.GetActiveParam("Size");
        var s = so == null ? Vector3.zero : (Vector3)so;
        var p = (Vector3)po;
        var v = move_ia.ReadValue<Vector2>();

        var hh = (envmgr.MainCamera.First().Height + s.y) / 2;
        var hw = (envmgr.MainCamera.First().Width + s.x) / 2;
        envmgr.SetActiveParam("Position", new Vector3(
        Mathf.Clamp(p.x + v.x * hw * Time.deltaTime, -hw, hw),
        Mathf.Clamp(p.y + v.y * hh * Time.deltaTime, -hh, hh),
        p.z));
    }
    protected virtual void ScaleAction()
    {
        if (scale_ia.phase != InputActionPhase.Started) { return; }
        var so = envmgr.GetActiveParam("Size");
        if (so == null) { return; }
        var s = (Vector3)so;
        var v = scale_ia.ReadValue<Vector2>();

        var diag = Mathf.Sqrt(Mathf.Pow(envmgr.MainCamera.First().Height, 2) + Mathf.Pow(envmgr.MainCamera.First().Width, 2));
        envmgr.SetActiveParam("Size", new Vector3(
        Mathf.Clamp(s.x + v.x * s.x * Time.deltaTime, 0, diag),
        Mathf.Clamp(s.y + v.y * s.y * Time.deltaTime, 0, diag),
        s.z));
    }
    protected virtual void OriAction()
    {
        if (ori_ia.phase != InputActionPhase.Started) { return; }
        var oo = envmgr.GetActiveParam("Ori");
        if (oo == null) { return; }
        var o = (float)oo;
        var v = ori_ia.ReadValue<float>();

        o = (o + v * 180 * Time.deltaTime) % 360f;
        envmgr.SetActiveParam("Ori", o < 0 ? 360f - o : o);
    }

    protected virtual void OnVisibleAction(InputAction.CallbackContext context)
    {
        if (!ex.Input || envmgr.MainCamera.Count == 0) { return; }
        var vo = envmgr.GetActiveParam("Visible");
        if (vo == null) { return; }
        var v = (bool)vo;

        envmgr.SetActiveParam("Visible", !v);
    }
    protected virtual void OnDiameterAction(float diameter)
    {
        if (ex.Input)
        {
            var dio = envmgr.GetActiveParam("Diameter");
            if (dio != null)
            {
                var d = (float)dio;
                envmgr.SetActiveParam("Diameter", Mathf.Max(0, d + Mathf.Pow(diameter * d * Time.deltaTime, 1)));
            }
        }
    }
    protected virtual void OnSpatialFreqAction(float sf)
    {
        if (ex.Input)
        {
            var sfo = envmgr.GetActiveParam("SpatialFreq");
            if (sfo != null)
            {
                var s = (float)sfo;
                envmgr.SetActiveParam("SpatialFreq", Mathf.Clamp(s + sf * s * Time.deltaTime, 0, 20f));
            }
        }
    }
    protected virtual void OnTemporalFreqAction(float tf)
    {
        if (ex.Input)
        {
            var tfo = envmgr.GetActiveParam("TemporalFreq");
            if (tfo != null)
            {
                var t = (float)tfo;
                envmgr.SetActiveParam("TemporalFreq", Mathf.Clamp(t + tf * t * Time.deltaTime, 0, 20f));
            }
        }
    }

    /// <summary>
    /// Polling and Processing Value Type Input
    /// </summary>
    protected override void OnUpdate()
    {
        if (!ex.Input || envmgr.MainCamera.Count == 0) { return; }
        MoveAction();
        ScaleAction();
        OriAction();
    }

    public override void OnSceneReady(List<ulong> clientids)
    {
        scalegrid.Clear();
        if (clientids.Count == 0) { return; }
        for (var i = 0; i < clientids.Count; i++)
        {
            var cname = $"OrthoCamera{(i == 0 ? "" : i)}";
            var oc = envmgr.SpawnMarkerOrthoCamera(cname, clientid: clientids[i]);
            oc.OnCameraChange += _ => appmgr.ui.UpdateView();
            var sg = envmgr.SpawnScaleGrid(oc, clientid: clientids[i], parse: false);
            scalegrid.Add(sg);
        }
    }

    public override bool Guide
    {
        get
        {
            if (scalegrid.Count == 0) { return false; }
            return scalegrid.First().Visible.Value;
        }
        set { foreach (var sg in scalegrid) { sg.Visible.Value = value; } }
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
        }
    }

    protected override void OnStartExperiment()
    {
        base.OnStartExperiment();
        SetEnvActiveParam("Visible", false);
    }
    protected override void OnExperimentStopped()
    {
        base.OnExperimentStopped();
        SetEnvActiveParam("Visible", false);
    }

    protected virtual void OnCONDEntered()
    {
        SetEnvActiveParam("Visible", true);
    }

    protected virtual void OnSUFICIEntered()
    {
        SetEnvActiveParam("Visible", false);
    }

    protected override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    EnterCondState(CONDSTATE.COND, true);
                    OnCONDEntered();
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    if (ex.PreICI <= 0 && ex.SufICI <= 0)
                    {
                        // for successive conditions without rest, make sure no extra logic updates(frames) are inserted.
                        // So first enter PREICI(new condtest starts at PREICI), then immediately enter COND.
                        if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                        EnterCondState(CONDSTATE.COND, true);
                    }
                    else
                    {
                        EnterCondState(CONDSTATE.SUFICI, true);
                        OnSUFICIEntered();
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                }
                break;
        }
    }
}