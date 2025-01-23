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
using MathNet.Numerics.Interpolation;
using Experica;
using Experica.Command;
using Experica.NetEnv;
using Display = Experica.NetEnv.Display;

/// <summary>
/// Present R,G,B colors and measure intensities, so that a color lookup table can
/// be constructed to linearize R,G,B intensity.
/// 
/// The linearized R,G,B color spectrals can be measured, so that Cone Excitations
/// can be calculated using the spectrals and Cone Fundamentals.
/// 
/// Required experiment parameters:
/// 
/// PRModel:        model name of the spectroradiometer from Photo Research, Inc. (e.g., PR701)
/// COM:            COM port of the spectroradiometer. (e.g., COM1)
/// Measure:        type of measurement. {"Spectral", "Intensity"}
/// N:              number of R,G,B levels to measure
/// PlotMeasure:    if plot measurement. (Bool)
/// FitType:        function used to fit the intensity measurement. {"Gamma", "LinearSpline", "CubicSpline"}
/// 
/// 
/// 
/// Interactive measurement can be enabled throught `Input`.
/// 
/// Function1Action: initailize spectroradiometer
/// Function2Action: measure
/// </summary>
public class DisplayCalibration : ExperimentLogic
{
    protected ISpectroRadioMeter spectroradiometer;
    Dictionary<string, List<object>> imeasurement;
    Dictionary<string, List<object>> smeasurement;
    IntensityMeasurementPlot iplot; SpectralMeasurementPlot splot;

    protected override void OnStart()
    {
        spectroradiometer = new PR(GetExParam<string>("COM"), GetExParam<string>("PRModel"));
    }

    bool PrepareSpectroradiometer()
    {
        spectroradiometer?.Close();
        /* Setup Measurement: 
           Primary Lens, AddOn Lens 1, AddOn Lens 2, Aperture, Photometric Units(1=Metric), 
           Detector Exposure Time(1000ms), Capture Mode(0=Single Capture), Number of Measure to Average(1=No Average), 
           Power or Energy(0=Power), Trigger Mode(0=Internal Trigger), View Shutter(0=Open), CIE Observer(0=2°)
        */
        var hr = (spectroradiometer?.Connect(1000) ?? false) && (spectroradiometer?.Setup("S,,,,1,1000,0,1,0,0,0,0", 1000) ?? false);
        if (hr)
        {
            Debug.Log("Spectroradiometer Ready.");
        }
        else
        {
            Debug.LogWarning("Spectroradiometer Initailization Failed.");
        }
        return hr;
    }

    protected override void OnStartExperiment()
    {
        if (string.IsNullOrEmpty(ex.Display_ID))
        {
            Base.WarningDialog("Display_ID is not set!");
        }
        SetEnvActiveParam("Visible", false);
        PrepareSpectroradiometer();
        switch (GetExParam<string>("Measure"))
        {
            case "Intensity":
                imeasurement = new Dictionary<string, List<object>>();
                break;
            case "Spectral":
                smeasurement = new Dictionary<string, List<object>>();
                break;
        }
        iplot?.Dispose();
        iplot = new IntensityMeasurementPlot();
        splot?.Dispose();
        splot = new SpectralMeasurementPlot();
    }

    protected override void OnExperimentStopped()
    {
        SetEnvActiveParam("Visible", false);
        spectroradiometer?.Close();
        if (Config.Display == null)
        {
            Config.Display = new Dictionary<string, Display>();
        }
        if (string.IsNullOrEmpty(ex.Display_ID))
        {
            Base.WarningDialog("Display_ID is not set!");
            return;
        }

        if (Base.YesNoDialog("Save Measurement to Configuration?"))
        {
            if (Config.Display.ContainsKey(ex.Display_ID))
            {
                if (imeasurement != null && imeasurement.Count > 0)
                {
                    Config.Display[ex.Display_ID].IntensityMeasurement = imeasurement;
                }
                if (smeasurement != null && smeasurement.Count > 0)
                {
                    Config.Display[ex.Display_ID].SpectralMeasurement = smeasurement;
                }
            }
            else
            {
                Config.Display[ex.Display_ID] = new Display() { ID = ex.Display_ID, IntensityMeasurement = imeasurement, SpectralMeasurement = smeasurement };
            }
        }
        if (Base.YesNoDialog("Save Measurement Data?"))
        {
            var path = Base.SaveFile("Save Measurement Data ...");
            if (!string.IsNullOrEmpty(path))
            {
                var ds = new Dictionary<string, Display>(Config.Display)
                {
                    [ex.Display_ID] = new Display() { ID = ex.Display_ID, IntensityMeasurement = imeasurement, SpectralMeasurement = smeasurement }
                };
                path.WriteYamlFile(ds);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        spectroradiometer?.Dispose();
        iplot?.Dispose();
        splot?.Dispose();
    }

    protected override void PrepareCondition()
    {
        var n = GetExParam("N");
        var cond = $"Color: [FactorDesign, {{Start: [0, 0, 0, 1], Stop: [1, 1, 1, 1], N: [{n}, {n}, {n}, 0], Method: Linear, OrthoCombine: False]}}";
        condmgr.PrepareCondition(cond.DeserializeYaml<Dictionary<string, List<object>>>().ProcessFactorDesign());
    }

    protected override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.NONE:
                if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold >= ex.PreICI)
                {
                    EnterCondState(CONDSTATE.COND);
                    SetEnvActiveParam("Visible", true);
                }
                break;
            case CONDSTATE.COND:
                if (CondHold >= ex.CondDur)
                {
                    // Make Measurement
                    switch (GetExParam<string>("Measure"))
                    {
                        case "Intensity":
                            // Measure Intensity Y, CIE x, y
                            var m1 = spectroradiometer?.Measure("1", 8000) as Dictionary<string, double>;
                            if (m1 != null)
                            {
                                foreach (var f in m1.Keys.Where(i => i == "Y"))
                                {
                                    if (imeasurement.ContainsKey(f))
                                    {
                                        imeasurement[f].Add(m1[f]);
                                    }
                                    else
                                    {
                                        imeasurement[f] = new List<object>() { m1[f] };
                                    }
                                }
                                var color = condmgr.Cond["Color"][condmgr.CondIndex];
                                if (imeasurement.ContainsKey("Color"))
                                {
                                    imeasurement["Color"].Add(color);
                                }
                                else
                                {
                                    imeasurement["Color"] = new List<object>() { color };
                                }
                            }
                            break;
                        case "Spectral":
                            // Measure Peak λ, Integrated Spectral, Integrated Photon, λs, λ Intensities
                            var m5 = spectroradiometer?.Measure("5", 10000) as Dictionary<string, object>;
                            if (m5 != null)
                            {
                                foreach (var f in m5.Keys.Where(i => i == "WL" || i == "Spectral"))
                                {
                                    if (smeasurement.ContainsKey(f))
                                    {
                                        smeasurement[f].Add(m5[f]);
                                    }
                                    else
                                    {
                                        smeasurement[f] = new List<object>() { m5[f] };
                                    }
                                }
                                var color = condmgr.Cond["Color"][condmgr.CondIndex];
                                if (smeasurement.ContainsKey("Color"))
                                {
                                    smeasurement["Color"].Add(color);
                                }
                                else
                                {
                                    smeasurement["Color"] = new List<object>() { color };
                                }
                            }
                            break;
                    }

                    EnterCondState(CONDSTATE.SUFICI);
                    if (ex.PreICI > 0 || ex.SufICI > 0)
                    {
                        SetEnvActiveParam("Visible", false);
                    }
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold >= ex.SufICI)
                {
                    // Update Measurement Plot
                    if (GetExParam<bool>("PlotMeasure"))
                    {
                        switch (GetExParam<string>("Measure"))
                        {
                            case "Intensity":
                                iplot?.Visualize(imeasurement, GetExParam("FitType"));
                                break;
                            case "Spectral":
                                splot?.Visualize(smeasurement);
                                break;
                        }
                    }

                    if (EnterCondState(CONDSTATE.PREICI) == EnterStateCode.ExFinish) { return; }
                }
                break;
        }
    }

    //public override void OnFunction1Action()
    //{
    //    if (ex.Input)
    //    {
    //        PrepareSpectroradiometer();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("Input Disabled.");
    //    }
    //}

    //public override void OnFunction2Action()
    //{
    //    if (ex.Input)
    //    {
    //        var measuretype = GetExParam<string>("Measure");
    //        switch (measuretype)
    //        {
    //            case "Intensity":
    //                // Measure Intensity Y, CIE x, y
    //                if (spectroradiometer?.Measure("1", 8000) is Dictionary<string, double> m1)
    //                {
    //                    Debug.Log($"x: {m1["x"]},    y: {m1["y"]},    Y: {m1["Y"]}");
    //                }
    //                else
    //                {
    //                    Debug.Log("No Measurement Data.");
    //                }
    //                break;
    //            default:
    //                Debug.LogWarning($"{measuretype} Measurement Not Implemented.");
    //                break;
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("Input Disabled.");
    //    }
    //}
}

public class IntensityMeasurementPlot : OxyPlotForm
{
    public IntensityMeasurementPlot()
    {
        Width = 400;
        Height = 400;
        Text = "Display Intensity and Calibration";
    }

    public override void Visualize(params object[] data)
    {
        var m = (Dictionary<string, List<object>>)data[0];
        var fittype = data[1].Convert<DisplayFitType>();
        if (m.Count == 0) return;
        Dictionary<string, double[]> x = null, y = null;
        var xx = Generate.LinearSpaced(100, 0, 1);
        double[] ryy = xx, gyy = xx, byy = xx, riy = xx, giy = xx, biy = xx;
        switch (fittype)
        {
            case DisplayFitType.Gamma:
                m.GetRGBIntensityMeasurement(out x, out y, false, true);
                double rgamma, ra, rc, ggamma, ga, gc, bgamma, ba, bc;
                Base.GammaFit(x["R"], y["R"], out rgamma, out ra, out rc);
                Base.GammaFit(x["G"], y["G"], out ggamma, out ga, out gc);
                Base.GammaFit(x["B"], y["B"], out bgamma, out ba, out bc);

                ryy = Generate.Map(xx, i => Base.GammaFunc(i, rgamma, ra, rc));
                gyy = Generate.Map(xx, i => Base.GammaFunc(i, ggamma, ga, gc));
                byy = Generate.Map(xx, i => Base.GammaFunc(i, bgamma, ba, bc));
                riy = Generate.Map(xx, i => Base.CounterGammaFunc(i, rgamma, ra, rc));
                giy = Generate.Map(xx, i => Base.CounterGammaFunc(i, ggamma, ga, gc));
                biy = Generate.Map(xx, i => Base.CounterGammaFunc(i, bgamma, ba, bc));
                break;
            case DisplayFitType.LinearSpline:
            case DisplayFitType.CubicSpline:
                m.GetRGBIntensityMeasurement(out x, out y, true, true);
                IInterpolation ri, gi, bi;
                if (Base.SplineFit(x["R"], y["R"], out ri, fittype))
                {
                    ryy = Generate.Map(xx, i => ri.Interpolate(i));
                }
                if (Base.SplineFit(x["G"], y["G"], out gi, fittype))
                {
                    gyy = Generate.Map(xx, i => gi.Interpolate(i));
                }
                if (Base.SplineFit(x["B"], y["B"], out bi, fittype))
                {
                    byy = Generate.Map(xx, i => bi.Interpolate(i));
                }
                IInterpolation rii, gii, bii;
                if (Base.SplineFit(y["R"], x["R"], out rii, fittype))
                {
                    riy = Generate.Map(xx, i => rii.Interpolate(i));
                }
                if (Base.SplineFit(y["G"], x["G"], out gii, fittype))
                {
                    giy = Generate.Map(xx, i => gii.Interpolate(i));
                }
                if (Base.SplineFit(y["B"], x["B"], out bii, fittype))
                {
                    biy = Generate.Map(xx, i => bii.Interpolate(i));
                }
                break;
        }
        plotview.Visualize(x, y, null, new Dictionary<string, OxyColor>() { { "R", OxyColors.Red }, { "G", OxyColors.Green }, { "B", OxyColors.Blue } },
            new Dictionary<string, Type>() { { "R", typeof(ScatterSeries) }, { "G", typeof(ScatterSeries) }, { "B", typeof(ScatterSeries) } },
            new Dictionary<string, double>() { { "R", 2 }, { "G", 2 }, { "B", 2 } },
            new Dictionary<string, LineStyle>() { { "R", LineStyle.Automatic }, { "G", LineStyle.Automatic }, { "B", LineStyle.Automatic } },
            "", "Color Component Value", "Intensity (cd/m2)", legendposition: LegendPosition.LeftTop);
        plotview.Visualize(xx, new Dictionary<string, double[]>() { { "RFit", ryy }, { "GFit", gyy }, { "BFit", byy } },
            null, new Dictionary<string, OxyColor>() { { "RFit", OxyColors.Red }, { "GFit", OxyColors.Green }, { "BFit", OxyColors.Blue } },
            new Dictionary<string, Type>() { { "RFit", typeof(LineSeries) }, { "GFit", typeof(LineSeries) }, { "BFit", typeof(LineSeries) } },
            new Dictionary<string, double>() { { "RFit", 1 }, { "GFit", 1 }, { "BFit", 1 } },
            new Dictionary<string, LineStyle>() { { "RFit", LineStyle.Solid }, { "GFit", LineStyle.Solid }, { "BFit", LineStyle.Solid } },
            isclear: false);
        plotview.Visualize(xx, new Dictionary<string, double[]>() { { "RCorr", riy }, { "GCorr", giy }, { "BCorr", biy } },
            null, new Dictionary<string, OxyColor>() { { "RCorr", OxyColors.Red }, { "GCorr", OxyColors.Green }, { "BCorr", OxyColors.Blue } },
            new Dictionary<string, Type>() { { "RCorr", typeof(LineSeries) }, { "GCorr", typeof(LineSeries) }, { "BCorr", typeof(LineSeries) } },
            new Dictionary<string, double>() { { "RCorr", 1 }, { "GCorr", 1 }, { "BCorr", 1 } },
            new Dictionary<string, LineStyle>() { { "RCorr", LineStyle.Dash }, { "GCorr", LineStyle.Dash }, { "BCorr", LineStyle.Dash } },
            isclear: false);
    }
}

public class SpectralMeasurementPlot : OxyPlotForm
{
    public SpectralMeasurementPlot()
    {
        Width = 400;
        Height = 400;
        Text = "Display Spectral";
    }

    public override void Visualize(params object[] data)
    {
        var m = (Dictionary<string, List<object>>)data[0];
        if (m.Count == 0) return;
        Dictionary<string, double[]> x = null;
        Dictionary<string, double[][]> yi = null;
        Dictionary<string, double[][]> y = null;
        m.GetRGBSpectralMeasurement(out x, out yi, out y);

        plotview.Model.IsLegendVisible = false;
        plotview.Clear();
        foreach (var f in x.Keys)
        {
            OxyColor c = OxyColors.Black;
            switch (f)
            {
                case "R":
                    c = OxyColors.Red;
                    break;
                case "G":
                    c = OxyColors.Green;
                    break;
                case "B":
                    c = OxyColors.Blue;
                    break;
            }
            for (var i = 0; i < yi[f].Length; i++)
            {
                var color = OxyColor.FromAColor((byte)(x[f][i] * 127.5 + 127.5), c);
                plotview.Visualize(yi[f][i], y[f][i], null, color, typeof(LineSeries), 1, LineStyle.Solid,
            "", "Wavelength (nm)", "Intensity (cd/m2)", isclear: false);
            }

        }
    }
}