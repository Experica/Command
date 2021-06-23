/*
RippleRecorder.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ripple;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using System.Threading;
using System;

namespace Experica
{
    public class RippleRecorder : IRecorder
    {
        int disposecount = 0;
        readonly int tickfreq, timeunitpersec;
        XippmexDotnet xippmexdotnet = new XippmexDotnet();

        readonly object xippmexlock = new object();
        readonly object apilock = new object();

        public RippleRecorder(int tickfreqency = 30000, int timeunitpersecond = 1000, double dinbitchange = 1, string recordpath = null)
        {
            tickfreq = tickfreqency;
            timeunitpersec = timeunitpersecond;
            if (!Connect())
            {
                Debug.Log("xippmex init failed.");
                return;
            }
            try
            {
                lock (xippmexlock)
                {
                    xippmexdotnet.diginbitchange(new MWNumericArray(dinbitchange));
                }
            }
            catch (Exception e) { Debug.LogException(e); }
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
            lock (apilock)
            {
                Disconnect();
                lock (xippmexlock)
                {
                    xippmexdotnet.Dispose();
                }
            }
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            bool isdin = false;
            dintime = null; dinvalue = null; MWArray[] d = null;
            try
            {
                lock (xippmexlock)
                {
                    d = xippmexdotnet.digin(3);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
            if (d != null)
            {
                var d1 = d[1] as MWCellArray;
                var d2 = d[2] as MWCellArray;
                var chn = d1.NumberOfElements;
                for (var i = 1; i <= chn; i++)
                {
                    var cdt = d1[i, 1] as MWNumericArray;
                    var cdv = d2[i, 1] as MWNumericArray;
                    if (!cdt.IsEmpty)
                    {
                        if (dintime == null)
                        {
                            dintime = new Dictionary<int, List<double>>(); dinvalue = new Dictionary<int, List<int>>();
                            isdin = true;
                        }
                        dintime[i] = new List<double>();
                        dinvalue[i] = new List<int>();
                        var t = (double[])cdt.ToVector(MWArrayComponent.Real);
                        var v = (double[])cdv.ToVector(MWArrayComponent.Real);
                        for (var j = 0; j < t.Length; j++)
                        {
                            dintime[i].Add(t[j] / tickfreq * timeunitpersec);
                            dinvalue[i].Add((int)v[j]);
                        }
                    }
                }
            }
            return isdin;
        }

        public bool Connect()
        {
            bool r = false;
            try
            {
                lock (xippmexlock)
                {
                    r = ((MWLogicalArray)xippmexdotnet.xippmex(1)[0]).ToVector()[0];
                }
            }
            catch (Exception e) { Debug.LogException(e); }
            return r;
        }

        public void Disconnect()
        {
            lock (xippmexlock)
            {
                xippmexdotnet.xippmex("close");
            }
        }

        public bool Connect(string host, int port)
        {
            return Connect();
        }

        public string RecordPath
        {
            set
            {
                try
                {
                    lock (xippmexlock)
                    {
                        var trellis = xippmexdotnet.xippmex(1, "opers");
                        if (trellis?.Length > 0)
                        {
                            xippmexdotnet.xippmex(1, "trial", trellis[0], MWNumericArray.Empty, value);
                        }
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
            get { throw new NotImplementedException(); }
        }

        public RecordStatus RecordStatus
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string RecordEpoch { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}