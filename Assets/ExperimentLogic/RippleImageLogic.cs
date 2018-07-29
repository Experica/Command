/*
RippleImageLogic.cs is part of the VLAB project.
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
using VLab;
using System.Collections.Generic;
using System.Linq;

public class RippleImageLogic : RippleCTLogic
{
    float diameter;

    protected override void GenerateFinalCondition()
    {
        var cond = new Dictionary<string, List<object>>
        {
            ["Image"] = Enumerable.Range(1, ex.GetParam("NumOfImage").Convert<int>()).Select(i => (object)i.ToString()).ToList()
        };
        condmanager.FinalizeCondition(cond);
    }

    protected override void OnStartExperiment()
    {
        base.OnStartExperiment();
        var mt = (MaskType)GetEnvActiveParam("MaskType");
        if (mt == MaskType.DiskFade || mt == MaskType.Disk)
        {
            diameter = (float)GetEnvActiveParam("Diameter");
            var mrr = (float)GetEnvActiveParam("MaskRadius") / 0.5f;
            SetEnvActiveParam("Diameter", diameter / mrr);
        }
    }

    protected override void StartExperimentTimeSync()
    {
        envmanager.InvokeActiveRPC("RpcPreLoadImageset", new object[] { 1, ex.GetParam("NumOfImage").Convert<int>() });
        recorder.RecordPath = ex.GetDataPath();
        timer.Timeout(config.NotifyLatency + ex.GetParam("PreLoadImageLatency").Convert<int>());
        pport.BitPulse(bit: config.StartSyncCh, duration_ms: 5);
        timer.Restart();
    }

    protected override void OnExperimentStopped()
    {
        base.OnExperimentStopped();
        var mt = (MaskType)GetEnvActiveParam("MaskType");
        if (mt == MaskType.DiskFade || mt == MaskType.Disk)
        {
            SetEnvActiveParam("Diameter", diameter);
        }
    }
}
