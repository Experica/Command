/*
ConditionManager.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using MathNet.Numerics.Random;
using MathNet.Numerics;
using System.Runtime.Remoting.Messaging;
using System.Numerics;
using System.Drawing;

namespace Experica
{
    public class ConditionManager
    {
        public Dictionary<string, List<object>> cond = new Dictionary<string, List<object>>();
        public Dictionary<string, IList> FinalCond { get; private set; } = new();
        public int NCond
        {
            get
            {
                if (FinalCond.Count == 0) { return 0; }
                return FinalCond.Values.First().Count;
            }
        }
        int ncond = 0;
        public int nblock = 0;

        public Dictionary<string, IList> finalblockcond = new Dictionary<string, IList>();
        public List<List<int>> condsamplespaces = new List<List<int>>();
        public List<int> blocksamplespace = new List<int>();
        public Dictionary<int, Dictionary<int, int>> condsamplespacerepeat = new Dictionary<int, Dictionary<int, int>>();
        public Dictionary<int, int> blockrepeat = new();
        public Dictionary<int, int> condrepeat = new();

        public System.Random RNG = new MersenneTwister();
        public SampleMethod CondSampleMethod { get; private set; } = SampleMethod.Ascending;
        public SampleMethod BlockSampleMethod { get; private set; } = SampleMethod.Ascending;

        public int scendingstep = 1;
        public int blockidx = -1;
        public int CondIndex { get; private set; } = -1;
        public int condsampleidx = -1;
        public int blocksampleidx = -1;
        public int nsampleignore = 0;

        public Dictionary<string, List<object>> ReadConditionFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Condition File: {path} not found.");
                return null;
            }
            return path.ReadYamlFile<Dictionary<string, List<object>>>();
        }

        public Dictionary<string, List<object>> ProcessCondition(Dictionary<string, List<object>> cond)
        {
            if (cond != null && cond.Count > 0)
            {
                cond.ProcessFactorDesign();
                cond.ProcessOrthoCombineFactor();
            }
            return cond;
        }

        public void FinalizeCondition(Dictionary<string, List<object>> cond)
        {
            if (cond == null || cond.Count == 0)
            {
                FinalCond.Clear();
            }
            else
            {
                var fln = cond.Values.Select(i => i.Count).ToArray();
                var minfln = fln.Min();
                var maxfln = fln.Max();
                if (minfln != maxfln)
                {
                    foreach (var f in cond.Keys)
                    {
                        cond[f] = cond[f].GetRange(0, minfln);
                    }
                }
                FinalCond = cond.FinalizeFactorValues();
            }
        }

        public void FinalizeCondition(string path) { FinalizeCondition(ProcessCondition(ReadConditionFile(path))); }


        public List<int> GetSampleSpace(List<int> space, SampleMethod samplemethod)
        {
            switch (samplemethod)
            {
                case SampleMethod.Ascending:
                    space.Sort();
                    break;
                case SampleMethod.Descending:
                    space.Sort();
                    space.Reverse();
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    space = space.SelectPermutation(RNG).ToList();
                    break;
            }
            return space;
        }

        public List<int> GetSampleSpace(int spacesize, SampleMethod samplemethod)
        {
            return samplemethod switch
            {
                SampleMethod.Descending => Enumerable.Range(0, spacesize).Reverse().ToList(),
                SampleMethod.UniformWithoutReplacement => Enumerable.Range(0, spacesize).SelectPermutation(RNG).ToList(),
                _ => Enumerable.Range(0, spacesize).ToList(),
            };
        }

        public void InitializeSampleSpaces(SampleMethod condsamplemethod, List<string> blockfactors, SampleMethod blocksamplemethod)
        {
            if (NCond == 0) { return; }
            CondSampleMethod = condsamplemethod;
            BlockSampleMethod = blocksamplemethod;
            condrepeat.Clear();
            blockrepeat.Clear();



            for (var i = 0; i < ncond; i++)
            {
                condrepeat[i] = 0;
            }

            finalblockcond.Clear();
            blocksamplespace.Clear();
            condsamplespaces.Clear();

            condsamplespacerepeat.Clear();
            blocksampleidx = -1;
            condsampleidx = -1;
            blockidx = -1;
            CondIndex = -1;
            nblock = 0;


            var vbf = FinalCond.Keys.Intersect(blockfactors).ToList();
            Dictionary<string, List<object>> blockorthofactorlevel = null, blockcond = new Dictionary<string, List<object>>();
            int bn = 0;
            if (vbf.Count > 0)
            {
                var bpfl = new Dictionary<string, List<object>>();
                foreach (var p in vbf)
                {
                    bpfl[p] = cond[p].Distinct().ToList();
                }
                blockorthofactorlevel = bpfl.OrthoCondOfFactorLevel();
                bn = blockorthofactorlevel.Values.First().Count;
            }
            if (bn < 2)
            {
                blocksamplespace.Add(0);
                condsamplespaces.Add(GetSampleSpace(ncond, condsamplemethod));
                ResetCondSampleSpace(0);
            }
            else
            {
                foreach (var bp in blockorthofactorlevel.Keys)
                {
                    blockcond[bp] = new List<object>();
                }
                for (var bi = 0; bi < bn; bi++)
                {
                    var l = Enumerable.Repeat(true, ncond).ToList();
                    foreach (var f in blockorthofactorlevel.Keys)
                    {
                        var fl = blockorthofactorlevel[f][bi];
                        l = cond[f].Select((v, i) => Equals(v, fl) & l[i]).ToList();
                    }
                    var space = Enumerable.Range(0, ncond).Where(i => l[i] == true).ToList();
                    if (space.Count > 0)
                    {
                        condsamplespaces.Add(GetSampleSpace(space, condsamplemethod));
                        ResetCondSampleSpace(condsamplespaces.Count - 1);
                        foreach (var bp in blockorthofactorlevel.Keys)
                        {

                            blockcond[bp].Add(blockorthofactorlevel[bp][bi]);
                        }
                    }
                }
                finalblockcond = blockcond.FinalizeFactorValues();
                blocksamplespace = GetSampleSpace(condsamplespaces.Count, blocksamplemethod);
            }
            foreach (var i in blocksamplespace)
            {
                blockrepeat[i] = 0;
            }

            nblock = blocksamplespace.Count;
        }

        public int SampleBlockSpace(int manualblockidx = 0)
        {
            if (ncond > 0)
            {
                switch (BlockSampleMethod)
                {
                    case SampleMethod.Ascending:
                    case SampleMethod.Descending:
                        blocksampleidx += scendingstep;
                        if (blocksampleidx > blocksamplespace.Count - 1)
                        {
                            blocksampleidx = 0;
                        }
                        blockidx = blocksamplespace[blocksampleidx];
                        break;
                    case SampleMethod.UniformWithReplacement:
                        blocksampleidx = RNG.Next(blocksamplespace.Count);
                        blockidx = blocksamplespace[blocksampleidx];
                        break;
                    case SampleMethod.UniformWithoutReplacement:
                        blocksampleidx++;
                        if (blocksampleidx > blocksamplespace.Count - 1)
                        {
                            blocksamplespace = GetSampleSpace(blocksamplespace, BlockSampleMethod);
                            blocksampleidx = 0;
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

        public int SampleCondSpace(int manualcondidx = -1)
        {
            if (NCond == 0) { return -1; }

            switch (CondSampleMethod)
            {
                case SampleMethod.Ascending:
                case SampleMethod.Descending:
                    condsampleidx += scendingstep;
                    if (condsampleidx > condsamplespaces[blockidx].Count - 1)
                    {
                        condsampleidx = 0;
                    }
                    CondIndex = condsamplespaces[blockidx][condsampleidx];
                    break;
                case SampleMethod.UniformWithReplacement:
                    condsampleidx = RNG.Next(condsamplespaces[blockidx].Count);
                    CondIndex = condsamplespaces[blockidx][condsampleidx];
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    condsampleidx++;
                    if (condsampleidx > condsamplespaces[blockidx].Count - 1)
                    {
                        condsamplespaces[blockidx] = GetSampleSpace(condsamplespaces[blockidx], CondSampleMethod);
                        condsampleidx = 0;
                    }
                    CondIndex = condsamplespaces[blockidx][condsampleidx];
                    break;
                case SampleMethod.Manual:
                    CondIndex = manualcondidx;
                    break;
            }
            condsamplespacerepeat[blockidx][CondIndex] += 1;
            condrepeat[CondIndex] += 1;

            return CondIndex;
        }

        public int SampleCondition(int condrepeat, int blockrepeat, int manualcondidx = 0, int manualblockidx = 0, bool istrysampleblock = true)
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
            return CondIndex;
        }

        public void PushCondition(int condidx, INetEnv envmanager, List<string> excludefactors = null)
        {
            if (ncond <= 0 || condidx < 0) { return; }
            var factors = (excludefactors == null || excludefactors.Count == 0) ? FinalCond.Keys : FinalCond.Keys.Except(excludefactors);
            foreach (var f in factors)
            {
                envmanager.SetParam(f, FinalCond[f][condidx]);
            }
        }

        public void PushBlock(int blockidx, INetEnv envmanager, List<string> excludefactors = null)
        {
            if (blockidx < 0 || finalblockcond.Count == 0 || finalblockcond.Values.First().Count == 0) { return; }
            var factors = excludefactors == null ? finalblockcond.Keys : finalblockcond.Keys.Except(excludefactors);
            foreach (var k in factors)
            {
                envmanager.SetParam(k, finalblockcond[k][blockidx]);
            }
        }

        public int CondRepeatInBlock(int condrepeat, int blockrepeat)
        {
            return (int)Math.Ceiling((decimal)(Math.Max(0, condrepeat) / Math.Max(1, blockrepeat)));
        }

        public bool IsCondRepeatInBlock(int condrepeat, int blockrepeat)
        {
            return IsCondSampleSpaceRepeat(CondRepeatInBlock(condrepeat, blockrepeat), blockidx);
        }

        public bool IsCondSampleSpaceRepeat(int n, int blockidx)
        {
            foreach (var c in condsamplespaces[blockidx])
            {
                if (!condsamplespacerepeat[blockidx].ContainsKey(c) || condsamplespacerepeat[blockidx][c] < n)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsCondRepeat(int n)
        {
            if (ncond <= 0 || n <= 0) return false;
            for (var i = 0; i < ncond; i++)
            {
                if (!condrepeat.ContainsKey(i) || condrepeat[i] < n)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsBlockRepeat(int n)
        {
            for (var i = 0; i < condsamplespaces.Count; i++)
            {
                if (!blockrepeat.ContainsKey(i) || blockrepeat[i] < n)
                {
                    return false;
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

        public List<int> CurrentCondSampleSpace
        { get { return condsamplespaces[blockidx]; } }



        //public static List<object> LinearSpacedMap<T>(int[] n, T b, T e, bool? isortho) where T : List<>
        //{
        //    var ls = new List<object>();
        //    var lms = new List<float[]>();
        //    Enumerable.Range(0, n.Length).Select(i =>
        //    {
        //        lms.Add(Generate.LinearSpacedMap(n[i], b[i], e[i], v => (float)v));
        //    });
        //    if(isortho.HasValue)
        //    {
        //        if(isortho.Value)
        //        {

        //        }
        //        else
        //        {
        //            foreach(var i in lms)
        //            {
        //                if(i.Length > 0)
        //                {
        //                    foreach (var v in i)
        //                    {
        //                        var cb = Copy(b); cb[j] = v;
        //                        ls.Add(cb);
        //                    }
        //                }
        //            }
        //            ls=ls.Distinct().ToList();
        //        }
        //    }
        //    else
        //    {

        //    }
        //    return ls;
        //}

    }

}