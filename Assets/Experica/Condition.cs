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
        Linear
    }

    public class FactorLevelDesign
    {
        public string factorname;
        public object start, end;
        public int[] n;
        public FactorLevelDesignMethod method;
        Type T;

        public FactorLevelDesign(string factorname, object startvalue, object endvalue, int[] nvalue,
            FactorLevelDesignMethod designmethod = FactorLevelDesignMethod.Linear)
        {
            T = startvalue.GetType();
            if (T != endvalue.GetType())
            {
                throw new ArgumentException("Type Inconsistency of startvalue and endvalue");
            }
            if (nvalue == null)
            {
                throw new NullReferenceException();
            }
            this.factorname = factorname;
            start = startvalue;
            end = endvalue;
            n = nvalue;
            method = designmethod;
        }

        public KeyValuePair<string, List<object>> FactorLevel()
        {
            List<object> ls = new List<object>();
            switch (method)
            {
                case FactorLevelDesignMethod.Linear:
                    if (T == typeof(float))
                    {
                        var s = (float)start;
                        var e = (float)end;
                        if (e > s)
                        {
                            ls = Generate.LinearSpacedMap(n[0], s, e, i => (object)(float)i).ToList();
                        }
                    }
                    else if (T == typeof(Vector3))
                    {
                        var s = (Vector3)start;
                        var e = (Vector3)end;
                        float[] xl = new float[] { 0 }, yl = new float[] { 0 }, zl = new float[] { 0 };
                        if (e.x > s.x)
                        {
                            xl = Generate.LinearSpacedMap(n[0], s.x, e.x, i => (float)i);
                        }
                        if (e.y > s.y && n.Length > 1)
                        {
                            yl = Generate.LinearSpacedMap(n[1], s.y, e.y, i => (float)i);
                        }
                        if (e.z > s.z && n.Length > 2)
                        {
                            zl = Generate.LinearSpacedMap(n[2], s.z, e.z, i => (float)i);
                        }
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
                    break;
            }
            return new KeyValuePair<string, List<object>>(factorname, ls);
        }
    }
}
