/*
SpikeGLXColor.cs is part of the Experica.
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
using UnityEngine;
using Experica;
using System.Collections.Generic;
using System.Linq;
using ColorSpace = Experica.NetEnv.ColorSpace;

/// <summary>
/// SpikeGLX Condition Test with Display-Confined Colors
/// </summary>
public class SpikeGLXColor : SpikeGLXCTLogic
{
    protected override void PrepareCondition()
    {
        var cond = new Dictionary<string, List<object>>();
        var colorspace = GetExParam<ColorSpace>("ColorSpace");
        var colorvar = GetExParam<string>("Color");
        var colorname = colorspace + "_" + colorvar;
        var ori = GetExParam<List<float>>("Ori");
        var sf = GetExParam<List<float>>("SpatialFreq");

        // get color
        List<Color> color = null;
        List<Color> wp = null;
        List<float> angle = null;
        var data = ex.Display_ID.GetColorData();
        if (data != null)
        {
            if (data.ContainsKey(colorname))
            {
                color = data[colorname].Convert<List<Color>>();

                var wpname = colorname + "_WP";
                if (data.ContainsKey(wpname))
                {
                    wp = data[wpname].Convert<List<Color>>();
                }
                var anglename = colorname + "_Angle";
                if (data.ContainsKey(anglename))
                {
                    angle = data[anglename].Convert<List<float>>();
                }
            }
            else
            {
                Debug.Log($"{colorname} is not found in colordata of {ex.Display_ID}.");
            }
        }

        // combine factor levels
        if (ori != null)
        {
            cond["Ori"] = ori.Select(i => (object)i).ToList();
        }
        if (sf != null)
        {
            cond["SpatialFreq"] = sf.Select(i => (object)i).ToList();
        }
        var colorcond = new Dictionary<string, List<object>>();
        if (color != null)
        {
            cond["_colorindex"] = Enumerable.Range(0, color.Count).Select(i => (object)i).ToList();
            var colorparam = "Color";
            if (ex.ID.StartsWith("Flash") || ex.ID.StartsWith("Color"))
            {
            }
            else
            {
                colorparam = "MaxColor";
            }
            colorcond[colorparam] = color.Select(i => (object)i).ToList();
            if (wp != null)
            {
                colorcond["BGColor"] = wp.Select(i => (object)i).ToList();
                if (colorparam == "MaxColor")
                {
                    colorcond["MinColor"] = wp.Select(i => (object)i).ToList();
                }
            }
            if (angle != null)
            {
                colorcond["Angle"] = angle.Select(i => (object)i).ToList();
            }
        }

        var fcond = cond.OrthoCombineFactor();
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

        pushexcludefactor= new List<string>() { "Angle" };
        condmgr.PrepareCondition(fcond);
    }
}