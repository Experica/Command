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
    /// Manage Experiment Query, Load/UnLoad, Start/Stop, and Holding the History of ExperimentLogic defined in Experiment
    /// </summary>
    public class ExperimentManager : MonoBehaviour
    {
        public AppManager appmgr;
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
                
                appmgr.ui.experimentlist.value = id;
            }
            else
            {
                Debug.LogError($"Can Not Find \"{id}\" in Experiment Definition List.");
            }
        }

        public void StartEx()
        {
            appmgr.ui.start.value = true;
        }

        public void StopEx()
        {
            appmgr.ui.start.value = false;
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
            var defs = indir.GetDefinationFiles();
            if (defs != null) { deffile = defs; }
        }

        public Experiment LoadEx(string exfilepath)
        {
            var ex = exfilepath.ReadYamlFile<Experiment>();
            // keep consistent file name and experiment ID
            var exfilename = Path.GetFileNameWithoutExtension(exfilepath);
            if (string.IsNullOrEmpty(ex.ID) || ex.ID != exfilename)
            {
                ex.ID = exfilename;
            }
            return ex.PrepareDefinition(appmgr.cfgmgr.config);
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
                ex.LogicPath = appmgr.cfgmgr.config.ExLogic;
                eltype = Type.GetType(ex.LogicPath);
                Debug.LogWarning($"No Valid ExperimentLogc For {ex.ID}, Use {ex.LogicPath} Instead.");
            }
            el = gameObject.AddComponent(eltype) as ExperimentLogic;
            el.ex = ex;
            el.appmgr = appmgr;
            RegisterCallback();
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

        void RegisterCallback()
        {
            el.condtestmgr.OnNewCondTest += appmgr.condtestpanel.OnNewCondTest;
            el.condtestmgr.OnCondTestClear += appmgr.condtestpanel.Clear;

            //el.condmanager.OnSamplingInitialized = uicontroller.condpanel.RefreshCondition;
            //el.condtestmanager.OnNotifyCondTest = uicontroller.OnNotifyCondTest;
            //el.condtestmanager.OnNotifyCondTestEnd = uicontroller.OnNotifyCondTestEnd;
            //el.condtestmanager.PushUICondTest = uicontroller.ctpanel.PushCondTest;
        }

        public bool NewEx(string id, string idcopyfrom)
        {
            if (!deffile.ContainsKey(idcopyfrom))
            {
                return NewEx(id);
            }
            else
            {
                if (!deffile.ContainsKey(id))
                {
                    var ex = deffile[idcopyfrom].ReadYamlFile<Experiment>();
                    ex.ID = id;
                    ex.Name = id;
                    ex.PrepareDefinition(appmgr.cfgmgr.config);

                    deffile[id] = Path.Combine(appmgr.cfgmgr.config.ExDir, id + ".yaml");
                    ex.SaveDefinition(deffile[id]);
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
                var ex = new Experiment { ID = id };
                ex.PrepareDefinition(appmgr.cfgmgr.config);

                deffile[id] = Path.Combine(appmgr.cfgmgr.config.ExDir, id + ".yaml");
                ex.SaveDefinition(deffile[id]);
                return true;
            }
        }

        /// <summary>
        /// Save the current experiment
        /// </summary>
        /// <param name="syncenvparam"></param>
        /// <returns></returns>
        public bool SaveEx(bool syncenvparam=true) => SaveEx(el.ex.ID,syncenvparam);

        /// <summary>
        /// Save the definition state of experiment to file
        /// </summary>
        /// <param name="id"></param>
        /// <param name="syncenvparam"></param>
        /// <returns></returns>
        public bool SaveEx(string id, bool syncenvparam=true)
        {
            if (!deffile.ContainsKey(id)) { return false; }
            var i = FindFirstInELHistory(id);
            if (i < 0) { return false; }

            var el = elhistory[i];
            if(syncenvparam && el.envmgr.TryGetParams(out Dictionary<string,object> ps))
            {
                el.ex.EnvParam = ps;
            }
            el.ex.SaveDefinition(deffile[id]);
            return true;
        }

        public void SaveAllEx()
        {
            foreach (var id in elhistory.Select(i => i.ex.ID))
            {
                SaveEx(id);
            }
        }

        public bool DeleteEx() => DeleteEx(el.ex.ID);

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

        /// <summary>
        /// Clear experiments in history
        /// </summary>
        /// <param name="excludelast"></param>
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


        public int FindDuplicateOfLast() => FindFirstInELHistory(elhistory.Last().ex.ID, true);

        /// <summary>
        /// Find first Experiment with `exid` in history
        /// </summary>
        /// <param name="exid"></param>
        /// <param name="excludelast"></param>
        /// <returns>index of Experiment in history, or -1 when not found</returns>
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
            // ExtendParam is designed to be static in Experiment run/stop lifecycle, so we can safely delete param in inherit that is not defined
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

        /// <summary>
        /// Try inherit param from Experiment's Properties or ExtendProperties
        /// </summary>
        /// <param name="name"></param>
        public void InheritExParam(string name)
        {
            var n = elhistory.Count;
            if (n < 2) { return; }
            if (el.ex.ContainsProperty(name)) { el.ex.SetProperty(name, elhistory[n - 2].ex.GetProperty(name)); }
            else { InheritExExtendProperty(name); }
        }

        /// <summary>
        /// Try inherit all params
        /// </summary>
        /// <param name="byobj">only params for a specific object</param>
        public void InheritEnv(string byobj = null)
        {
            if (elhistory.Count < 2) { return; }
            foreach (var ip in el.ex.EnvInheritParam.ToArray())
            {
                if (!ip.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {ip} is not a valid fullname, skip inherit search."); continue; };
                if (!string.IsNullOrEmpty(byobj) && byobj != ns[2]) { continue; }
                if (!el.envmgr.ContainsParamByFullName(ns[0], ns[1], ns[2])) { continue; }
                InheritEnvParam(ns[0], ns[1], ns[2], ip);
            }
        }

        public void InheritEnvParam(string FullName)
        {
            if (elhistory.Count < 2) { return; }
            if (!FullName.SplitEnvParamFullName(out var ns)) { Debug.LogError($"EnvParam: {FullName} is not a valid fullname, skip inherit search."); return; };
            // envparams are dynamic because of spawn/unspawn in a scene, so we don't delete param in inherit that not defined at the moment,
            // but may used later when corresponding object spawned(might accumulate unused param in the EnvInheritParam)
            if (!el.envmgr.ContainsParamByFullName(ns[0], ns[1], ns[2])) { return; }
            InheritEnvParam(ns[0], ns[1], ns[2], FullName);
        }

        void InheritEnvParam(string nvName, string nbName, string goName, string FullName)
        {
            for (var i = elhistory.Count - 2; i > -1; i--)
            {
                // since the scene of previous experiment probably unloaded, we can't safely retrieve envparams from envmanager,
                // but when config.IsSaveExOnQuit=true(default), the EnvParam will be filled whenever an experiment unselected and become history
                var envparam = elhistory[i].ex.EnvParam;
                object v = null;
                if (envparam.ContainsKey(FullName))
                {
                    v = envparam[FullName];
                }
                else if (appmgr.cfgmgr.config.EnvCrossInheritRule.ContainsKey(nbName))
                {
                    foreach (var p in envparam.Keys)
                    {
                        if (!p.SplitEnvParamFullName(out var ns)) { continue; }
                        if (nvName == ns[0] && appmgr.cfgmgr.config.EnvCrossInheritRule[nbName].Contains(ns[1]))
                        {
                            v = envparam[p];
                            break;
                        }
                    }
                }

                if (v != null)
                {
                    el.envmgr.SetParamByFullName(nvName, nbName, goName, v);
                    break;
                }
            }
        }

    }
}