/*
SpikeGLXColorCTLogic.cs is part of the Experica.
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

namespace Experica
{
    /// <summary>
    /// Condition Test with SpikeGLX Data Acquisition System, and with Predefined Color
    /// </summary>
    public class SpikeGLXColorCTLogic : SpikeGLXCTLogic
    {
        protected override void GenerateFinalCondition()
        {
            base.GenerateFinalCondition();

            var colorspace = GetExParam<ColorSpace>("ColorSpace");
            var colorvar = GetExParam<string>("Color");
            var colorname = colorspace + "_" + colorvar;

            // get color
            List<Color> color = null;
            var data = ex.Display_ID.GetColorData();
            if (data != null)
            {
                if (data.ContainsKey(colorname))
                {
                    color = data[colorname].Convert<List<Color>>();
                }
                else
                {
                    Debug.Log(colorname + " is not found in colordata of " + ex.Display_ID);
                }
            }

            if (color != null)
            {
                switch (ex.ID)
                {
                    case "HartleySubspace":
                    case "OriSFPhase":
                    case "OriSF":
                    case "Contrast":
                    case "Diameter":
                        SetEnvActiveParam("MaxColor", color[0]);
                        SetEnvActiveParam("MinColor", color[1]);
                        break;
                }
            }
        }
    }
}