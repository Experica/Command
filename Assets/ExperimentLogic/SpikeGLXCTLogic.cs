/*
SpikeGLXCTLogic.cs is part of the Experica.
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
    /// Condition Test with SpikeGLX Data Acquisition System
    /// </summary>
    public class SpikeGLXCTLogic : ConditionTestLogic
    {
        protected bool isspikeglxtriggered;

        protected override void OnStart()
        {
            pport = new ParallelPort(dataaddress: config.ParallelPort1);
            recorder = new SpikeGLXRecorder(host: config.RecordHost, port: config.RecordHostPort);
        }

        protected override void StartExperimentTimeSync()
        {
            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
            {
                isspikeglxtriggered = true;
                recorder.RecordPath = ex.GetDataPath();
                /* 
                SpikeGLX recorder set path through network and remote server receive
                message and change file path, all of which need time to complete.
                Trigger record TTL before file path change completion will
                not successfully start recording.

                Analysis also need time to clear signal buffer,
                otherwise the delayed action may clear the start TTL which is
                needed to mark the timer zero.
                */
                timer.Timeout(config.NotifyLatency);
                recorder.RecordStatus = RecordStatus.Recording;
                //pport.BitPulse(bit: config.StartSyncCh, duration_ms: 5);
            }
            /*
            Immediately after the TTL triggering SpikeGLX recording, we reset timer, 
            so that timer zero can be aligned with the triggering TTL.
            */
            timer.Restart();
        }

        protected override void StopExperimentTimeSync()
        {
            // Tail period to make sure lagged effect data are recorded before trigger recording stop
            timer.Timeout(ex.Display_ID.DisplayLatency(config.Display) + config.MaxDisplayLatencyError + config.OnlineSignalLatency);
            if (isspikeglxtriggered)
            {
                recorder.RecordStatus = RecordStatus.Stopped;
                //pport.BitPulse(bit: config.StopSyncCh, duration_ms: 5);
                isspikeglxtriggered = false;
            }
            timer.Stop();
        }

        protected override void Logic()
        {
            switch (CondState)
            {
                case CONDSTATE.NONE:
                    CondState = CONDSTATE.PREICI;
                    break;
                case CONDSTATE.PREICI:
                    if (PreICIHold >= ex.PreICI)
                    {
                        CondState = CONDSTATE.COND;
                        SyncEvent(CONDSTATE.COND.ToString());
                        SetEnvActiveParam("Visible", true);
                    }
                    break;
                case CONDSTATE.COND:
                    if (CondHold >= ex.CondDur)
                    {
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
                    }
                    break;
            }
        }
    }
}