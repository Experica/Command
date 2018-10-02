/*
GPIO.cs is part of the Experica.
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
using System.Collections.Generic;
using System;

namespace Experica
{
    public class SerialGPIO : IDisposable
    {
        bool disposed = false;
        SerialPort sp;
        int n;
        Timer timer = new Timer();
        double timeout;

        public SerialGPIO(string portname, int nio = 32, double timeout_ms = 1.0)
        {
            sp = new SerialPort(portname: portname, newline: "\r");
            n = nio;
            timeout = timeout_ms;
        }

        ~SerialGPIO()
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
            sp.WriteLine(cmd);
            var hr = timer.Timeout(x =>
            {
                var r = x.Read();
                var i = r.IndexOf(cmd);
                if (i > -1)
                {
                    var ii = r.LastIndexOf("\r");
                    if (ii > i + cmd.Length)
                    {
                        var resp = r.Substring(i + cmd.Length + 2, ii - (i + cmd.Length + 2));
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

        public int? Ver()
        {
            var r = cmdresp("ver", timeout);
            return r == null ? new int?() : int.Parse(r);
        }

        public int? ADC(int channel)
        {
            var r = cmdresp("adc read " + channel.ToString(), timeout);
            return r == null ? new int?() : int.Parse(r);
        }

        public void Write(int channel, bool value)
        {
            if (value)
            {
                sp.WriteLine("gpio set " + channel.ToString());
            }
            else
            {
                sp.WriteLine("gpio clear " + channel.ToString());
            }
        }

        public bool? Read(int channel)
        {
            var r = cmdresp("gpio read " + channel.ToString(), timeout);
            return r == null ? new bool?() : Convert.ToBoolean(int.Parse(r));
        }

        public void IODir(Int64 channelbits)
        {
            sp.WriteLine("gpio iodir " + Convert.ToString(channelbits, 16).PadLeft(n / 4, '0'));
        }

        public void IOMask(Int64 channelbits)
        {
            sp.WriteLine("gpio iomask " + Convert.ToString(channelbits, 16).PadLeft(n / 4, '0'));
        }

        public Int64? ReadAll()
        {
            var r = cmdresp("gpio readall", timeout);
            return r == null ? new Int64?() : Int64.Parse(r, System.Globalization.NumberStyles.HexNumber);
        }

        public int? Read0_7()
        {
            var r = cmdresp("gpio readall", timeout);
            return r == null ? new int?() : int.Parse(r.Substring(r.Length - 2), System.Globalization.NumberStyles.HexNumber);
        }

        public void WriteAll(Int64 channelbits)
        {
            sp.WriteLine("gpio writeall " + Convert.ToString(channelbits, 16).PadLeft(n / 4, '0'));
        }

        public bool Notify { get; set; }

    }
}