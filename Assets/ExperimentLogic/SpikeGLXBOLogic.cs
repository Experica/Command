/*
SpikeGLXColorLogic.cs is part of the Experica.
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
using UnityEngine;
using System.Linq;

namespace Experica
{
    /// <summary>
    /// SpikeGLX Condition Test with Display-Confined ColorSpace
    /// </summary>
    public class SpikeGLXBOLogic : SpikeGLXCTLogic
    {
        protected override void GenerateFinalCondition()
        {
            var cond = new Dictionary<string, List<object>>();
            var ori = GetExParam<List<float>>("Ori");
            var yoffset = GetExParam<List<float>>("YOffset");

            // combine factor levels
            if (ori != null)
            {
                cond["Ori"] = ori.Select(i => (object)i).ToList();
            }
            if (yoffset != null)
            {
                cond["PositionOffset"] = yoffset.Select(i => (object)new Vector3(0,i,0)).ToList();
            }
            var colorcond = new Dictionary<string, List<object>>();
            var color = GetEnvActiveParam<Color>("Color");
            var bgcolor = GetEnvActiveParam<Color>("BGColor");
            colorcond["Color"] = new List<object> { color, bgcolor };
            colorcond["BGColor"] = new List<object> { bgcolor, color };
            cond["_colorindex"] = new List<object> { 0, 1 };

            var fcond = cond.OrthoCondOfFactorLevel();
            if (fcond.ContainsKey("_colorindex"))
            {
                foreach (var i in fcond["_colorindex"])
                {
                    foreach (var f in colorcond.Keys)
                    {
                        if (!fcond.ContainsKey(f))
                        {
                            fcond[f] = new List<object> { colorcond[f][(int)i] };
                        }
                        else
                        {
                            fcond[f].Add(colorcond[f][(int)i]);
                        }
                    }
                }
                fcond.Remove("_colorindex");
            }
            condmanager.FinalizeCondition(fcond);
        }
    }
}