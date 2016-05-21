// --------------------------------------------------------------
// ConditionPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace VLab
{
    public class ConditionPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject condcontent, inputprefab,headertextprefab;

        public void OnConditionPanel(bool ison)
        {
            if (ison)
            {
                uicontroller.exmanager.el.PrepareCondition();
                CreateConditionUI();
            }
            else
            {
                DestroyConditionUI();
            }
        }

        void CreateConditionUI()
        {
            var cond = uicontroller.exmanager.el.condmanager.cond;
            var grid = condcontent.GetComponent<GridLayoutGroup>();
            var fn = cond.Keys.Count;
            if (fn > 0)
            {
                var rn = cond.First().Value.Count + 1;
                grid.constraintCount = rn;
            }
            foreach (var f in cond.Keys)
            {
                AddCondFactorLevels(f, cond[f], condcontent.transform);
            }

            UpdateViewRect();
        }

        public void UpdateViewRect()
        {
            var np = condcontent.transform.childCount;
            var grid = condcontent.GetComponent<GridLayoutGroup>();
            var rn = grid.constraintCount;
            var cn = Mathf.Floor(np / rn);
            var rt = (RectTransform)condcontent.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
        }

        void AddCondFactorLevels(string name, List<object> value,Transform parent)
        {
            var headertext = Instantiate(headertextprefab);
            headertext.name = name;
            headertext.GetComponentInChildren<Text>().text = name;
            headertext.transform.parent = parent;
            headertext.transform.localScale = new Vector3(1, 1, 1);

            for (var i = 0; i < value.Count; i++)
            {
                var inputvalue = Instantiate(inputprefab);
                inputvalue.name = name + "_" + (i+1).ToString();
                var ivif = inputvalue.GetComponent<InputField>();
                ivif.text = VLConvert.Convert<string>(value[i]); 

                inputvalue.transform.SetParent(parent);
                inputvalue.transform.localScale = new Vector3(1, 1, 1);
            }
        }

        void DestroyConditionUI()
        {
            for (var i = 0; i < condcontent.transform.childCount; i++)
            {
                Destroy(condcontent.transform.GetChild(i).gameObject);
            }
        }

    }
}