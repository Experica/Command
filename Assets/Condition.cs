/*
Condition.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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
        public Dictionary<string, List<object>> blockcond;
        public int nfactor;
        public int ncond;
        public Dictionary<int, Dictionary<int, int>> condsamplespacerepeat;
        public Dictionary<int, int> blockrepeat;
        public Dictionary<int, int> condrepeat;

        public System.Random rng = new MersenneTwister();
        public SampleMethod condsamplemethod = SampleMethod.Ascending;
        public SampleMethod blocksamplemethod = SampleMethod.Ascending;
        public List<int> blocksamplespace;
        public List<List<int>> condsamplespaces;
        public int scendingstep = 1;

        public int blockidx = -1;
        public int condidx = -1;
        public int condsampleidx = -1;
        public int blocksampleidx = -1;
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

        public void UpdateSampleSpace(SampleMethod condsamplemethod, List<string> blockparams, SampleMethod blocksamplemethod)
        {
            this.condsamplemethod = condsamplemethod;
            this.blocksamplemethod = blocksamplemethod;
            if (ncond > 0)
            {
                var vbp = cond.Keys.Intersect(blockparams).ToList();
                blockcond = new Dictionary<string, List<object>>();
                Dictionary<string, List<object>> blockcondfull = null; int bcn = 0;
                if (vbp.Count > 0)
                {
                    var bpfl = new Dictionary<string, List<object>>();
                    foreach (var p in vbp)
                    {
                        bpfl[p] = cond[p].Distinct().ToList();
                    }
                    blockcondfull = bpfl.OrthoCondOfFactorLevel();
                    bcn = blockcondfull.Values.First().Count;
                }
                condsamplespaces = new List<List<int>>();
                condsamplespacerepeat = new Dictionary<int, Dictionary<int, int>>();
                if (bcn < 2)
                {
                    condsamplespaces.Add(PrepareSampleSpace(ncond, condsamplemethod));
                    ResetCondSampleSpace(0);
                    blocksamplespace = new List<int> { 0 };
                }
                else
                {
                    foreach (var bp in blockcondfull.Keys)
                    {
                        blockcond[bp] = new List<object>();
                    }
                    for (var bci = 0; bci < bcn; bci++)
                    {
                        var l = Enumerable.Repeat(true, ncond).ToList();
                        foreach (var f in blockcondfull.Keys)
                        {
                            var fl = blockcondfull[f][bci];
                            l = cond[f].Select((v, i) => Equals(v, fl) & l[i]).ToList();
                        }
                        var space = Enumerable.Range(0, ncond).Where(i => l[i] == true).ToList();
                        if (space.Count > 0)
                        {
                            condsamplespaces.Add(PrepareSampleSpace(space, condsamplemethod));
                            ResetCondSampleSpace(condsamplespaces.Count - 1);
                            foreach (var bp in blockcondfull.Keys)
                            {

                                blockcond[bp].Add(blockcondfull[bp][bci]);
                            }
                        }
                    }
                    blocksamplespace = PrepareSampleSpace(condsamplespaces.Count, blocksamplemethod);
                }
                ResetBlockRepeat();
                ResetCondRepeat();
                blocksampleidx = -1;
                condsampleidx = -1;
                blockidx = -1;
                condidx = -1;
            }
        }

        public List<int> PrepareSampleSpace(List<int> space, SampleMethod samplemethod)
        {
            switch (samplemethod)
            {
                case SampleMethod.Descending:
                    space.Reverse();
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    space = rng.Shuffle(space);
                    break;
                default:
                    break;
            }
            return space;
        }

        public List<int> PrepareSampleSpace(int spacesize, SampleMethod samplemethod)
        {
            switch (samplemethod)
            {
                case SampleMethod.Descending:
                    return Enumerable.Range(0, spacesize).Reverse().ToList();
                case SampleMethod.UniformWithoutReplacement:
                    return rng.Permutation(spacesize);
                default:
                    return Enumerable.Range(0, spacesize).ToList();
            }
        }

        public int SampleBlockSpace(int manualblockidx = 0)
        {
            if (ncond > 0)
            {
                switch (blocksamplemethod)
                {
                    case SampleMethod.Ascending:
                    case SampleMethod.Descending:
                        blocksampleidx += scendingstep;
                        if (blocksampleidx > blocksamplespace.Count - 1)
                        {
                            blocksampleidx -= blocksamplespace.Count;
                        }
                        blockidx = blocksamplespace[blocksampleidx];
                        break;
                    case SampleMethod.UniformWithReplacement:
                        blocksampleidx = rng.Next(blocksamplespace.Count);
                        blockidx = blocksamplespace[blocksampleidx];
                        break;
                    case SampleMethod.UniformWithoutReplacement:
                        blocksampleidx++;
                        if (blocksampleidx > blocksamplespace.Count - 1)
                        {
                            blocksamplespace = PrepareSampleSpace(blocksamplespace.Count, blocksamplemethod);
                            blocksampleidx -= blocksamplespace.Count;
                        }
                        blockidx = blocksamplespace[blocksampleidx];
                        break;
                    case SampleMethod.Manual:
                        blockidx = manualblockidx;
                        break;
                }
                blockrepeat[blockidx] += 1;
                ResetCondSampleSpace(blockidx);
            }
            return blockidx;
        }

        public int SampleCondSpace(int manualcondidx = 0)
        {
            if (ncond > 0)
            {
                switch (condsamplemethod)
                {
                    case SampleMethod.Ascending:
                    case SampleMethod.Descending:
                        condsampleidx += scendingstep;
                        if (condsampleidx > condsamplespaces[blockidx].Count - 1)
                        {
                            condsampleidx = 0;
                        }
                        condidx = condsamplespaces[blockidx][condsampleidx];
                        break;
                    case SampleMethod.UniformWithReplacement:
                        condsampleidx = rng.Next(condsamplespaces[blockidx].Count);
                        condidx = condsamplespaces[blockidx][condsampleidx];
                        break;
                    case SampleMethod.UniformWithoutReplacement:
                        condsampleidx++;
                        if (condsampleidx > condsamplespaces[blockidx].Count - 1)
                        {
                            condsamplespaces[blockidx] = PrepareSampleSpace(condsamplespaces[blockidx], condsamplemethod);
                            condsampleidx = 0;
                        }
                        condidx = condsamplespaces[blockidx][condsampleidx];
                        break;
                    case SampleMethod.Manual:
                        condidx = manualcondidx;
                        break;
                }
                condsamplespacerepeat[blockidx][condidx] += 1;
                condrepeat[condidx] += 1;
            }
            return condidx;
        }

        public int CondRepeatInBlock(int condrepeat, int blockrepeat)
        {
            return (int)Math.Ceiling((decimal)(Math.Max(0, condrepeat) / Math.Max(1, blockrepeat)));
        }

        public int SampleCondition(int condrepeat, int blockrepeat, bool istrysampleblock = true, int manualblockidx = 0, int manualcondidx = 0)
        {
            if (ncond > 0)
            {
                if (nsampleignore == 0)
                {
                    if (blockidx < 0)
                    {
                        SampleBlockSpace(manualblockidx);
                    }
                    if (istrysampleblock)
                    {
                        if (IsCondRepeatInBlock(condrepeat, blockrepeat))
                        {
                            SampleBlockSpace(manualblockidx);
                        }
                    }
                    SampleCondSpace(manualcondidx);
                }
                else
                {
                    nsampleignore--;
                }
            }
            return condidx;
        }

        public void PushCondition(int condidx, EnvironmentManager envmanager, List<string> except = null, bool notifyui = true)
        {
            if (condidx < 0) return;
            if (cond == null) return;
            var factors = except == null ? cond.Keys : cond.Keys.Except(except);
            foreach (var k in factors)
            {
                envmanager.SetParam(k, cond[k][condidx], notifyui);
            }
        }

        public void PushBlock(int blockidx, EnvironmentManager envmanager, List<string> except = null, bool notifyui = true)
        {
            if (blockidx < 0) return;
            if (blockcond == null) return;
            var factors = except == null ? blockcond.Keys : blockcond.Keys.Except(except);
            foreach (var k in factors)
            {
                envmanager.SetParam(k, blockcond[k][blockidx], notifyui);
            }
        }

        public bool IsCondRepeatInBlock(int condrepeat, int blockrepeat)
        {
            return IsCondSampleSpaceRepeat(CondRepeatInBlock(condrepeat, blockrepeat), blockidx);
        }

        public bool IsCondSampleSpaceRepeat(int n, int blockidx)
        {
            foreach (var c in condsamplespaces[blockidx])
            {
                if (!condsamplespacerepeat[blockidx].ContainsKey(c))
                {
                    return false;
                }
                else
                {
                    if (condsamplespacerepeat[blockidx][c] < n)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool IsCondRepeat(int n)
        {
            for (var i = 0; i < ncond; i++)
            {
                if (!condrepeat.ContainsKey(i))
                {
                    return false;
                }
                else
                {
                    if (condrepeat[i] < n)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool IsBlockRepeat(int n)
        {
            for (var i = 0; i < condsamplespaces.Count; i++)
            {
                if (!blockrepeat.ContainsKey(i))
                {
                    return false;
                }
                else
                {
                    if (blockrepeat[i] < n)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ResetCondSampleSpace(int blockidx)
        {
            var samplespacerepeat = new Dictionary<int, int>();
            foreach (var i in condsamplespaces[blockidx])
            {
                samplespacerepeat[i] = 0;
            }
            condsamplespacerepeat[blockidx] = samplespacerepeat;
            condsampleidx = -1;
        }

        public void ResetCondRepeat()
        {
            condrepeat = new Dictionary<int, int>();
            for (var i = 0; i < ncond; i++)
            {
                condrepeat[i] = 0;
            }
        }

        public void ResetBlockRepeat()
        {
            blockrepeat = new Dictionary<int, int>();
            foreach (var i in blocksamplespace)
            {
                blockrepeat[i] = 0;
            }
        }

        public List<int> CurrentCondSampleSpace
        { get { return condsamplespaces[blockidx]; } }
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

    public class CondTestManager
    {
        public Dictionary<CONDTESTPARAM, List<object>> condtest = new Dictionary<CONDTESTPARAM, List<object>>();
        public Dictionary<CONDTESTPARAM, List<object>> trialtest = new Dictionary<CONDTESTPARAM, List<object>>();
        public int condtestidx = -1;
        public Action<CONDTESTPARAM, List<object>> OnNotifyCondTest;
        public Action<double> OnNotifyCondTestEnd;
        public Action OnStartCondTest, OnClearCondTest;
        public int notifyidx = 0;


        public virtual void NewCondTest(double starttime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0)
        {
            condtestidx++;
            if (condtestidx > 0)
            {
                OnStartCondTest();
                if (notifypercondtest > 0)
                {
                    if (((condtestidx - notifyidx) / notifypercondtest) >= 1)
                    {
                        NotifyCondTestAndEnd(notifyidx, notifyparam, starttime);
                        notifyidx = condtestidx;
                    }
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

        public void NotifyCondTestAndEnd(int startidx, List<CONDTESTPARAM> notifyparam, double endtime)
        {
            NotifyCondTest(startidx, notifyparam);
            OnNotifyCondTestEnd(endtime);
        }

        public void Clear()
        {
            condtest.Clear();
            condtestidx = -1;
            notifyidx = 0;
            OnClearCondTest();
        }

        public void AddToCondTest(CONDTESTPARAM paramname, object paramvalue)
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

        public void AddEventToCondTest(CONDTESTPARAM paramname, string eventname, double timestamp)
        {
            if (condtestidx == -1) return;
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