// --------------------------------------------------------------
// NewExperimentPanel.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VLab;

public class NewExperimentPanel : MonoBehaviour
{
    public VLUIController uimanager;
    public Text namecheck;
    public Button confirm, cancel;
    public InputField nameinput;
    public Dropdown copyfromnames;

    public void Confirm()
    {
        var newname = nameinput.text;
        var copyfrom = copyfromnames.captionText.text;
        uimanager.exmanager.NewExDef(newname, copyfrom);
        Cancel();
    }

    public void Cancel()
    {
        uimanager.controlpanel.CancelNewEx();
    }
	
    public void OnNewExName(string name)
    {
        if(uimanager.exmanager.IsExDefNameExist(name))
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
        os.AddRange(uimanager.exmanager.exdefnames);
        copyfromnames.AddOptions(os);
    }

    void Start()
    {
        UpdateCopyFrom();
    }
}
