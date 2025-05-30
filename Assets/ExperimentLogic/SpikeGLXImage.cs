/*
SpikeGLXImage.cs is part of the Experica.
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
using Experica.NetEnv;
using System.Collections.Generic;
using System.Linq;
using ColorSpace = Experica.NetEnv.ColorSpace;

/// <summary>
/// Image Test with SpikeGLX Data Acquisition System, and Predefined Colors
/// </summary>
public class SpikeGLXImage : SpikeGLXCondTest
{
    protected override void PrepareCondition()
    {
        var colorspace = GetExParam<ColorSpace>("ColorSpace");
        var colorvar = GetExParam<string>("Color");
        var colorname = colorspace + "_" + colorvar;

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

        if (color != null)
        {
            SetEnvActiveParam("MinColor", color[0]);
            SetEnvActiveParam("MaxColor", color[1]);
            if (wp != null)
            {
                SetEnvActiveParam("BGColor", wp[0]);
            }
        }

        // get imageset

        var imagesetname = GetEnvActiveParam<string>("ImageSet");
        if (imagesetname.QueryImageSet(out ImageSet imgset))
        {
            var cond = new Dictionary<string, List<object>>
            {
                ["Image"] = Enumerable.Range(0, imgset.Images.Length).Select(i => (object)i).ToList()
            };
            condmgr.PrepareCondition(cond);
            if (GetEnvActiveParam<ColorChannel>("ChannelModulate") == ColorChannel.None)
            {
                SetEnvActiveParam("BGColor", imgset.MeanColor);
            }
        }
        //var imagesetname = GetEnvActiveParam<string>("ImageSet");
        //var imageset = imagesetname.GetImageData();
        //if (imageset != null)
        //{
        //    var cond = new Dictionary<string, List<object>>
        //    {
        //        ["Image"] = imageset.Keys.Select(i => (object)i).ToList()
        //    };
        //    condmgr.PrepareCondition(cond);
        //}
    }
}