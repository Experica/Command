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

/// <summary>
/// Condition Test Logic {PreICI - Cond - SufICI} ..., and EnvParam manipulation with User Input Action
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected List<string> actionnames = new() { "Move","Scale","Visible","Ori" };
    Dictionary<string, InputAction> useractions=new();

    protected override void Enable()
    {
        useractions = InputSystem.actions.FindActionMap("Logic").actions.Where(i=>actionnames.Contains(i.name)).ToDictionary(i => i.name, i => i);
        useractions["Move"].performed += OnMoveAction;
        useractions["Scale"].performed += OnScaleAction;
        useractions["Visible"].performed += OnVisibleAction;
        useractions["Ori"].performed += OnOriAction;
    }
    protected override void Disable()
    {
        if (useractions.Count == 0) { return; }
        useractions["Move"].performed -= OnMoveAction;
        useractions["Scale"].performed -= OnScaleAction;
        useractions["Visible"].performed -= OnVisibleAction;
        useractions["Ori"].performed -= OnOriAction;
    }

    protected virtual void OnMoveAction(InputAction.CallbackContext context)
    {
        var position = context.ReadValue<Vector2>();
        if (ex.Input && envmanager.MainCamera.Count > 0)
        {
            var po = envmanager.GetActiveParam("Position");
            if (po != null)
            {
                var so = envmanager.GetActiveParam("Size");
                var p = (Vector3)po;
                var s = so == null ? Vector3.zero : (Vector3)so;
                var hh = (envmanager.MainCamera.First().Height + s.y) / 2;
                var hw = (envmanager.MainCamera.First().Width + s.x) / 2;
                envmanager.SetActiveParam("Position", new Vector3(
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
            var so = envmanager.GetActiveParam("Size");
            if (so != null)
            {
                var s = (Vector3)so;
                envmanager.SetActiveParam("Size", new Vector3(
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
            envmanager.SetActiveParam("Visible", context.ReadValue<float>() > 0);
        }
    }
    protected virtual void OnOriAction(InputAction.CallbackContext context)
    {
        var v = context.ReadValue<float>();
        if (ex.Input)
        {
            var oo = envmanager.GetActiveParam("Ori");
            if (oo != null)
            {
                var o = (float)oo;
                o = (o + v * 180 * Time.deltaTime) % 360f;
                envmanager.SetActiveParam("Ori", o < 0 ? 360f - o : o);
            }
        }
    }
    protected virtual void OnDiameterAction(float diameter)
    {
        if (ex.Input)
        {
            var dio = envmanager.GetActiveParam("Diameter");
            if (dio != null)
            {
                var d = (float)dio;
                envmanager.SetActiveParam("Diameter", Mathf.Max(0, d + Mathf.Pow(diameter * d * Time.deltaTime, 1)));
            }
        }
    }
    protected virtual void OnSpatialFreqAction(float sf)
    {
        if (ex.Input)
        {
            var sfo = envmanager.GetActiveParam("SpatialFreq");
            if (sfo != null)
            {
                var s = (float)sfo;
                envmanager.SetActiveParam("SpatialFreq", Mathf.Clamp(s + sf * s * Time.deltaTime, 0, 20f));
            }
        }
    }
    protected virtual void OnTemporalFreqAction(float tf)
    {
        if (ex.Input)
        {
            var tfo = envmanager.GetActiveParam("TemporalFreq");
            if (tfo != null)
            {
                var t = (float)tfo;
                envmanager.SetActiveParam("TemporalFreq", Mathf.Clamp(t + tf * t * Time.deltaTime, 0, 20f));
            }
        }
    }


    public override void OnReady()
    {
        foreach(var c in envmanager.MainCamera)
        {
           envmanager.SpawnScaleGrid(c);
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