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
//using FTD2XX_NET;

namespace Experica
{
    /// <summary>
    /// Implementation should be Thread Safe
    /// </summary>
    public interface IGPIO : IDisposable
    {
        byte In();
        void Out(byte value);
        void BitOut(int bit, bool value);
        void BitPulse(int bit, double duration_ms, double delay_ms = 0, bool ispositivepulse = true);
        bool Found { get; }
        double MaxFreq { get; }
    }

    public enum IODirection
    {
        Input,
        Output
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

    //public class FTDIGPIO : IGPIO
    //{
    //    #region IDisposable
    //    int disposecount = 0;

    //    ~FTDIGPIO()
    //    {
    //        Dispose(false);
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (1 == Interlocked.Exchange(ref disposecount, 1))
    //        {
    //            return;
    //        }
    //        if (disposing)
    //        {
    //            FTD2XX?.Close();
    //        }
    //    }
    //    #endregion
    //    FTDI FTD2XX;
    //    FTDI.FT_STATUS FTSTATUS;
    //    uint ndevice;
    //    FTDI.FT_DEVICE_INFO_NODE[] devices;

    //    uint NumBytesToWrite = 0;
    //    uint NumBytesToRead = 0;
    //    uint NumBytesWrite = 0;
    //    uint NumBytesRead = 0;
    //    byte[] outputbuffer;
    //    byte[] inputbuffer;

    //    public bool Found { get; }

    //    public FTDIGPIO()
    //    {
    //        FTD2XX = new FTDI();
    //        outputbuffer = new byte[8];
    //        inputbuffer = new byte[8];

    //        if (FTD2XX.GetNumberOfDevices(ref ndevice) == FTDI.FT_STATUS.FT_OK)
    //        {
    //            if (ndevice > 0)
    //            {
    //                devices = new FTDI.FT_DEVICE_INFO_NODE[ndevice];
    //                FTSTATUS = FTD2XX.GetDeviceList(devices);
    //                if (FTD2XX.OpenByDescription(devices[0].Description) == FTDI.FT_STATUS.FT_OK)
    //                {
    //                    Config();
    //                    Found = true;
    //                }
    //                else
    //                {
    //                    Debug.LogWarning($"Can Not Open Device: {devices[0].Description}.");
    //                }
    //            }
    //            else
    //            {
    //                Debug.LogWarning("No FTDI Device Detected.");
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Can Not Detect FTDI Devices.");
    //        }
    //    }

    //    void Config(byte direction = 0xFF)
    //    {
    //        FTSTATUS |= FTD2XX.ResetDevice();
    //        FTSTATUS |= FTD2XX.SetTimeouts(5000, 5000);
    //        FTSTATUS |= FTD2XX.SetLatency(0);
    //        FTSTATUS |= FTD2XX.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00, 0x00);
    //        FTSTATUS |= FTD2XX.SetBitMode(0x00, 0x00); // Reset
    //        FTSTATUS |= FTD2XX.SetBitMode(direction, 0x01); // Asyc Bit-Bang Mode    
    //        FTSTATUS |= FTD2XX.SetBaudRate(3000000);

    //        // Enable internal loop-back
    //        //Outputbuffer[NumBytesToSend++] = 0x84;
    //        //ftStatus = FTDIGPIO.Write(Outputbuffer, NumBytesToSend, ref NumBytesSent);
    //        //NumBytesToSend = 0; // Reset output buffer pointer

    //        //ftStatus = FTDIGPIO.GetRxBytesAvailable(ref NumBytesToRead);
    //        //if (NumBytesToRead!=0)
    //        //{
    //        //    Debug.LogError("Error - MPSSE receive buffer should be empty");
    //        //    FTDIGPIO.SetBitMode(0x00, 0x00);
    //        //    FTDIGPIO.Close();
    //        //}

    //        //    // Use 60MHz master clock (disable divide by 5)
    //        //    NumBytesToWrite = 0;
    //        //    outputbuffer[NumBytesToWrite++] = 0x8A;
    //        //    // Turn off adaptive clocking (may be needed for ARM)
    //        //    outputbuffer[NumBytesToWrite++] = 0x97;
    //        //    // Disable three-phase clocking
    //        //    outputbuffer[NumBytesToWrite++] = 0x8D;

    //        //    FTSTATUS = FTD2XX.Write(outputbuffer, NumBytesToWrite, ref NumBytesWrite);

    //        //    // Configure data bits low-byte of MPSSE port
    //        //    NumBytesToWrite = 0;
    //        //    outputbuffer[NumBytesToWrite++] = 0x82;
    //        //    // Initial state all low
    //        //    outputbuffer[NumBytesToWrite++] = 0x00;
    //        //    // Direction all output 
    //        //    outputbuffer[NumBytesToWrite++] = 0xFF;
    //        //    FTSTATUS = FTD2XX.Write(outputbuffer, NumBytesToWrite, ref NumBytesWrite);
    //    }

    //    public void Out(byte v)
    //    {
    //        outputbuffer[0] = v;
    //        FTD2XX.Write(outputbuffer, 1, ref NumBytesWrite);
    //    }

    //    public byte In()
    //    {
    //        FTD2XX.Read(inputbuffer, 1, ref NumBytesRead);
    //        return inputbuffer[0];
    //    }

    //    public void BitOut(int bit, bool value)
    //    {
    //        Out(value ? byte.MaxValue : byte.MinValue);
    //    }

    //    public void BitPulse(int bit, double duration_ms)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    public abstract class WaveBase
    {
        public IGPIO gpio;
        public int bit;
        public double startdelay_ms, stopdelay_ms, duration_ms;

        protected Thread thread;
        protected ManualResetEvent threadevent = new(false);
        protected bool threadbreak;
        protected readonly object apilock = new();

        public WaveBase(IGPIO gpio, int bit, double startdelay_ms, double stopdelay_ms, double duration_ms = double.PositiveInfinity)
        {
            this.gpio = gpio;
            this.bit = bit;
            this.startdelay_ms = startdelay_ms;
            this.stopdelay_ms = stopdelay_ms;
            this.duration_ms = duration_ms;
        }

        public virtual void Start()
        {
            lock (apilock)
            {
                thread ??= new(_Wave);
                if (!thread.IsAlive)
                {
                    thread.Start();
                }
                threadbreak = false;
                threadevent.Set();
            }
        }

        public virtual void Stop()
        {
            lock (apilock)
            {
                threadevent.Reset();
                threadbreak = true;
            }
        }

        protected virtual void _Wave() { }
    }

    /// <summary>
    /// Thread Safe PWM Digital Wave for a GPIO Bit Channel
    /// </summary>
    public class PWMWave : WaveBase, IFactorPushTarget
    {
        public double phasedelay, highdur_ms, lowdur_ms;

        public PWMWave(IGPIO gpio = null, int bit = 0, double highdur_ms = 10, double lowdur_ms = 10, double startdelay_ms = 0, double stopdelay_ms = 0, double phasedelay = 0, double duration_ms = double.PositiveInfinity)
            : base(gpio, bit, startdelay_ms, stopdelay_ms, duration_ms)
        {
            this.phasedelay = Math.Clamp(phasedelay, 0, 1);
            this.highdur_ms = highdur_ms;
            this.lowdur_ms = lowdur_ms;
        }

        public void SetWave(double freq, double duty = 0.5, double startdelay_ms = 0, double stopdelay_ms = 0, double phasedelay = 0, double duration_ms = double.PositiveInfinity)
        {
            this.startdelay_ms = startdelay_ms;
            this.stopdelay_ms = stopdelay_ms;
            this.phasedelay = Math.Clamp(phasedelay, 0, 1);
            this.duration_ms = duration_ms;
            SetWave(freq, duty);
        }

        public void SetWave(double freq, double duty = 0.5)
        {
            var mf = gpio?.MaxFreq ?? 1e4;
            freq = Math.Clamp(freq, 1 / mf, mf);
            duty = Math.Clamp(duty, 0, 1);
            var period_ms = 1.0 / freq * 1000;
            highdur_ms = period_ms * duty;
            lowdur_ms = period_ms - highdur_ms;
        }

        public void SetWaveFreq(double freq)
        {
            var mf = gpio?.MaxFreq ?? 1e4;
            freq = Math.Clamp(freq, 1 / mf, mf);
            var duty = highdur_ms / (highdur_ms + lowdur_ms);
            var period_ms = 1.0 / freq * 1000;
            highdur_ms = period_ms * duty;
            lowdur_ms = period_ms - highdur_ms;
        }

        public void SetWaveDuty(double duty)
        {
            duty = Math.Clamp(duty, 0, 1);
            var period_ms = highdur_ms + lowdur_ms;
            highdur_ms = period_ms * duty;
            lowdur_ms = period_ms - highdur_ms;
        }

        public bool SetParam(string name, object value)
        {
            switch (name)
            {
                case "Opto":
                    if ((int)value != 0) { Start(); }
                    break;
                case "OptoFreq":
                    SetWaveFreq((double)value);
                    break;
                case "OptoDuty":
                    SetWaveDuty((double)value);
                    break;
                default:
                    Debug.LogWarning($"Push Factor: {name} not supported in PWMWave.");
                    return false;
            }
            return true;
        }

        protected override void _Wave()
        {
            var timer = new Timer(); bool isbreakstarted;
            double starttime = 0, fliptime = 0, breaktime = 0;
        Break:
            threadevent.WaitOne();
            isbreakstarted = false;
            timer.Restart();

            timer.TimeoutMillisecond(startdelay_ms + phasedelay * (highdur_ms + lowdur_ms));
            starttime = timer.ElapsedMillisecond;
            while (true)
            {
                gpio?.BitOut(bit, true);
                fliptime = timer.ElapsedMillisecond;
                while ((timer.ElapsedMillisecond - fliptime) < highdur_ms)
                {
                    if (threadbreak)
                    {
                        if (!isbreakstarted)
                        {
                            breaktime = timer.ElapsedMillisecond;
                            isbreakstarted = true;
                        }
                        if (isbreakstarted && timer.ElapsedMillisecond - breaktime >= stopdelay_ms)
                        {
                            gpio?.BitOut(bit, false);
                            goto Break;
                        }
                    }
                    if (timer.ElapsedMillisecond - starttime >= duration_ms)
                    {
                        threadevent.Reset();
                        gpio?.BitOut(bit, false);
                        goto Break;
                    }
                }

                gpio?.BitOut(bit, false);
                fliptime = timer.ElapsedMillisecond;
                while ((timer.ElapsedMillisecond - fliptime) < lowdur_ms)
                {
                    if (threadbreak)
                    {
                        if (!isbreakstarted)
                        {
                            breaktime = timer.ElapsedMillisecond;
                            isbreakstarted = true;
                        }
                        if (isbreakstarted && timer.ElapsedMillisecond - breaktime >= stopdelay_ms)
                        {
                            goto Break;
                        }
                    }
                    if (timer.ElapsedMillisecond - starttime >= duration_ms)
                    {
                        threadevent.Reset();
                        goto Break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Thread Safe Poisson Spike Digital Wave for a GPIO Bit Channel
    /// </summary>
    public class PoissonSpikeWave : WaveBase, IFactorPushTarget
    {
        public double spikerate_spms, spikewidth_ms, refreshperiod_ms;
        System.Random rng = new MersenneTwister(true);

        public PoissonSpikeWave(IGPIO gpio = null, int bit = 0, double spikerate_sps = 10, double spikewidth_ms = 1, double refreshperiod_ms = 1, double startdelay_ms = 0, double stopdelay_ms = 0, double duration_ms = double.PositiveInfinity)
            : base(gpio, bit, startdelay_ms, stopdelay_ms, duration_ms)
        {
            this.spikewidth_ms = spikewidth_ms;
            this.refreshperiod_ms = refreshperiod_ms;
            spikerate_spms = spikerate_sps / 1000;
        }

        public bool SetParam(string name, object value)
        {
            throw new NotImplementedException();
        }

        protected override void _Wave()
        {
            var timer = new Timer(); bool isbreakstarted;
            double starttime = 0, fliptime = 0, breaktime = 0, isi = 0;
            Exponential isid;
        Break:
            threadevent.WaitOne();
            isbreakstarted = false;
            timer.Restart();

            timer.TimeoutMillisecond(startdelay_ms);
            starttime = timer.ElapsedMillisecond;
            isid = new Exponential(spikerate_spms, rng);
            while (true)
            {
                isi = isid.Sample();
                if (isi > refreshperiod_ms)
                {
                    fliptime = timer.ElapsedMillisecond;
                    while ((timer.ElapsedMillisecond - fliptime) < isi)
                    {
                        if (threadbreak)
                        {
                            if (!isbreakstarted)
                            {
                                breaktime = timer.ElapsedMillisecond;
                                isbreakstarted = true;
                            }
                            if (isbreakstarted && timer.ElapsedMillisecond - breaktime >= stopdelay_ms)
                            {
                                goto Break;
                            }
                        }
                        if (timer.ElapsedMillisecond - starttime >= duration_ms)
                        {
                            threadevent.Reset();
                            goto Break;
                        }
                    }

                    gpio?.BitOut(bit, true);
                    fliptime = timer.ElapsedMillisecond;
                    while ((timer.ElapsedMillisecond - fliptime) < spikewidth_ms)
                    {
                        if (threadbreak)
                        {
                            if (!isbreakstarted)
                            {
                                breaktime = timer.ElapsedMillisecond;
                                isbreakstarted = true;
                            }
                            if (isbreakstarted && timer.ElapsedMillisecond - breaktime >= stopdelay_ms)
                            {
                                gpio?.BitOut(bit, false);
                                goto Break;
                            }
                        }
                        if (timer.ElapsedMillisecond - starttime >= duration_ms)
                        {
                            threadevent.Reset();
                            gpio?.BitOut(bit, false);
                            goto Break;
                        }
                    }
                    gpio?.BitOut(bit, false);
                }
            }
        }
    }

    /// <summary>
    /// Thread Safe Digital Waves for GPIO Bit Channels
    /// </summary>
    public class GPIOWave
    {
        public IGPIO gpio;
        readonly object apilock = new();
        ConcurrentDictionary<int, ConcurrentDictionary<string, WaveBase>> waves = new();

        public GPIOWave(IGPIO gpio)
        {
            this.gpio = gpio;
        }

        public void SetBitWave(int bit, string name, WaveBase wave)
        {
            lock (apilock)
            {
                if (!waves.ContainsKey(bit)) { waves[bit] = new(); }
                wave.gpio = gpio;
                wave.bit = bit;
                waves[bit][name] = wave;
            }
        }

        public void Start(int bit, string name)
        {
            lock (apilock)
            {
                if (waves.ContainsKey(bit) && waves[bit].ContainsKey(name)) { waves[bit][name].Start(); }
            }
        }

        public void Stop(int bit, string name)
        {
            lock (apilock)
            {
                if (waves.ContainsKey(bit) && waves[bit].ContainsKey(name)) { waves[bit][name].Stop(); }
            }
        }

        public void Start(int[] bits, string[] names)
        {
            lock (apilock)
            {
                if (bits == null || names == null) { return; }
                var bs = bits.Distinct().ToArray();
                if (bs.Length == names.Length)
                {
                    for (var i = 0; i < bs.Length; i++)
                    {
                        Start(bs[i], names[i]);
                    }
                }
            }
        }

        public void Stop(int[] bits, string[] names)
        {
            lock (apilock)
            {
                if (bits == null || names == null) { return; }
                var bs = bits.Distinct().ToArray();
                if (bs.Length == names.Length)
                {
                    for (var i = 0; i < bs.Length; i++)
                    {
                        Stop(bs[i], names[i]);
                    }
                }
            }
        }

        public void StartAll()
        {
            lock (apilock)
            {
                foreach (var ws in waves.Values)
                {
                    foreach (var w in ws.Values)
                    {
                        w.Start();
                    }
                }
            }
        }

        public void StopAll()
        {
            lock (apilock)
            {
                foreach (var ws in waves.Values)
                {
                    foreach (var w in ws.Values)
                    {
                        w.Stop();
                    }
                }
            }
        }

    }
}