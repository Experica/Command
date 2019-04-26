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
        IDictionary Measure(string datareportformat, double timeout_ms);
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

        string cmdresp(string cmd, double timeout_ms, bool iscmd = true)
        {
            if (sp == null) return null;
            sp.receiveddata = "";
            if (iscmd)
            {
                sp.DiscardInBuffer();
                sp.WriteLine(cmd);
            }
            var hr = timer.Timeout(x =>
            {
                var r = x.Read();
                var i = r.LastIndexOf("\r\n");
                if (i > -1)
                {
                    return string.Join(",", r.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                }
                return null;
            }, sp, timeout_ms);

            if (hr.Result != null)
            {
                return (string)hr.Result;
            }
            else
            {
                if (timeout_ms > 0)
                {
                    Debug.Log("\"" + cmd + "\"" + " timeout: " + hr.ElapsedMillisecond + " ms");
                }
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

        public IDictionary Measure(string datareportformat, double timeout_ms)
        {
            switch (datareportformat)
            {
                // ErrorCode, UnitCode, Intensity Y, CIE x, y
                case "1":
                    var hr = cmdresp("M" + datareportformat, timeout_ms); // need at least 6s at BaudRate:9600
                    if (!string.IsNullOrEmpty(hr))
                    {
                        var names = new[] { "Error", "Unit", "Y", "x", "y" };
                        var t = hr.Split(',');
                        if (t.Length == names.Length)
                        {
                            var m = Enumerable.Range(0, t.Length).ToDictionary(i => names[i], i => t[i].Convert<double>());
                            if (m["Error"] == 0)
                            {
                                m.Remove("Error");
                                return m;
                            }
                        }
                    }
                    break;
                // ErrorCode, UnitCode, Peak λ, Integrated Spectral, Integrated Photon, λs, λ Intensities
                case "5":
                    cmdresp("M" + datareportformat, 0); // cmd and return, without reading response
                    var timer = new Timer();
                    timer.Timeout(timeout_ms); // need at least 8s at BaudRate:9600
                    hr = cmdresp("", timeout_ms, false); // now the full response should be returned
                    if (!string.IsNullOrEmpty(hr))
                    {
                        var d = hr.Split(',').Select(i => i.Convert<double>()).ToArray();
                        if (d.Length > 0 && d[0] == 0)
                        {
                            var m = new Dictionary<string, object>
                            {
                                ["Unit"] = d[1],
                                ["PeakWL"] = d[2],
                                ["IntegratedSpectral"] = d[3],
                                ["IntegratedPhoton"] = d[4]
                            };
                            var wl = new List<double>();
                            var wli = new List<double>();
                            for (var i = 5; i < d.Length; i = i + 2)
                            {
                                wl.Add(d[i]);
                                wli.Add(d[i + 1]);
                            }
                            m["WL"] = wl.ToArray();
                            m["Spectral"] = wli.ToArray();
                            return m;
                        }
                    }
                    break;
            }
            return null;
        }
    }
}