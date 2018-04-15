/*
VLLaser.cs is part of the VLAB project.
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
using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Linq;

namespace VLab
{
    public class Omicron : IDisposable
    {
        bool disposed = false;
        SerialPort sp;
        VLTimer timer = new VLTimer();
        /// <summary>
        /// Watt
        /// </summary>
        public float MaxPower;
        double timeout;

        public Omicron(string portname, int baudrate = 500000, float maxpower = 0.1f, double timeout_ms = 150.0)
        {
            sp = new SerialPort(portname: portname, baudrate: baudrate, newline: "\r");
            MaxPower = maxpower;
            timeout = timeout_ms;
        }

        ~Omicron()
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
                sp.Dispose();
                disposed = true;
            }
        }

        string cmdresp(string cmd, double timeout)
        {
            sp.WriteLine("?" + cmd);
            var hr = timer.Timeout(x =>
            {
                var r = x.Read();
                var i = r.LastIndexOf("!" + cmd);
                if (i > -1)
                {
                    var ii = r.LastIndexOf("\r");
                    if (ii > i)
                    {
                        var resp = r.Substring(i + cmd.Length + 1, ii - (i + cmd.Length + 1));
                        x.receiveddata = "";
                        x.DiscardInBuffer();
                        return resp;
                    }
                }
                return null;
            }, sp, timeout);

            if (hr.Result != null)
            {
                return (string)hr.Result;
            }
            else
            {
                Debug.Log("\"" + cmd + "\"" + " timeout: " + hr.ElapsedMillisecond + " ms");
                sp.receiveddata = "";
                sp.DiscardInBuffer();
                return null;
            }
        }

        /// <summary>
        /// Turn on laser in 30-150ms
        /// </summary>
        public void LaserOn()
        {
            sp.WriteLine("?LOn");
            //var r = cmdresp("LOn", timeout);
        }

        /// <summary>
        /// Turn off laser in 30-50ms
        /// </summary>
        public void LaserOff()
        {
            sp.WriteLine("?LOf");
            //var r = cmdresp("LOf", timeout);
        }

        public void PowerOn()
        {
            sp.WriteLine("?POn");
        }

        public void PowerOff()
        {
            sp.WriteLine("?POf");
        }

        public float Power
        {
            set
            {
                PowerRatio = value / MaxPower;
            }
        }

        /// <summary>
        /// Change power percentage in 60-125ms
        /// </summary>
        public float? PowerRatio
        {
            get
            {
                var r = cmdresp("GLP", timeout);
                return r == null ? new float?() : int.Parse(r, System.Globalization.NumberStyles.HexNumber) / (float)int.Parse("F".PadLeft(r.Length, 'F'), System.Globalization.NumberStyles.HexNumber);
            }
            set
            {
                if (value.HasValue && value >= 0 && value <= 1)
                {
                    sp.WriteLine("?SLP" + Convert.ToString((int)Math.Round(value.Value * 0xFFF), 16).PadLeft(3, '0'));
                }
            }
        }
    }

    public class Cobolt : IDisposable
    {
        bool disposed = false;
        SerialPort sp;
        VLTimer timer = new VLTimer();
        /// <summary>
        /// Watt
        /// </summary>
        public float MaxPower;
        double timeout;

        public Cobolt(string portname, int baudrate = 115200, float maxpower = 0.1f, double timeout_ms = 1.0)
        {
            sp = new SerialPort(portname: portname, baudrate: baudrate, newline: "\r");
            MaxPower = maxpower;
            timeout = timeout_ms;
        }

        ~Cobolt()
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
                sp.Dispose();
                disposed = true;
            }
        }

        string cmdresp(string cmd, double timeout, bool isecho = true)
        {
            sp.WriteLine(cmd);
            var hr = timer.Timeout(x =>
            {
                var r = x.Read();
                var i = r.IndexOf('\r');
                if (i > -1)
                {
                    if (isecho)
                    {
                        var ii = r.LastIndexOf("\r");
                        if (ii > i)
                        {
                            var resp = r.Substring(i + 2, ii - (i + 2));
                            x.receiveddata = "";
                            x.DiscardInBuffer();
                            return resp;
                        }
                    }
                    else
                    {
                        var resp = r.Substring(0, i);
                        x.receiveddata = "";
                        x.DiscardInBuffer();
                        return resp;
                    }
                }
                return null;
            }, sp, timeout);

            if (hr.Result != null)
            {
                return (string)hr.Result;
            }
            else
            {
                Debug.Log("\"" + cmd + "\"" + " timeout: " + hr.ElapsedMillisecond + " ms");
                sp.receiveddata = "";
                sp.DiscardInBuffer();
                return null;
            }
        }

        public float Power
        {
            set
            {
                sp.WriteLine("p " + value.ToString());
            }
        }

        public void ClearFault()
        {
            sp.WriteLine("cf");
        }

        public bool? AutoStart
        {
            get
            {
                var r = cmdresp("@cobas?", timeout, false);
                return r == null ? new bool?() : Convert.ToBoolean(int.Parse(r));
            }
            set
            {
                if (value.HasValue)
                {
                    sp.WriteLine("@cobas " + Convert.ToInt32(value.Value));
                }
            }
        }

        public void LaserOn()
        {
            var r = cmdresp("l1", timeout, false);
        }

        public void LaserOff()
        {
            var r = cmdresp("l0", timeout, false);
        }

        public float? PowerRatio
        {
            get
            {
                var r = cmdresp("p?", timeout);
                if (r == null)
                {
                    return new float?();
                }
                else
                {
                    float pr;
                    if (float.TryParse(r, out pr))
                    {
                        return pr / MaxPower;
                    }
                    else
                    {
                        return new float?();
                    }
                }
            }
            set
            {
                if (value.HasValue && value >= 0 && value <= 1)
                {
                    sp.WriteLine("p " + (value.Value * MaxPower).ToString());
                }
            }
        }
    }
}