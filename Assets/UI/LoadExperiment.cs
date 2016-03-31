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

    void Awake()
    {
        dropdown = gameObject.GetComponent<Dropdown>();
        exlogic = GameObject.Find("ExperimentLogic");
        exparam = GameObject.Find("ExContent").GetComponent<ExParam>();

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
        if(ex.subject==null)
        {
            ex.subject = new Subject();
        }
        OnLoad(ex);
    }

    public void OnLoad(Experiment ex)
    {
        ex.condtest = null;

        var logic = exlogic.GetComponent<ExperimentLogic>();
        logic.ex = ex;

        exparam.UpdateParam(ex);
    }

    void Start()
    {
        OnLoad();
    }
}
