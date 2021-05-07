using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;

namespace Experica.Command
{
    public class ExperimentSessionManager : MonoBehaviour
    {
        public UIController uicontroller;
        public List<string> exsfiles = new List<string>();
        public List<string> exsids = new List<string>();
        public ExperimentSessionLogic esl;

        public void GetExSessionFiles()
        {
            var exsfiledir = uicontroller.config.ExSessionDir;
            if (Directory.Exists(exsfiledir))
            {
                exsfiles = Directory.GetFiles(exsfiledir, "*.yaml", SearchOption.TopDirectoryOnly).ToList();
                exsids.Clear();
                foreach (var f in exsfiles)
                {
                    exsids.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            else
            {
                Directory.CreateDirectory(exsfiledir);
            }
        }

        public ExperimentSession LoadExSession(string exsfilepath)
        {
            var exs = exsfilepath.ReadYamlFile<ExperimentSession>();
            if (string.IsNullOrEmpty(exs.ID))
            {
                exs.ID = Path.GetFileNameWithoutExtension(exsfilepath);
            }
            return ValidateExperimentSession(exs);
        }

        ExperimentSession ValidateExperimentSession(ExperimentSession exs)
        {
            if (string.IsNullOrEmpty(exs.Name))
            {
                exs.Name = exs.ID;
            }
            return exs;
        }

        public void LoadESL(string exsfilepath)
        {
            LoadESL(LoadExSession(exsfilepath));
        }

        public void LoadESL(ExperimentSession exs)
        {
            var eslpath = exs.ExSessionLogicPath;
            Type esltype = null;
            if (!string.IsNullOrEmpty(eslpath))
            {
                if (File.Exists(eslpath))
                {
                    var assembly = eslpath.CompileFile();
                    esltype = assembly.GetExportedTypes()[0];
                }
                else
                {
                    esltype = Type.GetType(eslpath);
                }
            }

            if (esl != null)
            {
                Destroy(esl);
            }
            esl = gameObject.AddComponent(esltype) as ExperimentSessionLogic;
            esl.exsession = exs;
            esl.exmanager = uicontroller.exmanager;
            esl.OnBeginStartExperimentSession = uicontroller.OnBeginStartExperimentSession;
            esl.OnEndStartExperimentSession = uicontroller.OnEndStartExperimentSession;
            esl.OnBeginStopExperimentSession = uicontroller.OnBeginStopExperimentSession;
            esl.OnEndStopExperimentSession = uicontroller.OnEndStopExperimentSession;
        }

        public void SaveExSession(string id)
        {
            if (exsids.Contains(id))
            {
                exsfiles[exsids.IndexOf(id)].WriteYamlFile(esl.exsession);
            }
        }

        public void SaveAllExSession()
        {
            foreach (var n in exsids)
            {
                SaveExSession(n);
            }
        }
    }
}