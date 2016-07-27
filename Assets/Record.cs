// -----------------------------------------------------------------------------
// Record.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

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
