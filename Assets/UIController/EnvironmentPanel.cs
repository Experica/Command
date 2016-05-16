// --------------------------------------------------------------
// EnvironmentPanel.cs is part of the VLab project.
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

namespace VLab
{
    public class EnvironmentPanel : MonoBehaviour
    {
        public VLUIController uimanager;
        public GameObject svcontent, inputfield, togglebutton;
        public Dictionary<string, InputField> input = new Dictionary<string, InputField>();

        public void UpdateEnv(EnvironmentManager em)
        {
            for (var i = 0; i < svcontent.transform.childCount; i++)
            {
                UnityEngine.Object.Destroy(svcontent.transform.GetChild(i).gameObject);
            }
            AddView(em);
        }

        public void AddView(EnvironmentManager em)
        {
            var np = 0;
            foreach (var p in em.syncparam.Keys)
            {
                var i = p.IndexOf("@");
                var pn = p.Substring(0, i);
                var psc = p.Substring(i + 1);
                if (em.activesync.Contains(psc))
                {
                    AddParam(p, em.syncparam[p].PropertyType, em.GetParam(p), uimanager.exmanager.el.ex.envinheritparams.Contains(p), pn);
                    np += 1;
                }
            }

            var grid = svcontent.GetComponent<GridLayoutGroup>();
            var cn = grid.constraintCount;
            var rn = Mathf.Floor(np / (cn / 2.0f)) + 1;
            var rt = (RectTransform)svcontent.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * cn, (grid.cellSize.y + grid.spacing.y) * rn);
        }

        void AddParam(string name, Type T, object value, bool isinherit, string shortname)
        {
            if (value == null)
            {
                value = "";
            }
            var tb = UnityEngine.Object.Instantiate(togglebutton);
            tb.name = name + "_ToggleButton";
            tb.GetComponentInChildren<Text>().text = shortname;
            var tbt = tb.GetComponent<Toggle>();
            tbt.isOn = isinherit;
            tbt.onValueChanged.AddListener((ison) => uimanager.ToggleEnvInheritParam(name, ison));

            var inpf = UnityEngine.Object.Instantiate(inputfield);
            inpf.name = name;
            var ifif = inpf.GetComponent<InputField>();
            ifif.text = (string)VLConvert.Convert(value, typeof(string));
            ifif.onEndEdit.AddListener((v) => uimanager.SetEnvParam(name, v));
            input[name] = ifif;

            tb.transform.SetParent(svcontent.transform);
            inpf.transform.SetParent(svcontent.transform);
            tb.transform.localScale = new Vector3(1, 1, 1);
            inpf.transform.localScale = new Vector3(1, 1, 1);
        }

    }
}