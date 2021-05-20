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
        public ExperimentLogic el;
        public List<ExperimentLogic> elhistory = new List<ExperimentLogic>();
        public Timer timer = new Timer();
        public Dictionary<string, string> idfile = new Dictionary<string, string>();

        public double ELLoadTime, ELReadyTime, ELStartTime, ELStopTime;
        public double SinceELLoad { get { return timer.ElapsedMillisecond - ELLoadTime; } }
        public double SinceELReady { get { return timer.ElapsedMillisecond - ELReadyTime; } }
        public double SinceELStart { get { return timer.ElapsedMillisecond - ELStartTime; } }
        public double SinceELStop { get { return timer.ElapsedMillisecond - ELStopTime; } }

        public EXPERIMENTSTATUS ExperimentStatus = EXPERIMENTSTATUS.NONE;
        public int ELRepeat { get; private set; } = 0;
        public string ELID = null;

        public void ChangeEx(string id)
        {
            if (string.IsNullOrEmpty(id)) { return; }
            if (idfile.ContainsKey(id))
            {
                ELID = id;
                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                ELRepeat = 0;
                ELLoadTime = ELReadyTime = ELStartTime = ELStopTime = timer.ElapsedMillisecond;
                uicontroller.exs.value = uicontroller.exs.options.FindIndex(i => i.text == id);
            }
            else
            {
                Debug.LogWarning($"Can Not Find {id} in Experiment Directory: {uicontroller.config.ExDir}.");
            }
        }

        public void StartEx()
        {
            ExperimentStatus = EXPERIMENTSTATUS.STARTING;
            uicontroller.start.isOn = true;
        }

        public void OnELReady()
        {
            ELReadyTime = timer.ElapsedMillisecond;
        }

        public void OnELStart()
        {
            ELStartTime = timer.ElapsedMillisecond;
            ExperimentStatus = EXPERIMENTSTATUS.RUNNING;
        }

        public void OnELStop()
        {
            ELStopTime = timer.ElapsedMillisecond;
            ELRepeat++;
            ExperimentStatus = EXPERIMENTSTATUS.STOPPED;
        }

        public void RefreshIDFile()
        {
            var exdir = uicontroller.config.ExDir;
            if (Directory.Exists(exdir))
            {
                var exfiles = Directory.GetFiles(exdir, "*.yaml", SearchOption.TopDirectoryOnly);
                if (exfiles.Length == 0)
                {
                    Debug.Log($"Experiment Defination Directory \"{exdir}\" Is Empty, Skip Refreshing.");
                    return;
                }
                idfile.Clear();
                foreach (var f in exfiles)
                {
                    idfile[Path.GetFileNameWithoutExtension(f)] = f;
                }
            }
            else
            {
                Directory.CreateDirectory(exdir);
                Debug.Log($"Create Directory \"{exdir}\" For Experiment Defination.");
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
                ex.DataDir = uicontroller.config.DataDir;
                if (!Directory.Exists(ex.DataDir))
                {
                    Directory.CreateDirectory(ex.DataDir);
                    Debug.Log($"Create Data Directory \"{ex.DataDir}\".");
                }
            }
            if (ex.CondTest != null)
            {
                ex.CondTest = null;
            }
            if (ex.NotifyParam == null)
            {
                ex.NotifyParam = uicontroller.config.NotifyParams;
            }
            ex.Config = uicontroller.config;
            return ex;
        }

        public void LoadEL(string exfilepath)
        {
            LoadEL(LoadEx(exfilepath));
        }

        public void LoadEL(Experiment ex)
        {
            ELLoadTime = timer.ElapsedMillisecond;
            Type eltype = null;
            if (!string.IsNullOrEmpty(ex.LogicPath))
            {
                if (File.Exists(ex.LogicPath))
                {
                    var assembly = ex.LogicPath.CompileFile();
                    eltype = assembly.GetExportedTypes()[0];
                }
                else
                {
                    eltype = Type.GetType(ex.LogicPath);
                }
            }
            if (eltype == null)
            {
                ex.LogicPath = uicontroller.config.ExLogic;
                eltype = Type.GetType(ex.LogicPath);
                Debug.LogWarning($"No Valid ExperimentLogc For {ex.ID}, Use {ex.LogicPath} Instead.");
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
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
                if (!idfile.ContainsKey(id) && idfile.ContainsKey(idcopyfrom))
                {
                    var ex = idfile[idcopyfrom].ReadYamlFile<Experiment>();
                    ex.ID = id;
                    ex.Name = id;
                    LoadEL(ValidateExperiment(ex));

                    idfile[id] = Path.Combine(uicontroller.config.ExDir, id + ".yaml");
                    SaveEx(id);
                    return true;
                }
                return false;
            }
        }

        public bool NewEx(string id)
        {
            if (idfile.ContainsKey(id))
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

                idfile[id] = Path.Combine(uicontroller.config.ExDir, id + ".yaml");
                SaveEx(id);
                return true;
            }
        }

        public void SaveEx(string id)
        {
            if (idfile.ContainsKey(id))
            {
                var i = FindFirstInELHistory(id);
                if (i >= 0)
                {
                    var ex = elhistory[i].ex;
                    // Exclude data and config for saving experiment definition
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
                        idfile[id].WriteYamlFile(ex);
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
            foreach (var id in idfile.Keys)
            {
                SaveEx(id);
            }
        }

        public bool DeleteEx(string id)
        {
            if (idfile.ContainsKey(id))
            {
                var i = FindFirstInELHistory(id);
                if (i >= 0)
                {
                    Destroy(elhistory[i]);
                    elhistory.RemoveAt(i);
                }
                File.Delete(idfile[id]);
                idfile.Remove(id);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DeleteAllEx()
        {
            foreach (var id in idfile.Keys.ToArray())
            {
                DeleteEx(id);
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
                elhistory[idx].Dispose();
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
            el.condtestmanager.PushUICondTest = uicontroller.ctpanel.PushCondTest;
            el.condtestmanager.OnClearCondTest = uicontroller.ctpanel.ClearCondTest;
            el.envmanager.OnNotifyUI = uicontroller.envpanel.UpdateParamUI;
            el.ex.OnNotifyUI = uicontroller.expanel.UpdateParamUI;
            el.SyncFrame = uicontroller.netmanager.BeginSyncFrame;
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
            var ellex = elhistory.Last().ex;
            var ellexip = ellex.InheritParam;
            foreach (var ip in ellexip.ToArray())
            {
                if (Experiment.Properties.ContainsKey(ip) || ellex.Param.ContainsKey(ip))
                {
                    InheritExParam(ip);
                }
                else
                {
                    ellexip.Remove(ip);
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
            uicontroller.SyncCurrentDisplayCLUT();
        }

        public void InheritEnv(string toobject = null)
        {
            var ell = elhistory.Last();
            var eip = ell.ex.EnvInheritParam;
            foreach (var ip in eip.ToArray())
            {
                if (ell.envmanager.ContainsParam(ip, out string fullname))
                {
                    if (string.IsNullOrEmpty(toobject))
                    {
                        InheritEnvParam(fullname);
                    }
                    else
                    {
                        if (fullname.LastSplitTail() == toobject)
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
            string paramname = fullname.FirstSplitHead();
            string objectname = fullname.LastSplitTail();

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
                            if (uicontroller.config.EnvCrossInheritRule.IsEnvCrossInheritTo(objectname))
                            {
                                if (uicontroller.config.EnvCrossInheritRule.IsFollowEnvCrossInheritRule(objectname, showobj, paramname))
                                {
                                    foreach (var hpk in hp.Keys.ToArray())
                                    {
                                        if (hpk.FirstSplitHead() == paramname && hpk.LastSplitTail() == showobj)
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
                    else if (uicontroller.config.EnvCrossInheritRule.IsEnvCrossInheritTo(objectname))
                    {
                        foreach (var hpn in hp.Keys.ToArray())
                        {
                            string paramfrom = hpn.FirstSplitHead();
                            string objectfrom = hpn.LastSplitTail();
                            if (paramname == paramfrom && uicontroller.config.EnvCrossInheritRule.IsFollowEnvCrossInheritRule(objectname, objectfrom, paramname))
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