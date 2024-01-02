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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;

namespace Experica.Command
{
    /// <summary>
    /// Manage Experiment Query, Load/UnLoad, Start/Stop, and Holding the History of Running ExperimentLogic defined in Experiment
    /// </summary>
    public class ExperimentManager : MonoBehaviour
    {
        public UIController uicontroller;
        public Dictionary<string, string> deffile = new();
        List<ExperimentLogic> elhistory = new();
        public ExperimentLogic el;
        public EXPERIMENTSTATUS ExperimentStatus = EXPERIMENTSTATUS.NONE;
        public int Repeat { get; private set; } = 0;

        public Timer timer = new();
        public double ReadyTime, StartTime, StopTime;
        public double SinceReady => timer.ElapsedMillisecond - ReadyTime;
        public double SinceStart => timer.ElapsedMillisecond - StartTime;
        public double SinceStop => timer.ElapsedMillisecond - StopTime;

        public void ChangeEx(string id)
        {
            if (string.IsNullOrEmpty(id)) { Debug.LogError($"Invalid Experiment ID: \"{id}\"."); return; }
            if (deffile.ContainsKey(id))
            {
                uicontroller.ui.experimentlist.value = id;
            }
            else
            {
                Debug.LogError($"Can Not Find \"{id}\" in Experiment Directory: {uicontroller.config.ExDir}.");
            }
        }

        public void StartEx()
        {
            uicontroller.ui.start.value = true;
        }

        public void StopEx()
        {
            uicontroller.ui.start.value = false;
        }

        public void OnReady()
        {
            ReadyTime = timer.ElapsedMillisecond;
            ExperimentStatus = EXPERIMENTSTATUS.NONE;
            Repeat = 0;
        }

        public void OnStart()
        {
            StartTime = timer.ElapsedMillisecond;
            ExperimentStatus = EXPERIMENTSTATUS.RUNNING;
        }

        public void OnStop()
        {
            StopTime = timer.ElapsedMillisecond;
            ExperimentStatus = EXPERIMENTSTATUS.STOPPED;
            Repeat++;
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
            if (string.IsNullOrEmpty(ex.ID) || ex.ID != exfilename)
            {
                ex.ID = exfilename;
            }
            return ex.PrepareDefinition(uicontroller.config);
        }

        public void LoadEL(string exfilepath) { LoadEL(LoadEx(exfilepath)); }

        public void LoadEL(Experiment ex)
        {
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
            RegisterELCallback();
            AddEL(el);
        }

        public void AddEL(ExperimentLogic el)
        {
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
                    if (el.ex.ContainsProperty(ip)) { el.ex.SetProperty(ip, preel.ex.GetProperty(ip)); }
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

            //el.condmanager.OnSamplingInitialized = uicontroller.condpanel.RefreshCondition;
            //el.condtestmanager.OnNotifyCondTest = uicontroller.OnNotifyCondTest;
            //el.condtestmanager.OnNotifyCondTestEnd = uicontroller.OnNotifyCondTestEnd;
            //el.condtestmanager.PushUICondTest = uicontroller.ctpanel.PushCondTest;
            //el.condtestmanager.OnClearCondTest = uicontroller.ctpanel.ClearCondTest;

            // el.SyncFrame = uicontroller.netmanager.BeginSyncFrame;
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
                    LoadEL(ex.PrepareDefinition(uicontroller.config));

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
                LoadEL(ex.PrepareDefinition(uicontroller.config));

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
            foreach (var id in elhistory.Select(i => i.ex.ID))
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
                if (el == elhistory[i]) { el = null; }
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

        public void Clear(bool excludelast = false)
        {
            var n = excludelast ? elhistory.Count - 1 : elhistory.Count;
            for (var i = n - 1; i > -1; i--)
            {
                elhistory[i]?.Dispose();
                Destroy(elhistory[i]);
                elhistory.RemoveAt(i);
            }
            if (!excludelast) { el = null; }
        }


        public int FindDuplicateOfLast() { return FindFirstInELHistory(elhistory.Last().ex.ID, true); }

        public int FindFirstInELHistory(string exid, bool excludelast = false)
        {
            var n = excludelast ? elhistory.Count - 1 : elhistory.Count;
            for (var i = 0; i < n; i++)
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
            if (!el.ex.ContainsExtendProperty(name)) { el.ex.InheritParam.Remove(name); return; }
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
            if (el.ex.ContainsProperty(name)) { el.ex.SetProperty(name, elhistory[n - 2].ex.GetProperty(name)); }
            else { InheritExExtendProperty(name); }
        }

        public void InheritEnv(string byobj = null)
        {
            if (elhistory.Count < 2) { return; }
            foreach (var ip in el.ex.EnvInheritParam.ToArray())
            {
                if (!ip.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {ip} is not a valid fullname, skip inherit search."); continue; };
                if (!string.IsNullOrEmpty(byobj) && byobj != ns[2]) { continue; }
                if (!el.envmanager.ContainsParamByFullName(ns[0], ns[1], ns[2])) { el.ex.EnvInheritParam.Remove(ip); continue; }
                InheritEnvParam(ns[0], ns[1], ns[2], ip);
            }
        }

        public void InheritEnvParam(string FullName)
        {
            if (elhistory.Count < 2) { return; }
            if (!FullName.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {FullName} is not a valid fullname, skip inherit search."); return; };
            if (!el.envmanager.ContainsParamByFullName(ns[0], ns[1], ns[2])) { el.ex.EnvInheritParam.Remove(FullName); return; }
            InheritEnvParam(ns[0], ns[1], ns[2], FullName);
        }

        void InheritEnvParam(string nvName, string nbName, string goName, string FullName)
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

    }
}