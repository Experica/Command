// --------------------------------------------------------------
// NewExperimentPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VLab;

public class NewExperimentPanel : MonoBehaviour
{
    public VLUIController uicontroller;
    public Text namecheck;
    public Button confirm, cancel;
    public InputField nameinput;
    public Dropdown copyfromnames;

    public void Confirm()
    {
        var newname = nameinput.text;
        var copyfrom = copyfromnames.captionText.text;
        if (uicontroller.exmanager.NewExDef(newname, copyfrom))
        {
            uicontroller.controlpanel.exdropdown.options.Add(new Dropdown.OptionData(newname));
            uicontroller.controlpanel.exdropdown.value = uicontroller.controlpanel.exdropdown.options.Count - 1;
        }
        Cancel();
    }

    public void Cancel()
    {
        uicontroller.controlpanel.CancelNewEx();
    }

    public void OnNewExNameEndEdit(string name)
    {
        if (uicontroller.exmanager.exdefnames.Contains(name))
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

    public void UpdateCopyFrom()
    {
        var os = new List<string>();
        os.Add("");
        os.AddRange(uicontroller.exmanager.exdefnames);
        copyfromnames.AddOptions(os);
    }

    void Start()
    {
        UpdateCopyFrom();
    }
}
