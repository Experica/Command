/*
RippleCTLogic.cs is part of the Experica.
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
    public class RippleCTLogic : ConditionTestLogic
    {
        protected bool isrippletriggered;

        protected override void OnStart()
        {
            gpio = new ParallelPort(dataaddress: config.ParallelPort1);
            recorder = new RippleRecorder();
        }

        protected override void StartExperimentTimeSync()
        {
            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
            {
                isrippletriggered = true;
                recorder.RecordPath = ex.GetDataPath();
                /* 
                Ripple recorder set path through network and Trellis receive
                message and change file path, all of which need time to complete.
                Trigger record TTL before file path change completion will
                not successfully start recording.

                Analysis also need time to clear signal buffer,
                otherwise the delayed action may clear the start TTL pluse which is
                needed to mark the timer zero.
                */
                timer.Timeout(config.NotifyLatency);
                gpio.BitPulse(bit: config.StartSyncCh, duration_ms: 5);
            }
            /*
            Immediately after the TTL falling edge triggering ripple recording, we reset timer, 
            so timer zero can be aligned with the triggering TTL falling edge.
            */
            timer.Restart();
        }

        protected override void StopExperimentTimeSync()
        {
            // Tail period to make sure lagged effect data is recorded before trigger recording stop
            timer.Timeout(ex.Display_ID.DisplayLatency(config.Display) ?? 0 + config.MaxDisplayLatencyError + config.OnlineSignalLatency);
            if (isrippletriggered)
            {
                gpio.BitPulse(bit: config.StopSyncCh, duration_ms: 5);
            }
            timer.Stop();
        }

        protected override void Logic()
        {
            switch (CondState)
            {
                case CONDSTATE.NONE:
                    if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                    break;
                case CONDSTATE.PREICI:
                    if (PreICIHold >= ex.PreICI)
                    {
                        EnterCondState(CONDSTATE.COND);
                        SyncEvent(CONDSTATE.COND.ToString());
                        SetEnvActiveParam("Visible", true);
                    }
                    break;
                case CONDSTATE.COND:
                    if (CondHold >= ex.CondDur)
                    {
                        EnterCondState(CONDSTATE.SUFICI);
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
                        if (EnterCondState(CONDSTATE.PREICI) == EnterCode.NoNeed) { return; }
                    }
                    break;
            }
        }
    }
}