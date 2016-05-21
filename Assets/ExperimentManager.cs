// --------------------------------------------------------------
// ExperimentManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
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
        public VLApplicationManager appmanager;
        public List<ExperimentLogic> elhistory = new List<ExperimentLogic>();

        public List<string> exdefs = new List<string>();
        public List<string> exdefnames = new List<string>();
        public ExperimentLogic el;

        public void UpdateExDef()
        {
            var exdefdir = (string)appmanager.config["exdefdir"];
            if (Directory.Exists(exdefdir))
            {
                exdefs = Directory.GetFiles(exdefdir, "*.yaml", SearchOption.AllDirectories).ToList();
                exdefnames.Clear();
                foreach (var f in exdefs)
                {
                    exdefnames.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            else
            {
                Directory.CreateDirectory(exdefdir);
            }
        }

        public Experiment LoadExDef(string path)
        {
            var ex = Yaml.ReadYaml<Experiment>(path);
            if (string.IsNullOrEmpty( ex.name))
            {
                ex.name = Path.GetFileNameWithoutExtension(path);
            }
            return ValidateExperiment(ex);
        }

        Experiment ValidateExperiment(Experiment ex)
        {
            if (string.IsNullOrEmpty(ex.id))
            {
                ex.id = ex.name;
            }
            if (string.IsNullOrEmpty(ex.condtestdir))
            {
                var condtestdir = (string)appmanager.config["condtestdir"];
                if (!Directory.Exists(condtestdir))
                {
                    Directory.CreateDirectory(condtestdir);
                }
                ex.condtestdir = condtestdir;
            }
            if (ex.param == null)
            {
                ex.param = new Dictionary<string, object>();
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
            if (string.IsNullOrEmpty(elname))
            {
                eltype = Type.GetType((string)appmanager.config["defaultexperimentlogic"]);
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
                    eltype = Type.GetType((string)appmanager.config["defaultexperimentlogic"]);
                }
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
            el.ex = ex;
            AddEL(el);
        }

        public bool NewExDef(string name, string copyfrom)
        {
            if (string.IsNullOrEmpty(copyfrom))
            {
                return NewExDef(name);
            }
            else
            {
                if (exdefnames.Contains(name))
                {
                    return false;
                }
                else
                {
                    if (exdefnames.Contains(copyfrom))
                    {
                        var ex = Yaml.ReadYaml<Experiment>(exdefs[exdefnames.IndexOf(copyfrom)]);
                        ex.name = name;
                        ex.id = name;
                        LoadEx(ValidateExperiment(ex));

                        exdefnames.Add(name);
                        exdefs.Add(Path.Combine((string)appmanager.config["exdefdir"], name + ".yaml"));
                        SaveExDef(name);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
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
                LoadEx(ValidateExperiment(ex));

                exdefnames.Add(name);
                exdefs.Add(Path.Combine((string)appmanager.config["exdefdir"], name + ".yaml"));
                SaveExDef(name);
                return true;
            }
        }

        public void SaveExDef(string name)
        {
            if (exdefnames.Contains(name))
            {
                var i = FindFirstInELHistory(name);
                if (i >= 0)
                {
                    var ex = elhistory[i].ex;
                    var cond = ex.cond;
                    var condtest = ex.condtest;
                    ex.cond = null;
                    ex.condtest = null;
                    ex.envparam = elhistory[i].envmanager.GetParams();
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

        public int DeleteExDef(string name)
        {
            if (exdefnames.Contains(name))
            {
                var i = FindFirstInELHistory(name);
                if (i >= 0)
                {
                    Destroy(elhistory[i]);
                    elhistory.RemoveAt(i);
                }
                var di = exdefnames.IndexOf(name);
                File.Delete(exdefs[di]);
                exdefs.RemoveAt(di);
                exdefnames.RemoveAt(di);
                return di;
            }
            else
            {
                return -1;
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
            var i = FindFirstInELHistory(elhistory.Last().ex.id);
            if (i == elhistory.Count - 1)
            {
                i = -1;
            }
            return i;
        }

        public int FindFirstInELHistory(string exid)
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
            if (elhistory.Last().ex.exinheritparams.Count > 0)
            {
                foreach (var ip in elhistory.Last().ex.exinheritparams)
                {
                    if (ip != "exinheritparams")
                    {
                        InheritExParam(ip);
                    }
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
            if (elhistory.Last().ex.envinheritparams.Count > 0)
            {
                foreach (var ip in elhistory.Last().ex.envinheritparams)
                {
                    if (ip != "envinheritparams")
                    {
                        InheritEnvParam(ip);
                    }
                }
            }
        }

        public void InheritEnvParam(string name)
        {
            var hn = elhistory.Count;
            if (hn > 1)
            {
                for (var i = hn - 2; i > -1; i--)
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