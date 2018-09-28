/*
ConditionPanel.cs is part of the VLAB project.
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
using System.Collections.Generic;
using System.Linq;
using System;

namespace IExSys
{
    public class ConditionPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject condcontent, condheadcontent, inputprefab,
            blueheadertextprefab, redheadertextprefab, greenheadertextprefab, textprefab;
        public Canvas panel;
        public Toggle forceprepare;
        public int maxcondshow = 500;

        public void OnConditionPanel(bool ison)
        {
            if (ison)
            {
                var el = uicontroller.exmanager.el;
                if (el != null)
                {
                    el.PrepareCondition(el.regeneratecond);
                }
            }
            else
            {
                DestroyConditionUI();
            }
        }

        public void OnForcePrepare(bool isforceprepare)
        {
            var el = uicontroller.exmanager.el;
            if (el != null)
            {
                el.regeneratecond = isforceprepare;
            }
        }

        public void RefreshCondition()
        {
            if (panel.enabled)
            {
                DestroyConditionUI();
                CreateConditionUI();
            }
        }

        void CreateConditionUI()
        {
            var cond = uicontroller.exmanager.el.condmanager.cond;
            var grid = condcontent.GetComponent<GridLayoutGroup>();
            var fn = cond.Keys.Count;
            if (fn > 0)
            {
                var rn = cond.First().Value.Count;
                if (rn > maxcondshow) return;
                grid.constraintCount = rn;
                AddCondIndex(rn);

                if (uicontroller.exmanager.el.condmanager.nblock > 1)
                {
                    AddBlockIndex(rn);
                }

                foreach (var f in cond.Keys)
                {
                    AddCondFactorLevels(f, cond[f]);
                }
                UpdateViewRect(rn, fn + 1);
            }
        }

        public void UpdateViewRect(int rn, int cn)
        {
            var grid = condcontent.GetComponent<GridLayoutGroup>();
            var rt = (RectTransform)condcontent.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
        }

        void AddCondIndex(int condn)
        {
            var headertext = Instantiate(redheadertextprefab);
            headertext.name = "CondIndex";
            headertext.GetComponentInChildren<Text>().text = "CondIndex";
            headertext.transform.SetParent(condheadcontent.transform, false);

            for (var i = 0; i < condn; i++)
            {
                var textvalue = Instantiate(textprefab);
                textvalue.name = "CondIndex" + "_" + i;
                textvalue.GetComponent<Text>().text = i.ToString();

                textvalue.transform.SetParent(condcontent.transform, false);
            }
        }

        void AddBlockIndex(int condn)
        {
            var headertext = Instantiate(greenheadertextprefab);
            headertext.name = "BlockIndex";
            headertext.GetComponentInChildren<Text>().text = "BlockIndex";
            headertext.transform.SetParent(condheadcontent.transform, false);

            var condsamplesapces = uicontroller.exmanager.el.condmanager.condsamplespaces;
            for (var i = 0; i < condn; i++)
            {
                var textvalue = Instantiate(textprefab);
                for (var j = 0; j < condsamplesapces.Count; j++)
                {
                    if (condsamplesapces[j].Contains(i))
                    {
                        textvalue.name = "BlockIndex" + "_" + j;
                        textvalue.GetComponent<Text>().text = j.ToString();
                    }
                }
                textvalue.transform.SetParent(condcontent.transform, false);
            }
        }

        void AddCondFactorLevels(string name, List<object> value)
        {
            var headertext = Instantiate(blueheadertextprefab);
            headertext.name = name;
            headertext.GetComponentInChildren<Text>().text = name;
            headertext.transform.SetParent(condheadcontent.transform, false);

            for (var i = 0; i < value.Count; i++)
            {
                var inputvalue = Instantiate(inputprefab);
                inputvalue.name = name + "_" + i;
                var inputfield = inputvalue.GetComponent<InputField>();
                inputfield.text = value[i].Convert<string>();
                inputfield.onEndEdit.AddListener(s => value[inputvalue.name.Substring(inputvalue.name.LastIndexOf('_') + 1).Convert<int>()] = s);

                inputvalue.transform.SetParent(condcontent.transform, false);
            }
        }

        void DestroyConditionUI()
        {
            for (var i = 0; i < condheadcontent.transform.childCount; i++)
            {
                Destroy(condheadcontent.transform.GetChild(i).gameObject);
            }
            for (var i = 0; i < condcontent.transform.childCount; i++)
            {
                Destroy(condcontent.transform.GetChild(i).gameObject);
            }
            var rt = (RectTransform)condcontent.transform;
            rt.sizeDelta = new Vector2();
            rt.anchoredPosition = new Vector2();
        }

    }
}