// --------------------------------------------------------------
// ExperimentPanel.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using VLab;

public class ExperimentPanel:MonoBehaviour
{
    public VLUIController uimanager;
    public GameObject svcontent, inputfield, togglebutton,filepathinput,newexparamprefab;
    public Canvas canvas;
    public CanvasGroup panelcontentcanvasgroup,statusbarcanvasgroup;

    public Dictionary<string, InputField> input = new Dictionary<string, InputField>();
    public Dictionary<string, Toggle> toggle = new Dictionary<string, Toggle>();

    GameObject newexparampanel;

    public void UpdateEx(Experiment ex)
    {
        if(svcontent.transform.childCount==0)
        {
            AddView(ex);
        }
        else
        {
            UpdateView(ex);
        }
    }

    public void AddView(Experiment ex)
    {
        GameObject input;
        foreach(var p in Experiment.properties.Keys)
        {
            if(p=="condpath"||p=="environmentpath"||p=="experimentlogicpath")
            {
                input = filepathinput;
            }
            else
            {
                input = inputfield;
            }
            AddParam(p,Experiment.properties[p].PropertyType,ex.GetValue(p), ex.exinheritparams.Contains(p),
                input);
        }

        var np = Experiment.properties.Keys.Count;
        var grid = svcontent. GetComponent<GridLayoutGroup>();
        var cn = grid.constraintCount;
        var rn = Mathf.Floor( np / (cn / 2.0f))+1;
        var rt = (RectTransform)svcontent.transform;
        rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
    }

    public void UpdateView(Experiment ex)
    {
        foreach (var n in Experiment.properties.Keys)
        {
            toggle[n].isOn = ex.exinheritparams.Contains(n);
            var v = ex.GetValue(n);
            input[n].text = v == null ? "" : v.ToString();
        }
    }

    void AddParam(string name,Type T, object value,bool isinherit,GameObject inputprefab)
    {
        if (value == null)
        {
            value = "";
        }
        var tb = UnityEngine.Object.Instantiate(togglebutton);
        tb.name = name + "_ToggleButton";
        tb.GetComponentInChildren<Text>().text = name;
        var tbt = tb.GetComponent<Toggle>();
        tbt.isOn = isinherit;
        tbt.onValueChanged.AddListener((ison)=>  uimanager.ToggleExInheritParam(name, ison));
        toggle[name] = tbt;

        var inpf = UnityEngine.Object.Instantiate(inputprefab);
        inpf.name = name;
        var ifif = inpf.GetComponent<InputField>();
        ifif.text = (string)VLConvert.Convert( value,typeof(string));
        ifif.onEndEdit.AddListener((v)=>uimanager.SetExParam(name,v));
        input[name] = ifif;

        tb.transform.SetParent(svcontent. transform);
        inpf.transform.SetParent(svcontent.transform);
        tb.transform.localScale = new Vector3(1, 1, 1);
        inpf.transform.localScale = new Vector3(1, 1, 1);
    }    

    public void NewExParam()
    {
        newexparampanel = Instantiate(newexparamprefab);
        newexparampanel.name = "NewExParamPanel";
        newexparampanel.transform.SetParent(canvas.transform);
        ((RectTransform)newexparampanel.transform).anchoredPosition = new Vector2();
        newexparampanel.transform.localScale = new Vector3(1, 1, 1);

        newexparampanel.GetComponent<NewExParamPanel>().uimanager = uimanager;
        panelcontentcanvasgroup.interactable = false;
        statusbarcanvasgroup.interactable = false;
    }

    public void CancelNewExParam()
    {
        Destroy(newexparampanel);
        panelcontentcanvasgroup.interactable = true;
        statusbarcanvasgroup.interactable = true;
    }

    public void DeleteExParam()
    {

    }
}
