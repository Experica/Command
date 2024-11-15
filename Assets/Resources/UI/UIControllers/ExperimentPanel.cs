/*
ExperimentPanel.cs is part of the Experica.
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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace Experica.Command
{
    public class ExperimentPanel : MonoBehaviour
    {
        public UIController uicontroller;
        public GameObject content, newexparampanelprefab;
        GameObject newexparampanel;
        public Canvas canvas;
        public CanvasGroup panelcontentcanvasgroup, statusbarcanvasgroup;

        public Dictionary<string, Toggle> inherittoggle = new Dictionary<string, Toggle>();
        public Dictionary<string, InputField> inputfield = new Dictionary<string, InputField>();
        public Dictionary<string, Dropdown> dropdown = new Dictionary<string, Dropdown>();
        public Dictionary<string, GameObject> exparamgo = new Dictionary<string, GameObject>();
        public Dictionary<string, Toggle> exparamtoggle = new Dictionary<string, Toggle>();


        public void UpdateEx(Experiment ex)
        {
            if (content.transform.childCount == 0)
            {
                AddExUI(ex);
            }
            else
            {
                UpdateExUI(ex);
            }
        }

        public void AddExUI(Experiment ex)
        {
            foreach (var p in ex.Properties().Keys)
            {
                if (IsShowParam(p))
                {
                    var pa = ex.Properties()[p];
                    AddParamUI(p, pa.Type, ex.GetProperty(p), ex.InheritParam.Contains(p),
                        p.GetPrefab(pa.Type), content.transform);
                }
            }
            UpdateExParam(ex);
            UpdateContentRect();
        }

        public void UpdateExUI(Experiment ex)
        {
            foreach (var p in ex.Properties().Keys)
            {
                if (IsShowParam(p))
                {
                    inherittoggle[p].isOn = ex.InheritParam.Contains(p);
                    var v = ex.GetProperty(p);
                    if (dropdown.ContainsKey(p))
                    {
                        dropdown[p].value = dropdown[p].options.Select(i => i.text).ToList().IndexOf(v.Convert<string>());
                    }
                    else
                    {
                        inputfield[p].text = v == null ? "" : v.Convert<string>();
                    }
                }
            }
            UpdateExParam(ex);
            UpdateContentRect();
        }

        bool IsShowParam(string name)
        {
            var exhideparam = uicontroller.config.ExHideParams;
            return exhideparam.Contains(name) ? false : true;
        }

        public void UpdateExParam(Experiment ex)
        {
            foreach (var p in exparamgo.Keys.ToList())
            {
                if (inherittoggle.ContainsKey(p))
                {
                    inherittoggle.Remove(p);
                }
                if (dropdown.ContainsKey(p))
                {
                    dropdown.Remove(p);
                }
                if (inputfield.ContainsKey(p))
                {
                    inputfield.Remove(p);
                }
                Destroy(exparamgo[p]);
            }
            exparamgo.Clear();
            exparamtoggle.Clear();
            foreach (var p in ex.ExtendParam.Keys)
            {
                AddExParamUI(p, ex.ExtendParam[p], ex.InheritParam.Contains(p));
            }
        }

        public void AddExParamUI(string name, object value, bool isinherit)
        {
            var t = value.GetType();
            AddParamUI(name, t, value, isinherit, name.GetPrefab(t, true), content.transform);
        }

        public void UpdateContentRect()
        {
            var np = inherittoggle.Count;
            var grid = content.GetComponent<GridLayoutGroup>();
            var cn = grid.constraintCount;
            var rn = Mathf.Floor(np / cn) + 1;
            var rt = (RectTransform)content.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
        }

        public void AddParamUI(string name, Type T, object value, bool isinherit, GameObject prefab, Transform parent)
        {
            var go = Instantiate(prefab);
            go.name = name;
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var cgo = go.transform.GetChild(i).gameObject;
                var toggle = cgo.GetComponent<Toggle>();
                var inputfield = cgo.GetComponent<InputField>();
                var dropdown = cgo.GetComponent<Dropdown>();
                // Check Inherit ToggleButton
                if (toggle != null)
                {
                    cgo.GetComponentInChildren<Text>().text = name;
                    toggle.isOn = isinherit;
                    toggle.onValueChanged.AddListener(ison => uicontroller.ToggleExInherit(name, ison));
                    inherittoggle[name] = toggle;
                    // Check Select ToggleButton
                    for (var j = 0; j < cgo.transform.childCount; j++)
                    {
                        var cctoggle = cgo.transform.GetChild(j).gameObject.GetComponent<Toggle>();
                        if (cctoggle != null)
                        {
                            exparamtoggle[name] = cctoggle;
                            exparamgo[name] = go;
                        }
                    }
                }
                if (inputfield != null)
                {
                    inputfield.text = value == null ? "" : value.Convert<string>();
                    inputfield.onEndEdit.AddListener(s => uicontroller.exmanager.el.ex.SetParam(name, s));
                    this.inputfield[name] = inputfield;
                }
                if (dropdown != null)
                {
                    var vs = T.GetValue(); var v = value.ToString();
                    if (vs != null && vs.Contains(v))
                    {
                        dropdown.AddOptions(vs);
                        dropdown.value = vs.IndexOf(v);
                        dropdown.onValueChanged.AddListener(vi => uicontroller.exmanager.el.ex.SetParam(name, dropdown.captionText.text));
                        this.dropdown[name] = dropdown;
                    }
                }
            }
            go.transform.SetParent(parent, false);
        }

        public void UpdateParamUI(string name, object value)
        {
            if (dropdown.ContainsKey(name))
            {
                var dd = dropdown[name];
                var vs = dd.options.Select(i => i.text).ToList();
                dd.value = vs.IndexOf(value.ToString());
                return;
            }
            if (inputfield.ContainsKey(name))
            {
                inputfield[name].text = value == null ? "" : value.Convert<string>();
                return;
            }
        }

        public void NewExParamPanel()
        {
            newexparampanel = Instantiate(newexparampanelprefab);
            newexparampanel.name = "NewExParamPanel";
            newexparampanel.transform.SetParent(canvas.transform, false);
            newexparampanel.GetComponent<NewExParamPanel>().uicontroller = uicontroller;

            panelcontentcanvasgroup.interactable = false;
            statusbarcanvasgroup.interactable = false;
        }

        public void DeleteExParamPanel()
        {
            Destroy(newexparampanel);
            panelcontentcanvasgroup.interactable = true;
            statusbarcanvasgroup.interactable = true;
        }

        public void NewExParam(string name, object value)
        {
            uicontroller.exmanager.el.ex.ExtendParam[name] = value;
            AddExParamUI(name, value, false);
            UpdateContentRect();
        }

        public void DeleteExParam()
        {
            foreach (var s in exparamtoggle.Keys.ToList())
            {
                if (exparamtoggle[s].isOn)
                {
                    uicontroller.exmanager.el.ex.ExtendParam.Remove(s);
                    uicontroller.exmanager.el.ex.InheritParam.Remove(s);

                    exparamtoggle.Remove(s);
                    Destroy(exparamgo[s]);
                    exparamgo.Remove(s);
                }
            }
            UpdateContentRect();
        }

    }
}