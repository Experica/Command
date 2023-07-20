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
using System;
using Experica;

/// <summary>
/// Simple Condition Test Logic with Event Sync through GPIO and Display Marker
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected IGPIO gpio;
    protected bool syncvalue;

    /// <summary>
    /// init gpio and sync states
    /// </summary>
    protected override void OnStartExperiment()
    {
        if (ex.EventSyncProtocol.SyncMethods.Contains(SyncMethod.GPIO))
        {
            gpio = new ParallelPort(dataaddress: Config.ParallelPort0);
            if (!gpio.Found)
            {
                gpio = new FTDIGPIO();
            }
            if (!gpio.Found)
            {
                // gpio = new MCCDevice(config.MCCDevice, config.MCCDPort);
            }
            if (!gpio.Found)
            {
                Debug.LogWarning("No GPIO Sync Channel.");
            }
        }
        SetEnvActiveParam("Visible", false);
        SyncEvent();
    }

    /// <summary>
    /// release gpio and sync states
    /// </summary>
    protected override void OnExperimentStopped()
    {
        SetEnvActiveParam("Visible", false);
        SyncEvent();
        gpio?.Dispose();
    }

    protected EnterCode EnterCondState(CONDSTATE value, bool syncenter = false)
    {
        var c = base.EnterCondState(value);
        if (syncenter && c == EnterCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
    }

    protected EnterCode EnterTrialState(TRIALSTATE value, bool syncenter = false)
    {
        var c = base.EnterTrialState(value);
        if (syncenter && c == EnterCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
    }

    protected EnterCode EnterBlockState(BLOCKSTATE value, bool syncenter = false)
    {
        var c = base.EnterBlockState(value);
        if (syncenter && c == EnterCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
    }

    /// <summary>
    /// Sync and Register Event Name/Time/Value with External Device according to EventSyncProtocol
    /// </summary>
    /// <param name="e">Event Name, NullorEmpty will Reset Sync Channel to inactive state without event register</param>
    /// <param name="et">Event Time, Non-NaN value will register in `Event` as well as `SyncEvent`</param>
    /// <param name="ev">Event Value, Non-Null value will register in new `CONDTESTPARAM` if event is a valid `CONDTESTPARAM`</param>
    protected virtual void SyncEvent(string e = null, double et = double.NaN, object ev = null)
    {
        var esp = ex.EventSyncProtocol;
        if (esp.SyncMethods == null || esp.SyncMethods.Count == 0)
        {
            Debug.LogWarning("No SyncMethod in EventSyncProtocol, Skip SyncEvent ...");
            return;
        }
        bool addtosynclist = false;
        bool syncreset = string.IsNullOrEmpty(e);

        if (esp.nSyncChannel == 1 && esp.nSyncpEvent == 1)
        {
            syncvalue = !syncreset && !syncvalue;
            addtosynclist = !syncreset;
            for (var i = 0; i < esp.SyncMethods.Count; i++)
            {
                switch (esp.SyncMethods[i])
                {
                    case SyncMethod.Display:
                        SetEnvActiveParam("Mark", syncvalue);
                        break;
                    case SyncMethod.GPIO:
                        gpio?.BitOut(bit: Config.EventSyncCh, value: syncvalue);
                        break;
                }
            }
        }
        if (addtosynclist && ex.CondTestAtState != CONDTESTATSTATE.NONE)
        {
            if (!double.IsNaN(et))
            {
                condtestmanager.AddInList(CONDTESTPARAM.Event, e, et);
            }
            condtestmanager.AddInList(CONDTESTPARAM.SyncEvent, e);
            if (ev != null)
            {
                if (Enum.TryParse(e, out CONDTESTPARAM cte))
                {
                    condtestmanager.AddInList(cte, ev);
                }
                else
                {
                    Debug.LogWarning($"Skip Adding Event Value, {e} is not a valid CONDTESTPARAM.");
                }
            }
        }
    }

    protected override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
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
                    /*
                    for successive conditions without rest, 
                    make sure no extra logic updates(frames) are inserted.
                    */
                    if (ex.PreICI <= 0 && ex.SufICI <= 0)
                    {
                        // new condtest starts at PreICI
                        if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                        EnterCondState(CONDSTATE.COND, true);
                    }
                    else
                    {
                        EnterCondState(CONDSTATE.SUFICI, true);
                        SetEnvActiveParam("Visible", false);
                    }
                    SyncFrame();
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                    SyncFrame();
                }
                break;
        }
    }
}