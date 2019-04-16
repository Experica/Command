/*
DisplayCalibration.cs is part of the Experica.
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

namespace Experica
{
    public class DisplayCalibration : ExperimentLogic
    {
        protected ISpectroRadioMeter spectroradiometer;
        Dictionary<string, List<object>> measurement = new Dictionary<string, List<object>>();

        protected override void OnStart()
        {
            spectroradiometer = new PR(ex.GetParam("COM").Convert<string>(), ex.GetParam("PRModel").Convert<string>());
        }

        protected override void OnStartExperiment()
        {
            spectroradiometer?.Connect(1000);
            // Setup Measurement: Primary Lens, Add On Lens 1, Add On Lens 2, Aperture, Photometric Units(1=Metric), 
            // Detector Exposure Time(1000ms), Capture Mode(0=Single Capture), Number of Measure to Average(1=No Average), 
            // Power or Energy(0=Power), Trigger Mode(0=Internal Trigger), View Shutter(0=Open), CIE Observer(0=2°)
            spectroradiometer?.Setup("S,,,,1,1000,0,1,0,0,0,0", 1000);
        }

        protected override void OnStopExperiment()
        {
            spectroradiometer?.Close();
            if (ex.GetParam("SaveMeasure").Convert<bool>() && !string.IsNullOrEmpty(ex.Display_ID))
            {
                if (config.Display == null)
                {
                    config.Display = new Dictionary<string, Display>();
                }
                if (config.Display.ContainsKey(ex.Display_ID))
                {
                    config.Display[ex.Display_ID].Measurement = measurement;
                }
                else
                {
                    config.Display[ex.Display_ID] = new Display() { ID = ex.Display_ID, Measurement = measurement };
                }
            }
        }

        protected override void Logic()
        {
            switch (CondState)
            {
                case CONDSTATE.NONE:
                    CondState = CONDSTATE.PREICI;
                    break;
                case CONDSTATE.PREICI:
                    if (PreICIHold >= ex.PreICI)
                    {
                        CondState = CONDSTATE.COND;
                        SetEnvActiveParam("Visible", true);
                    }
                    break;
                case CONDSTATE.COND:
                    if (CondHold >= ex.CondDur)
                    {
                        // Measure Display Intensity Y, CIE x, y
                        var m = spectroradiometer?.Measure("1", 6000);
                        if (m != null)
                        {
                            foreach (var f in m.Keys)
                            {
                                if (measurement.ContainsKey(f))
                                {
                                    measurement[f].Add(m[f]);
                                }
                                else
                                {
                                    measurement[f] = new List<object>() { m[f] };
                                }
                            }
                            var color = condmanager.finalcond["Color"][condmanager.condidx];
                            if (measurement.ContainsKey("Color"))
                            {
                                measurement["Color"].Add(color);
                            }
                            else
                            {
                                measurement["Color"] = new List<object>() { color };
                            }
                        }

                        CondState = CONDSTATE.SUFICI;
                        if (ex.PreICI != 0 || ex.SufICI != 0)
                        {
                            SetEnvActiveParam("Visible", false);
                        }
                    }
                    break;
                case CONDSTATE.SUFICI:
                    if (SufICIHold >= ex.SufICI)
                    {
                        CondState = CONDSTATE.NONE;
                    }
                    break;
            }
        }
    }
}