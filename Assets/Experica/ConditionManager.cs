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

namespace Experica
{
    public class ConditionManager
    {
        public Dictionary<string, IList> Cond { get; private set; } = new();
        public int NCond
        {
            get
            {
                if (Cond.Count == 0) { return 0; }
                return Cond.Values.First().Count;
            }
        }
        public List<string> BlockFactor { get; private set; }=new();
        public List<string> NonBlockFactor { get; private set; } = new();
        public Dictionary<string, IList> BlockCond { get; private set; } = new();
        public int NBlock => CondSampleSpace.Count;
        public List<List<int>> CondSampleSpace { get; private set; } = new();
        public List<int> BlockSampleSpace { get; private set; } = new();
        
        public System.Random RNG = new MersenneTwister();
        public SampleMethod CondSampleMethod { get; private set; } = SampleMethod.Ascending;
        public SampleMethod BlockSampleMethod { get; private set; } = SampleMethod.Ascending;

        public int NSampleSkip = 0;
        public int ScendingStep  = 1;
        public int BlockIndex { get; private set; } = -1;
        public int CondIndex { get; private set; } = -1;
        int condsampleindex = -1;
        int blocksampleindex = -1;
        List<int> blockrepeat  = new();
        List<int> condrepeat  = new();
        List<List<int>> condofblockrepeat = new();


        public Dictionary<string, List<object>> ReadConditionFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Condition File: {path} Not Found.");
                return null;
            }
            return path.ReadYamlFile<Dictionary<string, List<object>>>();
        }

        public Dictionary<string, List<object>> ProcessCondition(Dictionary<string, List<object>> cond)
        {
            if (cond != null && cond.Count > 0)
            {
                cond = cond.ProcessFactorDesign();
                cond = cond.ProcessOrthoCombineFactor();
            }
            return cond;
        }

        public void FinalizeCondition(Dictionary<string, List<object>> cond)
        {
            if (cond == null || cond.Count == 0)
            {
                Cond.Clear();
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
                Cond = cond.FinalizeFactorValue();
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

        public void InitializeSampleSpace(SampleMethod condsamplemethod, SampleMethod blocksamplemethod, List<string> blockfactor)
        {
            CondSampleMethod = condsamplemethod;
            BlockSampleMethod = blocksamplemethod;
            BlockCond.Clear();
            BlockSampleSpace.Clear();
            CondSampleSpace.Clear();
            if (NCond == 0) { Debug.LogWarning("Empty Condition, Skip Init Sampling Space ..."); return; }

            if(blockfactor==null || blockfactor.Count==0) { BlockFactor.Clear(); }
            else { BlockFactor = Cond.Keys.Intersect(blockfactor).ToList(); }
            NonBlockFactor = Cond.Keys.Except(BlockFactor).ToList();
            var bfn = BlockFactor.Count;
            if (bfn==0 || bfn==Cond.Count || NCond==1)
            {
                BlockSampleSpace.Add(0);
                CondSampleSpace.Add(GetSampleSpace(NCond, CondSampleMethod));
            }
            else
            {
                BlockCond = Cond.CondGroup(BlockFactor, out List<List<int>> CondSampleSpace);
                BlockSampleSpace = GetSampleSpace(NBlock, BlockSampleMethod);
                for(var i = 0;i < NBlock; i++)
                {
                    CondSampleSpace[i] = GetSampleSpace(CondSampleSpace[i], CondSampleMethod);
                }
            }
        }

        public void ResetSampling()
        {
            condrepeat.Clear();
            blockrepeat.Clear();
            condofblockrepeat.Clear();

            condrepeat.AddRange(Enumerable.Repeat(0, NCond));
            blockrepeat.AddRange(Enumerable.Repeat(0,NBlock));
            for(var i = 0; i < NBlock; i++)
            {
                var cobr = new List<int>();
                cobr.AddRange(Enumerable.Repeat(0, CondSampleSpace[i].Count));
                condofblockrepeat.Add(cobr);
            }

            NSampleSkip = 0;
            ScendingStep = 1;
            blocksampleindex = -1;
            condsampleindex = -1;
            BlockIndex = -1;
            CondIndex = -1;
        }

        public void InitializeSampling(SampleMethod condsamplemethod, SampleMethod blocksamplemethod, List<string> blockfactor)
        {
            InitializeSampleSpace(condsamplemethod, blocksamplemethod, blockfactor);
            ResetSampling();
        }

        public void ResetCondOfBlockSampling(int blockidx)
        {
            for(var i = 0;i < condofblockrepeat[blockidx].Count;i++)
            {
                condofblockrepeat[blockidx][i]=0;
            }
            condsampleindex = -1;
        }

        public int SampleBlockSpace(int manualblockindex = 0)
        {
            if (NBlock == 0) { return -1; }
            switch (BlockSampleMethod)
            {
                case SampleMethod.Ascending:
                case SampleMethod.Descending:
                    blocksampleindex += ScendingStep;
                    if (blocksampleindex > NBlock - 1)
                    {
                        blocksampleindex = 0;
                    }
                    BlockIndex = BlockSampleSpace[blocksampleindex];
                    break;
                case SampleMethod.UniformWithReplacement:
                    blocksampleindex = RNG.Next(NBlock);
                    BlockIndex = BlockSampleSpace[blocksampleindex];
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    blocksampleindex++;
                    if (blocksampleindex > NBlock - 1)
                    {
                        BlockSampleSpace = GetSampleSpace(BlockSampleSpace, BlockSampleMethod);
                        blocksampleindex = 0;
                    }
                    BlockIndex = BlockSampleSpace[blocksampleindex];
                    break;
                case SampleMethod.Manual:
                    BlockIndex = manualblockindex;
                    break;
            }

            blockrepeat[BlockIndex] += 1;
            ResetCondOfBlockSampling(BlockIndex);
            return BlockIndex;
        }

        public int SampleCondSpace(int manualcondidx = 0)
        {
            if (NCond == 0) { return -1; }
            switch (CondSampleMethod)
            {
                case SampleMethod.Ascending:
                case SampleMethod.Descending:
                    condsampleindex += ScendingStep;
                    if (condsampleindex > CondSampleSpace[BlockIndex].Count - 1)
                    {
                        condsampleindex = 0;
                    }
                    CondIndex = CondSampleSpace[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.UniformWithReplacement:
                    condsampleindex = RNG.Next(CondSampleSpace[BlockIndex].Count);
                    CondIndex = CondSampleSpace[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    condsampleindex++;
                    if (condsampleindex > CondSampleSpace[BlockIndex].Count - 1)
                    {
                        CondSampleSpace[BlockIndex] = GetSampleSpace(CondSampleSpace[BlockIndex], CondSampleMethod);
                        condsampleindex = 0;
                    }
                    CondIndex = CondSampleSpace[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.Manual:
                    CondIndex = manualcondidx;
                    break;
            }
            condofblockrepeat[BlockIndex][CondIndex] += 1;
            condrepeat[CondIndex] += 1;
            return CondIndex;
        }

        public int SampleCondition(int condofblockrepeat, int manualcondindex = 0, int manualblockindex = 0, bool autosampleblock = true)
        {
            if (NCond == 0) { return -1; }
            if (NSampleSkip <= 0)
            {
                if (BlockIndex < 0) { SampleBlockSpace(manualblockindex); }
                if (autosampleblock) { if (IsCondOfBlockRepeat(BlockIndex, condofblockrepeat)) { SampleBlockSpace(manualblockindex); } }
                SampleCondSpace(manualcondindex);
            }
            else
            {
                NSampleSkip--;
            }
            return CondIndex;
        }

        public void PushCondition(int condindex, INetEnv envmanager,bool includeblockfactor=false, List<string> excludefactor = null)
        {
            if (NCond == 0 || condindex < 0) { return; }
            var condfactors = includeblockfactor ? Cond.Keys.ToList() : NonBlockFactor;
            var factors = excludefactor == null ? condfactors : condfactors.Except(excludefactor);
            foreach (var f in factors)
            {
                envmanager.SetParam(f, Cond[f][condindex]);
            }
        }

        public void PushBlock(int blockindex, INetEnv envmanager, List<string> excludefactor = null)
        {
            if (blockindex < 0 || BlockCond.Count == 0) { return; }
            var factors = excludefactor == null ? BlockFactor : BlockFactor.Except(excludefactor);
            foreach (var f in factors)
            {
                envmanager.SetParam(f, BlockCond[f][blockindex]);
            }
        }

        public bool IsCondRepeat(int condindex,int n) { return condrepeat[condindex] >= n; }
        public bool IsBlockRepeat(int blockindex, int n) { return blockrepeat[blockindex] >= n; }
        public bool IsCondOfBlockRepeat(int blockindex, int n)
        {
            for (var i = 0; i < condofblockrepeat[blockindex].Count; i++)
            {
                if (condofblockrepeat[blockindex][i] < n) { return false; }
            }
            return true;
        }

        public bool IsAllCondRepeat(int n)
        {
            if (NCond == 0) { return false; }
            for (var i = 0; i < NCond; i++)
            {
                if (condrepeat[i] < n) { return false; }
            }
            return true;
        }

        public bool IsCondAndBlockRepeat(int condofblockrepeat,int blockrepeat)
        {
            var total = Math.Max(0, condofblockrepeat) * Math.Max(1, blockrepeat);
            return IsAllCondRepeat(total);
        }

        public List<int> CurrentCondSampleSpace=> CondSampleSpace[BlockIndex];
        public int CurrentCondRepeat => condrepeat[CondIndex];
        public int CurrentBlockRepeat => blockrepeat[BlockIndex];
    }

}