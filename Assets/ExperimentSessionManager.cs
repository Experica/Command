/*
ExperimentSessionManager.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;

namespace Experica.Command
{
    /// <summary>
    /// Manage ExperimentSession
    /// </summary>
    public class ExperimentSessionManager : MonoBehaviour
    {
        public AppManager appmgr;
        public Dictionary<string, string> deffile = new();
        public ExperimentSessionLogic esl;


        public void CollectDefination(string indir)
        {
            var defs = indir.GetDefinationFiles();
            if (defs != null) { deffile = defs; }
        }

        public ExperimentSession LoadExSession(string exsfilepath)
        {
            var exs = exsfilepath.ReadYamlFile<ExperimentSession>();
            // keep consistent file name and experimentsession ID
            var exsfilename = Path.GetFileNameWithoutExtension(exsfilepath);
            if (string.IsNullOrEmpty(exs.ID) || exs.ID != exsfilename)
            {
                exs.ID = exsfilename;
            }
            return exs.PrepareDefinition();
        }

        public void LoadESL(string exsfilepath) { LoadESL(LoadExSession(exsfilepath)); }

        public void LoadESL(ExperimentSession exs)
        {
            Type esltype = null;
            if (!string.IsNullOrEmpty(exs.LogicPath))
            {
                if (File.Exists(exs.LogicPath))
                {
                    var assembly = exs.LogicPath.CompileFile();
                    esltype = assembly.GetExportedTypes()[0];
                }
                else
                {
                    esltype = Type.GetType(exs.LogicPath);
                }
            }
            if (esltype == null)
            {
                Debug.LogError($"No Valid ExperimentSessionLogic For {exs.ID}."); return;
            }

            if (esl != null)
            {
                Destroy(esl);
                esl = null;
            }
            esl = gameObject.AddComponent(esltype) as ExperimentSessionLogic;
            esl.exsession = exs;
            esl.exmgr = appmgr.exmgr;
            RegisterESLCallback();
        }

        void RegisterESLCallback()
        {
            esl.OnBeginStartExperimentSession = appmgr.OnBeginStartExperimentSession;
            esl.OnEndStartExperimentSession = appmgr.OnEndStartExperimentSession;
            esl.OnBeginStopExperimentSession = appmgr.OnBeginStopExperimentSession;
            esl.OnEndStopExperimentSession = appmgr.OnEndStopExperimentSession;
        }

        public bool NewExSession(string id, string idcopyfrom)
        {
            if (!deffile.ContainsKey(idcopyfrom))
            {
                return NewExSession(id);
            }
            else
            {
                if (!deffile.ContainsKey(id))
                {
                    var exs = deffile[idcopyfrom].ReadYamlFile<ExperimentSession>();
                    exs.ID = id;
                    exs.Name = id;
                    exs.PrepareDefinition();

                    deffile[id] = Path.Combine(appmgr.cfgmgr.config.ExSessionDir, id + ".yaml");
                    exs.SaveDefinition(deffile[id]);
                    return true;
                }
                return false;
            }
        }

        public bool NewExSession(string id)
        {
            if (deffile.ContainsKey(id))
            {
                return false;
            }
            else
            {
                var exs = new ExperimentSession { ID = id };
                exs.PrepareDefinition();

                deffile[id] = Path.Combine(appmgr.cfgmgr.config.ExSessionDir, id + ".yaml");
                exs.SaveDefinition(deffile[id]);
                return true;
            }
        }

        public bool SaveExSession() => SaveExSession(esl.exsession.ID);

        public bool SaveExSession(string id)
        {
            if (deffile.ContainsKey(id) && id == esl.exsession.ID)
            {
                esl.exsession.SaveDefinition(deffile[id]);
                return true;
            }
            return false;
        }

        public bool DeleteExSession() => DeleteExSession(esl.exsession.ID);

        public bool DeleteExSession(string id)
        {
            if (!deffile.ContainsKey(id)) { return false; }

            if (id == esl.exsession.ID)
            {
                Destroy(esl);
                esl = null;
            }

            File.Delete(deffile[id]);
            deffile.Remove(id);
            return true;
        }
    }
}