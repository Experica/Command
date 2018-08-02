/*
VLExtention.cs is part of the VLAB project.
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
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace VLab
{
    public enum CONDTESTPARAM
    {
        CondIndex,
        CondRepeat,
        BlockIndex,
        BlockRepeat,
        Event,
        SyncEvent,
        CONDSTATE,
        TRIALSTATE,
        BLOCKSTATE,
        TASKSTATE
    }

    public static class VLExtention
    {
        static Type TObject, TString, TBool, TInt, TFloat, TVector2, TVector3, TVector4, TColor, TListT;
        static readonly object apilock = new object();

        static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),
            typeof(int),typeof(uint),typeof(long),typeof(ulong),
            typeof(float),typeof(double),typeof(decimal)
        };

        static VLExtention()
        {
            TObject = typeof(object);
            TString = typeof(string);
            TBool = typeof(bool);
            TInt = typeof(int);
            TFloat = typeof(float);
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

#if VLAB
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

                    var fld = new FactorLevelDesign(f, so, eo, no, method);
                    conddesign[f] = fld.FactorLevel().Value;
                }
            }
            return conddesign;
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

        public static bool FirstAtSplit(this string name, out string head, out string tail)
        {
            head = null; tail = null;
            if (!string.IsNullOrEmpty(name))
            {
                var ati = name.IndexOf('@');
                if (ati < 0)
                {
                    head = name;
                    tail = null;
                    return false;
                }
                else if (ati == 0)
                {
                    if (name.Length >= 4)
                    {
                        head = null;
                        tail = name.Substring(1);
                        return false;
                    }
                }
                else
                {
                    if (name.Length >= 5)
                    {
                        head = name.Substring(0, ati);
                        tail = name.Substring(ati + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        public static string FirstAtSplitHead(this string name)
        {
            string head, tail;
            name.FirstAtSplit(out head, out tail);
            return head;
        }

        public static string FirstAtSplitTail(this string name)
        {
            string head, tail;
            name.FirstAtSplit(out head, out tail);
            return tail;
        }

        public static bool IsEnvParamFullName(this string name)
        {
            string head, tail;
            return name.FirstAtSplit(out head, out tail);
        }

        public static bool IsEnvParamFullName(this string name, out string shortname, out string fullname)
        {
            string head, tail;
            var t = name.FirstAtSplit(out head, out tail);
            if (t)
            {
                shortname = head;
                fullname = name;
            }
            else
            {
                shortname = name;
                fullname = null;
            }
            return t;
        }

        public static bool IsEnvParamShortName(this string name)
        {
            string head, tail;
            name.FirstAtSplit(out head, out tail);
            return head != null && tail == null;
        }

        public static bool LastAtSplit(this string name, out string head, out string tail)
        {
            head = null; tail = null;
            if (!string.IsNullOrEmpty(name))
            {
                var ati = name.LastIndexOf('@');
                if (ati < 0)
                {
                    head = name;
                    tail = null;
                    return false;
                }
                else if (ati == 0)
                {
                    if (name.Length >= 2)
                    {
                        head = null;
                        tail = name.Substring(1);
                        return false;
                    }
                }
                else
                {
                    if (name.Length >= 5)
                    {
                        head = name.Substring(0, ati);
                        tail = name.Substring(ati + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        public static string LastAtSplitHead(this string name)
        {
            string head, tail;
            name.LastAtSplit(out head, out tail);
            return head;
        }

        public static string LastAtSplitTail(this string name)
        {
            string head, tail;
            name.LastAtSplit(out head, out tail);
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

        public static float GetColorScale(float luminance, float contrast)
        {
            luminance = Mathf.Clamp(luminance, 0, 1);
            contrast = Mathf.Clamp(contrast, 0, 1);
            return 2 * luminance * contrast;
        }

        public static void GetColor(this float scale, Color minc, Color maxc, out Color sminc, out Color smaxc)
        {
            var ac = (minc + maxc) / 2;
            var acd = maxc - ac;
            sminc = new Color(ac.r - acd.r * scale, ac.g - acd.g * scale, ac.b - acd.b * scale, minc.a);
            smaxc = new Color(ac.r + acd.r * scale, ac.g + acd.g * scale, ac.b + acd.b * scale, maxc.a);
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

        public static Dictionary<string, Dictionary<string, List<string>>> ValidateEnvCrossInheritRule(this Dictionary<string, Dictionary<string, List<string>>> rule)
        {
            if (!rule.ContainsKey(EnvironmentObject.GratingQuad.ToString()))
            {
                var gratingquadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.Quad.ToString()] = new List<string> { "Ori", "Position" },
                    [EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position" }
                };
                rule[EnvironmentObject.GratingQuad.ToString()] = gratingquadinheritfrom;
            }
            if (!rule.ContainsKey(EnvironmentObject.Quad.ToString()))
            {
                var quadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Ori", "Position" },
                    [EnvironmentObject.ImageQuad.ToString()] = new List<string> { "Position" }
                };
                rule[EnvironmentObject.Quad.ToString()] = quadinheritfrom;
            }
            if (!rule.ContainsKey(EnvironmentObject.ImageQuad.ToString()))
            {
                var imagequadinheritfrom = new Dictionary<string, List<string>>
                {
                    [EnvironmentObject.GratingQuad.ToString()] = new List<string> { "Position", "Diameter" },
                    [EnvironmentObject.Quad.ToString()] = new List<string> { "Position" }
                };
                rule[EnvironmentObject.ImageQuad.ToString()] = imagequadinheritfrom;
            }
            return rule;
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

        public static string GetAddresses(this string experimenter, VLCFG config)
        {
            string addresses = null;
            if (string.IsNullOrEmpty(experimenter)) return addresses;
            var al = experimenter.Split(',', ';').Where(i => config.ExperimenterAddress.ContainsKey(i)).Select(i => config.ExperimenterAddress[i]).ToArray();
            if (al != null && al.Length > 0)
            {
                addresses = String.Join(",", al);
            }
            return addresses;
        }

        public static ILaser GetLaser(this string lasername, VLCFG config)
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

        public static Dictionary<string, Texture2D> LoadImageSet(this string imgsetdir, int startidx = 0, int numofimg = 10)
        {
            if (string.IsNullOrEmpty(imgsetdir)) return null;
            var imgs = new Dictionary<string, Texture2D>();
            for (var i = startidx; i < numofimg + startidx; i++)
            {
                var img = Resources.Load<Texture2D>(imgsetdir + "/" + i);
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
    }
}