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
using System.Collections.Generic;
using UnityEngine;
using SpikeGLX;
using MathWorks.MATLAB.NET.Arrays;
using System.Threading;
using System;

namespace Experica
{
    public class SpikeGLXRecorder : IRecorder
    {
        int disposecount = 0;
        SpikeGLXDotnet spikeglx = new SpikeGLXDotnet();

        readonly object spikeglxlock = new object();
        readonly object apilock = new object();

        public SpikeGLXRecorder(string host = "localhost", int port = 4142)
        {
            if (!Connect(host, port))
            {
                Debug.LogWarning($"Can't connect to SpikeGLX, make sure SpikeGLX command server is started and the Host: {host} / Port: {port} match the server.");
                return;
            }
            Host = host;
            Port = port;
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
                Disconnect();
                lock (spikeglxlock)
                {
                    spikeglx.Dispose();
                    spikeglx = null;
                }
            }
        }

        public void SetRecordingBeep(int onfreq = 1046, int ondur_ms = 700, int offfreq = 880, int offdur_ms = 800)
        {
            try
            {
                lock (spikeglxlock)
                {
                    spikeglx?.SetRecordingBeep(0, onfreq, ondur_ms, offfreq, offdur_ms);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public bool IsConnected { get; private set; }

        public bool Connect(string host = "localhost", int port = 4142)
        {
            bool r = false;
            try
            {
                lock (spikeglxlock)
                {
                    r = ((MWLogicalArray)spikeglx?.Connect(1, host, port)[0]).ToVector()[0];
                }
            }
            catch (Exception e) { Debug.LogException(e); }
            IsConnected = r;
            return r;
        }

        public bool Connect()
        {
            return Connect("localhost", 4142);
        }

        public void Disconnect()
        {
            try
            {
                lock (spikeglxlock)
                {
                    spikeglx?.Disconnect();
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        public string RecordPath
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
                        spikeglx?.SetFile(0, value);
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
                            spikeglx?.SetRecording(0, 1);
                        }
                        else if (value == RecordStatus.Stopped)
                        {
                            spikeglx?.SetRecording(0, 0);
                        }
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public string RecordEpoch { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DataFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AcqusitionStatus AcqusitionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}