// --------------------------------------------------------------
// Record.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using Ripple;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using System;

namespace VLab
{
    public enum VLRecordSystem
    {
        VLabRecord,
        Ripple,
        Plexon,
        TDT
    }

    public interface IRecorder
    {
        void SetRecordPath(string path);
    }

    public class RippleRecorder:IRecorder
    {
        XippmexDotNet xippmex = new XippmexDotNet();

        public void SetRecordPath(string path)
        {
            var trellis =  xippmex.xippmex(1, new MWCharArray("opers"));
            if(trellis.Length>0)
            {
                xippmex.xippmex(1, new MWCharArray("trial"), trellis[0], MWNumericArray.Empty, new MWCharArray(path));
            }
        }
    }

    public class VLabRecorder : IRecorder
    {
        public void SetRecordPath(string path)
        {
        }
    }

    public class RecordManager
    {
        public IRecorder recorder;

        public RecordManager(VLRecordSystem recordsystem= VLRecordSystem.VLabRecord)
        {
            switch (recordsystem)
            {
                case VLRecordSystem.Ripple:
                    recorder = new RippleRecorder();
                    break;
                default:
                    recorder = new VLabRecorder();
                    break;
            }
        }

    }
}
