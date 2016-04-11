using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class ExParam : MonoBehaviour
{
    public GameObject input, text,togglebutton;
    public Experiment experiment,preexperiment;
    public Dictionary<string, PropertyInfo> exproperty;
    public Dictionary<string, InputField> exinput=new Dictionary<string, InputField>();
    public Dictionary<string, Toggle> extoggle = new Dictionary<string, Toggle>();

    public void UpdateParam(Experiment ex)
    {
        if(experiment==null)
        {
            FirstUpdateParam(ex);
            return;
        }
        preexperiment = experiment;
        experiment = UpdateEx(preexperiment, ex);


        var np = 20;

        var grid = GetComponent<GridLayoutGroup>();
        var cn = grid.constraintCount;
        var rt = (RectTransform)transform;
        var width = Mathf.Max(rt.rect.width, 1 * (grid.cellSize.x + grid.spacing.x));
        var height = Mathf.Max(rt.rect.height, np / (cn / 2) * (grid.cellSize.y + grid.spacing.y));
        rt.sizeDelta = new Vector2(width, height);
    }

    public Experiment UpdateEx(Experiment prex,Experiment ex)
    {
        foreach(var n in exproperty.Keys)
        {
            var p = exproperty[n];
            var isinherit = ex.inheritparams.Contains(n);
            if (isinherit)
            {
                p.SetValue(ex, p.GetValue(prex, null), null);
            }
            if (exinput.ContainsKey(n))
            {
                extoggle[n].isOn = isinherit;
                exinput[n].text = p.GetValue(ex, null).ToString();
            }
        }
        return ex;
    }

    public void FirstUpdateParam(Experiment ex)
    {
        experiment = ex;
        exproperty = GetExProperty(experiment);

        AddParam("name", experiment.name);
        AddParam("id", experiment.id);
        AddParam("experimenter", experiment.experimenter);

        AddParam("subject_id", experiment.subject.id);
        AddParam("subject_gender", experiment.subject.gender);
        AddParam("subject_age", experiment.subject.age);
        AddParam("subject_size", experiment.subject.size);
        AddParam("subject_weight", experiment.subject.weight);

        AddParam("condtestpath", experiment.condtestpath);

        AddParam("recordsession", experiment.recordsession);
        AddParam("recordsite", experiment.recordsite);

        AddParam("condrepeat", experiment.condrepeat);
        AddParam("preICI", experiment.preICI);
        AddParam("conddur", experiment.conddur);
        AddParam("sufICI", experiment.sufICI);
        AddParam("preITI", experiment.preITI);
        AddParam("trialdur", experiment.trialdur);
        AddParam("sufITI", experiment.sufITI);
        AddParam("preIBI", experiment.preIBI);
        AddParam("blockdur", experiment.blockdur);
        AddParam("sufIBI", experiment.sufIBI);

        var np = 22;

        var grid = GetComponent<GridLayoutGroup>();
        var cn = grid.constraintCount;
        var rt = (RectTransform)transform;
        var width = Mathf.Max(rt.rect.width, 1 * (grid.cellSize.x + grid.spacing.x));
        var height = Mathf.Max(rt.rect.height, np / (cn / 2) * (grid.cellSize.y + grid.spacing.y));
        rt.sizeDelta = new Vector2(width, height);
    }

    public Dictionary<string, PropertyInfo> GetExProperty(Experiment ex)
    {
        var exp = new Dictionary<string, PropertyInfo>();
        foreach (var p in ex.GetType().GetProperties())
        {
            exp[p.Name] = p;
        }
        exp.Remove("inheritparams");
        return exp;
    }

    public void SetEx(string name, object value)
    {
        if (exproperty.ContainsKey(name))
        {
            var p = exproperty[name];
            p.SetValue(experiment,ConvertString.Convert(p.PropertyType, value), null);
        }
        else
        {
            experiment.param[name] = value;
        }
    }

    public void SetValue(InputField value)
    {
        SetEx(value.name, value.text);
    }

    public void ToggleInheritParams(string name, bool ison)
    {
        if(experiment.inheritparams.Contains(name))
        {
            if(!ison)
            {
                experiment.inheritparams.Remove(name);
            }
        }
        else
        {
            if(ison)
            {
                experiment.inheritparams.Add(name);
                var v = exproperty[name].GetValue(preexperiment, null);
                SetEx(name, v);
                exinput[name].text = v.ToString();
            }
        }
    }

    void AddParam(string name, object value)
    {
        if(value==null)
        {
            value = "";
        }
        var tb = Instantiate(togglebutton);
        tb.name = name + "_Togglebutton";
        tb.GetComponentInChildren<Text>().text = name;
        var tbt = tb.GetComponent<Toggle>();
        if (experiment.inheritparams.Contains(name))
        {
            tbt.isOn = true;
        }
        else
        {
            tbt.isOn = false;
        }
        tbt.onValueChanged.AddListener(delegate { ToggleInheritParams(name, tbt.isOn); });
        extoggle[name] = tbt;


        //var t = Instantiate(text);
        //t.name = name + "_Text";
        //t.GetComponent<Text>().text = name;

        var i = Instantiate(input);
        i.name = name;
        //foreach (var itc in i.GetComponentsInChildren<Text>())
        //{
        //    itc.text = value.ToString();
        //}
        var ii = i.GetComponent<InputField>();
        ii.text = value.ToString();
        ii.onEndEdit.AddListener(delegate { SetValue(ii); });
        exinput[name] = ii;

        //t.transform.SetParent(transform);
        tb.transform.SetParent(transform);
        i.transform.SetParent(transform);

        //t.transform.localScale = new Vector3(1, 1, 1);
        tb.transform.localScale = new Vector3(1, 1, 1);
        i.transform.localScale = new Vector3(1, 1, 1);

        
    }
}
