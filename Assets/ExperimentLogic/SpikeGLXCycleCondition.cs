/*
SpikeGLXCycleCondition.cs is part of the Experica.
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
using System.Collections.Generic;
using System;

namespace Experica
{
    /// <summary>
    /// Basic PreICI-Cond-SufICI Logic with Digital and ScreenMarker Event Sync
    /// </summary>
    public class SpikeGLXCycleCondition : SpikeGLXCTLogic
    {
        protected int nc = 0;

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
                SetEnvActiveParam("MaxColor", color[0]);
                SetEnvActiveParam("MinColor", color[1]);
            }
        }

        protected override void OnStartExperiment()
        {
            base.OnStartExperiment();
            nc = 0;
        }

        protected override void Logic()
        {
            switch (CondState)
            {
                case CONDSTATE.NONE:
                    EnterCondState(CONDSTATE.PREICI);
                    SyncFrame();
                    break;
                case CONDSTATE.PREICI:
                    if (PreICIHold >= ex.PreICI)
                    {
                        EnterCondState(CONDSTATE.COND);
                        SyncEvent(CONDSTATE.COND.ToString());
                        SetEnvActiveParam("Visible", true);
                        SyncFrame();
                    }
                    break;
                case CONDSTATE.COND:
                    var f = GetEnvActiveParam<float>("ModulateTemporalFreq");
                    var div = (float)CondHold / 1000f * f;
                    var c = Mathf.FloorToInt(div);
                    if (c > 0)
                    {
                        nc++;
                        if (nc >= ex.CondRepeat) { StartStopExperiment(false); return; }
                        // new condtest start at PreICI
                        EnterCondState(CONDSTATE.PREICI);
                        EnterCondState(CONDSTATE.COND);
                        SyncEvent(CONDSTATE.COND.ToString());
                    }
                    else
                    {
                        switch (GetExParam<string>("ModulateParam"))
                        {
                            case "ModulateTime":
                                SetEnvActiveParam("ModulateTimeSecond", div / f);
                                break;
                            case "Ori":
                                SetEnvActiveParam("Ori", div * 360f);
                                break;
                            case "DKLIsoLum":
                                SetEnvActiveParam("MinColor", (div * 360f).DKLIsoLum(GetExParam<float>("Intercept"), ex.Display_ID));
                                break;
                            case "DKLIsoSLM":
                                SetEnvActiveParam("MinColor", (div * 360f).DKLIsoSLM(GetExParam<float>("Intercept"), ex.Display_ID));
                                break;
                            case "DKLIsoLM":
                                SetEnvActiveParam("MinColor", (div * 360f).DKLIsoLM(GetExParam<float>("Intercept"), ex.Display_ID));
                                break;
                        }
                    }
                    SyncFrame();
                    break;
            }
        }
    }
}