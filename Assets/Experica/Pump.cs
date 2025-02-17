/*
Pump.cs is part of the Experica.
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

namespace Experica
{
    public enum Pump
    {
        Longer
    }

    public interface IPump : IDisposable
    {
        Pump Type { get; }
        void PowerOn();
        void PowerOff();
        void Start();
        void Stop();
        float Speed { get; set; }
        bool Direction_CCW { get; set; }
    }

    public class Longer : IPump
    {
        #region IDisposable
        int disposecount = 0;

        ~Longer()
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
            if (disposing)
            {
            }
            sp.Dispose();
        }
        #endregion

        oldSerialPort sp;
        Timer timer = new Timer();
        double timeout;

        public Longer(string portname, int baudrate = 1200, double timeout_ms = 1.0)
        {
            sp = new oldSerialPort(portname: portname, baudrate: baudrate, newline: "\r");
            timeout = timeout_ms;
        }

        string cmdresp(string cmd, double timeout, bool isecho = true)
        {
            sp.DiscardInBuffer();
            sp.receiveddata = "";
            sp.WriteLine(cmd);
            var hr = timer.TimeoutMillisecond(x =>
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
                            return r.Substring(i + 2, ii - (i + 2));
                        }
                    }
                    else
                    {
                        return r.Substring(0, i);
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
                return null;
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

        public void PowerOn()
        {
        }

        public void PowerOff()
        {
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Pump Type => Pump.Longer;

        public float Speed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Direction_CCW { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}