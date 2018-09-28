/*
VLRecord.cs is part of the VLAB project.
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
using System.Collections;
using Ripple;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace IExSys
{
    public enum RecordSystem
    {
        VLabRecord,
        Ripple,
        Plexon,
        TDT
    }

    public enum RecordStatus
    {
        Recording,
        Stopped,
        Paused
    }

    public interface IRecorder : IDisposable
    {
        string RecordPath { set; }
        RecordStatus RecordStatus { get; set; }
        bool DigitalInput(out List<double>[] dt, out List<int>[] dv);
    }

    public class RippleRecorder : IRecorder
    {
        int disposecount = 0;
        readonly int tickfreq, timeunitpersec;
        XippmexDotnet xippmexdotnet = new XippmexDotnet();

        public RippleRecorder(int tickfreqency = 30000, int timeunitpersecond = 1000, bool isdinbitchange = true, string recordpath = null)
        {
            tickfreq = tickfreqency;
            timeunitpersec = timeunitpersecond;
            if (isdinbitchange)
            {
                xippmexdotnet.diginbitchange(new MWNumericArray(1, true));
            }
            if (!string.IsNullOrEmpty(recordpath))
            {
                RecordPath = recordpath;
            }
        }

        ~RippleRecorder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (1 == Interlocked.Exchange(ref disposecount, 1))
            {
                return;
            }
            xippmexdotnet.xippmex("close");
            xippmexdotnet.Dispose();
        }

        public bool DigitalInput(out List<double>[] dt, out List<int>[] dv)
        {
            bool isdin = false;
            var d = xippmexdotnet.digin(2);
            var d0 = d[0] as MWCellArray;
            var d1 = d[1] as MWCellArray;
            var chn = d0.NumberOfElements;
            dt = new List<double>[chn]; dv = new List<int>[chn];
            for (var i = 0; i < chn; i++)
            {
                var cdt = d0[i + 1, 1] as MWNumericArray;
                var cdv = d1[i + 1, 1] as MWNumericArray;
                if (!cdt.IsEmpty)
                {
                    isdin = true;
                    dt[i] = new List<double>();
                    dv[i] = new List<int>();
                    var t = (double[])cdt.ToVector(MWArrayComponent.Real);
                    var v = (double[])cdv.ToVector(MWArrayComponent.Real);
                    for (var j = 0; j < t.Length; j++)
                    {
                        dt[i].Add(t[j] / tickfreq * timeunitpersec);
                        dv[i].Add((int)v[j]);
                    }
                }
            }
            return isdin;
        }

        public string RecordPath
        {
            set
            {
                try
                {
                    var trellis = xippmexdotnet.xippmex(1, "opers");
                    if (trellis.Length > 0)
                    {
                        xippmexdotnet.xippmex(1, "trial", trellis[0], MWNumericArray.Empty, value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }
        }

        public RecordStatus RecordStatus
        {
            get
            {
                try
                {
                    var trellis = xippmexdotnet.xippmex(1, "opers");
                    if (trellis.Length > 0)
                    {
                        var r = xippmexdotnet.xippmex(1, "trial", trellis[0])[0] as MWStructArray;
                        var rs = r["status", 1, 1] as MWCharArray;
                        return (RecordStatus)Enum.Parse(typeof(RecordStatus), rs.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
                return RecordStatus.Stopped;
            }
            set
            {
                try
                {
                    var trellis = xippmexdotnet.xippmex(1, "opers");
                    if (trellis.Length > 0)
                    {
                        var r = xippmexdotnet.xippmex(1, "trial", trellis[0], value.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }
        }

    }

    public class VLabRecorder : IRecorder
    {
        int disposecount = 0;
        public string RecordPath { set { } }
        public RecordStatus RecordStatus { get; set; }

        ~VLabRecorder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (1 == Interlocked.Exchange(ref disposecount, 1))
            {
                return;
            }
        }

        public bool DigitalInput(out List<double>[] dt, out List<int>[] dv)
        {
            throw new NotImplementedException();
        }
    }
}
