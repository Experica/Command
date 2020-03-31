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
using System.IO;
using UnityEngine;
using System.Linq;

namespace Experica
{
    /// <summary>
    /// SpikeGLX Condition Test with Display-Derived ColorSpace
    /// </summary>
    public class SpikeGLXColorLogic : SpikeGLXCTLogic
    {
        protected override void GenerateFinalCondition()
        {
            pushexcludefactors = new List<string>() { "HueAngle"};
            var cond = new Dictionary<string, List<object>>();
            var colorspace = ex.GetParam("ColorSpace").Convert<ColorSpace>();
            var colorvar = (string)ex.GetParam("Color");
            var colorname = colorspace + "_" + colorvar;
            var ori = ex.GetParam("Ori").Convert<List<float>>();
            var sf = ex.GetParam("SpatialFreq").Convert<List<float>>();
            List<Color> color = null;
            List<Color> wp = null;
            List<float> angle = null;

            // get color
            var file = Path.Combine("Data", ex.Display_ID, "colordata.yaml");
            if (!File.Exists(file))
            {
                // todo generate colordata
            }
            if (File.Exists(file))
            {
                var data = Yaml.ReadYamlFile<Dictionary<string, List<object>>>(file);
                if (data.ContainsKey(colorname))
                {
                    color = data[colorname].Convert<List<Color>>();
                    if (colorname.Contains("Hue"))
                    {
                        var huename = colorvar.Substring(0, colorvar.IndexOf('_') );
                        var wpname = colorname.Replace(huename, "WP");
                        if(data.ContainsKey(wpname))
                        {
                            wp = data[wpname].Convert<List<Color>>();
                        }
                        var anglename = colorname.Replace(huename, "HueAngle");
                        if (data.ContainsKey(anglename))
                        {
                            angle = data[anglename].Convert<List<float>>();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(colorname + " is not found in " + file);
                }
            }
            else
            {
                Debug.LogWarning($"Color Data: {file} Not Found.");
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
                cond["_colorindex"] = Enumerable.Range(0, color.Count).Select(i => (object)i).ToList(); ;
                var colorvarname = "Color";
                if (ex.ID.StartsWith("Flash") || ex.ID.StartsWith("Color"))
                {
                }
                else
                {
                    colorvarname = "MaxColor";
                }
                colorcond[colorvarname] = color.Select(i => (object)i).ToList();
                if (wp != null)
                {
                    colorcond["BGColor"] = wp.Select(i => (object)i).ToList();
                }
                if (angle != null)
                {
                    colorcond["HueAngle"] = angle.Select(i => (object)i).ToList();
                }
            }

            var fcond = cond.OrthoCondOfFactorLevel();
            if (fcond.ContainsKey("_colorindex"))
            {
                foreach (var i in fcond["_colorindex"])
                {
                    foreach (var f in colorcond.Keys)
                    {
                        if (!fcond.ContainsKey(f))
                        {
                            fcond[f] = new List<object> { colorcond[f].First() };
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