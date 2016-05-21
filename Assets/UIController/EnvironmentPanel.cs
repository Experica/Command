// --------------------------------------------------------------
// EnvironmentPanel.cs is part of the VLAB project.
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
using System.Linq;
using System;

namespace VLab
{
    public class EnvironmentPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject svcontent, inputfield, togglebutton,
            togglebuttoninputfield, togglebuttonfilepathinput, togglebuttondropdown;
        public Dictionary<string, InputField> input = new Dictionary<string, InputField>();
        public Dictionary<string, Dropdown> dropdowns = new Dictionary<string, Dropdown>();
        public void UpdateEnv(EnvironmentManager em)
        {
            for (var i = 0; i < svcontent.transform.childCount; i++)
            {
                Destroy(svcontent.transform.GetChild(i).gameObject);
            }
            AddView(em);
        }

        public void AddView(EnvironmentManager em)
        {
            var isshowinactive = (bool)VLConvert.Convert(uicontroller.appmanager.config["isshowinactiveenvparam"], typeof(bool));
            foreach (var p in em.net_syncvar.Keys)
            {
                var pt = em.net_syncvar[p].PropertyType;
                var pv = em.GetParam(p);
                if (isshowinactive)
                {
                    AddParam(p, pt, pv,
                        uicontroller.exmanager.el.ex.envinheritparams.Contains(p),
                        ChoosePrefab(p,pt),svcontent.transform);
                }
                else
                {
                    if(em.isparamactive(p))
                    {
                        AddParam(p, pt, pv,
                            uicontroller.exmanager.el.ex.envinheritparams.Contains(p),
                            ChoosePrefab(p, pt), svcontent.transform);
                    }
                }
            }

            UpdateViewRect();
        }

        public GameObject ChoosePrefab(string name, Type T)
        {
            GameObject prefab;
            var idx = name.LastIndexOf("path");
            if (idx >= 0 && idx == (name.Length - 4))
            {
                prefab = togglebuttonfilepathinput;
            }
            else
            {
                if (T.IsEnum)
                {
                    prefab = togglebuttondropdown;
                }
                else
                {
                    prefab = togglebuttoninputfield;
                }
            }
            return prefab;
        }

        public void UpdateViewRect()
        {
            var np = svcontent.transform.childCount;
            var grid = svcontent.GetComponent<GridLayoutGroup>();
            var cn = grid.constraintCount;
            var rn = Mathf.Floor(np / cn) + 1;
            var rt = (RectTransform)svcontent.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
        }

        public void UpdateParamUI(string name,object value)
        {
            if (input.ContainsKey(name))
            {
                input[name].text = value.ToString();
            }
            if (dropdowns.ContainsKey(name))
            {
                //dropdowns[name].value = value;
            }
        }

        void AddParam(string name, Type T, object value, bool isinherit,GameObject prefab,Transform parent)
        {
            var go = Instantiate(prefab);
            go.name = name;
            var isshowenvparamfullname = (bool)VLConvert.Convert(uicontroller.appmanager.config["isshowenvparamfullname"], typeof(bool));

            for (var i = 0; i < go.transform.childCount; i++)
            {
                var cgo = go.transform.GetChild(i).gameObject;
                var toggle = cgo.GetComponent<Toggle>();
                var inputfield = cgo.GetComponent<InputField>();
                var dropdown = cgo.GetComponent<Dropdown>();
                if (toggle != null)
                {
                    cgo.GetComponentInChildren<Text>().text = isshowenvparamfullname?name:EnvironmentManager.GetSyncVarName(name);
                    toggle.isOn = isinherit;
                    toggle.onValueChanged.AddListener((ison) => uicontroller.ToggleEnvInheritParam(name, ison));
                }
                if (inputfield != null)
                {
                    if (value == null)
                    {
                        value = "";
                    }
                    inputfield.text = (string)VLConvert.Convert(value, typeof(string));
                    inputfield.onEndEdit.AddListener((v) => uicontroller.SetEnvParam(name, v));
                    input[name] = inputfield;
                }
                if (dropdown != null)
                {
                    var vs = Enum.GetNames(T).ToList();
                    if (value == null || !vs.Contains(value.ToString()))
                    {
                        value = vs[0];
                    }
                    dropdown.AddOptions(vs);
                    dropdown.value = vs.IndexOf(value.ToString());
                    dropdown.onValueChanged.AddListener((v) => uicontroller.SetEnvParam(name, dropdown.captionText.text));
                    dropdowns[name] = dropdown;
                }
            }

            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(1, 1, 1);
        }

    }
}