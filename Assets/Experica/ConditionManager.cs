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
        public List<string> BlockFactor { get; private set; } = new();
        public List<string> NonBlockFactor { get; private set; } = new();
        public int NBlockCond
        {
            get
            {
                if (BlockCond.Count == 0) { return 0; }
                return BlockCond.Values.First().Count;
            }
        }
        public Dictionary<string, IList> BlockCond { get; private set; } = new();
        public int NBlock => BlockSampleSpace.Count;
        public List<List<int>> CondSampleSpaces { get; private set; } = new();
        public List<int> BlockSampleSpace { get; private set; } = new();
        public SampleMethod CondSampleMethod { get; private set; } = SampleMethod.Ascending;
        public SampleMethod BlockSampleMethod { get; private set; } = SampleMethod.Ascending;

        public int NSampleSkip = 0;
        public int ScendingStep = 1;
        public System.Random RNG = new MersenneTwister();
        public int BlockIndex { get; private set; } = -1;
        public int CondIndex { get; private set; } = -1;

        int condsampleindex = -1;
        int blocksampleindex = -1;
        List<int> blockrepeat = new();
        List<int> condrepeat = new();
        List<List<int>> condofblockrepeat = new();


        public static Dictionary<string, List<object>> ReadConditionFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Condition File: {path} Not Found.");
                return null;
            }
            return path.ReadYamlFile<Dictionary<string, List<object>>>();
        }

        public static Dictionary<string, List<object>> ProcessCondition(Dictionary<string, List<object>> cond)
        {
            if (cond != null && cond.Count > 0)
            {
                cond = cond.ProcessFactorDesign();
                cond = cond.ProcessOrthoCombineFactor();
            }
            return cond;
        }

        /// <summary>
        /// prepare and set the `Cond`
        /// </summary>
        /// <param name="cond"></param>
        public void PrepareCondition(Dictionary<string, List<object>> cond)
        {
            if (cond == null || cond.Count == 0)
            {
                Cond.Clear(); return;
            }
            else
            {
                var fln = cond.Values.Select(i => i.Count).ToArray();
                var minfln = fln.Min();
                var maxfln = fln.Max();
                if (minfln != maxfln)
                {
                    foreach (var f in cond.Keys.ToArray())
                    {
                        cond[f] = cond[f].GetRange(0, minfln);
                    }
                }
                if (cond.Values.First().Count == 0) { Cond.Clear(); return; }
                Cond = cond.SpecializeFactorValue();
            }
        }

        public void PrepareCondition(string path) { PrepareCondition(ProcessCondition(ReadConditionFile(path))); }


        List<int> GetSampleSpace(List<int> space, SampleMethod samplemethod)
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

        List<int> GetSampleSpace(int spacesize, SampleMethod samplemethod)
        {
            return samplemethod switch
            {
                SampleMethod.Descending => Enumerable.Range(0, spacesize).Reverse().ToList(),
                SampleMethod.UniformWithoutReplacement => Enumerable.Range(0, spacesize).SelectPermutation(RNG).ToList(),
                _ => Enumerable.Range(0, spacesize).ToList(),
            };
        }

        /// <summary>
        /// partition cond to blocks, prepare block factors/values, and init block and cond index sampling spaces
        /// </summary>
        /// <param name="condsamplemethod"></param>
        /// <param name="blocksamplemethod"></param>
        /// <param name="blockfactor"></param>
        public void InitializeSampleSpace(SampleMethod condsamplemethod, SampleMethod blocksamplemethod, List<string> blockfactor)
        {
            CondSampleMethod = condsamplemethod;
            BlockSampleMethod = blocksamplemethod;
            BlockCond.Clear();
            BlockSampleSpace.Clear();
            CondSampleSpaces.Clear();
            if (NCond == 0) { Debug.LogWarning("Empty Condition, Skip Init Sampling Space ..."); return; }

            if (blockfactor == null || blockfactor.Count == 0) { BlockFactor.Clear(); }
            else { BlockFactor = Cond.Keys.Intersect(blockfactor).ToList(); }
            NonBlockFactor = Cond.Keys.Except(BlockFactor).ToList();
            var bfn = BlockFactor.Count;
            if (bfn == 0 || bfn == Cond.Count || NCond == 1) // essentially no blocking, but all considered as one block containing all conditions
            {
                BlockSampleSpace.Add(0);
                CondSampleSpaces.Add(GetSampleSpace(NCond, CondSampleMethod));
            }
            else
            {
                BlockCond = Cond.CondGroup(BlockFactor, out List<List<int>> gi);
                BlockSampleSpace = GetSampleSpace(NBlockCond, BlockSampleMethod);
                gi.ForEach(i => CondSampleSpaces.Add(GetSampleSpace(i, CondSampleMethod)));
            }
        }

        public void ResetSampling()
        {
            condrepeat.Clear();
            blockrepeat.Clear();
            condofblockrepeat.Clear();

            condrepeat.AddRange(Enumerable.Repeat(0, NCond));
            blockrepeat.AddRange(Enumerable.Repeat(0, NBlock));
            for (var i = 0; i < NBlock; i++)
            {
                var cobr = new List<int>();
                cobr.AddRange(Enumerable.Repeat(0, CondSampleSpaces[i].Count));
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

        public void ResetCondOfBlockSampling(int blockindex)
        {
            for (var i = 0; i < condofblockrepeat[blockindex].Count; i++)
            {
                condofblockrepeat[blockindex][i] = 0;
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

        public int SampleCondSpace(int manualcondindex = 0)
        {
            if (NCond == 0) { return -1; }
            switch (CondSampleMethod)
            {
                case SampleMethod.Ascending:
                case SampleMethod.Descending:
                    condsampleindex += ScendingStep;
                    if (condsampleindex > CondSampleSpaces[BlockIndex].Count - 1)
                    {
                        condsampleindex = 0;
                    }
                    CondIndex = CondSampleSpaces[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.UniformWithReplacement:
                    condsampleindex = RNG.Next(CondSampleSpaces[BlockIndex].Count);
                    CondIndex = CondSampleSpaces[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    condsampleindex++;
                    if (condsampleindex > CondSampleSpaces[BlockIndex].Count - 1)
                    {
                        CondSampleSpaces[BlockIndex] = GetSampleSpace(CondSampleSpaces[BlockIndex], CondSampleMethod);
                        condsampleindex = 0;
                    }
                    CondIndex = CondSampleSpaces[BlockIndex][condsampleindex];
                    break;
                case SampleMethod.Manual:
                    CondIndex = manualcondindex;
                    for (var i = 0; i < NBlock; i++)
                    {
                        var j = CondSampleSpaces[i].IndexOf(CondIndex);
                        if (j > -1) { BlockIndex = i; condsampleindex = j; break; }
                    }
                    break;
            }
            condofblockrepeat[BlockIndex][condsampleindex] += 1;
            condrepeat[CondIndex] += 1;
            return CondIndex;
        }

        public int SampleCondition(int condofblockrepeat = 1, int manualcondindex = 0, int manualblockindex = 0, bool autosampleblock = true)
        {
            if (NCond == 0) { return -1; }
            if (NSampleSkip < 1)
            {
                if (BlockIndex < 0) { SampleBlockSpace(manualblockindex); }
                if (autosampleblock) { if (IsAllCondOfBlockRepeated(BlockIndex, condofblockrepeat)) { SampleBlockSpace(manualblockindex); } }
                SampleCondSpace(manualcondindex);
            }
            else
            {
                NSampleSkip--;
            }
            return CondIndex;
        }

        /// <summary>
        /// Push the factors/value of a condition to NetEnv scene
        /// </summary>
        /// <param name="condindex"></param>
        /// <param name="envmanager"></param>
        /// <param name="includeblockfactor"></param>
        /// <param name="excludefactor"></param>
        public void PushCondition(int condindex, INetEnv envmanager, bool includeblockfactor = false, List<string> excludefactor = null)
        {
            if (condindex < 0 || NCond == 0) { return; }
            var condfactors = includeblockfactor ? Cond.Keys.ToList() : NonBlockFactor;
            var factors = excludefactor == null ? condfactors : condfactors.Except(excludefactor);
            foreach (var f in factors)
            {
                envmanager.SetParam(f, Cond[f][condindex]);
            }
        }

        /// <summary>
        /// Push the factors/value of a block to NetEnv scene
        /// </summary>
        /// <param name="blockindex"></param>
        /// <param name="envmanager"></param>
        /// <param name="excludefactor"></param>
        public void PushBlock(int blockindex, INetEnv envmanager, List<string> excludefactor = null)
        {
            if (blockindex < 0 || NBlockCond == 0) { return; }
            var factors = excludefactor == null ? BlockFactor : BlockFactor.Except(excludefactor);
            foreach (var f in factors)
            {
                envmanager.SetParam(f, BlockCond[f][blockindex]);
            }
        }


        /// <summary>
        /// whether a condition have been sampled on total certain times
        /// </summary>
        /// <param name="condindex"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsCondRepeated(int condindex, int n) { return condrepeat[condindex] >= n; }
        /// <summary>
        /// whether a block have been sampled certain times
        /// </summary>
        /// <param name="blockindex"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsBlockRepeated(int blockindex, int n) { return blockrepeat[blockindex] >= n; }
        /// <summary>
        /// whether all conditions in a block have been sampled certain times
        /// </summary>
        /// <param name="blockindex"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsAllCondOfBlockRepeated(int blockindex, int n)
        {
            for (var i = 0; i < condofblockrepeat[blockindex].Count; i++)
            {
                if (condofblockrepeat[blockindex][i] < n) { return false; }
            }
            return true;
        }
        /// <summary>
        /// whether all conditions have been sampled on total certain times
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsAllCondRepeated(int n)
        {
            if (NCond == 0) { return false; }
            for (var i = 0; i < NCond; i++)
            {
                if (condrepeat[i] < n) { return false; }
            }
            return true;
        }
        /// <summary>
        /// whether all conditions of all blocks have been sampled certain times, 
        /// the individual condition is required to repeat `condofblockrepeat` * `blockrepeat` times.
        /// </summary>
        /// <param name="condofblockrepeat"></param>
        /// <param name="blockrepeat"></param>
        /// <returns></returns>
        public bool IsCondOfAndBlockRepeated(int condofblockrepeat, int blockrepeat)
        {
            var total = Math.Max(0, condofblockrepeat) * Math.Max(1, blockrepeat);
            return IsAllCondRepeated(total);
        }

        public List<int> CurrentCondSampleSpace => CondSampleSpaces[BlockIndex];
        public int CurrentCondRepeat => condrepeat[CondIndex];
        public int CurrentBlockRepeat => blockrepeat[BlockIndex];
    }

}