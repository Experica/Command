/*
RippleImageLogic.cs is part of the Experica.
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
using System.Collections.Generic;
using System.Linq;
using Experica.NetEnv;

namespace Experica.Command
{
    public class RippleImageLogic : RippleCTLogic
    {
        protected float diameterbeforeadjust;
        protected bool isdiameteradjusted;

        protected override void PrepareCondition()
        {
            var cond = new Dictionary<string, List<object>>
            {
                ["Image"] = Enumerable.Range((int)GetEnvActiveParam("StartIndex"), (int)GetEnvActiveParam("NumOfImage")).Select(i => (object)i).ToList()
            };
            condmgr.PrepareCondition(cond);
        }

        protected override void OnStartExperiment()
        {
            base.OnStartExperiment();
            var mt = (MaskType)GetEnvActiveParam("MaskType");
            if (mt == MaskType.DiskFade || mt == MaskType.Disk)
            {
                isdiameteradjusted = true;
                diameterbeforeadjust = (float)GetEnvActiveParam("Diameter");
                var mrr = (float)GetEnvActiveParam("MaskRadius") / 0.5f;
                SetEnvActiveParam("Diameter", diameterbeforeadjust / mrr);
            }
        }

        protected override void OnExperimentStopped()
        {
            base.OnExperimentStopped();
            if (isdiameteradjusted)
            {
                SetEnvActiveParam("Diameter", diameterbeforeadjust);
                isdiameteradjusted = false;
            }
        }
    }
}