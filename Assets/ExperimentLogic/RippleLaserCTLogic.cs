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

namespace Experica.Command
{
    public class RippleLaserCTLogic : RippleLaserLogic
    {
        protected override void GenerateCondition()
        {
            pushexcludefactor = new List<string>() { "LaserPower", "LaserFreq", "LaserPower2", "LaserFreq2" };

            // get laser conditions
            laser = ex.GetParam("Laser").Convert<string>().GetLaser(Config);
            switch (laser?.Type)
            {
                case Laser.Omicron:
                    lasersignalch = Config.SignalCh0;
                    break;
                case Laser.Cobolt:
                    lasersignalch = Config.SignalCh1;
                    break;
            }
            laser2 = ex.GetParam("Laser2").Convert<string>().GetLaser(Config);
            switch (laser2?.Type)
            {
                case Laser.Omicron:
                    laser2signalch = Config.SignalCh0;
                    break;
                case Laser.Cobolt:
                    laser2signalch = Config.SignalCh1;
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
            }
            if (laser2signalch != null)
            {
                if (p2 != null)
                {
                    lcond["LaserPower2"] = p2.Where(i => i > 0).Select(i => (object)i).ToList();
                }
                if (f2 != null)
                {
                    lcond["LaserFreq2"] = f2.Where(i => i != Vector4.zero).Select(i => (object)i).ToList();
                }
            }
            lcond = lcond.OrthoCondOfFactorLevel();

            var addzero = ex.GetParam("AddZeroCond").Convert<bool>();
            if (lasersignalch != null)
            {
                if (p != null && addzero)
                {
                    lcond["LaserPower"].Insert(0, 0f);
                }
                if (f != null && addzero)
                {
                    lcond["LaserFreq"].Insert(0, Vector4.zero);
                }
            }
            if (laser2signalch != null)
            {
                if (p2 != null && addzero)
                {
                    lcond["LaserPower2"].Insert(0, 0f);
                }
                if (f2 != null && addzero)
                {
                    lcond["LaserFreq2"].Insert(0, Vector4.zero);
                }
            }

            // get base conditions
            condmanager.FinalizeCondition(ex.CondPath);
            var bcond = condmanager.Cond;

            // combine laser and base conditions
            var fcond = new Dictionary<string, List<object>>()
            {
                {"l",Enumerable.Range(0,lcond.First().Value.Count).Select(i=>(object)i).ToList() },
                {"b",Enumerable.Range(0,bcond.First().Value.Count).Select(i=>(object)i).ToList() }
            };
            fcond = fcond.OrthoCondOfFactorLevel();
            foreach (var bf in bcond.Keys)
            {
                fcond[bf] = new List<object>();
            }
            foreach (var lf in lcond.Keys)
            {
                fcond[lf] = new List<object>();
            }
            for (var i = 0; i < fcond["l"].Count; i++)
            {
                var bci = (int)fcond["b"][i];
                var lci = (int)fcond["l"][i];
                foreach (var bf in bcond.Keys)
                {
                    fcond[bf].Add(bcond[bf][bci]);
                }
                foreach (var lf in lcond.Keys)
                {
                    fcond[lf].Add(lcond[lf][lci]);
                }
            }
            fcond.Remove("b"); fcond.Remove("l");

            condmanager.FinalizeCondition(fcond);
        }
    }
}