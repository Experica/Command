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
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;
using System;
using FTD2XX_NET;

namespace Experica
{
    public interface IGPIO : IDisposable
    {
        byte In();
        void Out(byte value);
        void BitOut(int bit, bool value);
        void BitPulse(int bit, double duration_ms);
        bool Found { get; }
    }

    public enum IODirection
    {
        Input,
        Output
    }

    public enum DigitalWaveType
    {
        PWM,
        PoissonSpike
    }

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
            sp.DiscardInBuffer();
            sp.receiveddata = "";
            sp.WriteLine(cmd);
            var hr = timer.TimeoutMillisecond(x =>
            {
                var r = x.Read();
                var i = r.IndexOf(cmd);
                if (i > -1)
                {
                    var ii = r.LastIndexOf("\r");
                    if (ii > i + cmd.Length)
                    {
                        return r.Substring(i + cmd.Length + 2, ii - (i + cmd.Length + 2));
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

    public class FTDIGPIO : IGPIO
    {
        #region IDisposable
        int disposecount = 0;

        ~FTDIGPIO()
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
                FTD2XX?.Close();
            }
        }
        #endregion
        FTDI FTD2XX;
        FTDI.FT_STATUS FTSTATUS;
        uint ndevice;
        FTDI.FT_DEVICE_INFO_NODE[] devices;

        uint NumBytesToWrite = 0;
        uint NumBytesToRead = 0;
        uint NumBytesWrite = 0;
        uint NumBytesRead = 0;
        byte[] outputbuffer;
        byte[] inputbuffer;

        public bool Found { get; }

        public FTDIGPIO()
        {
            FTD2XX = new FTDI();
            outputbuffer = new byte[8];
            inputbuffer = new byte[8];

            if (FTD2XX.GetNumberOfDevices(ref ndevice) == FTDI.FT_STATUS.FT_OK)
            {
                if (ndevice > 0)
                {
                    devices = new FTDI.FT_DEVICE_INFO_NODE[ndevice];
                    FTSTATUS = FTD2XX.GetDeviceList(devices);
                    if (FTD2XX.OpenByDescription(devices[0].Description) == FTDI.FT_STATUS.FT_OK)
                    {
                        Config();
                        Found = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Can Not Open Device: {devices[0].Description}.");
                    }
                }
                else
                {
                    Debug.LogWarning("No FTDI Device Detected.");
                }
            }
            else
            {
                Debug.LogWarning("Can Not Detect FTDI Devices.");
            }
        }

        void Config(byte direction = 0xFF)
        {
            FTSTATUS |= FTD2XX.ResetDevice();
            FTSTATUS |= FTD2XX.SetTimeouts(5000, 5000);
            FTSTATUS |= FTD2XX.SetLatency(0);
            FTSTATUS |= FTD2XX.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00, 0x00);
            FTSTATUS |= FTD2XX.SetBitMode(0x00, 0x00); // Reset
            FTSTATUS |= FTD2XX.SetBitMode(direction, 0x01); // Asyc Bit-Bang Mode    
            FTSTATUS |= FTD2XX.SetBaudRate(3000000);

            // Enable internal loop-back
            //Outputbuffer[NumBytesToSend++] = 0x84;
            //ftStatus = FTDIGPIO.Write(Outputbuffer, NumBytesToSend, ref NumBytesSent);
            //NumBytesToSend = 0; // Reset output buffer pointer

            //ftStatus = FTDIGPIO.GetRxBytesAvailable(ref NumBytesToRead);
            //if (NumBytesToRead!=0)
            //{
            //    Debug.LogError("Error - MPSSE receive buffer should be empty");
            //    FTDIGPIO.SetBitMode(0x00, 0x00);
            //    FTDIGPIO.Close();
            //}

            //    // Use 60MHz master clock (disable divide by 5)
            //    NumBytesToWrite = 0;
            //    outputbuffer[NumBytesToWrite++] = 0x8A;
            //    // Turn off adaptive clocking (may be needed for ARM)
            //    outputbuffer[NumBytesToWrite++] = 0x97;
            //    // Disable three-phase clocking
            //    outputbuffer[NumBytesToWrite++] = 0x8D;

            //    FTSTATUS = FTD2XX.Write(outputbuffer, NumBytesToWrite, ref NumBytesWrite);

            //    // Configure data bits low-byte of MPSSE port
            //    NumBytesToWrite = 0;
            //    outputbuffer[NumBytesToWrite++] = 0x82;
            //    // Initial state all low
            //    outputbuffer[NumBytesToWrite++] = 0x00;
            //    // Direction all output 
            //    outputbuffer[NumBytesToWrite++] = 0xFF;
            //    FTSTATUS = FTD2XX.Write(outputbuffer, NumBytesToWrite, ref NumBytesWrite);
        }

        public void Out(byte v)
        {
            outputbuffer[0] = v;
            FTD2XX.Write(outputbuffer, 1, ref NumBytesWrite);
        }

        public byte In()
        {
            FTD2XX.Read(inputbuffer, 1, ref NumBytesRead);
            return inputbuffer[0];
        }

        public void BitOut(int bit, bool value)
        {
            Out(value ? byte.MaxValue : byte.MinValue);
        }

        public void BitPulse(int bit, double duration_ms)
        {
            throw new NotImplementedException();
        }
    }

    public class GPIOWave
    {
        IGPIO gpio;
        public IGPIO GPIO { get { lock (apilock) { return gpio; } } set { lock (apilock) { gpio = value; } } }
        readonly float lowcutofffreq, highcutofffreq;
        System.Random rng = new MersenneTwister(true);
        readonly object apilock = new object();

        ConcurrentDictionary<int, Thread> bitthread = new ConcurrentDictionary<int, Thread>();
        ConcurrentDictionary<int, ManualResetEvent> bitthreadevent = new ConcurrentDictionary<int, ManualResetEvent>();
        ConcurrentDictionary<int, bool> bitthreadbreak = new ConcurrentDictionary<int, bool>();

        ConcurrentDictionary<int, DigitalWaveType> bitwave = new ConcurrentDictionary<int, DigitalWaveType>();
        ConcurrentDictionary<int, double> bitlatency_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitphase = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bithighdur_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitlowdur_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitspikerate = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitspikewidth_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitrefreshperiod_ms = new ConcurrentDictionary<int, double>();

        public GPIOWave(IGPIO gpio, float lowcutofffreq = 0.00001f, float highcutofffreq = 10000f)
        {
            this.gpio = gpio;
            this.lowcutofffreq = lowcutofffreq;
            this.highcutofffreq = highcutofffreq;
        }

        public void SetBitWave(int bit, double highdur_ms, double lowdur_ms, double latency_ms = 0, double phase = 0)
        {
            lock (apilock)
            {
                bitlatency_ms[bit] = latency_ms;
                bitphase[bit] = phase;
                bithighdur_ms[bit] = highdur_ms;
                bitlowdur_ms[bit] = lowdur_ms;
                bitwave[bit] = DigitalWaveType.PWM;
            }
        }

        public void SetBitWave(int bit, float freq, double latency_ms = 0, double phase = 0)
        {
            lock (apilock)
            {
                bitlatency_ms[bit] = latency_ms;
                bitphase[bit] = phase;
                var halfcycle = (1.0 / Mathf.Clamp(freq, lowcutofffreq, highcutofffreq)) * 1000.0 / 2.0;
                bithighdur_ms[bit] = halfcycle;
                bitlowdur_ms[bit] = halfcycle;
                bitwave[bit] = DigitalWaveType.PWM;
            }
        }

        public void SetBitWave(int bit, double rate_sps, double spikewidth_ms = 2, double refreshperiod_ms = 2, double latency_ms = 0, double phase = 0)
        {
            lock (apilock)
            {
                bitlatency_ms[bit] = latency_ms;
                bitphase[bit] = phase;
                bitspikerate[bit] = rate_sps / 1000;
                bitspikewidth_ms[bit] = spikewidth_ms;
                bitrefreshperiod_ms[bit] = refreshperiod_ms;
                bitwave[bit] = DigitalWaveType.PoissonSpike;
            }
        }

        public void Start(params int[] bs)
        {
            lock (apilock)
            {
                var vbs = bitwave.Keys.Intersect(bs).ToArray();
                var vbn = vbs.Length;
                if (vbn > 0)
                {
                    foreach (var b in vbs)
                    {
                        if (!bitthread.ContainsKey(b))
                        {
                            bitthread[b] = new Thread(_BitWave);
                            bitthreadevent[b] = new ManualResetEvent(false);
                        }
                        if (!bitthread[b].IsAlive)
                        {
                            bitthread[b].Start(b);
                        }
                        bitthreadbreak[b] = false;
                    }
                    foreach (var b in vbs)
                    {
                        bitthreadevent[b].Set();
                    }
                }
            }
        }

        public void StartAll()
        {
            Start(bitwave.Keys.ToArray());
        }

        public void Stop(params int[] bs)
        {
            lock (apilock)
            {
                var vbs = bitthread.Keys.Intersect(bs).ToArray();
                var vbn = vbs.Length;
                if (vbn > 0)
                {
                    foreach (var b in vbs)
                    {
                        bitthreadevent[b].Reset();
                        bitthreadbreak[b] = true;
                    }
                }
            }
        }

        public void StopAll()
        {
            Stop(bitthread.Keys.ToArray());
        }

        void _BitWave(object p)
        {
            ThreadBitWave((int)p);
        }

        void ThreadBitWave(int bit)
        {
            var timer = new Timer(); bool isbreakstarted;
            double start = 0; double breakstart = 0;
            Break:
            bitthreadevent[bit].WaitOne();
            isbreakstarted = false;
            timer.Restart();
            switch (bitwave[bit])
            {
                case DigitalWaveType.PWM:
                    timer.TimeoutMillisecond(bitlatency_ms[bit] + bitphase[bit] * (bithighdur_ms[bit] + bitlowdur_ms[bit]));
                    while (true)
                    {
                        gpio.BitOut(bit, true);
                        start = timer.ElapsedMillisecond;
                        while ((timer.ElapsedMillisecond - start) < bithighdur_ms[bit])
                        {
                            if (bitthreadbreak[bit])
                            {
                                if (!isbreakstarted)
                                {
                                    breakstart = timer.ElapsedMillisecond;
                                    isbreakstarted = true;
                                }
                                if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= bitlatency_ms[bit])
                                {
                                    gpio.BitOut(bit, false);
                                    goto Break;
                                }
                            }
                        }

                        gpio.BitOut(bit, false);
                        start = timer.ElapsedMillisecond;
                        while ((timer.ElapsedMillisecond - start) < bitlowdur_ms[bit])
                        {
                            if (bitthreadbreak[bit])
                            {
                                if (!isbreakstarted)
                                {
                                    breakstart = timer.ElapsedMillisecond;
                                    isbreakstarted = true;
                                }
                                if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= bitlatency_ms[bit])
                                {
                                    goto Break;
                                }
                            }
                        }
                    }
                case DigitalWaveType.PoissonSpike:
                    timer.TimeoutMillisecond(bitlatency_ms[bit] + bitphase[bit] / bitspikerate[bit]);
                    var isid = new Exponential(bitspikerate[bit], rng);
                    while (true)
                    {
                        var i = isid.Sample();
                        if (i > bitrefreshperiod_ms[bit])
                        {
                            start = timer.ElapsedMillisecond;
                            while ((timer.ElapsedMillisecond - start) < i)
                            {
                                if (bitthreadbreak[bit])
                                {
                                    if (!isbreakstarted)
                                    {
                                        breakstart = timer.ElapsedMillisecond;
                                        isbreakstarted = true;
                                    }
                                    if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= bitlatency_ms[bit])
                                    {
                                        goto Break;
                                    }
                                }
                            }

                            gpio.BitOut(bit, true);
                            start = timer.ElapsedMillisecond;
                            while ((timer.ElapsedMillisecond - start) < bitspikewidth_ms[bit])
                            {
                                if (bitthreadbreak[bit])
                                {
                                    if (!isbreakstarted)
                                    {
                                        breakstart = timer.ElapsedMillisecond;
                                        isbreakstarted = true;
                                    }
                                    if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= bitlatency_ms[bit])
                                    {
                                        gpio.BitOut(bit, false);
                                        goto Break;
                                    }
                                }
                            }
                            gpio.BitOut(bit, false);
                        }
                    }
            }
        }
    }
}