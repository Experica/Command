// -----------------------------------------------------------------------------
// ExperimentManager.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

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
        public VLUIController uicontroller;
        public List<ExperimentLogic> elhistory = new List<ExperimentLogic>();

        public List<string> exfiles = new List<string>();
        public List<string> exids = new List<string>();
        public ExperimentLogic el;

        void Awake()
        {
            GetExFiles();
        }

        public void GetExFiles()
        {
            var exfiledir = (string)appmanager.config[VLCFG.ExDir];
            if (Directory.Exists(exfiledir))
            {
                exfiles = Directory.GetFiles(exfiledir, "*.yaml", SearchOption.AllDirectories).ToList();
                exids.Clear();
                foreach (var f in exfiles)
                {
                    exids.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            else
            {
                Directory.CreateDirectory(exfiledir);
            }
        }

        public Experiment LoadEx(string exfilepath)
        {
            var ex = Yaml.ReadYaml<Experiment>(exfilepath);
            if (string.IsNullOrEmpty(ex.ID))
            {
                ex.ID = Path.GetFileNameWithoutExtension(exfilepath);
            }
            return ValidateExperiment(ex);
        }

        Experiment ValidateExperiment(Experiment ex)
        {
            if (string.IsNullOrEmpty(ex.Name))
            {
                ex.Name = ex.ID;
            }
            if(ex.Subject_ID==null)
            {
                ex.Subject_ID = "";
            }
            if(ex.Experimenter==null)
            {
                ex.Experimenter = "";
            }
            if (string.IsNullOrEmpty(ex.DataDir))
            {
                var datadir = (string)appmanager.config[VLCFG.DataDir];
                if (!Directory.Exists(datadir))
                {
                    Directory.CreateDirectory(datadir);
                }
                ex.DataDir = datadir;
            }
            if (ex.Param == null)
            {
                ex.Param = new Dictionary<string, object>();
            }
            if (ex.EnvParam == null)
            {
                ex.EnvParam = new Dictionary<string, object>();
            }
            if (ex.ExInheritParam == null)
            {
                ex.ExInheritParam = new List<string>();
            }
            if (ex.EnvInheritParam == null)
            {
                ex.EnvInheritParam = new List<string>();
            }
            if (ex.CondTest != null)
            {
                ex.CondTest = null;
            }
            if (ex.NotifyParam == null)
            {
                ex.NotifyParam = (List<CONDTESTPARAM>)appmanager.config[VLCFG.NotifyParams];
            }
            return ex;
        }

        public void LoadEL(string exfilepath)
        {
            LoadEL(LoadEx(exfilepath));
        }

        public void LoadEL(Experiment ex)
        {
            var elname = ex.ExLogicPath;
            Type eltype = null;
            if (!string.IsNullOrEmpty(elname))
            {
                if (File.Exists(elname))
                {
                    var assembly = CompilerService.Compile(elname);
                    eltype = assembly.GetExportedTypes()[0];
                }
                else
                {
                    eltype = Type.GetType(elname);
                }
            }
            if (eltype == null)
            {
                var elpath = (string)appmanager.config[VLCFG.ExLogic];
                eltype = Type.GetType(elpath);
                ex.ExLogicPath = elpath;
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
            el.ex = ex;
            AddEL(el);
        }

        public bool NewEx(string id, string idcopyfrom)
        {
            if (string.IsNullOrEmpty(idcopyfrom))
            {
                return NewEx(id);
            }
            else
            {
                if (exids.Contains(id))
                {
                    return false;
                }
                else
                {
                    if (exids.Contains(idcopyfrom))
                    {
                        var ex = Yaml.ReadYaml<Experiment>(exfiles[exids.IndexOf(idcopyfrom)]);
                        ex.ID = id;
                        ex.Name = id;
                        LoadEL(ValidateExperiment(ex));

                        exids.Add(id);
                        exfiles.Add(Path.Combine((string)appmanager.config[VLCFG.ExDir], id + ".yaml"));
                        SaveEx(id);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public bool NewEx(string id)
        {
            if (exids.Contains(id))
            {
                return false;
            }
            else
            {
                var ex = new Experiment();
                ex.ID = id;
                ex.Name = id;
                LoadEL(ValidateExperiment(ex));

                exids.Add(id);
                exfiles.Add(Path.Combine((string)appmanager.config[VLCFG.ExDir], id + ".yaml"));
                SaveEx(id);
                return true;
            }
        }

        public void SaveEx(string id)
        {
            if (exids.Contains(id))
            {
                var i = FindFirstInELHistory(id);
                if (i >= 0)
                {
                    var ex = elhistory[i].ex;
                    // Exclude data, only save experiment parameters
                    var datapath = ex.DataPath;
                    var condtest = ex.CondTest;
                    var cond = ex.Cond;
                    ex.DataPath = null;
                    ex.CondTest = null;
                    ex.Cond = null;
                    ex.EnvParam = elhistory[i].envmanager.GetParams();
                    try
                    {
                        Yaml.WriteYaml(exfiles[exids.IndexOf(id)], ex);
                    }
                    finally
                    {
                        ex.DataPath = datapath;
                        ex.CondTest = condtest;
                        ex.Cond = cond;
                    }
                }
            }
        }

        public void SaveAllEx()
        {
            foreach (var n in exids)
            {
                SaveEx(n);
            }
        }

        public int DeleteEx(string id)
        {
            if (exids.Contains(id))
            {
                var i = FindFirstInELHistory(id);
                if (i >= 0)
                {
                    Destroy(elhistory[i]);
                    elhistory.RemoveAt(i);
                }
                var idi = exids.IndexOf(id);
                File.Delete(exfiles[idi]);
                exfiles.RemoveAt(idi);
                exids.RemoveAt(idi);
                return idi;
            }
            else
            {
                return -1;
            }
        }

        public void DeleteAllEx()
        {
            foreach (var n in exids.ToArray())
            {
                DeleteEx(n);
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
            RemoveDuplicateEx();
            AddELCallback();
        }

        void RemoveDuplicateEx()
        {
            var idx = FindDuplicateOfLast();
            if (idx >= 0)
            {
                Destroy(elhistory[idx]);
                elhistory.RemoveAt(idx);
            }
        }

        void AddELCallback()
        {
            el.OnBeginStartExperiment = uicontroller.OnBeginStartExperiment;
            el.OnEndStartExperiment = uicontroller.OnEndStartExperiment;
            el.OnBeginStopExperiment = uicontroller.OnBeginStopExperiment;
            el.OnEndStopExperiment = uicontroller.OnEndStopExperiment;
            el.OnBeginPauseExperiment = uicontroller.OnBeginPauseExperiment;
            el.OnEndPauseExperiment = uicontroller.OnEndPauseExperiment;
            el.OnBeginResumeExperiment = uicontroller.OnBeginResumeExperiment;
            el.OnEndResumeExpeirment = uicontroller.OnEndResumeExpeirment;
            el.condtestmanager.OnNotifyCondTest = uicontroller.OnNotifyCondTest;
            el.condtestmanager.OnNotifyCondTestEnd = uicontroller.OnNotifyCondTestEnd;
            el.envmanager.OnNotifyUI = uicontroller.envpanel.UpdateParamUI;
            el.ex.OnNotifyUI = uicontroller.expanel.UpdateParamUI;
        }

        public int FindDuplicateOfLast()
        {
            var i = FindFirstInELHistory(elhistory.Last().ex.ID);
            if (i == elhistory.Count - 1)
            {
                i = -1;
            }
            return i;
        }

        public int FindFirstInELHistory(string exid)
        {
            for (var i = 0; i < elhistory.Count; i++)
            {
                if (elhistory[i].ex.ID == exid)
                {
                    return i;
                }
            }
            return -1;
        }

        public void InheritEx()
        {
            if (elhistory.Last().ex.ExInheritParam.Count > 0)
            {
                foreach (var ip in elhistory.Last().ex.ExInheritParam.ToArray())
                {
                    InheritExParam(ip);
                }
            }
        }

        public void InheritExParam(string name)
        {
            var hn = elhistory.Count;
            if (hn > 1)
            {
                if (Experiment.Properties.ContainsKey(name))
                {
                    elhistory.Last().ex.SetValue(name, elhistory[hn - 2].ex.GetValue(name));
                }
                else
                {
                    for (var i = hn - 2; i > -1; i--)
                    {
                        if (elhistory[i].ex.Param.ContainsKey(name))
                        {
                            elhistory.Last().ex.Param[name] = elhistory[i].ex.Param[name];
                            break;
                        }
                    }
                }
            }
        }

        public void OnServerSceneChanged(string scenename)
        {
            el.envmanager.AddScene(scenename);
            el.envmanager.SetParams(el.ex.EnvParam);
            InheritEnv();
        }

        public void InheritEnv(string forobjname = null)
        {
            foreach (var ip in elhistory.Last().ex.EnvInheritParam.ToArray())
            {
                InheritEnvParam(ip, forobjname);
            }
        }

        public void InheritEnvParam(string name, string forobjname = null)
        {
            if (forobjname != null && name.IsEnvParamFullName())
            {
                if (name.LastAtSplitTail() != forobjname)
                {
                    return;
                }
            }

            var hn = elhistory.Count;
            if (hn > 1)
            {
                for (var i = hn - 2; i > -1; i--)
                {
                    var em = elhistory[i].envmanager;
                    var v = em.GetParam(name);
                    if (v != null)
                    {
                        if (forobjname == null)
                        {
                            elhistory.Last().envmanager.SetParam(name, v);
                        }
                        else
                        {
                            if (name.IsEnvParamShortName())
                            {
                                foreach (var p in elhistory.Last().envmanager.net_syncvar.Keys)
                                {
                                    var pshort = p.FirstAtSplitHead();
                                    var pobj = p.LastAtSplitTail();
                                    if (pshort == name && pobj == forobjname)
                                    {
                                        elhistory.Last().envmanager.SetParam(p, v);
                                    }
                                }
                            }
                            else
                            {
                                elhistory.Last().envmanager.SetParam(name, v);
                            }
                        }
                        break;
                    }
                }
            }
        }

    }
}