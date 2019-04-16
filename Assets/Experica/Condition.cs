/*
Condition.cs is part of the Experica.
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
using System;
using System.Linq;
using MathNet.Numerics;

namespace Experica
{
    public enum FactorLevelDesignMethod
    {
        Linear,
        Logarithm
    }

    public class FactorLevelDesign
    {
        public string factorname;
        public object start, end;
        public int[] n;
        public FactorLevelDesignMethod method;
        public bool isortho;
        Type T;

        public FactorLevelDesign(string factorname, object startvalue, object endvalue, int[] nvalue,
            FactorLevelDesignMethod designmethod = FactorLevelDesignMethod.Linear, bool isortho = true)
        {
            T = startvalue.GetType();
            if (T != endvalue.GetType())
            {
                throw new ArgumentException("Type Inconsistency of startvalue and endvalue");
            }
            this.factorname = factorname;
            start = startvalue;
            end = endvalue;
            n = nvalue ?? throw new NullReferenceException();
            method = designmethod;
            this.isortho = isortho;
        }

        public KeyValuePair<string, List<object>> FactorLevel()
        {
            List<object> ls = new List<object>();
            switch (method)
            {
                case FactorLevelDesignMethod.Linear:
                    if (T.IsNumeric())
                    {
                        var s = start.Convert<float>();
                        var e = end.Convert<float>();
                        if (e > s)
                        {
                            ls = Generate.LinearSpacedMap(n[0], s, e, i => (object)(float)i).ToList();
                        }
                    }
                    else if (T == typeof(Vector3))
                    {
                        var s = (Vector3)start;
                        var e = (Vector3)end;
                        float[] xl = new float[] { s.x }, yl = new float[] { s.y }, zl = new float[] { s.z };
                        bool isx = false, isy = false, isz = false;
                        if (e.x > s.x)
                        {
                            isx = true;
                            xl = Generate.LinearSpacedMap(n[0], s.x, e.x, i => (float)i);
                        }
                        if (e.y > s.y && n.Length > 1)
                        {
                            isy = true;
                            yl = Generate.LinearSpacedMap(n[1], s.y, e.y, i => (float)i);
                        }
                        if (e.z > s.z && n.Length > 2)
                        {
                            isz = true;
                            zl = Generate.LinearSpacedMap(n[2], s.z, e.z, i => (float)i);
                        }
                        if (isortho)
                        {
                            for (var xi = 0; xi < xl.Length; xi++)
                            {
                                for (var yi = 0; yi < yl.Length; yi++)
                                {
                                    for (var zi = 0; zi < zl.Length; zi++)
                                    {
                                        ls.Add(new Vector3(xl[xi], yl[yi], zl[zi]));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (isx)
                            {
                                for (var xi = 0; xi < xl.Length; xi++)
                                {
                                    ls.Add(new Vector3(xl[xi], yl[0], zl[0]));
                                }
                            }
                            if (isy)
                            {
                                for (var yi = 0; yi < yl.Length; yi++)
                                {
                                    ls.Add(new Vector3(xl[0], yl[yi], zl[0]));
                                }
                            }
                            if (isz)
                            {
                                for (var zi = 0; zi < zl.Length; zi++)
                                {
                                    ls.Add(new Vector3(xl[0], yl[0], zl[zi]));
                                }
                            }
                        }
                    }
                    else if (T == typeof(Color))
                    {
                        var s = (Color)start;
                        var e = (Color)end;
                        float[] rl = new float[] { s.r }, gl = new float[] { s.g }, bl = new float[] { s.b }, al = new float[] { s.a };
                        bool isr = false, isg = false, isb = false, isa = false;
                        if (e.r > s.r)
                        {
                            isr = true;
                            rl = Generate.LinearSpacedMap(n[0], s.r, e.r, i => (float)i);
                        }
                        if (e.g > s.g && n.Length > 1)
                        {
                            isg = true;
                            gl = Generate.LinearSpacedMap(n[1], s.g, e.g, i => (float)i);
                        }
                        if (e.b > s.b && n.Length > 2)
                        {
                            isb = true;
                            bl = Generate.LinearSpacedMap(n[2], s.b, e.b, i => (float)i);
                        }
                        if (e.a > s.a && n.Length > 3)
                        {
                            isa = true;
                            al = Generate.LinearSpacedMap(n[3], s.a, e.a, i => (float)i);
                        }
                        if (isortho)
                        {
                            for (var ri = 0; ri < rl.Length; ri++)
                            {
                                for (var gi = 0; gi < gl.Length; gi++)
                                {
                                    for (var bi = 0; bi < bl.Length; bi++)
                                    {
                                        for (var ai = 0; ai < al.Length; ai++)
                                        {
                                            ls.Add(new Color(rl[ri], gl[gi], bl[bi], al[ai]));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (isr)
                            {
                                for (var ri = 0; ri < rl.Length; ri++)
                                {
                                    ls.Add(new Color(rl[ri], gl[0], bl[0], al[0]));
                                }
                            }
                            if (isg)
                            {
                                for (var gi = 0; gi < gl.Length; gi++)
                                {
                                    ls.Add(new Color(rl[0], gl[gi], bl[0], al[0]));
                                }
                            }
                            if (isb)
                            {
                                for (var bi = 0; bi < bl.Length; bi++)
                                {
                                    ls.Add(new Color(rl[0], gl[0], bl[bi], al[0]));
                                }
                            }
                            if (isa)
                            {
                                for (var ai = 0; ai < al.Length; ai++)
                                {
                                    ls.Add(new Color(rl[0], gl[0], bl[0], al[ai]));
                                }
                            }
                        }
                    }
                    break;
            }
            return new KeyValuePair<string, List<object>>(factorname, ls);
        }
    }
}
