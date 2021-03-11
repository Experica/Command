/*
Extension.cs is part of the Experica.
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
using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Mail;
#if COMMAND
using System.Windows.Forms;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Experica
{
    public enum CONDTESTPARAM
    {
        CondIndex,
        CondRepeat,
        TrialIndex,
        TrialRepeat,
        BlockIndex,
        BlockRepeat,
        Event,
        SyncEvent,
        CONDSTATE,
        TRIALSTATE,
        BLOCKSTATE,
        TASKSTATE
    }

    public enum DataFormat
    {
        YAML,
        EXPERICA
    }

    public static class Extension
    {
        public const uint ExDataVersion = 2;
        static Dictionary<string, Dictionary<string, List<object>>> colordata = new Dictionary<string, Dictionary<string, List<object>>>();
        static Dictionary<string, Dictionary<string, Texture2D>> imagedata = new Dictionary<string, Dictionary<string, Texture2D>>();

        static Type TObject, TString, TBool, TInt, TFloat, TDouble, TVector2, TVector3, TVector4, TColor, TListT;
        static readonly object apilock = new object();

        static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),
            typeof(int),typeof(uint),typeof(long),typeof(ulong),
            typeof(float),typeof(double),typeof(decimal)
        };

        static Extension()
        {
            TObject = typeof(object);
            TString = typeof(string);
            TBool = typeof(bool);
            TInt = typeof(int);
            TFloat = typeof(float);
            TDouble = typeof(double);
            TVector2 = typeof(Vector2);
            TVector3 = typeof(Vector3);
            TVector4 = typeof(Vector4);
            TColor = typeof(Color);
            TListT = typeof(List<>);
        }

        public static bool IsNumeric(this Type type)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
        }

        public static IList<T> AsList<T>(this object o) => o as IList<T>;
        public static IList AsList(this object o) => o as IList;

        public static T Convert<T>(this object value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static object Convert(this object value, Type CT)
        {
            lock (apilock)
            {
                if (value == null)
                {
                    return null;
                }
                Type VT = value.GetType();
                if (VT == CT)
                {
                    return value;
                }

                if (VT == TFloat)
                {
                    var v = (float)value;
                    if (CT == TString)
                    {
                        return v.ToString("G4");
                    }
                }
                else if (VT.IsGenericType && VT.GetGenericTypeDefinition() == TListT)
                {
                    if (CT.IsGenericType && CT.GetGenericTypeDefinition() == TListT)
                    {
                        var CTT = CT.GetGenericArguments()[0];
                        var v = Activator.CreateInstance(CT).AsList();
                        foreach (var i in value.AsList())
                        {
                            v.Add(i.Convert(CTT));
                        }
                        return v;
                    }
                    else if (CT == TVector3)
                    {
                        var v = value.AsList();
                        var vn = v.Count;
                        float x = 0, y = 0, z = 0;
                        if (vn > 0)
                        {
                            x = v[0].Convert<float>();
                        }
                        if (vn > 1)
                        {
                            y = v[1].Convert<float>();
                        }
                        if (vn > 2)
                        {
                            z = v[2].Convert<float>();
                        }
                        return new Vector3(x, y, z);
                    }
                    else if (CT == TColor)
                    {
                        var v = value.AsList();
                        var vn = v.Count;
                        float r = 0, g = 0, b = 0, a = 1;
                        if (vn > 0)
                        {
                            r = v[0].Convert<float>();
                        }
                        if (vn > 1)
                        {
                            g = v[1].Convert<float>();
                        }
                        if (vn > 2)
                        {
                            b = v[2].Convert<float>();
                        }
                        if (vn > 3)
                        {
                            a = v[3].Convert<float>();
                        }
                        return new Color(r, g, b, a);
                    }
                    else if (CT == TString)
                    {
                        var v = value.AsList();
                        var vn = v.Count;
                        if (vn == 0) return "[]";

                        var vs = new string[vn];
                        for (var i = 0; i < vn; i++)
                        {
                            vs[i] = v[i].Convert<string>();
                        }
                        return "[" + string.Join(", ", vs) + "]";
                    }
                }
                else if (VT == TVector2)
                {
                    var v = (Vector2)value;
                    if (CT == TString)
                    {
                        return v.x.ToString("G4") + " " + v.y.ToString("G4");
                    }
                }
                else if (VT == TVector3)
                {
                    var v = (Vector3)value;
                    if (CT == TString)
                    {
                        return String.Join(" ", Enumerable.Range(0, 3).Select(i => v[i].ToString("G4")));
                    }
                }
                else if (VT == TVector4)
                {
                    var v = (Vector4)value;
                    if (CT == TString)
                    {
                        return String.Join(" ", Enumerable.Range(0, 4).Select(i => v[i].ToString("G4")));
                    }
                }
                else if (VT == TColor)
                {
                    var v = (Color)value;
                    if (CT == TString)
                    {
                        return String.Join(" ", Enumerable.Range(0, 4).Select(i => v[i].ToString("G4")));
                    }
                }
                else if (VT == TString)
                {
                    var vstr = (string)value;
                    if (CT == TBool)
                    {
                        return bool.Parse(vstr);
                    }
                    else if (CT == TInt)
                    {
                        return int.Parse(vstr);
                    }
                    else if (CT == TFloat)
                    {
                        return float.Parse(vstr);
                    }
                    else if (CT == TDouble)
                    {
                        return double.Parse(vstr);
                    }
                    else if (CT == TVector2)
                    {
                        var vs = vstr.Split(' ');
                        return new Vector2(float.Parse(vs[0]), float.Parse(vs[1]));
                    }
                    else if (CT == TVector3)
                    {
                        var vs = vstr.Split(' ');
                        return new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
                    }
                    else if (CT == TVector4)
                    {
                        var vs = vstr.Split(' ');
                        return new Vector4(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                    }
                    else if (CT == TColor)
                    {
                        var vs = vstr.Split(' ');
                        return new Color(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                    }
                    else if (CT.IsEnum && Enum.IsDefined(CT, vstr))
                    {
                        return Enum.Parse(CT, vstr);
                    }
                    else if (CT.IsGenericType && CT.GetGenericTypeDefinition() == TListT)
                    {
                        var CTT = CT.GetGenericArguments()[0];
                        var v = Activator.CreateInstance(CT).AsList();
                        var si = vstr.IndexOf('[') + 1; var ei = vstr.LastIndexOf(']');
                        var vs = vstr.Substring(si, ei - si).Split(',').Select(i => i.Trim()).ToList();
                        foreach (var i in vs)
                        {
                            v.Add(i.Convert(CTT));
                        }
                        return v;
                    }
                }
                else
                {
                    if (CT == TString)
                    {
                        return value.ToString();
                    }
                }
                return System.Convert.ChangeType(value, CT);
            }
        }

        public static List<int> Permutation(this System.Random rng, int maxexclusive)
        {
            var seq = Enumerable.Repeat(-1, maxexclusive).ToList();
            int i, j;
            for (i = 0; i < maxexclusive; i++)
            {
                do
                {
                    j = rng.Next(maxexclusive);
                }
                while (seq[j] >= 0);
                seq[j] = i;
            }
            return seq;
        }

        public static List<T> Shuffle<T>(this System.Random rng, List<T> seq)
        {
            return rng.Permutation(seq.Count).Select(i => seq[i]).ToList();
        }

        public static void Scale01(this List<double> data)
        {
            if (data == null || data.Count < 2) return;
            var min = data.Min();
            var max = data.Max();
            var range = max - min;
            for (var i = 0; i < data.Count; i++)
            {
                data[i] = (data[i] - min) / range;
            }
        }

        //public static Dictionary<string, List<object>> ResolveConditionReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    return cond.ResolveCondFactorReference(param).ResolveCondLevelReference(param);
        //}

        ///// <summary>
        ///// Replace all factor values with known reference in experiment parameters
        ///// </summary>
        ///// <param name="cond"></param>
        ///// <param name="param"></param>
        ///// <returns></returns>
        //public static Dictionary<string, List<object>> ResolveCondFactorReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    foreach (var f in cond.Keys.ToList())
        //    {
        //        if (f.Count() > 1 && f.First() == '$')
        //        {
        //            var rf = f.Substring(1);
        //            if (param.ContainsKey(rf) && param[rf] != null && param[rf].Type.IsList())
        //            {
        //                var fl = cond[f]; fl.Clear();
        //                foreach (var i in (IEnumerable)param[rf].Value)
        //                {
        //                    fl.Add(i);
        //                }
        //                cond.Remove(f);
        //                cond[rf] = fl;
        //            }
        //        }
        //    }
        //    return cond;
        //}

        ///// <summary>
        ///// Replace factor values with known reference in experiment parameter
        ///// </summary>
        ///// <param name="cond"></param>
        ///// <param name="param"></param>
        ///// <returns></returns>
        //public static Dictionary<string, List<object>> ResolveCondLevelReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    foreach (var f in cond.Keys)
        //    {
        //        for (var i = 0; i < cond[f].Count; i++)
        //        {
        //            if (cond[f][i].GetType() == typeof(string))
        //            {
        //                var v = (string)cond[f][i];
        //                if (v.Count() > 1 && v.First() == '$')
        //                {
        //                    var r = v.Substring(1);
        //                    if (param.ContainsKey(r) && param[r] != null)
        //                    {
        //                        cond[f][i] = param[r].Value;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return cond;
        //}

#if COMMAND
        public static Dictionary<string, List<object>> FactorLevelOfDesign(this Dictionary<string, List<object>> conddesign)
        {
            foreach (var f in conddesign.Keys.ToArray())
            {
                if (conddesign[f].Count >= 5 && conddesign[f][0].GetType() == typeof(string) && (string)conddesign[f][0] == "factorleveldesign")
                {
                    var start = conddesign[f][1];
                    var end = conddesign[f][2];
                    var n = conddesign[f][3];
                    var method = conddesign[f][4].Convert<FactorLevelDesignMethod>();
                    var isortho = true;
                    if (conddesign[f].Count > 5)
                    {
                        isortho = conddesign[f][5].Convert<bool>();
                    }

                    object so, eo; int[] no;
                    if (start.GetType() == typeof(List<object>))
                    {
                        if (((List<object>)start).Count < 4)
                        {
                            so = start.Convert<Vector3>();
                            eo = end.Convert<Vector3>();
                        }
                        else
                        {
                            so = start.Convert<Color>();
                            eo = end.Convert<Color>();
                        }
                        no = ((List<object>)n).Select(i => i.Convert<int>()).ToArray();
                    }
                    else
                    {
                        so = start.Convert<float>();
                        eo = end.Convert<float>();
                        no = new int[] { n.Convert<int>() };
                    }

                    var fld = new FactorLevelDesign(f, so, eo, no, method, isortho);
                    conddesign[f] = fld.FactorLevel().Value;
                }
            }
            return conddesign;
        }

        public static string GetAddresses(this string experimenter, CommandConfig config)
        {
            string addresses = null;
            if (string.IsNullOrEmpty(experimenter)) return addresses;
            var al = experimenter.Split(',', ';').Where(i => config.ExperimenterAddress.ContainsKey(i)).Select(i => config.ExperimenterAddress[i]).ToArray();
            if (al != null && al.Length > 0)
            {
                addresses = string.Join(",", al);
            }
            return addresses;
        }

        public static ILaser GetLaser(this string lasername, CommandConfig config)
        {
            switch (lasername)
            {
                case "luxx473":
                    return new Omicron(config.SerialPort1);
                case "mambo594":
                    return new Cobolt(config.SerialPort2);
            }
            return null;
        }

        public static Assembly CompileFile(this string sourcepath)
        {
            return File.ReadAllText(sourcepath).Compile();
        }

        public static Assembly Compile(this string source)
        {
            // currently not really needed, so desable them


            //var sourcetree = CSharpSyntaxTree.ParseText(source);
            //var compilation = CSharpCompilation.Create("sdfsdf")
            //    .AddReferences()
            //    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            //                     .WithOptimizationLevel(OptimizationLevel.Release))
            //    .AddSyntaxTrees(sourcetree);
            //using (var asm = new MemoryStream())
            //{
            //    var emitresult = compilation.Emit(asm);
            //    if (emitresult.Success)
            //    {
            //        return Assembly.Load(asm.GetBuffer());
            //    }
            //}
            return null;
        }

        public static string OpenFile(string title = "Open File ...")
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = title,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "File (*.yaml;*.cs)|*.yaml;*.cs|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static string SaveFile(string title = "Save File ...")
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = title,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "File (*.yaml;*.cs)|*.yaml;*.cs|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static string ChooseDir(string title = "Choose Directory ...")
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = title;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        public static bool YesNoDialog(string msg = "Yes or No?")
        {
            if (MessageBox.Show(msg, "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                return true;
            }
            return false;
        }

        public static void WarningDialog(string msg = "This is a Warning.")
        {
            MessageBox.Show(msg, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static Display GetDisplay(this string displayid, Dictionary<string,Display> displays)
        {
            if (!string.IsNullOrEmpty(displayid) && displays != null && displays.ContainsKey(displayid))
            {
                return displays[displayid];
            }
            Debug.LogWarning($"Display ID: {displayid} can not be found.");
            return null;
        }

        public static double? DisplayLatency(this string displayid, Dictionary<string, Display> displays)
        {
            var d = displayid.GetDisplay(displays);
            if (d!=null)
            {
                if (d.Latency > 0) { return d.Latency; }
                return Math.Max(d.RiseLag, d.FallLag);
            }
            return null;
        }

        public static double GammaFunc(double x, double gamma, double a = 1, double c = 0)
        {
            return a * Math.Pow(x, gamma) + c;
        }

        public static double CounterGammaFunc(double x, double gamma, double a = 1, double c = 0)
        {
            return a * Math.Pow(x, 1 / gamma) + c;
        }

        public static bool GammaFit(double[] x, double[] y, out double gamma, out double amp, out double cons)
        {
            gamma = 0; amp = 0; cons = 0;
            try
            {
                var param = Fit.Curve(x, y, (g, a, c, i) => GammaFunc(i, g, a, c), 1, 1, 0);
                gamma = param.Item1; amp = param.Item2; cons = param.Item3;
                return true;
            }
            catch (Exception) { }
            return false;
        }

        public static bool SplineFit(double[] x, double[] y, out IInterpolation spline, DisplayFitType fittype = DisplayFitType.LinearSpline)
        {
            spline = null;
            try
            {
                switch (fittype)
                {
                    case DisplayFitType.LinearSpline:
                        spline = Interpolate.Linear(x, y);
                        return true;
                    case DisplayFitType.CubicSpline:
                        spline = Interpolate.CubicSpline(x, y);
                        return true;
                }
                return false;
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Get Independent R,G,B channel measurement
        /// </summary>
        /// <param name="m"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isnormalize"></param>
        /// <param name="issort"></param>
        public static void GetRGBIntensityMeasurement(this Dictionary<string, List<object>> m, out Dictionary<string, double[]> x, out Dictionary<string, double[]> y, bool isnormalize = false, bool issort = false)
        {
            var colors = m["Color"].Convert<List<Color>>();
            var intensities = m["Y"].Convert<List<double>>();

            var rs = new List<double>(); var gs = new List<double>(); var bs = new List<double>();
            var rys = new List<double>(); var gys = new List<double>(); var bys = new List<double>();
            for (var j = 0; j < colors.Count; j++)
            {
                var c = colors[j]; var i = intensities[j];
                if (c.r == 0 && c.g == 0 && c.b == 0)
                {
                    rs.Add(c.r);
                    rys.Add(i);
                    gs.Add(c.g);
                    gys.Add(i);
                    bs.Add(c.b);
                    bys.Add(i);
                }
                else
                {
                    if (c.g == 0 && c.b == 0)
                    {
                        rs.Add(c.r);
                        rys.Add(i);
                    }
                    if (c.r == 0 && c.b == 0)
                    {
                        gs.Add(c.g);
                        gys.Add(i);
                    }
                    if (c.r == 0 && c.g == 0)
                    {
                        bs.Add(c.b);
                        bys.Add(i);
                    }
                }
            }
            if (issort)
            {
                Sorting.Sort(rs, rys); Sorting.Sort(gs, gys); Sorting.Sort(bs, bys);
            }
            if (isnormalize)
            {
                rys.Scale01(); gys.Scale01(); bys.Scale01();
            }
            x = new Dictionary<string, double[]>() { { "R", rs.ToArray() }, { "G", gs.ToArray() }, { "B", bs.ToArray() } };
            y = new Dictionary<string, double[]>() { { "R", rys.ToArray() }, { "G", gys.ToArray() }, { "B", bys.ToArray() } };
        }

        public static void GetRGBSpectralMeasurement(this Dictionary<string, List<object>> m, out Dictionary<string, double[]> x, out Dictionary<string, double[][]> yi, out Dictionary<string, double[][]> y)
        {
            var colors = m["Color"].Convert<List<Color>>();
            var wls = m["WL"].Convert<List<double[]>>();
            var wlis = m["Spectral"].Convert<List<double[]>>();

            var rs = new List<double>(); var gs = new List<double>(); var bs = new List<double>();
            var rwls = new List<double[]>(); var gwls = new List<double[]>(); var bwls = new List<double[]>();
            var rwlis = new List<double[]>(); var gwlis = new List<double[]>(); var bwlis = new List<double[]>();
            for (var j = 0; j < colors.Count; j++)
            {
                var c = colors[j]; var wl = wls[j]; var wli = wlis[j];
                if (c.r == 0 && c.g == 0 && c.b == 0)
                {
                    rs.Add(c.r);
                    rwls.Add(wl);
                    rwlis.Add(wli);
                    gs.Add(c.g);
                    gwls.Add(wl);
                    gwlis.Add(wli);
                    bs.Add(c.b);
                    bwls.Add(wl);
                    bwlis.Add(wli);
                }
                else
                {
                    if (c.g == 0 && c.b == 0)
                    {
                        rs.Add(c.r);
                        rwls.Add(wl);
                        rwlis.Add(wli);
                    }
                    if (c.r == 0 && c.b == 0)
                    {
                        gs.Add(c.g);
                        gwls.Add(wl);
                        gwlis.Add(wli);
                    }
                    if (c.r == 0 && c.g == 0)
                    {
                        bs.Add(c.b);
                        bwls.Add(wl);
                        bwlis.Add(wli);
                    }
                }
            }
            x = new Dictionary<string, double[]>() { { "R", rs.ToArray() }, { "G", gs.ToArray() }, { "B", bs.ToArray() } };
            yi = new Dictionary<string, double[][]> { { "R", rwls.ToArray() }, { "G", gwls.ToArray() }, { "B", bwls.ToArray() } };
            y = new Dictionary<string, double[][]>() { { "R", rwlis.ToArray() }, { "G", gwlis.ToArray() }, { "B", bwlis.ToArray() } };
        }

        public static Texture3D GenerateRGBGammaCLUT(double rgamma, double ggamma, double bgamma, double ra, double ga, double ba, double rc, double gc, double bc, int n)
        {
            var xx = Generate.LinearSpaced(n, 0, 1);
            var riy = Generate.Map(xx, i => (float)CounterGammaFunc(i, rgamma,ra,rc));
            var giy = Generate.Map(xx, i => (float)CounterGammaFunc(i, ggamma,ga,gc));
            var biy = Generate.Map(xx, i => (float)CounterGammaFunc(i, bgamma,ba,bc));

            var clut = new Texture3D(n, n, n, TextureFormat.RGB24, false);
            for (var r = 0; r < n; r++)
            {
                for (var g = 0; g < n; g++)
                {
                    for (var b = 0; b < n; b++)
                    {
                        clut.SetPixel(r, g, b, new Color(riy[r], giy[g], biy[b]));
                    }
                }
            }
            clut.Apply();
            return clut;
        }

        public static Texture3D GenerateRGBSplineCLUT(IInterpolation rii, IInterpolation gii, IInterpolation bii, int n)
        {
            var xx = Generate.LinearSpaced(n, 0, 1);
            var riy = Generate.Map(xx, i => (float)rii.Interpolate(i));
            var giy = Generate.Map(xx, i => (float)gii.Interpolate(i));
            var biy = Generate.Map(xx, i => (float)bii.Interpolate(i));

            var clut = new Texture3D(n, n, n, TextureFormat.RGB24, false);
            for (var r = 0; r < n; r++)
            {
                for (var g = 0; g < n; g++)
                {
                    for (var b = 0; b < n; b++)
                    {
                        clut.SetPixel(r, g, b, new Color(riy[r], giy[g], biy[b]));
                    }
                }
            }
            clut.Apply();
            return clut;
        }

        /// <summary>
        /// Prepare Color Look-Up Table based on display R,G,B intensity measurement
        /// </summary>
        /// <param name="display"></param>
        /// <param name="forceprepare"></param>
        /// <returns></returns>
        public static bool PrepareCLUT(this Display display, bool forceprepare = false)
        {
            if (display.CLUT != null && !forceprepare) { return true; }
            var m = display.IntensityMeasurement;
            if (m == null || m.Count == 0) { return false; }

            Dictionary<string, double[]> x, y;
            switch (display.FitType)
            {
                case DisplayFitType.Gamma:
                    m.GetRGBIntensityMeasurement(out x, out y, false, true);
                    double rgamma, ra, rc, ggamma, ga, gc, bgamma, ba, bc;
                    GammaFit(x["R"], y["R"], out rgamma, out ra, out rc);
                    GammaFit(x["G"], y["G"], out ggamma, out ga, out gc);
                    GammaFit(x["B"], y["B"], out bgamma, out ba, out bc);
                    display.CLUT = GenerateRGBGammaCLUT(rgamma, ggamma, bgamma,ra,ga,ba,rc,gc,bc, display.CLUTSize);
                    break;
                case DisplayFitType.LinearSpline:
                case DisplayFitType.CubicSpline:
                    m.GetRGBIntensityMeasurement(out x, out y, true, true);
                    IInterpolation rii, gii, bii;
                    SplineFit(y["R"], x["R"], out rii, display.FitType);
                    SplineFit(y["G"], x["G"], out gii, display.FitType);
                    SplineFit(y["B"], x["B"], out bii, display.FitType);
                    if (rii != null && gii != null && bii != null)
                    {
                        display.CLUT = GenerateRGBSplineCLUT(rii, gii, bii, display.CLUTSize);
                    }
                    break;
            }
            return display.CLUT == null ? false : true;
        }
#endif

        public static Dictionary<string, List<object>> OrthoCondOfFactorLevel(this Dictionary<string, List<object>> fsls)
        {
            foreach (var f in fsls.Keys.ToArray())
            {
                if (fsls[f].Count == 0)
                {
                    fsls.Remove(f);
                }
            }

            var fs = fsls.Keys.ToArray();
            var fn = fs.Length;
            if (fn > 1)
            {
                var cond = new Dictionary<string, List<object>>();
                int[] irn = new int[fn];
                int[] ln = new int[fn];
                irn[0] = 1;
                int cn = 1;
                for (var i = 0; i < fn; i++)
                {
                    var n = fsls[fs[i]].Count;
                    ln[i] = n;
                    cn *= n;
                    if (i > 0)
                    {
                        irn[i] = ln[i - 1] * irn[i - 1];
                    }
                }

                for (var fi = 0; fi < fn; fi++)
                {
                    List<object> ir = new List<object>();
                    for (var l = 0; l < ln[fi]; l++)
                    {
                        for (var r = 0; r < irn[fi]; r++)
                        {
                            ir.Add(fsls[fs[fi]][l]);
                        }
                    }
                    var orn = cn / ir.Count;
                    List<object> or = new List<object>();
                    for (var r = 0; r < orn; r++)
                    {
                        or.AddRange(ir);
                    }
                    cond[fs[fi]] = or;
                }
                return cond;
            }
            else
            {
                return fsls;
            }
        }

        public static Type GetFactorValueType(this string factorname)
        {
            switch (factorname)
            {
                case "Luminance":
                case "Contrast":
                case "Diameter":
                case "SpatialFreq":
                case "SpatialPhase":
                case "TemporalFreq":
                case "Ori":
                case "OriOffset":
                case "Ori_Final":
                case "Speed":
                    return typeof(float);
                case "Rotation":
                case "RotationOffset":
                case "Rotation_Final":
                case "Position":
                case "PositionOffset":
                case "Position_Final":
                    return typeof(Vector3);
                case "Color":
                case "BGColor":
                    return typeof(Color);
                default:
                    return null;
            }
        }

        public static Dictionary<string, IList> FinalizeFactorValues(this Dictionary<string, List<object>> cond)
        {
            if (cond == null) return null;
            var final = new Dictionary<string, IList>();
            foreach (var f in cond.Keys.ToArray())
            {
                var fvt = f.GetFactorValueType() ?? cond[f][0].GetType();
                var fvs = Activator.CreateInstance(typeof(List<>).MakeGenericType(fvt)).AsList();
                cond[f].ForEach(i => fvs.Add(i.Convert(fvt)));
                final[f] = fvs;
            }
            return final;
        }

        public static void FirstSplit(this string name, out string head, out string tail, string del = "@")
        {
            head = null; tail = null;
            if (!string.IsNullOrEmpty(name))
            {
                var n = del.Length;
                var i = name.IndexOf(del);
                if (i == 0)
                {
                    tail = name.Substring(n);
                }
                else if (i > 0)
                {
                    head = name.Substring(0, i);
                    tail = name.Substring(i + n);
                }
            }
        }

        public static string FirstSplitHead(this string name, string del = "@")
        {
            name.FirstSplit(out string head, out _, del);
            return head;
        }

        public static string FirstSplitTail(this string name, string del = "@")
        {
            name.FirstSplit(out _, out string tail, del);
            return tail;
        }

        public static bool IsEnvParamFullName(this string name)
        {
            return name.IsEnvParamFullName(out _, out _, out _);
        }

        public static bool IsEnvParamFullName(this string name, out string varname, out string nbsoname, out string fullname)
        {
            name.FirstSplit(out varname, out nbsoname, "@");
            var t = !string.IsNullOrEmpty(varname) && !string.IsNullOrEmpty(nbsoname) && nbsoname.Length >= 3; // shortest nbso name: {nb}@{so}
            fullname = t ? name : null;
            return t;
        }

        public static void LastSplit(this string name, out string head, out string tail, string del = "@")
        {
            head = null; tail = null;
            if (!string.IsNullOrEmpty(name))
            {
                var n = del.Length;
                var i = name.LastIndexOf(del);
                if (i == 0)
                {
                    tail = name.Substring(n);
                }
                else if (i > 0)
                {
                    head = name.Substring(0, i);
                    tail = name.Substring(i + n);
                }
            }
        }

        public static string LastSplitHead(this string name, string del = "@")
        {
            name.LastSplit(out string head, out _, del);
            return head;
        }

        public static string LastSplitTail(this string name, string del = "@")
        {
            name.LastSplit(out _, out string tail, del);
            return tail;
        }

        public static List<string> GetValue(this Type T)
        {
            if (T.IsEnum)
            {
                return Enum.GetNames(T).ToList();
            }
            else if (T == typeof(bool))
            {
                return new List<string> { "True", "False" };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Luminance span based on average luminance and michelson contrast(symmatric min and max luminance)
        /// </summary>
        /// <param name="luminance"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        public static float LuminanceSpan(float luminance, float contrast)
        {
            return 2 * luminance * contrast;
        }

        /// <summary>
        /// Symmatric scale between mincolor and maxcolor
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="minc"></param>
        /// <param name="maxc"></param>
        /// <param name="sminc"></param>
        /// <param name="smaxc"></param>
        public static void ScaleColor(this float scale, Color minc, Color maxc, out Color sminc, out Color smaxc)
        {
            var mc = (minc + maxc) / 2;
            var dmc = maxc - mc;
            sminc = new Color(mc.r - dmc.r * scale, mc.g - dmc.g * scale, mc.b - dmc.b * scale, minc.a);
            smaxc = new Color(mc.r + dmc.r * scale, mc.g + dmc.g * scale, mc.b + dmc.b * scale, maxc.a);
        }

        public static Vector3 RotateZCCW(this Vector3 v, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v;
        }

        public static string[] ValidStrings(params string[] ss)
        {
            var r = new List<string>();
            if (ss.Length > 0)
            {
                foreach (var s in ss)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        r.Add(s);
                    }
                }
            }
            return r.ToArray();
        }

        public static bool IsFollowEnvCrossInheritRule(this Dictionary<string, Dictionary<string, List<string>>> rule, string to, string from, string param)
        {
            if (rule.ContainsKey(to))
            {
                var fp = rule[to];
                if (fp.ContainsKey(from) && fp[from].Contains(param))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsEnvCrossInheritTo(this Dictionary<string, Dictionary<string, List<string>>> rule, string to)
        {
            return rule.ContainsKey(to);
        }

        public static void Mail(this string to, string subject, string body)
        {
            if (string.IsNullOrEmpty(to)) return;
            var smtp = new SmtpClient() { Host = "smtp.gmail.com", Port = 587, EnableSsl = true, Credentials = new NetworkCredential("vlabsys@gmail.com", "VLab$y$tem") };
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            smtp.Send("vlabsys@gmail.com", to, subject, body);
        }

        /// <summary>
        /// Load textures from a AssetBundle
        /// </summary>
        /// <param name="imagesetname"></param>
        /// <returns></returns>
        public static Dictionary<string, Texture2D> LoadTextures(this string imagesetname)
        {
            if (string.IsNullOrEmpty(imagesetname)) return null;
            var file = Path.Combine(UnityEngine.Application.streamingAssetsPath, imagesetname);
            if (File.Exists(file))
            {
                var isab = AssetBundle.LoadFromFile(file);
                var ins = isab.GetAllAssetNames().Select(i => Path.GetFileNameWithoutExtension(i));
                if (ins != null && ins.Count() > 0)
                {
                    var imgset = new Dictionary<string, Texture2D>();
                    foreach (var n in ins)
                    {
                        imgset[n] = isab.LoadAsset<Texture2D>(n);
                    }
                    return imgset;
                }
                else
                {
                    Debug.LogWarning($"Image Data: {file} Empty.");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"Image Data: {file} Not Found.");
                return null;
            }
        }

        public static Dictionary<string, Texture2D> FillRawTextures8(this Dictionary<string, List<List<Byte>>> imgdata)
        {
            if (imgdata == null) return null;

            var imgsize = imgdata["imagesize"];
            var h = (int)imgsize[0][0]; var w = (int)imgsize[1][0];
            var imgs = imgdata["images"];
            var imgset = new Dictionary<string, Texture2D>();
            for (var i = 0; i < imgs.Count(); i++)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                var ps = t.GetRawTextureData<Color32>();
                for (var j = 0; j < imgs[i].Count(); j++)
                {
                    var c = imgs[i][j];
                    ps[j] = new Color32(c, c, c, 255);
                }
                t.Apply();
                imgset[(i + 1).ToString()] = t;
            }
            return imgset;
        }

        public static Dictionary<string, Texture2D> FillRawTextures32(this Dictionary<string, List<List<UInt32>>> imgdata)
        {
            if (imgdata == null) return null;

            var imgsize = imgdata["imagesize"];
            var h = (int)imgsize[0][0]; var w = (int)imgsize[1][0];
            var imgs = imgdata["images"];
            var imgset = new Dictionary<string, Texture2D>();
            for (var i = 0; i < imgs.Count(); i++)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                var ps = t.GetRawTextureData<Color32>();
                for (var j = 0; j < imgs[i].Count(); j++)
                {
                    var c = BitConverter.GetBytes(imgs[i][j]);
                    ps[j] = new Color32(c[3], c[2], c[1], c[0]);
                }
                t.Apply();
                imgset[(i + 1).ToString()] = t;
            }
            return imgset;
        }

        /// <summary>
        /// Load raw textures from a file
        /// </summary>
        /// <param name="imagesetname"></param>
        /// <returns></returns>
        public static Dictionary<string, Texture2D> LoadRawTextures(this string imagesetname)
        {
            if (string.IsNullOrEmpty(imagesetname)) return null;
            var files = Directory.GetFiles("Data", imagesetname + ".*", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                var file = files[0]; var ext = Path.GetExtension(file);
                switch (ext)
                {
                    case ".mp8":
                        return MsgPack.ImageSet8Serializer.Unpack(File.OpenRead(file)).FillRawTextures8();
                    case ".mp32":
                        return MsgPack.ImageSet32Serializer.Unpack(File.OpenRead(file)).FillRawTextures32();
                    case ".yaml":
                        return Yaml.ReadYamlFile<Dictionary<string, List<List<UInt32>>>>(file).FillRawTextures32();
                }
                return null;
            }
            else
            {
                Debug.LogWarning($"Image Data: {Path.Combine("Data", imagesetname)} Not Found.");
                return null;
            }
        }

        public static Dictionary<string, Texture2D> Load(this string imageset, int startidx = 0, int numofimg = 10)
        {
            if (string.IsNullOrEmpty(imageset)) return null;
            var imgs = new Dictionary<string, Texture2D>();

            //Addressables.LoadAssetsAsync

            for (var i = startidx; i < numofimg + startidx; i++)
            {
                var img = Resources.Load<Texture2D>(imageset + "/" + i);
                if (img != null)
                {
                    imgs[i.ToString()] = img;
                }
            }
            return imgs;
        }

        public static Texture2DArray LoadImageSet(this string imgsetdir, int startidx = 0, int numofimg = 10, bool forcereload = false)
        {
            if (string.IsNullOrEmpty(imgsetdir)) return null;
            Texture2DArray imgarray;
            if (!forcereload)
            {
                imgarray = Resources.Load<Texture2DArray>(imgsetdir + ".asset");
                if (imgarray != null) return imgarray;
            }
            var img = Resources.Load<Texture2D>(imgsetdir + "/" + startidx);
            if (img == null) return null;

            imgarray = new Texture2DArray(img.width, img.height, numofimg + startidx, img.format, false);
            imgarray.SetPixels(img.GetPixels(), startidx);
            for (var i = startidx + 1; i < numofimg + startidx; i++)
            {
                img = Resources.Load<Texture2D>(imgsetdir + "/" + i);
                if (img != null)
                {
                    imgarray.SetPixels(img.GetPixels(), i);
                }
            }
            imgarray.Apply();
            return imgarray;
        }

        public static Dictionary<string, List<object>> GetColorData(this string Display_ID, bool forceload = false)
        {
            if (!forceload && colordata.ContainsKey(Display_ID))
            {
                return colordata[Display_ID];
            }
            var file = Path.Combine("Data", Display_ID, "colordata.yaml");
            if (!File.Exists(file))
            {
                // generate colordata
            }
            if (File.Exists(file))
            {
                var data = Yaml.ReadYamlFile<Dictionary<string, List<object>>>(file);
                colordata[Display_ID] = data;
                return data;
            }
            else
            {
                Debug.LogWarning($"Color Data: {file} Not Found.");
                return null;
            }
        }

        public static Dictionary<string, Texture2D> GetImageData(this string imagesetname, bool forceload = false)
        {
            if (!forceload && imagedata.ContainsKey(imagesetname))
            {
                return imagedata[imagesetname];
            }
            var imgset = imagesetname.LoadTextures();
            if (imgset != null)
            {
                imagedata[imagesetname] = imgset;
                return imgset;
            }
            imgset = imagesetname.LoadRawTextures();
            if (imgset != null)
            {
                imagedata[imagesetname] = imgset;
                return imgset;
            }
            return null;
        }
    }
}