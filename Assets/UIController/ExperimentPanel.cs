// --------------------------------------------------------------
// ExperimentPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using VLab;

public class ExperimentPanel:MonoBehaviour
{
    public VLUIController uicontroller;
    public GameObject svcontent, inputfield, togglebutton,filepathinput,newexparamprefab, 
        togglebuttoninputfield, togglebuttondirinput, togglebuttonfilepathinput, togglebuttondropdown,
        toggletogglebuttoninputfield, toggletogglebuttondirinput, toggletogglebuttonfilepathinput, toggletogglebuttondropdown;
    public Canvas canvas;
    public CanvasGroup panelcontentcanvasgroup,statusbarcanvasgroup;

    public Dictionary<string, InputField> input = new Dictionary<string, InputField>();
    public Dictionary<string, Toggle> inherittoggle = new Dictionary<string, Toggle>();
    public Dictionary<string, Dropdown> dropdowns = new Dictionary<string, Dropdown>();
    public Dictionary<string, Toggle> customparamtoggle = new Dictionary<string, Toggle>();
    public Dictionary<string, GameObject> customparamgo = new Dictionary<string, GameObject>();

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
        foreach(var p in Experiment.properties.Keys)
        {
            var vt = Experiment.properties[p].PropertyType;
            var v = ex.GetValue(p);
            AddParam(p,vt,v, ex.exinheritparams.Contains(p),
                ChoosePrefab(p,vt,false),svcontent.transform);
        }

        UpdateExCustomParam(ex);
        UpdateViewRect();
     }

    public GameObject ChoosePrefab(string name, Type T,bool iscustom)
    {
        GameObject prefab;
        var pi = name.LastIndexOf("path");
        var di = name.LastIndexOf("dir");
        if (pi >= 0 && pi == (name.Length - 4))
        {
            if (iscustom)
            {
                prefab = toggletogglebuttonfilepathinput;
            }
            else
            {
                prefab = togglebuttonfilepathinput;
            }
        }
        else if(di >= 0 && di == (name.Length - 3))
        {
            if (iscustom)
            {
                prefab = toggletogglebuttondirinput;
            }
            else
            {
                prefab = togglebuttondirinput;
            }
        }
        else
        {
            if (T.IsEnum)
            {
                if (iscustom)
                {
                    prefab = toggletogglebuttondropdown;
                }
                else
                {
                    prefab = togglebuttondropdown;
                }
            }
            else
            {
                if (iscustom)
                {
                    prefab = toggletogglebuttoninputfield;
                }
                else
                {
                    prefab = togglebuttoninputfield;
                }
            }
        }
        return prefab;
    }

    public void UpdateExCustomParam(Experiment ex)
    {
        foreach(var go in customparamtoggle.Values)
        {
            Destroy(go);
        }
        customparamtoggle.Clear();
        foreach(var p in ex.param.Keys)
        {
            AddCustomParam(p, ex.param[p], ex.exinheritparams.Contains(p));
        }
    }

    public void AddCustomParam(string name,object value,bool isinherit)
    {
        var pt = value.GetType();
        AddParam(name, pt, value, isinherit,
            ChoosePrefab(name,pt,true), svcontent.transform);
    }

    public void UpdateViewRect()
    {
        var np =svcontent. transform.childCount;
        var grid = svcontent.GetComponent<GridLayoutGroup>();
        var cn = grid.constraintCount;
        var rn = Mathf.Floor(np / cn ) + 1;
        var rt = (RectTransform)svcontent.transform;
        rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
    }

    public void UpdateView(Experiment ex)
    {
        foreach (var n in Experiment.properties.Keys)
        {
            inherittoggle[n].isOn = ex.exinheritparams.Contains(n);
            var T = Experiment.properties[n].PropertyType;
            var v = ex.GetValue(n);
            if (T.IsEnum)
            {
                var vs = Enum.GetNames(T).ToList();
                if (v == null || !vs.Contains(v.ToString()))
                {
                    v = vs[0];
                }
                dropdowns[n].value = vs.IndexOf(v.ToString());
            }
            else
            {
                input[n].text = v == null ? "" : (string)VLConvert.Convert(v, typeof(string));
            }
        }

        UpdateExCustomParam(ex);
        UpdateViewRect();
    }

    public void UpdateParamUI(string name,object value)
    {
        if(input.ContainsKey(name))
        {
            input[name].text = value.ToString();
        }
        if(dropdowns.ContainsKey(name))
        {
            //dropdowns[name].value = value
        }
    }

    void AddParam(string name,Type T, object value,bool isinherit,GameObject prefab,Transform parent)
    {
        var go = Instantiate(prefab);
        go.name = name;

        for(var i=0;i<go.transform.childCount;i++)
        {
            var cgo = go.transform.GetChild(i).gameObject;
            var toggle = cgo.GetComponent<Toggle>();
            var inputfield = cgo.GetComponent<InputField>();
            var dropdown = cgo.GetComponent<Dropdown>();
            if(toggle!=null)
            {
                cgo.GetComponentInChildren<Text>().text = name;
                toggle.isOn = isinherit;
                toggle.onValueChanged.AddListener((ison) => uicontroller.ToggleExInheritParam(name, ison));
                inherittoggle[name] = toggle;
                for(var j=0;j<cgo.transform.childCount;j++)
                {
                    var ctoggle = cgo.transform.GetChild(j).gameObject.GetComponent<Toggle>();
                    if (ctoggle != null)
                    {
                        customparamtoggle[name] = ctoggle;
                        customparamgo[name] = go;
                    }
                }
            }
            if(inputfield!=null)
            {
                if (value == null)
                {
                    value = "";
                }
                inputfield.text = (string)VLConvert.Convert(value, typeof(string));
                inputfield.onEndEdit.AddListener((v) => uicontroller.SetExParam(name, v));
                input[name] = inputfield;
            }
            if(dropdown!=null)
            {
                var vs = Enum.GetNames(T).ToList();
                if (value == null || !vs.Contains(value.ToString()))
                {
                    value = vs[0];
                }
                dropdown.AddOptions(vs);
                dropdown.value = vs.IndexOf(value.ToString());
                dropdown.onValueChanged.AddListener((v) => uicontroller.SetExParam(name, dropdown.captionText.text));
                dropdowns[name] = dropdown;
            }
        }

        go.transform.SetParent(parent);
        go.transform.localScale = new Vector3(1, 1, 1);
    }    

    public void NewExParam()
    {
        newexparampanel = Instantiate(newexparamprefab);
        newexparampanel.name = "NewExParamPanel";
        newexparampanel.transform.SetParent(canvas.transform);
        ((RectTransform)newexparampanel.transform).anchoredPosition = new Vector2();
        newexparampanel.transform.localScale = new Vector3(1, 1, 1);

        newexparampanel.GetComponent<NewExParamPanel>().uicontroller = uicontroller;
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
        var ps = new List<string>(customparamgo.Keys);
        foreach (var p in ps)
        {
            if (customparamtoggle[p].isOn)
            {
                uicontroller.exmanager.el.ex.param.Remove(p);
                uicontroller.exmanager.el.ex.exinheritparams.Remove(p);

                var go = customparamgo[p];
                customparamtoggle.Remove(p);
                customparamgo.Remove(p);
                Destroy(go);
            }
        }
        UpdateViewRect();
    }
}
