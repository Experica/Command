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
using Unity.Netcode;

/// <summary>
/// Condition Test Logic {PreICI - Cond - SufICI} ..., with ScaleGrid visual guide and EnvParam manipulation through User Input Action
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected Dictionary<string, InputAction> useractions = new();
    protected ScaleGrid scalegrid;

    protected override void Enable()
    {
        useractions = InputSystem.actions.FindActionMap("Logic").actions.ToDictionary(i => i.name, i => i);
        useractions["Move"].started += OnMoveAction;
        useractions["Scale"].started += OnScaleAction;
        useractions["Visible"].performed += OnVisibleAction;
        useractions["Ori"].performed += OnOriAction;
    }
    protected override void Disable()
    {
        useractions["Move"].started -= OnMoveAction;
        useractions["Scale"].started -= OnScaleAction;
        useractions["Visible"].performed -= OnVisibleAction;
        useractions["Ori"].performed -= OnOriAction;
    }

    protected virtual void OnMoveAction(InputAction.CallbackContext context)
    {
        var position = context.ReadValue<Vector2>();
        if (ex.Input && envmgr.MainCamera.Count > 0)
        {
            var po = envmgr.GetActiveParam("Position");
            if (po != null)
            {
                var so = envmgr.GetActiveParam("Size");
                var p = (Vector3)po;
                var s = so == null ? Vector3.zero : (Vector3)so;
                var hh = (envmgr.MainCamera.First().Height + s.y) / 2;
                var hw = (envmgr.MainCamera.First().Width + s.x) / 2;
                envmgr.SetActiveParam("Position", new Vector3(
                Mathf.Clamp(p.x + position.x * hw * Time.deltaTime, -hw, hw),
                Mathf.Clamp(p.y + position.y * hh * Time.deltaTime, -hh, hh),
                p.z));
            }
        }
    }
    protected virtual void OnScaleAction(InputAction.CallbackContext context)
    {
        var size = context.ReadValue<Vector2>();
        if (ex.Input)
        {
            var so = envmgr.GetActiveParam("Size");
            if (so != null)
            {
                var s = (Vector3)so;
                envmgr.SetActiveParam("Size", new Vector3(
                    Mathf.Max(0, s.x + size.x * s.x * Time.deltaTime),
                    Mathf.Max(0, s.y + size.y * s.y * Time.deltaTime),
                    s.z));
            }
        }
    }
    protected virtual void OnVisibleAction(InputAction.CallbackContext context)
    {
        if (ex.Input)
        {
            envmgr.SetActiveParam("Visible", context.ReadValue<float>() > 0);
        }
    }
    protected virtual void OnOriAction(InputAction.CallbackContext context)
    {
        var v = context.ReadValue<float>();
        if (ex.Input)
        {
            var oo = envmgr.GetActiveParam("Ori");
            if (oo != null)
            {
                var o = (float)oo;
                o = (o + v * 180 * Time.deltaTime) % 360f;
                envmgr.SetActiveParam("Ori", o < 0 ? 360f - o : o);
            }
        }
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


    public override void OnSceneReady(List<ulong> clientids)
    {
        if (clientids.Count == 0) { return; }
        for (var i = 0; i < clientids.Count; i++)
        {
            var cname = $"OrthoCamera{(i==0 ? "" : i)}";
            var oc = envmgr.SpawnMarkerOrthoCamera( cname);
            oc.OnReport = (string name, object value) => envmgr.SetParamByGameObject(name, cname, value);
            scalegrid = envmgr.SpawnScaleGrid(oc, spawn: true, parse: true);
            //appmgr.networkcontroller.NetworkShowOnlyTo(oc.NetworkObject, clientids[i]);
        }
    }

    public override bool Guide
    {
        get => scalegrid?.Visible.Value ?? false;
        set { if (scalegrid != null) { scalegrid.Visible.Value = value; } }
    }

    public override bool NetVisible
    {
        get { if (scalegrid != null) {return !IsNetworkHideFromAll(scalegrid); } else {return false; } }
        set { if (scalegrid != null) { NetworkShowHideAll(scalegrid, value); } }
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

    protected override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
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
                        SetEnvActiveParam("Visible", false);
                    }
                    SyncFrame?.Invoke();
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                    SyncFrame?.Invoke();
                }
                break;
        }
    }
}