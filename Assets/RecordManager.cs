// --------------------------------------------------------------
// RecordManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using XippmexDotNet;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET;
using MathWorks.MATLAB.NET.Utility;

namespace VLab
{
    public enum VLRecordSystem
    {
        Ripple,
        Plexon,
        TDT
    }

    public class RecordManager
    {
        XippmexDotNet.XippmexDotNet xippmex;
        public RecordManager(VLRecordSystem recordsystem)
        {
            switch (recordsystem)
            {
                case VLRecordSystem.Ripple:
                    xippmex = new XippmexDotNet.XippmexDotNet();
                    break;
            }
        }

        public void Help()
        {
            //MWArray[] output = new MWArray[1];
            //var output = xippmex.xippmex(1,new MWCharArray( "time"));
        }

        public void RecordPath()
        {
        }

    }
}
