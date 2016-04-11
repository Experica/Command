using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

public class LoadExperiment: MonoBehaviour
{
    Dropdown dropdown;
    GameObject exlogic;
    ExParam exparam;
    string[] exfiles;
    CondParam condpath;

    void Awake()
    {
        dropdown = gameObject.GetComponent<Dropdown>();
        exlogic = GameObject.Find("ExperimentLogic");
        exparam = GameObject.Find("ExContent").GetComponent<ExParam>();
        condpath = GameObject.Find("ConditionPath").GetComponent<CondParam>();

        UpdateEx();
    }

    public void UpdateEx()
    {
        if (Directory.Exists("Experiment/"))
        {
            exfiles = Directory.GetFiles("Experiment/", "*.yaml", SearchOption.AllDirectories);
            if(exfiles.Count()!=0)
            {
                dropdown.ClearOptions();
                var os = new List<string>();
                foreach (var f in exfiles)
                {
                    os.Add(Path.GetFileNameWithoutExtension(f));
                }
                dropdown.AddOptions(os);
            }
        }
        else
        {
            Directory.CreateDirectory("Experiment/");
        }
    }

    public void OnLoad()
    {
        var ex = Yaml.ReadYaml<Experiment>(exfiles[dropdown.value]);
        if(ex.name==null)
        {
            ex.name = dropdown.captionText.text;
        }
        if(ex.id==null)
        {
            ex.id = ex.name;
        }
        if(ex.experimenter==null)
        {
            ex.experimenter = "";
        }
        if(ex.subject==null)
        {
            ex.subject = new Subject();
        }
        if(ex.param==null)
        {
            ex.param = new Dictionary<string, object>();
        }
        if(ex.condpath==null)
        {
            ex.condpath = "";
        }
        if(ex.condtestpath==null)
        {
            ex.condtestpath = "ConditionTest/";
            if (!Directory.Exists(ex.condtestpath))
            {
                Directory.CreateDirectory(ex.condtestpath);
            }
        }
        if(ex.inheritparams==null)
        {
            ex.inheritparams = new List<string>();
        }
        OnLoad(ex);
    }

    public void OnLoad(Experiment ex)
    {
        ex.condtest = null;

        var logic = exlogic.GetComponent<ExperimentLogic>();
        logic.ex = ex;

        exparam.UpdateParam(logic.ex);
        condpath.UpdateParam(logic);
    }

    public void SaveEx()
    {
        var ex = exlogic.GetComponent<ExperimentLogic>().ex;
        var cond = ex.cond;
        var condtest = ex.condtest;
        ex.cond = null;
        ex.condtest = null;
        try
        {
            Yaml.WriteYaml(exfiles[dropdown.value], ex);
        }
        finally
        {
            ex.cond = cond;
            ex.condtest = condtest;
        }
    }

    void Start()
    {
        OnLoad();
    }
}
