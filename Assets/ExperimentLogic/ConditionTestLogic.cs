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
using Experica.Command;
using System.Linq;

/// <summary>
/// Condition Test Logic with EventSyncRoute through GPIO and Display Marker
/// </summary>
public class ConditionTestLogic : ExperimentLogic
{
    protected IGPIO gpio;
    protected bool syncvalue;

    /// <summary>
    /// init gpio and inactive sync state
    /// </summary>
    protected override void OnStartExperiment()
    {
        if (ex.EventSyncProtocol.Routes.Contains(EventSyncRoute.DigitalOut))
        {
            gpio = new ParallelPort(dataaddress: Config.ParallelPort0);
            //if (!gpio.Found)
            //{
            //    gpio = new FTDIGPIO();
            //}
            if (!gpio.Found)
            {
                // gpio = new MCCDevice(config.MCCDevice, config.MCCDPort);
            }
            if (!gpio.Found)
            {
                Debug.LogWarning("No GPIO for DigitalOut EventSyncRoute.");
            }
        }
        SetEnvActiveParam("Visible", false);
        SyncEvent();
    }

    /// <summary>
    /// return to inactive sync state and release gpio
    /// </summary>
    protected override void OnExperimentStopped()
    {
        SetEnvActiveParam("Visible", false);
        SyncEvent();
        gpio?.Dispose();
    }

    /// <summary>
    /// Sync to External Device and Register Event Name/Time/Value according to EventSyncProtocol
    /// </summary>
    /// <param name="name">Event Name, NullorEmpty will Reset Sync Channel to inactive state without event register</param>
    /// <param name="time">Event Time, Non-NaN value will register in `Event` as well as `SyncEvent`</param>
    /// <param name="value">Event Value, Non-Null value will register in `CONDTESTPARAM` if event name is a valid `CONDTESTPARAM`</param>
    protected virtual void SyncEvent(string name = null, double time = double.NaN, object value = null)
    {
        var esp = ex.EventSyncProtocol;
        if (esp.Routes == null || esp.Routes.Length == 0)
        {
            Debug.LogWarning("No SyncRoute in EventSyncProtocol, Skip SyncEvent ...");
            return;
        }
        bool addtosynclist = false;
        bool syncreset = string.IsNullOrEmpty(name);

        if (esp.NChannel == 1 && esp.NEdgePEvent == 1)
        {
            addtosynclist = !syncreset;
            syncvalue = addtosynclist && !syncvalue;

            for (var i = 0; i < esp.Routes.Length; i++)
            {
                switch (esp.Routes[i])
                {
                    case EventSyncRoute.Display:
                        SetEnvActiveParam("Mark", syncvalue);
                        break;
                    case EventSyncRoute.DigitalOut:
                        gpio?.BitOut(bit: Config.EventSyncCh, value: syncvalue);
                        break;
                }
            }
        }
        if (addtosynclist && ex.CondTestAtState != CONDTESTATSTATE.NONE)
        {
            if (!double.IsNaN(time))
            {
                condtestmanager.AddInList(CONDTESTPARAM.Event, name, time);
            }
            condtestmanager.AddInList(CONDTESTPARAM.SyncEvent, name);
            if (value != null)
            {
                if (Enum.TryParse(name, out CONDTESTPARAM ctp))
                {
                    condtestmanager.AddInList(ctp, value);
                }
                else
                {
                    Debug.LogWarning($"Skip Adding Event Value: {value}, because \"{name}\" is not a valid CONDTESTPARAM.");
                }
            }
        }
    }

    protected EnterStateCode EnterCondState(CONDSTATE value, bool sync = false)
    {
        var c = base.EnterCondState(value);
        if (sync && c == EnterStateCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
    }

    protected EnterStateCode EnterTrialState(TRIALSTATE value, bool sync = false)
    {
        var c = base.EnterTrialState(value);
        if (sync && c == EnterStateCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
    }

    protected EnterStateCode EnterBlockState(BLOCKSTATE value, bool sync = false)
    {
        var c = base.EnterBlockState(value);
        if (sync && c == EnterStateCode.Success)
        {
            SyncEvent(value.ToString());
        }
        return c;
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