/*
SpikeGLXOptoCS.cs is part of the Experica.
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
/// SpikeGLX TwoGrating Center-Surround Test with User-Defined Factors and optogenetic manipulation 
/// </summary>
public class SpikeGLXOptoCS : SpikeGLXOpto
{
    protected override void OnCONDEntered()
    {
        SetEnvActiveParam("Visible@Grating@Grating0", true);
        SetEnvActiveParam("Visible@Grating@Grating1", true);
    }

    protected override void OnSUFICIEntered()
    {
        SetEnvActiveParam("Visible@Grating@Grating0", false);
        SetEnvActiveParam("Visible@Grating@Grating1", false);
    }

    protected override void PrepareCondition()
    {
        var cond = new Dictionary<string, List<object>>();
        var colorspace = GetExParam<ColorSpace>("ColorSpace");
        var colorvar = GetExParam<string>("Color");
        var colorname = colorspace + "_" + colorvar;
        var ori = GetExParam<List<float>>("Ori");
        var sf = GetExParam<List<float>>("SpatialFreq");
        var diameter0 = GetExParam<List<float>>("Diameter0");
        var diameter1 = GetExParam<List<float>>("Diameter1");

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

        if (wp != null) { SetEnvActiveParam("BGColor", wp[0]); }

        // combine factor levels
        if (ori != null)
        {
            cond["Ori"] = ori.Select(i => (object)i).ToList();
        }
        if (sf != null)
        {
            cond["SpatialFreq"] = sf.Select(i => (object)i).ToList();
        }
        if (diameter0 != null)
        {
            cond["Diameter@Grating@Grating0"] = diameter0.Cast<object>().ToList();
        }
        if (diameter1 != null)
        {
            cond["Diameter@Grating@Grating1"] = diameter1.Cast<object>().ToList();
        }

        // combine color levels
        var colorcond = new Dictionary<string, List<object>>();
        var cscond = new Dictionary<string, List<object>>();
        if (color != null)
        {
            colorcond["MaxColor@Grating@Grating0"] = Enumerable.Range(0, color.Count).Cast<object>().ToList();
            colorcond["MaxColor@Grating@Grating1"] = Enumerable.Range(0, color.Count + 1).Cast<object>().ToList();
            cscond = colorcond.OrthoCombineFactor();
            for (var i = 0; i < cscond.Values.First().Count; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    var af = $"Angle{j}";
                    var cf = $"MaxColor@Grating@Grating{j}";
                    var ci = (int)cscond[cf][i];
                    var av = ci == color.Count ? float.NaN : angle[ci];
                    if (!cscond.ContainsKey(af))
                    {
                        cscond[af] = new List<object> { av };
                    }
                    else
                    {
                        cscond[af].Add(av);
                    }
                    var cv = ci == color.Count ? wp[0] : color[ci];
                    cscond[cf][i] = cv;
                }
            }
            cond["_colorindex"] = Enumerable.Range(0, cscond.Values.First().Count).Cast<object>().ToList();
        }

        var fcond = cond.OrthoCombineFactor();
        if (fcond.ContainsKey("_colorindex"))
        {
            foreach (var i in fcond["_colorindex"])
            {
                foreach (var f in cscond.Keys)
                {
                    if (!fcond.ContainsKey(f))
                    {
                        fcond[f] = new List<object> { cscond[f][(int)i] };
                    }
                    else
                    {
                        fcond[f].Add(cscond[f][(int)i]);
                    }
                }
            }
            fcond.Remove("_colorindex");
        }

        // get optogenetics conditions
        var optofreq = GetExParam<List<double>>("OptoFreq");
        var optoduty = GetExParam<List<double>>("OptoDuty");
        var nopto = GetExParam<bool>("AddNoOpto");
        var ocond = new Dictionary<string, List<object>>()
        {
            ["Opto"] = new List<object> { true }
        };
        if (optofreq != null) { ocond["OptoFreq"] = optofreq.Cast<object>().ToList(); }
        if (optoduty != null) { ocond["OptoDuty"] = optoduty.Cast<object>().ToList(); }
        ocond = ocond.OrthoCombineFactor();
        if (nopto)
        {
            ocond["Opto"].Insert(0, false);
            ocond["OptoFreq"].Insert(0, 0.0);
            ocond["OptoDuty"].Insert(0, 0.0);
        }

        // combine optogenetics conditions with base conditions
        var ofcond = ocond.OrthoCombineCondition(fcond);
        condmgr.PrepareCondition(ofcond);
        pushexcludefactor = new List<string>() { "Angle0", "Angle1" };
    }
}
