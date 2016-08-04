// -----------------------------------------------------------------------------
// EnvironmentPanel.cs is part of the VLAB project.
// Copyright (c) 2016 Li Alex Zhang and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

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
        public GameObject content;

        public Dictionary<string, Toggle> inherittoggle = new Dictionary<string, Toggle>();
        public Dictionary<string, InputField> inputfield = new Dictionary<string, InputField>();
        public Dictionary<string, Dropdown> dropdown = new Dictionary<string, Dropdown>();


        public void UpdateEnv(EnvironmentManager em)
        {
            for (var i = 0; i < content.transform.childCount; i++)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }
            AddEnvUI(em);
        }

        public void AddEnvUI(EnvironmentManager em)
        {
            var isshowinactive = (bool)uicontroller.appmanager.config[VLCFG.IsShowInactiveEnvParam];
            var isshowfullname = (bool)uicontroller.appmanager.config[VLCFG.IsShowEnvParamFullName];
            foreach (var fullname in em.net_syncvar.Keys)
            {
                string paramname, nb;
                fullname.FirstAtSplit(out paramname, out nb);
                var showname = isshowfullname ? fullname : paramname;
                var T = em.net_syncvar[fullname].Type;
                if (!isshowinactive)
                {
                    if (em.activenet.Contains(nb))
                    {
                        AddParamUI(fullname, showname, paramname, T, em.GetParam(fullname),
                            uicontroller.exmanager.el.ex.EnvInheritParam.Contains(paramname),
                            paramname.GetPrefab(T), content.transform);
                    }
                }
                else
                {
                    AddParamUI(fullname, showname, paramname, T, em.GetParam(fullname),
                        uicontroller.exmanager.el.ex.EnvInheritParam.Contains(paramname),
                            paramname.GetPrefab(T), content.transform);
                }
            }
            UpdateContentRect();
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

        public void UpdateParamUI(string fullname, object value)
        {
            if (dropdown.ContainsKey(fullname))
            {
                var dd = dropdown[fullname];
                var vs = dd.options.Select(i => i.text).ToList();
                dd.value = vs.IndexOf(value.ToString());
                return;
            }
            if (inputfield.ContainsKey(fullname))
            {
                inputfield[fullname].text = value == null ? "" : value.Convert<string>();
                return;
            }
        }

        public void AddParamUI(string fullname, string showname, string paramname, Type T, object value, bool isinherit, GameObject prefab, Transform parent)
        {
            var go = Instantiate(prefab);
            go.name = fullname;
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var cgo = go.transform.GetChild(i).gameObject;
                var toggle = cgo.GetComponent<Toggle>();
                var inputfield = cgo.GetComponent<InputField>();
                var dropdown = cgo.GetComponent<Dropdown>();
                // Check Inherit ToggleButton
                if (toggle != null)
                {
                    cgo.GetComponentInChildren<Text>().text = showname;
                    toggle.isOn = isinherit;
                    toggle.onValueChanged.AddListener(ison => uicontroller.ToggleEnvInherit(fullname, paramname, ison));
                    inherittoggle[fullname] = toggle;
                }
                if (inputfield != null)
                {
                    inputfield.text = value == null ? "" : value.Convert<string>();
                    inputfield.onEndEdit.AddListener(s => uicontroller.exmanager.el.envmanager.SetParam(fullname, s));
                    this.inputfield[fullname] = inputfield;
                }
                if (dropdown != null)
                {
                    var vs = T.GetValue();
                    if (vs != null && vs.Contains(value.ToString()))
                    {
                        dropdown.AddOptions(vs);
                        dropdown.value = vs.IndexOf(value.ToString());
                        dropdown.onValueChanged.AddListener(vi => uicontroller.exmanager.el.envmanager.SetParam(fullname, dropdown.captionText.text));
                        this.dropdown[fullname] = dropdown;
                    }
                }
            }
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(1, 1, 1);
        }

        public void OffsetToPosition()
        {
            var el = uicontroller.exmanager.el;
           if(el!=null)
            {
                var ori = el.envmanager.GetActiveParam("Ori");
                var orioffset = el.envmanager.GetActiveParam("OriOffset");
                var p = el.envmanager.GetActiveParam("Position");
                var poffset = el.envmanager.GetActiveParam("PositionOffset");
                if(ori!=null && orioffset!=null && p!=null && poffset!=null)
                {
                    var cp = ((Vector3)poffset).RotateZCCW((float)ori + (float)orioffset)+(Vector3)p;
                    el.envmanager.SetActiveParam("PositionOffset", new Vector3(), true);
                    el.envmanager.SetActiveParam("Position", cp, true);
                }
            }
        }

    }
}