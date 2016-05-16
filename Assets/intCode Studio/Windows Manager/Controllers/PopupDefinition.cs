// --------------------------------------------------------------
// PopupDefinition.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class PopupDefinition
{
    [SerializeField]
    private string tittleText;

    [SerializeField]
    private string descriptionText;

    [SerializeField]
    private string confirmText;

    [SerializeField]
    private bool hasCancelButton = true;

    [SerializeField]
    private string cancelText;

    public string TittleText
    {
        get { return tittleText; }
    }

    public string DescriptionText
    {
        get { return descriptionText; }
    }

    public string ConfirmText
    {
        get { return confirmText; }
    }

    public bool HasCancelButton
    {
        get { return hasCancelButton; }
    }

    public string CancelText
    {
        get { return cancelText; }
    }
}