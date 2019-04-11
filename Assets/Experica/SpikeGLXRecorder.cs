/*
SpikeGLXRecorder.cs is part of the Experica.
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
using HHMI;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using System.Threading;
using System;

namespace Experica
{
    public class SpikeGLXRecorder : IRecorder
    {
        int disposecount = 0;
        SpikeGLXDotnet spikeglxdotnet = new SpikeGLXDotnet();

        readonly object spikeglxlock = new object();
        readonly object apilock = new object();

        public SpikeGLXRecorder(string host = "localhost", int port = 4142, string recordpath = null)
        {
            if (!Connect(host, port))
            {
                Debug.Log("Can't connect to SpikeGLX remote server, make sure remote server is started and the server IP and Port are correct.");
                return;
            }
            if (!string.IsNullOrEmpty(recordpath))
            {
                RecordPath = recordpath;
            }
        }

        ~SpikeGLXRecorder()
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
                Close();
                lock (spikeglxlock)
                {
                    spikeglxdotnet.Dispose();
                }
            }
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public bool Connect(string host = "localhost", int port = 4142)
        {
            bool r = false;
            try
            {
                lock (spikeglxlock)
                {
                    r = ((MWLogicalArray)spikeglxdotnet.Connect(1, host, port)[0]).ToVector()[0];
                }
            }
            catch (Exception e) { Debug.LogException(e); }
            return r;
        }

        public bool Connect()
        {
            return Connect("localhost", 4142);
        }

        public void Close()
        {
            lock (spikeglxlock)
            {
                spikeglxdotnet.Disconnect();
            }
        }

        public string RecordPath
        {
            set
            {
                try
                {
                    lock (spikeglxlock)
                    {
                        spikeglxdotnet.SetFile(0, value);
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public RecordStatus RecordStatus
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                try
                {
                    lock (spikeglxlock)
                    {
                        if (value == RecordStatus.Recording)
                        {
                            spikeglxdotnet.SetRecording(0, 1);
                        }
                        else if (value == RecordStatus.Stopped)
                        {
                            spikeglxdotnet.SetRecording(0, 0);
                        }
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}