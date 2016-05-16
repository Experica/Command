// --------------------------------------------------------------
// Tools.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class Tools
{
    public static string GetMethodName(Func<IEnumerator> method)
    {
        return method.Method.Name;
    }
}