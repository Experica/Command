/*
ParamPrefab.cs is part of the VLAB project.
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
using System.IO;
using System.Linq;

namespace IExSys
{
    public enum ParamUI
    {
        ToggleButtonGreen,
        InputField,
        DropDown,
        DirInput,
        PathInput,
        ToggleButtonGreenInputField,
        ToggleButtonGreenDropDown,
        ToggleButtonGreenDirInput,
        ToggleButtonGreenPathInput,
        SelectToggleButtonGreenInputField,
        SelectToggleButtonGreenDropDown,
        SelectToggleButtonGreenDirInput,
        SelectToggleButtonGreenPathInput
    }

    public static class ParamPrefab
    {
        static Dictionary<ParamUI, GameObject> prefabs = new Dictionary<ParamUI, GameObject>();

        static ParamPrefab()
        {
            foreach (var pui in Enum.GetValues(typeof(ParamUI)))
            {
                var v = (ParamUI)pui;
                prefabs[v] = Resources.Load<GameObject>(Path.Combine("UI", v.ToString()));
            }
        }

        public static GameObject GetPrefab(this string name, Type T, bool isselectable = false)
        {
            var pi = name.LastIndexOf("Path");
            var di = name.LastIndexOf("Dir");
            ParamUI pui;
            if (pi > 0 && pi == (name.Length - 4))
            {
                pui = isselectable ? ParamUI.SelectToggleButtonGreenPathInput : ParamUI.ToggleButtonGreenPathInput;
            }
            else if (di > 0 && di == (name.Length - 3))
            {
                pui = isselectable ? ParamUI.SelectToggleButtonGreenDirInput : ParamUI.ToggleButtonGreenDirInput;
            }
            else
            {
                if (T.IsEnum || T == typeof(bool))
                {
                    pui = isselectable ? ParamUI.SelectToggleButtonGreenDropDown : ParamUI.ToggleButtonGreenDropDown;
                }
                else
                {
                    pui = isselectable ? ParamUI.SelectToggleButtonGreenInputField : ParamUI.ToggleButtonGreenInputField;
                }
            }
            return prefabs[pui];
        }
    }
}