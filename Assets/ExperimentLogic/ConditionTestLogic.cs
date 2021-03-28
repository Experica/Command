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

namespace Experica
{
    /// <summary>
    /// Simple PreICI-Cond-SufICI Logic with Event Sync through GPIO and Display Marker
    /// </summary>
    public class ConditionTestLogic : ExperimentLogic
    {
        protected IGPIO gpio;
        protected bool syncvalue;

        protected override void OnStartExperiment()
        {
            if (ex.EventSyncProtocol.SyncMethods.Contains(SyncMethod.GPIO))
            {
                gpio = new ParallelPort(dataaddress: config.ParallelPort1);
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

        protected override void OnExperimentStopped()
        {
            SetEnvActiveParam("Visible", false);
            SyncEvent();
            gpio?.Dispose();
        }

        /// <summary>
        /// Sync and Register Event with External Device through EventSyncProtocol
        /// </summary>
        /// <param name="e">Event Name, NullorEmpty will Reset Sync Channel to low/false state without event register</param>
        protected virtual void SyncEvent(string e = null)
        {
            var esp = ex.EventSyncProtocol;
            if (esp.SyncMethods == null || esp.SyncMethods.Count == 0)
            {
                Debug.LogWarning("No SyncMethod in EventSyncProtocol, Skip SyncEvent ...");
                return;
            }
            bool addtosynclist = false;
            bool syncreset = string.IsNullOrEmpty(e) ? true : false;

            if (esp.nSyncChannel == 1 && esp.nSyncpEvent == 1)
            {
                syncvalue = syncreset ? false : !syncvalue;
                addtosynclist = syncreset ? false : true;
                for (var i = 0; i < esp.SyncMethods.Count; i++)
                {
                    switch (esp.SyncMethods[i])
                    {
                        case SyncMethod.Display:
                            SetEnvActiveParam("Mark", syncvalue);
                            break;
                        case SyncMethod.GPIO:
                            gpio?.BitOut(bit: config.EventSyncCh, value: syncvalue);
                            break;
                    }
                }
            }
            if (addtosynclist && ex.CondTestAtState != CONDTESTATSTATE.NONE)
            {
                condtestmanager.AddInList(CONDTESTPARAM.SyncEvent, e);
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
                        EnterCondState(CONDSTATE.COND);
                        SyncEvent(CONDSTATE.COND.ToString());
                        SetEnvActiveParam("Visible", true);
                        SyncFrame();
                    }
                    break;
                case CONDSTATE.COND:
                    if (CondHold >= ex.CondDur)
                    {
                        /*
                        for successive conditions without rest, 
                        make sure no extra updates(frames) are inserted.
                        */
                        if (ex.PreICI <= 0 && ex.SufICI <= 0)
                        {
                            // new condtest start at PreICI
                            if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                            EnterCondState(CONDSTATE.COND);
                            SyncEvent(CONDSTATE.COND.ToString());
                        }
                        else
                        {
                            EnterCondState(CONDSTATE.SUFICI);
                            SyncEvent(CONDSTATE.SUFICI.ToString());
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
}