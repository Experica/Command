/*
IntanRecorder.cs is part of the Experica.
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
using System.Threading;
using System;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text;

namespace Experica
{
    public class IntanRecorder : IRecorder
    {
        int disposecount = 0;
        readonly object api = new();
        Socket intancmd = new(SocketType.Stream,ProtocolType.Tcp);

        public IntanRecorder(string host = "localhost", int port = 5000)
        {
            if (!Connect(host, port))
            {
                Debug.LogWarning($"Can't connect to Intan RHX, make sure RHX command server is started and the Host: {host} / Port: {port} match the server.");
            }
        }

        ~IntanRecorder()
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
            intancmd.Shutdown(SocketShutdown.Both);
        }

        int send(string cmd) => intancmd.Send(Encoding.UTF8.GetBytes(cmd), SocketFlags.None);

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public bool IsConnected { get; private set; }

        public bool Connect(string host = "localhost", int port = 5000)
        {
            lock (api)
            {
                bool r = false;
                try
                {
                    intancmd.Connect(host, port);
                    send("execute clearalldataoutputs");
                    Host = host;
                    Port = port;
                    r = true;
                }
                catch (Exception e) { Debug.LogException(e); }
                IsConnected = r;
                return r;
            }
        }

        public void Disconnect()
        {
            intancmd.Disconnect(true);
        }

        public bool StartRecordAndAcquisite()
        {
            send("set runmode record");
            return true;
        }

        public bool StopAcquisiteAndRecord()
        {
            send("set runmode stop");
            return true;
        }

        public string RecordPath
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                lock (api)
                {
                    try
                    {
                        send($"set Filename.BaseFilename {Path.GetFileNameWithoutExtension(value)}");
                        send($"set Filename.Path {Path.GetDirectoryName(value)}");
                    }
                    catch (Exception e) { Debug.LogException(e); }
                }
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
                lock (api)
                {
                    try
                    {
                        if (value == RecordStatus.Recording)
                        {
                            send("set runmode record");
                        }
                        else if (value == RecordStatus.Stopped)
                        {
                            send("set runmode stop");
                        }
                    }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
        }

        public string DataFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AcquisitionStatus AcquisitionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

   
}