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
using Experica;


namespace Experica.Command
{
    public class ExperimentManager : MonoBehaviour
    {
        public UIController uicontroller;
        public ExperimentLogic el;
        public List<ExperimentLogic> elhistory = new ();
        public Timer timer = new ();
        public Dictionary<string, string> deffile = new ();

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
            if (deffile.ContainsKey(id))
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

        public void CollectDefination(string indir)
        {
            var defs = indir.GetDefinationFiles("Experiment");
            if (defs != null) { deffile = defs; }
        }

        public Experiment LoadEx(string exfilepath)
        {
            var ex = exfilepath.ReadYamlFile<Experiment>();
            var exfilename = Path.GetFileNameWithoutExtension(exfilepath);
            if (string.IsNullOrEmpty(ex.ID) || ex.ID!=exfilename)
            {
                ex.ID = exfilename;
            }
            return PrepareExperiment(ex);
        }

        Experiment PrepareExperiment(Experiment ex)
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
            ex.InitializeDataSource();
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
            //uicontroller.condpanel.forceprepare.isOn = el.regeneratecond;
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
                if (!deffile.ContainsKey(id) && deffile.ContainsKey(idcopyfrom))
                {
                    var ex = deffile[idcopyfrom].ReadYamlFile<Experiment>();
                    ex.ID = id;
                    ex.Name = id;
                    LoadEL(PrepareExperiment(ex));

                    deffile[id] = Path.Combine(uicontroller.config.ExDir, id + ".yaml");
                    SaveEx(id);
                    return true;
                }
                return false;
            }
        }

        public bool NewEx(string id)
        {
            if (deffile.ContainsKey(id))
            {
                return false;
            }
            else
            {
                var ex = new Experiment
                {
                    ID = id
                };
                LoadEL(PrepareExperiment(ex));

                deffile[id] = Path.Combine(uicontroller.config.ExDir, id + ".yaml");
                SaveEx(id);
                return true;
            }
        }

        public void SaveEx(string id)
        {
            if (!deffile.ContainsKey(id)) { return; }
            var i = FindFirstInELHistory(id);
            if (i < 0) { return; }

            var el = elhistory[i];
            el.ex.EnvParam = el.envmanager.GetParams();
            el.ex.SaveDefinition(deffile[id]);
        }

        public void SaveAllEx()
        {
            foreach (var id in elhistory.Select(i=>i.ex.ID))
            {
                SaveEx(id);
            }
        }

        public bool DeleteEx(string id)
        {
            if (!deffile.ContainsKey(id)) { return false; }

            var i = FindFirstInELHistory(id);
            if (i >= 0)
            {
                elhistory[i].Dispose();
                Destroy(elhistory[i]);
                elhistory.RemoveAt(i);
            }
            File.Delete(deffile[id]);
            deffile.Remove(id);
            return true;
        }

        public void DeleteAllEx()
        {
            foreach (var id in deffile.Keys.ToArray())
            {
                DeleteEx(id);
            }
        }

        public void AddEL(ExperimentLogic el)
        {
            RegisterELCallback();
            if (elhistory.Count == 0)
            {
                elhistory.Add(el);
            }
            else
            {
                var preel = elhistory.Last();
                preel.enabled = false;
                elhistory.Add(el);
                // Inherit params
                foreach (var ip in el.ex.InheritParam.ToArray())
                {
                    if (el.ex.ContainsProperty(ip)) { el.ex.SetProperty(ip,preel.ex.GetProperty(ip)); }
                    else { InheritExExtendProperty(ip); }
                }
                // Remove duplicate of the current el
                var i = FindDuplicateOfLast();
                if (i >= 0)
                {
                    elhistory[i].Dispose();
                    Destroy(elhistory[i]);
                    elhistory.RemoveAt(i);
                }
            }
        }

        void RegisterELCallback()
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
            //el.envmanager.OnNotifyUI = uicontroller.envpanel.UpdateParamUI;
            //el.ex.OnNotifyUI = uicontroller.expanel.UpdateParamUI;
            // el.SyncFrame = uicontroller.netmanager.BeginSyncFrame;
        }

        public int FindDuplicateOfLast()
        {
            var i = FindFirstInELHistory(elhistory.Last().ex.ID);
            if (i == elhistory.Count - 1) { i = -1; }
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


        void InheritExExtendProperty(string name)
        {
            if(!el.ex.ContainsExtendProperty(name)) { /* delete? */;return; }
            for (var i = elhistory.Count - 2; i > -1; i--)
            {
                if (elhistory[i].ex.ContainsExtendProperty(name))
                {
                    el.ex.SetExtendProperty(name, elhistory[i].ex.GetExtendProperty(name));
                    break;
                }
            }
        }

        public void InheritExParam(string name)
        {
            var n = elhistory.Count;
            if (n < 2) { return; }
            if (el.ex.ContainsProperty(name)) { el.ex.SetProperty(name, elhistory[n-2].ex.GetProperty(name)); }
            else { InheritExExtendProperty(name); }
        }

        public void InheritEnv(string byobj = null)
        {
            if (elhistory.Count <2) { return; }
            foreach (var ip in el.ex.EnvInheritParam.ToArray())
            {
                if(!ip.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {ip} not a valid fullname, skip inherit search.");continue; };
                if (!string.IsNullOrEmpty(byobj) && byobj != ns[2]) { continue; }
                if (!el.envmanager.ContainsParamByFullName(ns[0], ns[1], ns[2]))                { /* delete? */;continue; }
                InheritEnvParam(ns[0], ns[1], ns[2],ip);
            }
        }

        public void InheritEnvParam(string FullName)
        {
            if (elhistory.Count < 2) { return; }
            if (!FullName.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {FullName} not a valid fullname, skip inherit search."); return; };
            if (!el.envmanager.ContainsParamByFullName(ns[0], ns[1], ns[2])) { /* delete? */; return; }
            InheritEnvParam(ns[0], ns[1], ns[2], FullName);
        }

        void InheritEnvParam(string nvName,string nbName,string goName,string FullName)
        {
            for (var i = elhistory.Count - 2; i > -1; i--)
            {
                var envparam = elhistory[i].ex.EnvParam; // config.IsSaveExOnQuit=true will update EnvParam whenever an experiment unselected
                object v = null;
                if (envparam.ContainsKey(FullName))
                {
                    v = envparam[FullName];
                }
                else if (uicontroller.config.EnvCrossInheritRule.IsEnvCrossInheritTo(goName))
                {
                    foreach (var p in envparam.Keys)
                    {
                        string paramfrom = p.FirstSplitHead();
                        string objectfrom = p.LastSplitTail();
                        if (nvName == paramfrom && uicontroller.config.EnvCrossInheritRule.IsFollowEnvCrossInheritRule(goName, objectfrom, nvName))
                        {
                            v = envparam[p];
                            break;
                        }
                    }
                }

                if (v != null)
                {
                    el.envmanager.SetParamByFullName(nvName, nbName, goName, v);
                    break;
                }
            }
        }

        //public void InheritEnvParam(string fullname)
        //{
        //    if (!fullname.SplitEnvParamFullName(out var ns)) { return; }
        //    string paramname = fullname.FirstSplitHead();
        //    string objectname = fullname.LastSplitTail();

        //    var hn = elhistory.Count;
        //    if (hn > 1)
        //    {
        //        object v = null;
        //        for (var i = hn - 2; i > -1; i--)
        //        {
        //            var hp = elhistory[i].ex.EnvParam;
        //            if (hp.ContainsKey("Show@Showroom@ShowroomManager"))
        //            {
        //                var showobj = hp["Show@Showroom@ShowroomManager"].ToString();
        //                if (showobj == objectname && hp.ContainsKey(fullname))
        //                {
        //                    v = hp[fullname];
        //                }
        //                else
        //                {
        //                    if (uicontroller.config.EnvCrossInheritRule.IsEnvCrossInheritTo(objectname))
        //                    {
        //                        if (uicontroller.config.EnvCrossInheritRule.IsFollowEnvCrossInheritRule(objectname, showobj, paramname))
        //                        {
        //                            foreach (var hpk in hp.Keys.ToArray())
        //                            {
        //                                if (hpk.FirstSplitHead() == paramname && hpk.LastSplitTail() == showobj)
        //                                {
        //                                    v = hp[hpk];
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                    else if (hp.ContainsKey(fullname))
        //                    {
        //                        v = hp[fullname];
        //                    }
        //                }
        //            }
        //            else if (hp.ContainsKey(fullname))
        //            {
        //                v = hp[fullname];
        //            }
        //            else if (uicontroller.config.EnvCrossInheritRule.IsEnvCrossInheritTo(objectname))
        //            {
        //                foreach (var hpn in hp.Keys.ToArray())
        //                {
        //                    string paramfrom = hpn.FirstSplitHead();
        //                    string objectfrom = hpn.LastSplitTail();
        //                    if (paramname == paramfrom && uicontroller.config.EnvCrossInheritRule.IsFollowEnvCrossInheritRule(objectname, objectfrom, paramname))
        //                    {
        //                        v = hp[hpn];
        //                        break;
        //                    }
        //                }
        //            }

        //            if (v != null)
        //            {
        //                elhistory.Last().envmanager.SetParam(fullname, v);
        //                break;
        //            }
        //        }
        //    }
        //}

    }
}