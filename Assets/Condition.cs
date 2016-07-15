// --------------------------------------------------------------
// Condition.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using MathNet.Numerics.Random;
using MathNet.Numerics;

namespace VLab
{
    public class ConditionManager
    {
        public Dictionary<string, List<object>> cond;
        public int nfactor;
        public int ncond;
        public List<int> condsamplespace;
        public Dictionary<int, int> condrepeat;

        public System.Random rng = new MersenneTwister();
        public SampleMethod samplemethod = SampleMethod.Ascending;
        public int scendingstep = 1;

        public int sampleidx = -1;
        public int condidx = -1;
        public int nsampleignore = 0;

        public Dictionary<string, List<object>> ReadCondition(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return Yaml.ReadYaml<Dictionary<string, List<object>>>(path);
        }

        public void TrimCondition(Dictionary<string, List<object>> cond)
        {
            this.cond = cond;
            nfactor = cond.Keys.Count;
            if (nfactor > 0)
            {
                var fvn = new int[nfactor];
                for (var i = 0; i < nfactor; i++)
                {
                    fvn[i] = cond.Values.ElementAt(i).Count;
                }
                var minfvn = fvn.Min();
                var maxfvn = fvn.Max();
                if (minfvn != maxfvn)
                {
                    foreach (var k in cond.Keys)
                    {
                        cond[k] = cond[k].GetRange(0, minfvn);
                    }
                }
                ncond = minfvn;
            }
        }

        public List<int> UpdateSampleSpace(SampleMethod samplemethod, bool resetcondrepeat = true)
        {
            this.samplemethod = samplemethod;
            return UpdateSampleSpace(resetcondrepeat);
        }

        public virtual List<int> UpdateSampleSpace(bool resetcondrepeat = true)
        {
            switch (samplemethod)
            {
                case SampleMethod.Descending:
                    condsamplespace = Enumerable.Range(0, ncond).Reverse().ToList();
                    sampleidx = -1;
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    condsamplespace = rng.Sequence(ncond);
                    sampleidx = -1;
                    break;
                default:
                    condsamplespace = Enumerable.Range(0, ncond).ToList();
                    sampleidx = -1;
                    break;
            }
            if (resetcondrepeat)
            {
                ResetCondRepeat();
            }
            return condsamplespace;
        }

        public int SampleCondIndex()
        {
            if (nsampleignore == 0)
            {
                switch (samplemethod)
                {
                    case SampleMethod.Ascending:
                    case SampleMethod.Descending:
                        sampleidx += scendingstep;
                        if (sampleidx > condsamplespace.Count - 1)
                        {
                            sampleidx -= condsamplespace.Count;
                        }
                        condidx = condsamplespace[sampleidx];
                        break;
                    case SampleMethod.UniformWithReplacement:
                        sampleidx = rng.Next(condsamplespace.Count);
                        condidx = condsamplespace[sampleidx];
                        break;
                    case SampleMethod.UniformWithoutReplacement:
                        if (sampleidx >= condsamplespace.Count - 1)
                        {
                            UpdateSampleSpace(false);
                        }
                        sampleidx++;
                        condidx = condsamplespace[sampleidx];
                        break;
                }
                condrepeat[condidx] += 1;
            }
            else
            {
                nsampleignore--;
            }
            return condidx;
        }

        public void PushCondition(int condidx, EnvironmentManager envmanager)
        {
            foreach (var k in cond.Keys)
            {
                envmanager.SetParam(k, cond[k][condidx]);
            }
        }

        public void SamplePushCondition(EnvironmentManager envmanager)
        {
            PushCondition(SampleCondIndex(), envmanager);
        }

        public bool IsCondRepeat(int n)
        {
            foreach (var i in condrepeat.Values)
            {
                if (i < n)
                {
                    return false;
                }
            }
            return true;
        }

        public void ResetCondRepeat()
        {
            condrepeat = new Dictionary<int, int>();
            foreach (var i in condsamplespace)
            {
                condrepeat[i] = 0;
            }
        }
    }

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
            if (n == null)
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

    public class CondTestManager
    {
        public Dictionary<CONDTESTPARAM, List<object>> condtest = new Dictionary<CONDTESTPARAM, List<object>>();
        public int condtestidx = -1;
        public Action<CONDTESTPARAM, List<object>> OnNotifyCondTest;
        public Action<double> OnNotifyEnd;
        public int notifyidx = 0;
        public virtual void NewCondTest(double starttime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0)
        {
            condtestidx++;
            if (notifypercondtest > 0 && condtestidx > 0)
            {
                if (((condtestidx - notifyidx) / notifypercondtest) >= 1)
                {
                    NotifyCondTestEnd(notifyidx, notifyparam, starttime);
                    notifyidx = condtestidx;
                }
            }
        }

        public void NotifyCondTest(int startidx, List<CONDTESTPARAM> notifyparam)
        {
            if (startidx < condtestidx)
            {
                foreach (var p in notifyparam)
                {
                    if (condtest.ContainsKey(p))
                    {
                        OnNotifyCondTest(p, condtest[p].GetRange(startidx, condtestidx - startidx));
                    }
                }
            }
        }

        public void NotifyCondTestEnd(int startidx, List<CONDTESTPARAM> notifyparam, double endtime)
        {
            NotifyCondTest(startidx, notifyparam);
            OnNotifyEnd(endtime);
        }

        public void Clear()
        {
            condtest.Clear();
            condtestidx = -1;
            notifyidx = 0;
        }

        public void Add(CONDTESTPARAM paramname, object paramvalue)
        {
            if (condtest.ContainsKey(paramname))
            {
                var vs = condtest[paramname];
                for (var i = vs.Count; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                vs.Add(paramvalue);
            }
            else
            {
                var vs = new List<object>();
                for (var i = 0; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                vs.Add(paramvalue);
                condtest[paramname] = vs;
            }
        }

        public void AddEvent(CONDTESTPARAM paramname, string eventname, double timestamp)
        {
            if (condtest.ContainsKey(paramname))
            {
                var vs = condtest[paramname];
                for (var i = vs.Count; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                if (vs.Count < (condtestidx + 1))
                {
                    var es = new List<Dictionary<string, double>>();
                    var e = new Dictionary<string, double>();
                    e[eventname] = timestamp;
                    es.Add(e);
                    vs.Add(es);
                }
                else
                {
                    var es = (List<Dictionary<string, double>>)vs[condtestidx];
                    var e = new Dictionary<string, double>();
                    e[eventname] = timestamp;
                    es.Add(e);
                }
            }
            else
            {
                var vs = new List<object>();
                for (var i = 0; i < condtestidx; i++)
                {
                    vs.Add(null);
                }
                var es = new List<Dictionary<string, double>>();
                var e = new Dictionary<string, double>();
                e[eventname] = timestamp;
                es.Add(e);
                vs.Add(es);
                condtest[paramname] = vs;
            }
        }
    }

}