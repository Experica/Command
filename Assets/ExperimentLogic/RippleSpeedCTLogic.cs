/*
RippleSpeedCTLogic.cs is part of the VLAB project.
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

public class RippleSpeedCTLogic : RippleCTLogic
{
    List<string> factorpushexcept = new List<string>() { "Speed" };

    protected override void GenerateFinalCondition()
    {

        // convert speed to temporal frequency
        float sf = envmanager.GetParam("SpatialFreq").Convert<float>();

        // get base conditions
        var bcond = condmanager.ProcessCondition(condmanager.ReadConditionFile(ex.CondPath));
        bcond["TemporalFreq"] = bcond["Speed"].Convert<List<float>>().Select(i => (object)(i * sf)).ToList();
        condmanager.FinalizeCondition(bcond);
    }

    protected override void SamplePushCondition(int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
    {
        // Block sample and push defered into logic
        condmanager.PushCondition(condmanager.SampleCondition(ex.CondRepeat, ex.BlockRepeat, manualcondidx, manualblockidx, false),
            envmanager, factorpushexcept);
    }
}
