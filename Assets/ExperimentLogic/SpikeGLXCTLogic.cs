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
            base.OnStart();
            recorder = new SpikeGLXRecorder(host: config.RecordHost, port: config.RecordHostPort);
        }

        protected override void StartExperimentTimeSync()
        {
            if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
            {
                recorder.RecordPath = ex.GetDataPath();
                /* 
                SpikeGLX recorder set path through network and remote server receive
                message and change file path, all of which need time to complete.
                Trigger record before file path change completion will
                not successfully start recording.
                */
                timer.Timeout(config.NotifyLatency);
                recorder.RecordStatus = RecordStatus.Recording;
                isspikeglxtriggered = true;
            }
            base.StartExperimentTimeSync();
        }

        protected override void StopExperimentTimeSync()
        {
            // Tail period to make sure lagged effect data are recorded before trigger recording stop
            timer.Timeout(ex.Display_ID.DisplayLatency(config.Display) + config.MaxDisplayLatencyError);
            if (isspikeglxtriggered)
            {
                recorder.RecordStatus = RecordStatus.Stopped;
                isspikeglxtriggered = false;
            }
            base.StopExperimentTimeSync();
        }
    }
}