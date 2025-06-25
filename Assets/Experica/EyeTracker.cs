/*
EyeTracker.cs is part of the Experica.
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
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using MessagePack;
using NetMQ.Sockets;
using System.Collections.Generic;
using NetMQ;
using System.Text;


namespace Experica
{
    public enum EyeTracker
    {
        EyeLink,
        Tobii,
        PupilLabs_Core
    }

    public interface IEyeTracker : IRecorder
    {
        EyeTracker Type { get; }
        float PupilSize { get; }
        Vector2 Gaze { get; }
    }

    public class PupilLabsCore : IEyeTracker
    {
        int disposecount = 0;
        readonly object api = new();
        RequestSocket pupil_remote;
        string sub_port;
        SubscriberSocket subscriber;
        List<Vector2> gazes = new();

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

        public static PupilLabsCore TryGetPupilLabsCore(string host = "localhost", int port = 50020)
        {
            var t = new PupilLabsCore();
            if (t.Connect(host, port)) { return t; } else
            {
                Debug.LogWarning("Can't Connect to PupilLabs Core, return Null.");
                return null; 
            }
        }

        public PupilLabsCore()
        {
        }

        ~PupilLabsCore()
        {
            Dispose(false);
        }

        public bool StartRecordAndAcquisite()
        {
            pupil_remote.SendFrame(Encoding.UTF8.GetBytes("R"));
            pupil_remote.ReceiveFrameString();
            return true;
        }

        public bool StopAcquisiteAndRecord()
        {
            pupil_remote.SendFrame(Encoding.UTF8.GetBytes("r"));
            pupil_remote.ReceiveFrameString();
            return true;
        }

        public bool Connect(string host = "localhost", int port = 50020)
        {
            pupil_remote = new RequestSocket($"tcp://{host}:{port}");
            pupil_remote.SendFrame( Encoding.UTF8.GetBytes("SUB_PORT"));
            if (pupil_remote.TryReceiveFrameString(out sub_port))
            {
                 //pupil_remote.TryReceiveFrameString(out sub_port);
                subscriber = new SubscriberSocket($"tcp://{host}:{sub_port}");

                subscriber.Subscribe("gaze.");
                return true;
            }
            pupil_remote.Close();
            return false;
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public EyeTracker Type => EyeTracker.PupilLabs_Core;

        public float PupilSize => throw new NotImplementedException();

        public Vector2 Gaze
        {
            get
            {
                var t = subscriber.ReceiveMultipartBytes(2);
                var m = MsgPack.DeserializeMsgPack<Dictionary<string,object>>(t[1]);
                return m["norm_pos"].Convert<Vector2>();
            }
        }

        void receivegaze()
        {
            var t = subscriber.ReceiveMultipartBytes(2);
            var m = MsgPack.DeserializeMsgPack<Dictionary<string, object>>(t[1]);
            gazes.Add( m["norm_pos"].Convert<Vector2>());
        }

        public string DataFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RecordPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public RecordStatus RecordStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AcquisitionStatus AcquisitionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}