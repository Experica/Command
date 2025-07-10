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
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CircularBuffer;

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
        Vector2 Gaze2D { get; }
        Vector3 Gaze3D { get; }
        float SamplingRate { get; set; }
        float ConfidenceThreshold { get; set; }
    }

    public class PupilLabsCore : IEyeTracker
    {
        int disposecount = 0;
        readonly object api = new();
        readonly object gazelock = new();
        RequestSocket req_socket;
        string sub_port;
        SubscriberSocket sub_socket;
        CancellationTokenSource cts = new();
        CircularBuffer<Vector2> gazes = new(30);

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
            Disconnect();
        }

        public static PupilLabsCore TryGetPupilLabsCore(string host = "localhost", int port = 50020, float fs = 200, float confthreshold = 0.9f)
        {
            var t = new PupilLabsCore(fs, confthreshold);
            if (t.Connect(host, port)) { return t; }
            else
            {
                Debug.LogWarning("Can't Connect to PupilLabs Core, return Null.");
                return null;
            }
        }

        public PupilLabsCore(float fs = 200, float confthreshold = 0.9f)
        {
            SamplingRate = fs;
            ConfidenceThreshold = confthreshold;
        }

        ~PupilLabsCore()
        {
            Dispose(false);
        }

        public bool StartRecordAndAcquisite()
        {
            req_socket.SendFrame(Encoding.UTF8.GetBytes("R"));
            req_socket.ReceiveFrameString();
            return true;
        }

        public bool StopAcquisiteAndRecord()
        {
            req_socket.SendFrame(Encoding.UTF8.GetBytes("r"));
            req_socket.ReceiveFrameString();
            return true;
        }

        public bool Connect(string host = "localhost", int port = 50020)
        {
            req_socket = new RequestSocket($"tcp://{host}:{port}");
            req_socket.TrySendFrame("SUB_PORT");
            if (req_socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out sub_port))
            {
                sub_socket = new SubscriberSocket($"tcp://{host}:{sub_port}");

                sub_socket.Subscribe("surfaces.");
                Task.Run(() => receivegaze(cts.Token));
                return true;
            }
            req_socket.Dispose();
            req_socket = null;
            return false;
        }

        public void Disconnect()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            sub_socket?.Dispose();
            sub_socket = null;
            req_socket?.Dispose();
            req_socket = null;
            NetMQConfig.Cleanup(false);
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public EyeTracker Type => EyeTracker.PupilLabs_Core;

        public float PupilSize => throw new NotImplementedException();

        public Vector2 Gaze2D
        {
            get
            {
                lock (gazelock) { return gazes.IsEmpty ? Vector2.zero : gazes.Back(); }
            }
        }

        void receivegaze(CancellationToken token)
        {
            var msg = new NetMQMessage();
            string topic;
            byte[] payload;
            Dictionary<string, object> payloadDict;
            Dictionary<object, object> gazeDict;
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(Mathf.FloorToInt(1000f / SamplingRate));
                if (sub_socket.TryReceiveMultipartMessage(ref msg))
                {
                    if (msg.FrameCount == 2)
                    {
                        topic = msg[0].ConvertToString();
                        payload = msg[1].ToByteArray();
                        payloadDict = MsgPack.DeserializeMsgPack<Dictionary<string, object>>(payload);

                        if (payloadDict.ContainsKey("gaze_on_surfaces"))
                        {
                            foreach (var gazeObj in payloadDict["gaze_on_surfaces"].AsList())
                            {
                                gazeDict = gazeObj as Dictionary<object, object>;
                                if (gazeDict.ContainsKey("norm_pos"))
                                {
                                    var confidence = (double)gazeDict["confidence"];
                                    if (confidence < ConfidenceThreshold) { continue; }
                                    var normPosList = gazeDict["norm_pos"].AsList();
                                    var gaze = new Vector2(Convert.ToSingle(normPosList[0]), Convert.ToSingle(normPosList[1]));
                                    lock (gazelock)
                                    {
                                        gazes.PushBack(gaze);
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        public string DataFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RecordPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public RecordStatus RecordStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AcquisitionStatus AcquisitionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Vector3 Gaze3D => throw new NotImplementedException();

        public float SamplingRate { get; set; }
        public float ConfidenceThreshold { get; set; }
    }
}