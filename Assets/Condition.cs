// --------------------------------------------------------------
// Condition.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VLab
{
    public enum SampleMethod
    {
        Ascending = 0,
        Descending = 1,
        UniformWithReplacement = 2,
        UniformWithoutReplacement = 3
    }

    public class ConditionManager
    {
        public Dictionary<string, List<object>> cond;
        public int nfactor;
        public int ncond;
        public List<int> popcondidx;
        public Dictionary<int, int> condrepeat;
        public int sampleidx = -1;
        public SampleMethod samplemethod = SampleMethod.Ascending;
        public int scendingstep = 1;
        public int condidx = -1;
        public int sampleignores = 0;
        public System.Random rng = new System.Random();
        public EnvironmentManager envmanager = new EnvironmentManager();


        public Dictionary<string, List<object>> ReadCondition(string path)
        {
            if (!File.Exists(path))
            {
                UnityEngine.Debug.Log("File Does not exist.");
                return null;
            }
            return Yaml.ReadYaml<Dictionary<string, List<object>>>(path);
        }

        public void TrimCondition(Dictionary<string, List<object>> cond)
        {
            this.cond = cond;
            nfactor = cond.Keys.Count;
            if (nfactor == 0)
            {
                UnityEngine.Debug.Log("Condition Empty.");
            }
            else
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
                    UnityEngine.Debug.Log("Cut Condition to Minimum Length.");
                }
                ncond = minfvn;
            }
        }

        public List<int> UpdateCondPopulation(bool resetcondrepeat = true)
        {
            switch (samplemethod)
            {
                case SampleMethod.Ascending:
                    popcondidx = Enumerable.Range(0, ncond).ToList();
                    sampleidx = -1;
                    break;
                case SampleMethod.Descending:
                    popcondidx = Enumerable.Range(0, ncond).Reverse().ToList();
                    sampleidx = -1;
                    break;
                case SampleMethod.UniformWithReplacement:
                    popcondidx = Enumerable.Range(0, ncond).ToList();
                    sampleidx = -1;
                    break;
                case SampleMethod.UniformWithoutReplacement:
                    popcondidx = Enumerable.Range(0, ncond).ToList();
                    sampleidx = -1;
                    break;
            }
            if (resetcondrepeat)
            {
                condrepeat = new Dictionary<int, int>();
                foreach (var i in popcondidx)
                {
                    condrepeat[i] = 0;
                }
            }
            return popcondidx;
        }

        public int SampleCondIdx()
        {
            if (sampleignores == 0)
            {
                switch (samplemethod)
                {
                    case SampleMethod.Ascending:
                        sampleidx += scendingstep;
                        if (sampleidx > popcondidx.Count - 1)
                        {
                            sampleidx -= popcondidx.Count;
                        }
                        condidx = popcondidx[sampleidx];
                        break;
                    case SampleMethod.Descending:
                        sampleidx += scendingstep;
                        if (sampleidx > popcondidx.Count - 1)
                        {
                            sampleidx -= popcondidx.Count;
                        }
                        condidx = popcondidx[sampleidx];
                        break;
                    case SampleMethod.UniformWithReplacement:
                        sampleidx = rng.Next(popcondidx.Count);
                        condidx = popcondidx[sampleidx];
                        break;
                    case SampleMethod.UniformWithoutReplacement:
                        if (popcondidx.Count == 0)
                        {
                            UpdateCondPopulation(false);
                        }
                        sampleidx = rng.Next(popcondidx.Count);
                        condidx = popcondidx[sampleidx];
                        popcondidx.RemoveAt(sampleidx);
                        break;
                }
                condrepeat[condidx] += 1;
            }
            else
            {
                sampleignores--;
            }
            return condidx;
        }

        public void PushCondition(int idx)
        {
            foreach (var kv in cond)
            {
                envmanager.SetParam(kv.Key, kv.Value[idx]);
            }
        }

        public void SamplePushCondition()
        {
            PushCondition(SampleCondIdx());
        }

        public bool IsFinishRepeat(int n)
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
    }

    public interface IFactorLevelDesign
    {
        string FactorName { get; set; }
        KeyValuePair<string, List<object>> GenerateFactorLevels();
    }

    public enum DesignMethod
    {
        Linear = 0
    }

    public class FactorLevel : IFactorLevelDesign
    {
        string name;
        public object start, end, step;
        public DesignMethod method;

        public FactorLevel(string factorname, object startvalue, object endvalue, object stepvalue, DesignMethod designmethod = DesignMethod.Linear)
        {
            name = factorname;
            start = startvalue;
            end = endvalue;
            step = stepvalue;
            method = designmethod;
        }

        public string FactorName
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public KeyValuePair<string, List<object>> GenerateFactorLevels()
        {
            List<object> ls = new List<object>();
            ls.Add(start);
            switch (method)
            {
                case DesignMethod.Linear:
                    if (start.GetType() == typeof(float))
                    {
                        while ((float)ls.Last() < (float)end)
                        {
                            ls.Add((float)ls.Last() + (float)step);
                        }
                    }
                    if (start.GetType() == typeof(Vector3))
                    {
                        int xn = Mathf.FloorToInt((((Vector3)end).x - ((Vector3)start).x) / ((Vector3)step).x);
                        int yn = Mathf.FloorToInt((((Vector3)end).y - ((Vector3)start).y) / ((Vector3)step).y);
                        int zn = Mathf.Max(1, Mathf.FloorToInt((((Vector3)end).z - ((Vector3)start).z) / ((Vector3)step).z));
                        for (var xi = 0; xi < xn; xi++)
                        {
                            for (var yi = 0; yi < yn; yi++)
                            {
                                for (var zi = 0; zi < zn; zi++)
                                {
                                    ls.Add(new Vector3(
                                        ((Vector3)start).x + xi * ((Vector3)step).x,
                                        ((Vector3)start).y + yi * ((Vector3)step).y,
                                        ((Vector3)start).z + zi * ((Vector3)step).z));
                                }
                            }
                        }
                    }
                    break;
            }
            return new KeyValuePair<string, List<object>>(name, ls);
        }
    }

    public class ConditionDesigner
    {
        public Dictionary<string, List<object>> factorslevels = new Dictionary<string, List<object>>();

        public ConditionDesigner()
        {

        }

        public ConditionDesigner(params IFactorLevelDesign[] flds)
        {
            foreach (var fld in flds)
            {
                AddFactorLevels(fld);
            }
        }

        public void AddFactorLevels(IFactorLevelDesign fld)
        {
            var fls = fld.GenerateFactorLevels();
            factorslevels.Add(fls.Key, fls.Value);
        }

        public Dictionary<string, List<object>> GenerateCondition()
        {
            return GenerateCondition(factorslevels);
        }

        public static Dictionary<string, List<object>> GenerateCondition(Dictionary<string, List<object>> fsls)
        {
            Dictionary<string, List<object>> cond = new Dictionary<string, List<object>>();

            var fs = fsls.Keys.ToArray();
            var fn = fs.Length;
            if (fn == 0)
            {
                return cond;
            }
            else if (fn == 1)
            {
                cond[fs[0]] = fsls[fs[0]];
                return cond;
            }
            else
            {
                int[] ln = new int[fn];
                int[] ern = new int[fn];
                int cn = 1;
                ern[0] = 1;
                for (var i = 0; i < fn; i++)
                {
                    var n = fsls[fs[i]].Count;
                    cn *= n;
                    ln[i] = n;

                    if (i > 0)
                    {
                        ern[i] = ln[i - 1] * ern[i - 1];
                    }
                }

                for (var fi = 0; fi < fn; fi++)
                {
                    List<object> erls = new List<object>();
                    for (var j = 0; j < ln[fi]; j++)
                    {
                        for (var i = 0; i < ern[fi]; i++)
                        {
                            erls.Add(fsls[fs[fi]][j]);
                        }
                    }

                    var rn = cn / erls.Count;
                    List<object> cls = new List<object>();
                    for (var i = 0; i < rn; i++)
                    {
                        cls.AddRange(erls);
                    }
                    cond[fs[fi]] = cls;
                }
                return cond;
            }
        }
    }


}