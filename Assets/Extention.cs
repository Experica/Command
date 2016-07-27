// -----------------------------------------------------------------------------
// Extention.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VLab
{
    public static class Extention
    {
        public static T Convert<T>(this object value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static object Convert(this object value, Type ToT)
        {
            Type VT = value.GetType();
            if (ToT == VT)
            {
                return value;
            }
            else if (ToT == typeof(Vector3))
            {
                if (VT == typeof(string))
                {
                    var t = (string)value;
                    var vs = t.Substring(1, t.Length - 2).Split(',');
                    return new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
                }
                else
                {
                    return System.Convert.ChangeType(value, ToT);
                }
            }
            else if (ToT == typeof(Color))
            {
                if (VT == typeof(string))
                {
                    var t = (string)value;
                    var vs = t.Substring(t.IndexOf('(') + 1, t.Length - 2).Split(',');
                    return new Color(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                }
                else
                {
                    return System.Convert.ChangeType(value, ToT);
                }
            }
            else if (ToT == typeof(string))
            {
                if (VT == typeof(Vector3))
                {
                    return ((Vector3)value).ToString("G3");
                }
                else if (VT == typeof(Color))
                {
                    return ((Color)value).ToString("G3").Substring(4);
                }
                else
                {
                    return value.ToString();
                }
            }
            else if (ToT.IsEnum)
            {
                if (Enum.IsDefined(ToT, value))
                {
                    if (VT == typeof(string))
                    {
                        return Enum.Parse(ToT, (string)value);
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    return Activator.CreateInstance(ToT);
                }
            }
            else
            {
                return System.Convert.ChangeType(value, ToT);
            }
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

        public static Dictionary<string, List<object>> ResolveConditionReference(this Dictionary<string, List<object>> cond,Dictionary<string,object> param)
        {
            return cond.ResolveCondFactorReference(param).ResolveCondLevelReference(param);
        }

        public static Dictionary<string, List<object>> ResolveCondFactorReference(this Dictionary<string, List<object>> cond, Dictionary<string, object> param)
        {
            // Replace Factor with known reference in parameter
            foreach (var f in cond.Keys.ToArray())
            {
                if (f.Count() > 1 && f.First() == '$')
                {
                    var fname = f.Substring(1);
                    var fl = cond[f];
                    if (param.ContainsKey(fname) && param[fname] != null &&
                       typeof(List<object>).IsInstanceOfType(param[fname]))
                    {
                        fl = param[fname] as List<object>;
                    }
                    cond.Remove(f);
                    cond[fname] = fl;
                }
            }
            return cond;
        }

        public static Dictionary<string, List<object>> ResolveCondLevelReference(this Dictionary<string, List<object>> cond, Dictionary<string, object> param)
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
                                cond[f][i] = param[lname];
                            }
                        }
                    }
                }
            }
            return cond;
        }
#if VLab
        public static Dictionary<string, List<object>> FactorLevelOfDesign(this Dictionary<string, List<object>> conddesign)
        {
            foreach(var f in conddesign.Keys)
            {
                if(conddesign[f].Count>=5&&conddesign[f][0].GetType()==typeof(string)&&(string)conddesign[f][0]=="factorleveldesign")
                {
                    var start = conddesign[f][1];
                    var end = conddesign[f][2];
                    var n = (int[])conddesign[f][3];
                    var method =conddesign[f][4].Convert<FactorLevelDesignMethod>();
                    var fld = new FactorLevelDesign(f, start, end, n, method);

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

    }
}