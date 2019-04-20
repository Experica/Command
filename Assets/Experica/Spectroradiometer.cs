/*
Spectroradiometer.cs is part of the Experica.
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
using System;
using System.Linq;

namespace Experica
{
    public enum SpectroRadioMeter
    {
        PhotoResearch
    }

    public interface ISpectroRadioMeter : IDisposable
    {
        SpectroRadioMeter Type { get; }
        bool Connect(double timeout_ms);
        void Close();
        bool Setup(string setupfields, double timeout_ms);
        Dictionary<string, float> Measure(string datareportformat, double timeout_ms);
    }

    public class PR : ISpectroRadioMeter
    {
        bool disposed = false;
        SerialPort sp;
        readonly string model;
        Timer timer = new Timer();

        public PR(string portname, string prmodel)
        {
            switch (prmodel)
            {
                case "PR701":
                    sp = new SerialPort(portname: portname, baudrate: 9600, handshake: System.IO.Ports.Handshake.RequestToSend, newline: "\r");
                    break;
                default:
                    Debug.Log("Photo Research Model " + prmodel + " is not yet supported.");
                    break;
            }
            model = prmodel;
        }

        ~PR()
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
            if (!disposed)
            {
                if (disposing)
                {
                }
                Close();
                sp.Dispose();
                disposed = true;
            }
        }

        public SpectroRadioMeter Type { get { return SpectroRadioMeter.PhotoResearch; } }

        string cmdresp(string cmd, double timeout_ms)
        {
            if (sp == null) return null;
            sp.DiscardInBuffer();
            sp.receiveddata = "";
            sp.WriteLine(cmd);
            var hr = timer.Timeout(x =>
            {
                var r = x.Read();
                var i = r.LastIndexOf("\n");
                if (i > -1)
                {
                    var ii = r.LastIndexOf("\r");
                    if (ii > -1 && ii < i)
                    {
                        return r.Substring(0, r.Length - 2);
                    }
                }
                return null;
            }, sp, timeout_ms);

            if (hr.Result != null)
            {
                return (string)hr.Result;
            }
            else
            {
                Debug.Log("\"" + cmd + "\"" + " timeout: " + hr.ElapsedMillisecond + " ms");
                return null;
            }
        }

        public bool Connect(double timeout_ms)
        {
            return cmdresp(model, timeout_ms) == " REMOTE MODE";
        }

        public void Close()
        {
            sp.WriteLine("Q");
            sp.Close();
        }

        public bool Setup(string setupfields, double timeout_ms)
        {
            return cmdresp(setupfields, timeout_ms) == "0000";
        }

        public Dictionary<string, float> Measure(string datareportformat, double timeout_ms)
        {
            var hr = cmdresp("M" + datareportformat, timeout_ms);
            if (!string.IsNullOrEmpty(hr))
            {
                switch (datareportformat)
                {
                    // ErrorCode, UnitCode, Intensity Y, CIE x, y
                    case "1":
                        var names = new[] { "Error", "Unit", "Y", "x", "y" };
                        var t = hr.Split(',');
                        if (t.Length == names.Length)
                        {
                            var m = Enumerable.Range(0, t.Length).ToDictionary(i => names[i], i => t[i].Convert<float>());
                            if (m["Error"] == 0)
                            {
                                m.Remove("Error");
                                return m;
                            }
                        }
                        break;
                }
            }
            return null;
        }
    }
}