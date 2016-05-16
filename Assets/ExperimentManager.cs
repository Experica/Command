// --------------------------------------------------------------
// ExperimentManager.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace VLab
{
    public class ExperimentManager : MonoBehaviour
    {
        public List<ExperimentLogic> elhistory = new List<ExperimentLogic>();
        public readonly string exdir = "Experiment";
        public readonly string ctdir = "ConditionTest";

        public List<string> exdefs = new List<string>();
        public List<string> exdefnames = new List<string>();

        public ExperimentLogic el;

        public void UpdateExDef()
        {
            if (Directory.Exists(exdir))
            {
                exdefs = Directory.GetFiles(exdir, "*.yaml", SearchOption.AllDirectories).ToList();
                foreach (var edfp in exdefs)
                {
                    exdefnames.Add(Path.GetFileNameWithoutExtension(edfp));
                }
            }
            else
            {
                Directory.CreateDirectory(exdir);
            }
        }

        public Experiment LoadExDef(string path)
        {
            var ex = Yaml.ReadYaml<Experiment>(path);
            if (ex.name == null)
            {
                ex.name = Path.GetFileNameWithoutExtension(path);
            }
            if (ex.id == null)
            {
                ex.id = ex.name;
            }
            if (ex.experimenter == null)
            {
                ex.experimenter = "";
            }
            if (ex.subject_id == null)
            {
                ex.subject_id = "";
            }
            if (ex.param == null)
            {
                ex.param = new Dictionary<string, object>();
            }
            if (ex.condpath == null)
            {
                ex.condpath = "";
            }
            if (ex.condtestdir == null)
            {
                if (!Directory.Exists(ctdir))
                {
                    Directory.CreateDirectory(ctdir);
                }
                ex.condtestdir = ctdir;
            }
            if (ex.envparam == null)
            {
                ex.envparam = new Dictionary<string, object>();
            }
            if (ex.exinheritparams == null)
            {
                ex.exinheritparams = new List<string>();
            }
            if (ex.envinheritparams == null)
            {
                ex.envinheritparams = new List<string>();
            }
            return ex;
        }

        public void LoadEx(string path)
        {
            LoadEx(LoadExDef(path));
        }

        public void LoadEx(Experiment ex)
        {
            var elname = ex.experimentlogicpath;
            Type eltype;
            if (elname == null || elname == "")
            {
                eltype = Type.GetType("ConditionTest");
            }
            else if (File.Exists(elname))
            {
                var assembly = CompilerService.Compile(elname);
                eltype = assembly.GetExportedTypes()[0];
            }
            else
            {
                eltype = Type.GetType(elname);
                if (eltype == null)
                {
                    eltype = Type.GetType("ConditionTest");
                }
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
            el.ex = ex;
            AddEL(el);
        }

        public bool NewExDef(string name, string copyfrom)
        {
            if (exdefnames.Contains(name))
            {
                return false;
            }
            else
            {
                var i = FindInELHistory(copyfrom);
                if (i >= 0)
                {
                    var ex = elhistory[i].ex.DeepCopy();
                    ex.name = name;
                    ex.id = name;
                    LoadEx(ex);

                    exdefnames.Add(name);
                    exdefs.Add(Path.Combine(exdir, name + ".yaml"));
                    SaveExDef(name);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsExDefNameExist(string name)
        {
            return exdefnames.Contains(name) ;
        }

        public bool NewExDef(string name)
        {
            if (exdefnames.Contains(name))
            {
                return false;
            }
            else
            {
                var ex = new Experiment();
                ex.name = name;
                ex.id = name;
                LoadEx(ex);

                exdefnames.Add(name);
                exdefs.Add(Path.Combine(exdir, name + ".yaml"));
                SaveExDef(name);
                return true;
            }
        }

        public void SaveExDef(string name)
        {
            if (exdefnames.Contains(name))
            {
                var i = FindInELHistory(name);
                if (i >= 0)
                {
                    var ex = elhistory[i].ex;
                    var cond = ex.cond;
                    var condtest = ex.condtest;
                    ex.cond = null;
                    ex.condtest = null;
                    ex.envparam = elhistory[i].envmanager.GetEnvParam();
                    try
                    {
                        Yaml.WriteYaml(exdefs[exdefnames.IndexOf(name)], ex);
                    }
                    finally
                    {
                        ex.cond = cond;
                        ex.condtest = condtest;
                    }
                }
            }
        }

        public void SaveAllExDef()
        {
            foreach (var n in exdefnames)
            {
                SaveExDef(n);
            }
        }

        public void DeleteExDef(string name)
        {
            if (exdefnames.Contains(name))
            {
                var i = FindInELHistory(name);
                if (i >= 0)
                {
                    elhistory.RemoveAt(i);
                }
                var di = exdefnames.IndexOf(name);
                exdefnames.RemoveAt(di);
                exdefs.RemoveAt(di);
                File.Delete(exdefs[di]);
            }
        }

        public void DeleteAllExDef()
        {
            foreach (var n in exdefnames)
            {
                DeleteExDef(n);
            }
        }

        public void AddEL(ExperimentLogic el)
        {
            if (elhistory.Count > 0)
            {
                elhistory.Last().enabled = false;
            }
            elhistory.Add(el);
            InheritEx();
            var idx = FindDuplicateOfLast();
            if (idx >= 0)
            {
                Destroy(elhistory[idx]);
                elhistory.RemoveAt(idx);
            }
        }

        public int FindDuplicateOfLast()
        {
            var i = FindInELHistory(elhistory.Last().ex.id);
            if (i == elhistory.Count - 1)
            {
                i = -1;
            }
            return i;
        }

        public int FindInELHistory(string exid)
        {
            var idx = -1;
            for (var i = 0; i < elhistory.Count; i++)
            {
                if (elhistory[i].ex.id == exid)
                {
                    return i;
                }
            }
            return idx;
        }

        public void InheritEx()
        {
            if (elhistory.Last().ex.exinheritparams.Count != 0)
            {
                foreach (var pn in elhistory.Last().ex.exinheritparams)
                {
                    InheritExParam(pn);
                }
            }
        }

        public void InheritExParam(string name)
        {
            var hn = elhistory.Count;
            if (hn > 1)
            {
                if (Experiment.properties.ContainsKey(name))
                {
                    elhistory.Last().ex.SetValue(name, elhistory[hn - 2].ex.GetValue(name));
                }
                else
                {
                    for (var i = hn - 2; i > -1; i--)
                    {
                        if (elhistory[i].ex.param.ContainsKey(name))
                        {
                            elhistory.Last().ex.param[name] = elhistory[i].ex.param[name];
                            break;
                        }
                    }
                }
            }
        }

        public void InheritEnv()
        {
            if (elhistory.Last().ex.envinheritparams.Count != 0)
            {
                foreach (var pn in elhistory.Last().ex.envinheritparams)
                {
                    InheritEnvParam(pn);
                }
            }
        }

        public void InheritEnvParam(string name)
        {
            var hn = elhistory.Count;
            if (hn > 1)
            {
                for (var i = elhistory.Count - 2; i > -1; i--)
                {
                    var em = elhistory[i].envmanager;
                    var v = em.GetParam(name);
                    if (v != null)
                    {
                        elhistory.Last().envmanager.SetParam(name, v);
                        break;
                    }
                }
            }
        }

    }
}