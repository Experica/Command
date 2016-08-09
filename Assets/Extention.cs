/*
Extention.cs is part of the VLAB project.
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

namespace VLab
{
    public enum CONDTESTPARAM
    {
        CondIndex,
        CondRepeat,
        CONDSTATE,
        TRIALSTATE,
        BLOCKSTATE,
        TASKSTATE
    }

    public enum ParamType
    {
        String,
        Float,
        Bool,
        Vector3,
        Color,
        ListOfString,
        ListOfFloat,
        ListOfBool
    }

    public class Param
    {
        ParamType t;
        public ParamType Type { get { return t; } }
        object v;
        public object Value
        {
            get { return v; }
            set { v = value.Convert(t); }
        }

        public Param(ParamType t, object v)
        {
            this.t = t;
            this.v = v;
        }
    }

    public static class Extention
    {
        public static T Convert<T>(this object value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static object Convert(this object value, ParamType type)
        {
            switch (type)
            {
                case ParamType.Float:
                    return  value.Convert<float>();
                case ParamType.ListOfFloat:
                    return  value.Convert<List<float>>();
                case ParamType.ListOfString:
                    return  value.Convert<List<string>>();
                case ParamType.Vector3:
                    return  value.Convert<Vector3>();
                case ParamType.Color:
                    return value.Convert<Color>();
                default:
                    return value.Convert<string>();
            }
        }

        public static object Convert(this object value, Type CT)
        {
            Type VT = value.GetType();
            if (VT == CT)
            {
                return value;
            }
            else if (VT == typeof(List<string>))
            {
                var v = (List<string>)value;
                if (CT == typeof(string))
                {
                    return v.Count == 0 ? "[]" : "[" + v.Aggregate((i, j) => i + ", " + j) + "]";
                }
            }
            else if (VT==typeof(List<CONDTESTPARAM>))
            {
                var v = (List<CONDTESTPARAM>)value;
                if (CT == typeof(string))
                {
                    return v.Count == 0 ? "[]" : "[" + v.Select(i=>i.ToString()).Aggregate((i, j) => i + ", " + j) + "]";
                }
            }
            else if (VT== typeof(List<object>))
            {
                var v = (List<object>)value;
                var vn = v.Count;
                if(CT==typeof(Vector3))
                {
                    float x=0, y=0, z=0;
                    if(vn>0)
                    {
                        x = v[0].Convert<float>();
                    }
                    if(vn>1)
                    {
                        y = v[1].Convert<float>();
                    }
                    if(vn>2)
                    {
                        z = v[2].Convert<float>();
                    }
                    return new Vector3(x, y, z);
                }
                else if(CT==typeof(Color))
                {
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
                    return new Color(r, g, b,a);
                }
                else if (CT == typeof(string))
                {
                    return v.Count == 0 ? "[]" : "[" + v.Aggregate((i, j) => i.Convert<string>() + ", " + j.Convert<string>()) + "]";
                }
            }
            else if (VT == typeof(Vector3))
            {
                var v = (Vector3)value;
                if(CT==typeof(string))
                {
                    return v.ToString("G3");
                }
            }
            else if (VT == typeof(Color))
            {
                var v = (Color)value;
                if(CT==typeof(string))
                {
                    return v.ToString("G3").Substring(4);
                }
            }
            else if (VT == typeof(string))
            {
                var v = (string)value;
                if (CT == typeof(float))
                {
                    return float.Parse(v);
                }
                else    if (CT==typeof(Vector3))
                {
                    var vs = v.Substring(1, v.Length - 2).Split(',');
                    return new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
                }
                else if(CT==typeof(Color))
                {
                    var vs = v.Substring(v.IndexOf('(') + 1, v.Length - 2).Split(',');
                    return new Color(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                }
                else if(CT.IsEnum&& Enum.IsDefined(CT, v))
                {
                    return Enum.Parse(CT, v);
                }
                else if(CT==typeof(List<float>))
                {
                    var vs = v.Substring(v.IndexOf('['), v.Length - 2).Split(',');
                    return vs.Select(i => float.Parse(i)).ToList();
                }
                else if(CT==typeof(List<string>))
                {
                    var vs = v.Substring(v.IndexOf('['), v.Length - 2).Split(',');
                    return vs.ToList();
                }
            }
            else
            {
                if(CT==typeof(string))
                {
                    return value.ToString();
                }
            }
            return System.Convert.ChangeType(value, CT);
        }

        public static List<int> Sequence(this System.Random rng, int maxvalue)
        {
            var seq = Enumerable.Repeat(-1, maxvalue).ToList();
            int i, j;
            for (i = 0; i < maxvalue; i++)
            {
                do
                {
                    j = rng.Next(maxvalue);
                }
                while (seq[j] >= 0);
                seq[j] = i;
            }
            return seq;
        }

        public static Dictionary<string, List<object>> OrthoCondOfFactorLevel(this Dictionary<string, List<object>> fsls)
        {
            foreach(var f in fsls.Keys.ToArray())
            {
                if(fsls[f].Count==0)
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

        public static Dictionary<string, List<object>> ResolveConditionReference(this Dictionary<string, List<object>> cond,Dictionary<string,Param> param)
        {
            return cond.ResolveCondFactorReference(param).ResolveCondLevelReference(param);
        }

        public static Dictionary<string, List<object>> ResolveCondFactorReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        {
            // Replace Factor with known reference in parameter
            foreach (var f in cond.Keys.ToArray())
            {
                if (f.Count() > 1 && f.First() == '$')
                {
                    var fname = f.Substring(1);
                    var fl = cond[f];
                    if (param.ContainsKey(fname) && param[fname] != null &&param[fname].Type.IsList())
                    {
                        fl.Clear();
                        foreach(var i in (IEnumerable)param[fname].Value)
                        {
                            fl.Add(i);
                        }
                    }
                    cond.Remove(f);
                    cond[fname] = fl;
                }
            }
            return cond;
        }

        public static Dictionary<string, List<object>> ResolveCondLevelReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        {
            // Replace Level with known reference in parameter
            foreach (var f in cond.Keys)
            {
                for (var i = 0; i < cond[f].Count; i++)
                {
                    if (cond[f][i].GetType() == typeof(string))
                    {
                        var l = (string)cond[f][i];
                        if (l.Count() > 1 && l.First() == '$')
                        {
                            var lname = l.Substring(1);
                            if (param.ContainsKey(lname) && param[lname] != null)
                            {
                                cond[f][i] = param[lname].Value;
                            }
                        }
                    }
                }
            }
            return cond;
        }
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
                        var s = (List<object>)start;
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
        public static bool FirstAtSplit(this string name,out string head,out string tail)
        {
            head = null;tail = null;
            if(!string.IsNullOrEmpty(name))
            {
                var ati = name.IndexOf('@');
                if(ati<0)
                {
                    head = name;
                    tail = null;
                    return false;
                }
                else if(ati==0)
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

        public static bool IsEnvParamShortName(this string name)
        {
            string head, tail;
            name.FirstAtSplit(out head, out tail);
            return head != null && tail == null;
        }

        public static bool LastAtSplit(this string name,out string head,out string tail)
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

        public static float GetColorScale(float luminance, float contrast )
        {
            luminance = Mathf.Clamp(luminance, 0, 1);
            contrast = Mathf.Clamp(contrast, 0, 1);
            return 2*luminance * contrast;
        }

        public static void GetColor(this float scale,Color minc,Color maxc, out Color sminc,out Color smaxc)
        {
            var ac = (minc + maxc) / 2;
            var acd = maxc - ac;
             sminc = new Color(ac.r - acd.r * scale, ac.g - acd.g * scale, ac.b - acd.b * scale, minc.a);
             smaxc = new Color(ac.r + acd.r * scale, ac.g + acd.g * scale, ac.b + acd.b * scale, maxc.a);
        }

        public static Vector3 RotateZCCW(this Vector3 v,float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v;
        }

        public static bool IsList(this ParamType type)
        {
            switch(type)
            {
                case ParamType.ListOfBool:
                case ParamType.ListOfFloat:
                case ParamType.ListOfString:
                    return true;
                default:
                    return false;
            }
        }
       
    }
}