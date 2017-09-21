using System.Collections;
using System.Collections.Generic;
using System;

namespace VLab
{
    public class SerialGPIO : IDisposable
    {
        bool disposed = false;
        SerialPort sp;
        int n;
        VLTimer timer = new VLTimer();
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
                Console.WriteLine(hr.ElapsedMillisecond);
                return (string)hr.Result;
            }
            else
            {
                //Debug.Log("Get Omicron Laser PowerRatio timeout: " + hr.ElapsedMillisecond+" ms");
                Console.WriteLine("\"" + cmd + "\"" + " timeout: " + hr.ElapsedMillisecond + " ms");
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