/*
RippleLaserCTLogic.cs is part of the Experica.
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
using UnityEngine;

namespace Experica
{
    public class RippleLaserCTLogic : RippleLaserLogic
    {
        protected override void GenerateFinalCondition()
        {
            pushexcludefactors = new List<string>() { "LaserPower", "LaserFreq", "LaserPower2", "LaserFreq2", "LaserIndex" };

            // get laser conditions
            laser = ex.GetParam("Laser").Convert<string>().GetLaser(config);
            lasersignalch = null;
            switch (laser?.Type)
            {
                case Laser.Omicron:
                    lasersignalch = config.SignalCh1;
                    break;
                case Laser.Cobolt:
                    lasersignalch = config.SignalCh2;
                    break;
            }
            laser2 = ex.GetParam("Laser2").Convert<string>().GetLaser(config);
            laser2signalch = null;
            switch (laser2?.Type)
            {
                case Laser.Omicron:
                    laser2signalch = config.SignalCh1;
                    break;
                case Laser.Cobolt:
                    laser2signalch = config.SignalCh2;
                    break;
            }

            var p = ex.GetParam("LaserPower").Convert<List<float>>();
            var f = ex.GetParam("LaserFreq").Convert<List<Vector4>>();
            var p2 = ex.GetParam("LaserPower2").Convert<List<float>>();
            var f2 = ex.GetParam("LaserFreq2").Convert<List<Vector4>>();
            var lcond = new Dictionary<string, List<object>>();
            if (lasersignalch != null)
            {
                if (p != null)
                {
                    lcond["LaserPower"] = p.Where(i => i > 0).Select(i => (object)i).ToList();
                }
                if (f != null)
                {
                    lcond["LaserFreq"] = f.Where(i => i != Vector4.zero).Select(i => (object)i).ToList();
                }
                if (laser2signalch != null)
                {
                    lcond["LaserPower2"] = new List<object>();
                    lcond["LaserFreq2"] = new List<object>();
                    lcond["LaserPower2"].Insert(0, 0f);
                    lcond["LaserFreq2"].Insert(0, Vector4.zero);
                }
            }
            lcond = lcond.OrthoCondOfFactorLevel();
            var l2cond = new Dictionary<string, List<object>>();
            if (laser2signalch != null)
            {
                if (p2 != null)
                {
                    l2cond["LaserPower2"] = p2.Where(i => i > 0).Select(i => (object)i).ToList();
                }
                if (f2 != null)
                {
                    l2cond["LaserFreq2"] = f2.Where(i => i != Vector4.zero).Select(i => (object)i).ToList();
                }
                if (lasersignalch != null)
                {
                    l2cond["LaserPower"] = new List<object>();
                    l2cond["LaserFreq"] = new List<object>();
                    l2cond["LaserPower"].Insert(0, 0f);
                    l2cond["LaserFreq"].Insert(0, Vector4.zero);
                }
            }
            l2cond = l2cond.OrthoCondOfFactorLevel();

            // merge laser and laser2 conditions and add zero condition
            var flcond = new Dictionary<string, List<object>>();
            var addzero = ex.GetParam("AddZeroCond").Convert<bool>();
            if (lasersignalch != null)
            {
                if (laser2signalch != null)
                {
                    flcond["LaserPower"] = lcond["LaserPower"].Concat(l2cond["LaserPower"]).ToList();
                    flcond["LaserFreq"] = lcond["LaserFreq"].Concat(l2cond["LaserFreq"]).ToList();
                }
                else
                {
                    flcond["LaserPower"] = lcond["LaserPower"];
                    flcond["LaserFreq"] = lcond["LaserFreq"];
                }
                if (p != null && addzero)
                {
                    flcond["LaserPower"].Insert(0, 0f);
                }
                if (f != null && addzero)
                {
                    flcond["LaserFreq"].Insert(0, Vector4.zero);
                }
            }
            if (laser2signalch != null)
            {
                if (lasersignalch != null)
                {
                    flcond["LaserPower2"] = lcond["LaserPower2"].Concat(l2cond["LaserPower2"]).ToList();
                    flcond["LaserFreq2"] = lcond["LaserFreq2"].Concat(l2cond["LaserFreq2"]).ToList();
                }
                else
                {
                    flcond["LaserPower2"] = l2cond["LaserPower2"];
                    flcond["LaserFreq2"] = l2cond["LaserFreq2"];
                }
                if (p2 != null && addzero)
                {
                    flcond["LaserPower2"].Insert(0, 0f);
                }
                if (f2 != null && addzero)
                {
                    flcond["LaserFreq2"].Insert(0, Vector4.zero);
                }
            }

            // get base conditions
            var bcond = condmanager.GenerateFinalCondition(ex.CondPath);

            // combine laser and base conditions
            var fcond = new Dictionary<string, List<object>>()
            {
                {"LaserIndex",Enumerable.Range(0,flcond.First().Value.Count).Select(i=>(object)i).ToList() },
                {"BaseCondIndex",Enumerable.Range(0,bcond.First().Value.Count).Select(i=>(object)i).ToList() }
            };
            fcond = fcond.OrthoCondOfFactorLevel();
            foreach (var bf in bcond.Keys)
            {
                fcond[bf] = new List<object>();
            }
            foreach (var lf in flcond.Keys)
            {
                fcond[lf] = new List<object>();
            }
            for (var i = 0; i < fcond["LaserIndex"].Count; i++)
            {
                var bci = (int)fcond["BaseCondIndex"][i];
                var lci = (int)fcond["LaserIndex"][i];
                foreach (var bf in bcond.Keys)
                {
                    fcond[bf].Add(bcond[bf][bci]);
                }
                foreach (var lf in flcond.Keys)
                {
                    fcond[lf].Add(flcond[lf][lci]);
                }
            }

            fcond.Remove("BaseCondIndex");

            condmanager.FinalizeCondition(fcond);
        }
    }
}