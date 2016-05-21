// --------------------------------------------------------------
// NewExParamPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VLab;

public class NewExParamPanel : MonoBehaviour
{
    public VLUIController uicontroller;
    public Text namecheck;
    public Button confirm, cancel;
    public InputField nameinput,valueinput;

    public void OnNewExParamName(string name)
    {
        if(uicontroller.exmanager.el.ex.param.ContainsKey(name))
        {
            namecheck.text = "Name Exists";
            confirm.interactable = false;
        }
        else
        {
            namecheck.text = "";
            confirm.interactable = true;
        }
    }

    public void Confirm()
    {
        var newname = nameinput.text;
        var value = valueinput.text;
        uicontroller.exmanager.el.ex.param.Add(newname, value);

        uicontroller.expanel.AddCustomParam(newname, value, false);
        uicontroller.expanel.UpdateViewRect();
        Cancel();
    }

    public void Cancel()
    {
        uicontroller.expanel.CancelNewExParam();
    }
}
