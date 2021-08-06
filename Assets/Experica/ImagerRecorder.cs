/*
ImagerRecorder.cs is part of the Experica.
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
using MathWorks.MATLAB.NET.Arrays;
using System.Threading;
using System;

namespace Experica
{
    public class ImagerRecorder : IRecorder
    {
        int disposecount = 0;
        ImagerCommand.Command imager = new ImagerCommand.Command();
        readonly object apilock = new object();

        public ImagerRecorder(string host = "localhost", int port = 10000)
        {
            if (!Connect(host, port))
            {
                Debug.LogWarning($"Can't connect to Imager, make sure Imager command server is started and the Host: {host} / Port: {port} match the server.");
                return;
            }
            Host = host;
            Port = port;
        }

        ~ImagerRecorder()
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
                imager = null;
            }
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public bool IsConnected { get; private set; }

        public bool Connect(string host = "localhost", int port = 10000)
        {
            bool r = false;
            try
            {
                imager.Connect(host, (uint)port);
                r = true;
            }
            catch (Exception e) { Debug.LogException(e); }
            IsConnected = r;
            return r;
        }

        public bool Connect()
        {
            return Connect("localhost", 10000);
        }

        public void Disconnect()
        {
            try
            {
                imager.Disconnect();
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        public string RecordPath
        {
            get
            {
                try
                {
                    return imager.RecordPath;
                }
                catch (Exception e) { Debug.LogException(e); }
                return null;
            }
            set
            {
                try
                {
                    imager.RecordPath = value;
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public AcqusitionStatus AcqusitionStatus
        {
            get
            {
                try
                {
                    if (imager.IsAcqusiting)
                    {
                        return AcqusitionStatus.Acqusiting;
                    }
                    else
                    {
                        return AcqusitionStatus.Stopped;
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
                return AcqusitionStatus.Stopped;
            }
            set
            {
                try
                {
                    if (value == AcqusitionStatus.Acqusiting)
                    {
                        imager.IsAcqusiting = true;
                    }
                    else if (value == AcqusitionStatus.Stopped)
                    {
                        imager.IsAcqusiting = false;
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public RecordStatus RecordStatus
        {
            get
            {
                try
                {
                    if (imager.IsRecording)
                    {
                        return RecordStatus.Recording;
                    }
                    else
                    {
                        return RecordStatus.Stopped;
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
                return RecordStatus.Stopped;
            }
            set
            {
                try
                {
                    if (value == RecordStatus.Recording)
                    {
                        imager.IsRecording = true;
                    }
                    else if (value == RecordStatus.Stopped)
                    {
                        imager.IsRecording = false;
                    }
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public string RecordEpoch
        {
            get
            {
                try
                {
                    return imager.RecordEpoch;
                }
                catch (Exception e) { Debug.LogException(e); }
                return null;
            }
            set
            {
                try
                {
                    imager.RecordEpoch = value;
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public string DataFormat
        {
            get
            {
                try
                {
                    return imager.DataFormat;
                }
                catch (Exception e) { Debug.LogException(e); }
                return null;
            }
            set
            {
                try
                {
                    imager.DataFormat = value;
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}