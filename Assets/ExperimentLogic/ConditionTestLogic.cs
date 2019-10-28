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
namespace Experica
{
    /// <summary>
    /// Basic PreICI-Cond-SufICI Logic with Digital and ScreenMarker Event Sync
    /// </summary>
    public class ConditionTestLogic : ExperimentLogic
    {
        protected IGPIO gpio;
        protected bool syncvalue;

        protected override void OnStart()
        {
            var syncmethod = ex.EventSyncProtocol.SyncMethods;
            if (syncmethod.Contains(SyncMethod.GPIO))
            {
                gpio = new MCCDevice(config.MCCDevice, config.MCCDPort);
                if (gpio.Found) return;
                gpio = new ParallelPort(dataaddress: config.ParallelPort1);
            }
        }

        protected override void OnStartExperiment()
        {
            SetEnvActiveParam("Visible", false);
            SyncEvent();
        }

        protected override void OnExperimentStopped()
        {
            SetEnvActiveParam("Visible", false);
            SyncEvent();
        }

        /// <summary>
        /// Register and Sync Event with External Device through EventSyncProtocol
        /// </summary>
        /// <param name="e">Event Name, NullorEmpty will Reset Sync Channel to low/false state, without event register</param>
        protected virtual void SyncEvent(string e = "")
        {
            var esp = ex.EventSyncProtocol;
            if (esp.SyncMethods == null || esp.SyncMethods.Count == 0)
            {
                UnityEngine.Debug.LogWarning("No SyncMethod in EventSyncProtocol, Skip SyncEvent ...");
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
            switch (BlockState)
            {
                case BLOCKSTATE.NONE:
                    BlockState = BLOCKSTATE.PREIBI;
                    break;
                case BLOCKSTATE.PREIBI:
                    if (PreIBIHold >= ex.PreIBI)
                    {
                        BlockState = BLOCKSTATE.BLOCK;
                    }
                    break;
                case BLOCKSTATE.BLOCK:
                    switch (TrialState)
                    {
                        case TRIALSTATE.NONE:
                            TrialState = TRIALSTATE.PREITI;
                            break;
                        case TRIALSTATE.PREITI:
                            if (PreITIHold >= ex.PreITI)
                            {
                                TrialState = TRIALSTATE.TRIAL;
                            }
                            break;
                        case TRIALSTATE.TRIAL:
                            switch (CondState)
                            {
                                case CONDSTATE.NONE:
                                    CondState = CONDSTATE.PREICI;
                                    break;
                                case CONDSTATE.PREICI:
                                    if (PreICIHold >= ex.PreICI)
                                    {
                                        // State transition: PREICI -> COND
                                        CondState = CONDSTATE.COND;
                                        SyncEvent(CONDSTATE.COND.ToString());
                                        SetEnvActiveParam("Visible", true);
                                    }
                                    break;
                                case CONDSTATE.COND:
                                    if (CondHold >= ex.CondDur)
                                    {
                                        // State transition: COND -> SUFICI
                                        CondState = CONDSTATE.SUFICI;
                                        if (ex.PreICI != 0 || ex.SufICI != 0)
                                        {
                                            SyncEvent(CONDSTATE.SUFICI.ToString());
                                            SetEnvActiveParam("Visible", false);
                                        }
                                    }
                                    break;
                                case CONDSTATE.SUFICI:
                                    if (SufICIHold >= ex.SufICI)
                                    {
                                        CondState = CONDSTATE.NONE;
                                        if (TrialHold >= ex.TrialDur)
                                        {
                                            TrialState = TRIALSTATE.SUFITI;
                                        }
                                    }
                                    break;
                            }
                            break;
                        case TRIALSTATE.SUFITI:
                            if (SufITIHold >= ex.SufITI)
                            {
                                TrialState = TRIALSTATE.NONE;
                                if (BlockHold >= ex.BlockDur)
                                {
                                    BlockState = BLOCKSTATE.SUFIBI;
                                }
                            }
                            break;
                    }
                    break;
                case BLOCKSTATE.SUFIBI:
                    if (SufIBIHold >= ex.SufIBI)
                    {
                        BlockState = BLOCKSTATE.NONE;
                    }
                    break;
            }
        }
    }
}