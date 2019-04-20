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
using System.Linq;
using System.Collections.Generic;
using System;
using OxyPlot;
using OxyPlot.Series;
using MathNet.Numerics;

namespace Experica
{
    public class DisplayCalibration : ExperimentLogic
    {
        protected ISpectroRadioMeter spectroradiometer;
        Dictionary<string, List<object>> measurement = new Dictionary<string, List<object>>();
        MeasurementPlot plot;

        protected override void OnStart()
        {
            spectroradiometer = new PR(ex.GetParam("COM").Convert<string>(), ex.GetParam("PRModel").Convert<string>());
        }

        protected override void OnStartExperiment()
        {
            if (string.IsNullOrEmpty(ex.Display_ID))
            {
                Extension.WarningDialog("Display_ID is not set!");
            }
            SetEnvActiveParam("Visible", false);
            spectroradiometer?.Connect(1000);
            // Setup Measurement: Primary Lens, Add On Lens 1, Add On Lens 2, Aperture, Photometric Units(1=Metric), 
            // Detector Exposure Time(1000ms), Capture Mode(0=Single Capture), Number of Measure to Average(1=No Average), 
            // Power or Energy(0=Power), Trigger Mode(0=Internal Trigger), View Shutter(0=Open), CIE Observer(0=2°)
            spectroradiometer?.Setup("S,,,,1,1000,0,1,0,0,0,0", 1000);
            plot?.Dispose();
            plot = new MeasurementPlot();
        }

        protected override void OnStopExperiment()
        {
            SetEnvActiveParam("Visible", false);
            spectroradiometer?.Close();
            if (!string.IsNullOrEmpty(ex.Display_ID) && Extension.YesNoDialog("Save Measurement to Configuration?"))
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

        void OnDestroy()
        {
            plot?.Dispose();
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
                        // Plot Measurement
                        if (ex.GetParam("PlotMeasure").Convert<bool>())
                        {
                            plot.Visualize(measurement);
                        }
                        CondState = CONDSTATE.NONE;
                    }
                    break;
            }
        }
    }

    public class MeasurementPlot : OxyPlotForm
    {
        public MeasurementPlot()
        {
            Width = 400;
            Height = 400;
            Text = "Display Measurement and Calibration";
        }

        public override void Visualize(object data)
        {
            var m = (Dictionary<string, List<object>>)data;
            if (m.Count == 0) return;
            Dictionary<string, double[]> x, y;
            m.GetRGBMeasurement(out x, out y, false, false);

            var color = new Dictionary<string, OxyColor>() { { "R", OxyColors.Red }, { "G", OxyColors.Green }, { "B", OxyColors.Blue } };
            var seriestype = new Dictionary<string, Type>() { { "R", typeof(ScatterSeries) }, { "G", typeof(ScatterSeries) }, { "B", typeof(ScatterSeries) } };
            var linewidth = new Dictionary<string, double>() { { "R", 2 }, { "G", 2 }, { "B", 2 } };
            var linestyle = new Dictionary<string, LineStyle>() { { "R", LineStyle.Automatic }, { "G", LineStyle.Automatic }, { "B", LineStyle.Automatic } };
            plotview.Visualize(x, y, null, color, seriestype, linewidth, linestyle, "", "Color Component Value", "Intensity", legendposition: LegendPosition.LeftTop);

            double rgamma, ra, rc, ggamma, ga, gc, bgamma, ba, bc;
            Extension.GammaFit(x["R"], y["R"], out rgamma, out ra, out rc);
            Extension.GammaFit(x["G"], y["G"], out ggamma, out ga, out gc);
            Extension.GammaFit(x["B"], y["B"], out bgamma, out ba, out bc);
            var xx = Generate.LinearSpaced(16, 0, 1);
            var ryys = Generate.Map(xx, i => Extension.GammaFunc(i, rgamma, ra, rc));
            var gyys = Generate.Map(xx, i => Extension.GammaFunc(i, ggamma, ga, gc));
            var byys = Generate.Map(xx, i => Extension.GammaFunc(i, bgamma, ba, bc));
            plotview.Visualize(xx, new Dictionary<string, double[]>() { { "RFit", ryys }, { "GFit", gyys }, { "BFit", byys } },
                null, new Dictionary<string, OxyColor>() { { "RFit", OxyColors.Red }, { "GFit", OxyColors.Green }, { "BFit", OxyColors.Blue } },
                new Dictionary<string, Type>() { { "RFit", typeof(LineSeries) }, { "GFit", typeof(LineSeries) }, { "BFit", typeof(LineSeries) } },
                new Dictionary<string, double>() { { "RFit", 1 }, { "GFit", 1 }, { "BFit", 1 } },
                new Dictionary<string, LineStyle>() { { "RFit", LineStyle.Solid }, { "GFit", LineStyle.Solid }, { "BFit", LineStyle.Solid } },
                isclear: false);

            var rcys = Generate.Map(xx, i => Extension.InverseGammaFunc(i, rgamma, ra, rc));
            var gcys = Generate.Map(xx, i => Extension.InverseGammaFunc(i, ggamma, ga, gc));
            var bcys = Generate.Map(xx, i => Extension.InverseGammaFunc(i, bgamma, ba, bc));
            plotview.Visualize(xx, new Dictionary<string, double[]>() { { "RCorr", rcys }, { "GCorr", gcys }, { "BCorr", bcys } },
                null, new Dictionary<string, OxyColor>() { { "RCorr", OxyColors.Red }, { "GCorr", OxyColors.Green }, { "BCorr", OxyColors.Blue } },
                new Dictionary<string, Type>() { { "RCorr", typeof(LineSeries) }, { "GCorr", typeof(LineSeries) }, { "BCorr", typeof(LineSeries) } },
                new Dictionary<string, double>() { { "RCorr", 1 }, { "GCorr", 1 }, { "BCorr", 1 } },
                new Dictionary<string, LineStyle>() { { "RCorr", LineStyle.Dash }, { "GCorr", LineStyle.Dash }, { "BCorr", LineStyle.Dash } },
                isclear: false);
        }
    }
}