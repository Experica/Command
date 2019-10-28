/*
ParallelPort.cs is part of the Experica.
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
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;

namespace Experica
{
    public static class Inpout
    {
        [DllImport("inpoutx64", EntryPoint = "IsInpOutDriverOpen")]
        static extern int IsInpOutDriverOpen();
        [DllImport("inpoutx64", EntryPoint = "Out32")]
        static extern void Out8(ushort PortAddress, byte Data);
        [DllImport("inpoutx64", EntryPoint = "Inp32")]
        static extern byte Inp8(ushort PortAddress);

        [DllImport("inpoutx64", EntryPoint = "DlPortWritePortUshort")]
        static extern void Out16(ushort PortAddress, ushort Data);
        [DllImport("inpoutx64", EntryPoint = "DlPortReadPortUshort")]
        static extern ushort Inp16(ushort PortAddress);

        [DllImport("inpoutx64", EntryPoint = "DlPortWritePortUlong")]
        static extern void Out64(ulong PortAddress, ulong Data);
        [DllImport("inpoutx64", EntryPoint = "DlPortReadPortUlong")]
        static extern ulong Inp64(ulong PortAddress);

        [DllImport("inpoutx64", EntryPoint = "GetPhysLong")]
        static extern int GetPhysLong(ref byte PortAddress, ref uint Data);
        [DllImport("inpoutx64", EntryPoint = "SetPhysLong")]
        static extern int SetPhysLong(ref byte PortAddress, uint Data);

        static readonly object apilock = new object();

        static Inpout()
        {
            try
            {
                lock (apilock)
                {
                    if (IsInpOutDriverOpen() == 0)
                    {
                        Debug.Log("Unable to open parallel port driver: Inpoutx64.");
                    }
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static void Output8(ushort PortAddress, byte Data)
        {
            lock (apilock)
            {
                Out8(PortAddress, Data);
            }
        }

        public static void Output16(ushort PortAddress, ushort Data)
        {
            lock (apilock)
            {
                Out16(PortAddress, Data);
            }
        }

        public static byte Input8(ushort PortAddress)
        {
            lock (apilock)
            {
                return Inp8(PortAddress);
            }
        }
    }

    public enum ParallelPortDataMode
    {
        Input,
        Output
    }

    public class ParallelPort:IGPIO
    {
        int dataaddress;
        public int DataAddress { get { lock (apilock) { return dataaddress; } } set { lock (apilock) { dataaddress = value; } } }
        public int StatusAddress { get { lock (apilock) { return dataaddress + 1; } } }
        public int ControlAddress { get { lock (apilock) { return dataaddress + 2; } } }
        ParallelPortDataMode datamode;
        public ParallelPortDataMode DataMode
        {
            get { lock (apilock) { return datamode; } }
            set
            {
                lock (apilock)
                {
                    Inpout.Output8((ushort)ControlAddress, (byte)(value == ParallelPortDataMode.Input ? 0x01 << 5 : 0x00));
                    datamode = value;
                }
            }
        }

        public bool Found => true;

        int datavalue;
        readonly object apilock = new object();

        public ParallelPort(int dataaddress = 0xB010, ParallelPortDataMode datamode = ParallelPortDataMode.Output)
        {
            DataAddress = dataaddress;
            DataMode = datamode;
        }

        public byte Inp()
        {
            lock (apilock)
            {
                if (DataMode == ParallelPortDataMode.Output)
                {
                    DataMode = ParallelPortDataMode.Input;
                }
                return Inpout.Input8((ushort)dataaddress);
            }
        }

        public void Out(int data)
        {
            lock (apilock)
            {
                if (DataMode == ParallelPortDataMode.Input)
                {
                    DataMode = ParallelPortDataMode.Output;
                }
                Inpout.Output16((ushort)dataaddress, (ushort)data);
            }
        }

        public void Out(byte data)
        {
            lock (apilock)
            {
                if (DataMode == ParallelPortDataMode.Input)
                {
                    DataMode = ParallelPortDataMode.Output;
                }
                Inpout.Output8((ushort)dataaddress, data);
            }
        }

        public void BitOut(int bit = 0, bool value = true)
        {
            lock (apilock)
            {
                var v = value ? (0x01 << bit) : ~(0x01 << bit);
                datavalue = value ? datavalue | v : datavalue & v;
                Out(datavalue);
            }
        }

        public void SetBits(int[] bits, bool[] values)
        {
            lock (apilock)
            {
                if (bits != null && values != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length == values.Length)
                    {
                        for (var i = 0; i < bs.Length; i++)
                        {
                            var v = values[i] ? (0x01 << bs[i]) : ~(0x01 << bs[i]);
                            datavalue = values[i] ? datavalue | v : datavalue & v;
                        }
                        Out(datavalue);
                    }
                }
            }
        }

        public bool GetBit(int bit = 0)
        {
            lock (apilock)
            {
                var v = Convert.ToString(Inp(), 2).PadLeft(16, '0');
                return v[15 - bit] == '1' ? true : false;
            }
        }

        public bool[] GetBits(int[] bits)
        {
            lock (apilock)
            {
                var vs = new List<bool>();
                if (bits != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length > 0)
                    {
                        var v = Convert.ToString(Inp(), 2).PadLeft(16, '0');
                        foreach (var b in bs)
                        {
                            vs.Add(v[15 - b] == '1' ? true : false);
                        }
                    }
                }
                return vs.ToArray();
            }
        }

        public void BitPulse(int bit = 0, double duration_ms = 1)
        {
            var timer = new Timer();
            BitOut(bit, true);
            timer.Timeout(duration_ms);
            BitOut(bit, false);
        }

        void _BitPulse(object p)
        {
            var param = (List<object>)p;
            BitPulse((int)param[0], (double)param[1]);
        }

        public void ASyncBitPulse(int bit = 0, double duration_ms = 1)
        {
            lock (apilock)
            {
                var t = new Thread(new ParameterizedThreadStart(_BitPulse));
                t.Start(new List<object>() { bit, duration_ms });
            }
        }

        public void BitsPulse(int[] bits, double[] durations_ms)
        {
            lock (apilock)
            {
                if (bits != null && durations_ms != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length == durations_ms.Length)
                    {
                        for (var i = 0; i < bs.Length; i++)
                        {
                            BitPulse(bs[i], durations_ms[i]);
                        }
                    }
                }
            }
        }

        public void ASyncBitsPulse(int[] bits, double[] durations_ms)
        {
            lock (apilock)
            {
                if (bits != null && durations_ms != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length == durations_ms.Length)
                    {
                        for (var i = 0; i < bs.Length; i++)
                        {
                            ASyncBitPulse(bs[i], durations_ms[i]);
                        }
                    }
                }
            }
        }
    }

    public enum DigitalWave
    {
        HighLow,
        PoissonSpike
    }

    /// <summary>
    /// Parallel Port wave can be reliably delivered upon 10kHz
    /// </summary>
    public class ParallelPortWave
    {
        ParallelPort pport;
        public ParallelPort ParallelPort { get { lock (apilock) { return pport; } } set { lock (apilock) { pport = value; } } }
        readonly float lowcutofffreq, highcutofffreq;
        System.Random rng = new MersenneTwister(true);
        readonly object apilock = new object();

        ConcurrentDictionary<int, Thread> bitthread = new ConcurrentDictionary<int, Thread>();
        ConcurrentDictionary<int, ManualResetEvent> bitthreadevent = new ConcurrentDictionary<int, ManualResetEvent>();
        ConcurrentDictionary<int, bool> bitthreadbreak = new ConcurrentDictionary<int, bool>();

        ConcurrentDictionary<int, DigitalWave> bitwave = new ConcurrentDictionary<int, DigitalWave>();
        ConcurrentDictionary<int, double> bitlatency_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitphase = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bithighdur_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitlowdur_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitspikerate = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitspikewidth_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> bitrefreshperiod_ms = new ConcurrentDictionary<int, double>();

        public ParallelPortWave(ParallelPort pp, float lowcutofffreq = 0.00001f, float highcutofffreq = 10000f)
        {
            pport = pp;
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
                bitwave[bit] = DigitalWave.HighLow;
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
                bitwave[bit] = DigitalWave.HighLow;
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
                bitwave[bit] = DigitalWave.PoissonSpike;
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
                case DigitalWave.HighLow:
                    timer.Timeout(bitlatency_ms[bit] + bitphase[bit] * (bithighdur_ms[bit] + bitlowdur_ms[bit]));
                    while (true)
                    {
                        pport.BitOut(bit, true);
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
                                    pport.BitOut(bit, false);
                                    goto Break;
                                }
                            }
                        }

                        pport.BitOut(bit, false);
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
                case DigitalWave.PoissonSpike:
                    timer.Timeout(bitlatency_ms[bit] + bitphase[bit] / bitspikerate[bit]);
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

                            pport.BitOut(bit, true);
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
                                        pport.BitOut(bit, false);
                                        goto Break;
                                    }
                                }
                            }
                            pport.BitOut(bit, false);
                        }
                    }
            }
        }
    }
}