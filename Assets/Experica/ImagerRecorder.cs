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
using System;

namespace Experica
{
    public class ImagerRecorder : IRecorder
    {
        ImagerCommand.Command imager = new ImagerCommand.Command();

        public ImagerRecorder(string host = "localhost", int port = 10000)
        {
            if (!Connect(host, port))
            {
                Debug.LogWarning($"Can't connect to Imager, make sure Imager command server is running and the Host: {host} / Port: {port} match the server.");
            }
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
            Disconnect();
            imager = null;
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
            try
            {
                imager.Connect(host, (uint)port);
                Host = host; Port = port;
                IsConnected = true;
                return true;
            }
            catch (Exception e) { Debug.LogException(e); }
            IsConnected = false;
            return false;
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

        public AcquisitionStatus AcquisitionStatus
        {
            get
            {
                try
                {
                    return imager.IsAcquisiting ? AcquisitionStatus.Acquisiting : AcquisitionStatus.Stopped;
                }
                catch (Exception e) { Debug.LogException(e); }
                return AcquisitionStatus.None;
            }
            set
            {
                try
                {
                    if (value == AcquisitionStatus.Acquisiting)
                    {
                        imager.IsAcquisiting = true;
                    }
                    else if (value == AcquisitionStatus.Stopped)
                    {
                        imager.IsAcquisiting = false;
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
                    return imager.IsRecording ? RecordStatus.Recording : RecordStatus.Stopped;
                }
                catch (Exception e) { Debug.LogException(e); }
                return RecordStatus.None;
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