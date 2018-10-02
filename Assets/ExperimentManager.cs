/*
ExperimentManager.cs is part of the Experica.
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
using UnityEngine.Networking;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace Experica.Command
{
    public class ExperimentManager : MonoBehaviour
    {
        public UIController uicontroller;
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
            var exfiledir = uicontroller.config.ExDir;
            if (Directory.Exists(exfiledir))
            {
                exfiles = Directory.GetFiles(exfiledir, "*.yaml", SearchOption.AllDirectories).ToList();
                exids.Clear();
                foreach (var f in exfiles)
                {
                    exids.Add(Path.GetFileNameWithoutExtension(f));
                }
                var firsttestid = uicontroller.config.FirstTestID;
                if (exids.Contains(firsttestid))
                {
                    var i = exids.IndexOf(firsttestid);
                    exids.RemoveAt(i);
                    exfiles.RemoveAt(i);
                    exids.Insert(0, firsttestid);
                    exfiles.Insert(0, Path.Combine(exfiledir, firsttestid + ".yaml"));
                }
            }
            else
            {
                Directory.CreateDirectory(exfiledir);
            }
        }

        public Experiment LoadEx(string exfilepath)
        {
            var ex = exfilepath.ReadYamlFile<Experiment>();
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
            if (string.IsNullOrEmpty(ex.Subject_Name))
            {
                ex.Subject_Name = ex.Subject_ID;
            }
            if (string.IsNullOrEmpty(ex.DataDir))
            {
                var datadir = uicontroller.config.DataDir;
                if (!Directory.Exists(datadir))
                {
                    Directory.CreateDirectory(datadir);
                }
                ex.DataDir = datadir;
            }
            if (ex.CondTest != null)
            {
                ex.CondTest = null;
            }
            if (ex.NotifyParam == null)
            {
                ex.NotifyParam = uicontroller.config.NotifyParams;
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
                    // need to add runtime compiler service
                    //var assembly = CompilerService.Compile(elname);
                    //eltype = assembly.GetExportedTypes()[0];
                }
                else
                {
                    eltype = Type.GetType(elname);
                }
            }
            if (eltype == null)
            {
                var elpath = uicontroller.config.ExLogic;
                eltype = Type.GetType(elpath);
                ex.ExLogicPath = elpath;
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
            el.config = uicontroller.config;
            el.ex = ex;
            uicontroller.condpanel.forceprepare.isOn = el.regeneratecond;
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
                if (!exids.Contains(id) && exids.Contains(idcopyfrom))
                {
                    var ex = exfiles[exids.IndexOf(idcopyfrom)].ReadYamlFile<Experiment>();
                    ex.ID = id;
                    ex.Name = id;
                    LoadEL(ValidateExperiment(ex));

                    exids.Add(id);
                    exfiles.Add(Path.Combine(uicontroller.config.ExDir, id + ".yaml"));
                    SaveEx(id);
                    return true;
                }
                return false;
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
                var ex = new Experiment
                {
                    ID = id
                };
                LoadEL(ValidateExperiment(ex));

                exids.Add(id);
                exfiles.Add(Path.Combine(uicontroller.config.ExDir, id + ".yaml"));
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
                    // Exclude data and config, only save experiment definition
                    var datapath = ex.DataPath;
                    var condtest = ex.CondTest;
                    var config = ex.Config;
                    var cond = ex.Cond;
                    ex.DataPath = null;
                    ex.CondTest = null;
                    ex.Config = null;
                    ex.Cond = null;
                    ex.EnvParam = elhistory[i].envmanager.GetParams();
                    try
                    {
                        exfiles[exids.IndexOf(id)].WriteYamlFile(ex);
                    }
                    finally
                    {
                        ex.DataPath = datapath;
                        ex.CondTest = condtest;
                        ex.Config = config;
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
            el.OnConditionPrepared = uicontroller.condpanel.RefreshCondition;
            el.condtestmanager.OnNotifyCondTest = uicontroller.OnNotifyCondTest;
            el.condtestmanager.OnNotifyCondTestEnd = uicontroller.OnNotifyCondTestEnd;
            el.condtestmanager.PushUICondTest = uicontroller.ctpanel.StartCondTest;
            el.condtestmanager.OnClearCondTest = uicontroller.ctpanel.ClearCondTest;
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
                    elhistory.Last().ex.SetProperty(name, elhistory[hn - 2].ex.GetProperty(name));
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

        public void PrepareEnv(string scenename)
        {
            el.envmanager.ParseScene(scenename);
            el.envmanager.SetParams(el.ex.EnvParam);
            InheritEnv();
        }

        public void InheritEnv(string toobject = null)
        {
            string paramname, fullname;
            var eip = elhistory.Last().ex.EnvInheritParam;
            foreach (var ip in eip.ToArray())
            {
                if (ip.IsEnvParamFullName(out paramname, out fullname))
                {
                    if (String.IsNullOrEmpty(toobject))
                    {
                        InheritEnvParam(fullname);
                    }
                    else
                    {
                        if (fullname.LastAtSplitTail() == toobject)
                        {
                            InheritEnvParam(fullname);
                        }
                    }
                }
                else
                {
                    eip.Remove(ip);
                }

            }
        }

        public void InheritEnvParam(string fullname)
        {
            string paramname = fullname.FirstAtSplitHead();
            string objectname = fullname.LastAtSplitTail();

            var hn = elhistory.Count;
            if (hn > 1)
            {
                object v = null;
                for (var i = hn - 2; i > -1; i--)
                {
                    var hp = elhistory[i].ex.EnvParam;
                    if (hp.ContainsKey("Show@Showroom@ShowroomManager"))
                    {
                        var showobj = hp["Show@Showroom@ShowroomManager"].ToString();
                        if (showobj == objectname && hp.ContainsKey(fullname))
                        {
                            v = hp[fullname];
                        }
                        else
                        {
                            if (uicontroller.envcrossinheritrule.IsEnvCrossInheritTo(objectname))
                            {
                                if (uicontroller.envcrossinheritrule.IsFollowEnvCrossInheritRule(objectname, showobj, paramname))
                                {
                                    foreach (var hpk in hp.Keys.ToArray())
                                    {
                                        if (hpk.FirstAtSplitHead() == paramname && hpk.LastAtSplitTail() == showobj)
                                        {
                                            v = hp[hpk];
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (hp.ContainsKey(fullname))
                            {
                                v = hp[fullname];
                            }
                        }
                    }
                    else if (hp.ContainsKey(fullname))
                    {
                        v = hp[fullname];
                    }
                    else if (uicontroller.envcrossinheritrule.IsEnvCrossInheritTo(objectname))
                    {
                        foreach (var hpn in hp.Keys.ToArray())
                        {
                            string paramfrom = hpn.FirstAtSplitHead();
                            string objectfrom = hpn.LastAtSplitTail();
                            if (paramname == paramfrom && uicontroller.envcrossinheritrule.IsFollowEnvCrossInheritRule(objectname, objectfrom, paramname))
                            {
                                v = hp[hpn];
                                break;
                            }
                        }
                    }

                    if (v != null)
                    {
                        elhistory.Last().envmanager.SetParam(fullname, v);
                        break;
                    }
                }
            }
        }

    }
}