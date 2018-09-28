/*
VLParallelPort.cs is part of the VLAB project.
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

namespace IExSys
{
    public static class Inpout
    {
        [DllImport("inpoutx64.dll", EntryPoint = "IsInpOutDriverOpen")]
        static extern int IsInpOutDriverOpen();
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        static extern void Out8(ushort PortAddress, byte Data);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        static extern byte Inp8(ushort PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUshort")]
        static extern void Out16(ushort PortAddress, ushort Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUshort")]
        static extern ushort Inp16(ushort PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUlong")]
        static extern void Out64(ulong PortAddress, ulong Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUlong")]
        static extern ulong Inp64(ulong PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "GetPhysLong")]
        static extern int GetPhysLong(ref byte PortAddress, ref uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "SetPhysLong")]
        static extern int SetPhysLong(ref byte PortAddress, uint Data);

        static object lockobject;
        static Inpout()
        {
            lockobject = new object();
            try
            {
                if (IsInpOutDriverOpen() == 0)
                {
                    Debug.Log("Unable to open parallel port driver: Inpoutx64");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        public static void Output8(ushort PortAddress, byte Data)
        {
            lock (lockobject)
            {
                Out8(PortAddress, Data);
            }
        }

        public static void Output16(ushort PortAddress, ushort Data)
        {
            lock (lockobject)
            {
                Out16(PortAddress, Data);
            }
        }

        public static byte Input8(ushort PortAddress)
        {
            lock (lockobject)
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

    public class ParallelPort
    {
        uint dataaddress;
        public uint DataAddress { get { lock (lockobj) { return dataaddress; } } set { lock (lockobj) { dataaddress = value; } } }
        public uint StatusAddress { get { return DataAddress + 1; } }
        public uint ControlAddress { get { return DataAddress + 2; } }
        ParallelPortDataMode datamode;
        public ParallelPortDataMode DataMode
        {
            get { lock (lockobj) { return datamode; } }
            set
            {
                lock (lockobj)
                {
                    Inpout.Output8((ushort)ControlAddress, (byte)(value == ParallelPortDataMode.Input ? 0x01 << 5 : 0x00));
                    datamode = value;
                }
            }
        }
        uint valuecache;
        object lockobj = new object();

        public ParallelPort(uint dataaddress = 0xB010, ParallelPortDataMode datamode = ParallelPortDataMode.Output)
        {
            this.dataaddress = dataaddress;
            DataMode = datamode;
        }

        public byte Inp()
        {
            lock (lockobj)
            {
                if (DataMode == ParallelPortDataMode.Output)
                {
                    DataMode = ParallelPortDataMode.Input;
                }
                return Inpout.Input8((ushort)dataaddress);
            }
        }

        public void Out(uint data)
        {
            lock (lockobj)
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
            lock (lockobj)
            {
                if (DataMode == ParallelPortDataMode.Input)
                {
                    DataMode = ParallelPortDataMode.Output;
                }
                Inpout.Output8((ushort)dataaddress, data);
            }
        }

        public void SetBit(uint bit = 0, bool value = true)
        {
            lock (lockobj)
            {
                var t = value ? (1u << (int)bit) : ~(1u << (int)bit);
                valuecache = value ? valuecache | t : valuecache & t;
                Out(valuecache);
            }
        }

        public void SetBits(uint[] bits, bool[] values)
        {
            lock (lockobj)
            {
                if (bits != null && values != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Count() == values.Length)
                    {
                        for (var i = 0; i < bs.Count(); i++)
                        {
                            var t = values[i] ? (1u << (int)bs[i]) : ~(1u << (int)bs[i]);
                            valuecache = values[i] ? valuecache | t : valuecache & t;
                        }
                        Out(valuecache);
                    }
                }
            }
        }

        public bool GetBit(uint bit = 0)
        {
            lock (lockobj)
            {
                var t = Convert.ToString(Inp(), 2).PadLeft(16, '0');
                return t[15 - (int)bit] == '1' ? true : false;
            }
        }

        public bool[] GetBits(uint[] bits)
        {
            lock (lockobj)
            {
                var vs = new List<bool>();
                if (bits != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Count() != 0)
                    {
                        var t = Convert.ToString(Inp(), 2).PadLeft(16, '0');
                        foreach (var b in bs)
                        {
                            vs.Add(t[15 - (int)b] == '1' ? true : false);
                        }
                    }
                }
                return vs.ToArray();
            }
        }

        public void BitPulse(uint bit = 0, double duration_ms = 1)
        {
            lock (lockobj)
            {
                var timer = new VLTimer();
                SetBit(bit);
                timer.Timeout(duration_ms);
                SetBit(bit, false);
            }
        }

        void _BitPulse(object p)
        {
            var param = (List<object>)p;
            BitPulse((uint)param[0], (double)param[1]);
        }

        public void ConcurrentBitPulse(uint bit = 0, double duration_ms = 1)
        {
            lock (lockobj)
            {
                var t = new Thread(new ParameterizedThreadStart(_BitPulse));
                t.Start(new List<object>() { bit, duration_ms });
            }
        }

        public void BitsPulse(uint[] bits, double[] durations_ms)
        {
            lock (lockobj)
            {
                if (bits != null && durations_ms != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Count() == durations_ms.Length)
                    {
                        for (var i = 0; i < bs.Count(); i++)
                        {
                            BitPulse(bs[i], durations_ms[i]);
                        }
                    }
                }
            }
        }

        public void ConcurrentBitsPulse(uint[] bits, double[] durations_ms)
        {
            lock (lockobj)
            {
                if (bits != null && durations_ms != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Count() == durations_ms.Length)
                    {
                        for (var i = 0; i < bs.Count(); i++)
                        {
                            ConcurrentBitPulse(bs[i], durations_ms[i]);
                        }
                    }
                }
            }
        }
    }

    public enum BitWave
    {
        HighLow,
        PoissonSpike
    }

    /// <summary>
    /// wave can be reliably delivered upon 10kHz
    /// </summary>
    public class ParallelPortWave
    {
        ParallelPort pport;
        float lowcutofffreq, highcutofffreq;
        System.Random rng = new MersenneTwister(true);

        ConcurrentDictionary<uint, Thread> bitthread = new ConcurrentDictionary<uint, Thread>();
        ConcurrentDictionary<uint, ManualResetEvent> bitthreadevent = new ConcurrentDictionary<uint, ManualResetEvent>();
        ConcurrentDictionary<uint, bool> bitthreadbreak = new ConcurrentDictionary<uint, bool>();

        ConcurrentDictionary<uint, BitWave> bitwave = new ConcurrentDictionary<uint, BitWave>();
        ConcurrentDictionary<uint, double> bitlatency_ms = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bitphase = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bithighdur_ms = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bitlowdur_ms = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bitspikerate = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bitspikewidth_ms = new ConcurrentDictionary<uint, double>();
        ConcurrentDictionary<uint, double> bitrefreshperiod_ms = new ConcurrentDictionary<uint, double>();


        public ParallelPortWave(ParallelPort pp, float lowcutofffreq = 0.00001f, float highcutofffreq = 10000f)
        {
            pport = pp;
            this.lowcutofffreq = lowcutofffreq;
            this.highcutofffreq = highcutofffreq;
        }

        public void SetBitWave(uint bit, double highdur_ms, double lowdur_ms, double latency_ms = 0, double phase = 0)
        {

            bitlatency_ms[bit] = latency_ms;
            bitphase[bit] = phase;
            bithighdur_ms[bit] = highdur_ms;
            bitlowdur_ms[bit] = lowdur_ms;
            bitwave[bit] = BitWave.HighLow;
        }

        public void SetBitWave(uint bit, float freq, double latency_ms = 0, double phase = 0)
        {
            bitlatency_ms[bit] = latency_ms;
            bitphase[bit] = phase;
            var halfcycle = (1.0 / Mathf.Clamp(freq, lowcutofffreq, highcutofffreq)) * 1000.0 / 2.0;
            bithighdur_ms[bit] = halfcycle;
            bitlowdur_ms[bit] = halfcycle;
            bitwave[bit] = BitWave.HighLow;
        }

        public void SetBitWave(uint bit, double rate_sps, double spikewidth_ms = 2, double refreshperiod_ms = 2, double latency_ms = 0, double phase = 0)
        {
            bitlatency_ms[bit] = latency_ms;
            bitphase[bit] = phase;
            bitspikerate[bit] = rate_sps / 1000;
            bitspikewidth_ms[bit] = spikewidth_ms;
            bitrefreshperiod_ms[bit] = refreshperiod_ms;
            bitwave[bit] = BitWave.PoissonSpike;
        }

        public void Start(params uint[] bs)
        {
            var vbs = bitlatency_ms.Keys.Intersect(bs).ToArray();
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
                    bitthreadbreak[b] = false;
                }
                foreach (var b in vbs)
                {
                    if (!bitthread[b].IsAlive)
                    {
                        bitthread[b].Start(b);
                    }
                    bitthreadevent[b].Set();
                }
            }
        }

        public void Stop(params uint[] bs)
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

        public void StopAll()
        {
            Stop(bitthread.Keys.ToArray());
        }

        void ThreadBitWave(uint bit)
        {
            var timer = new VLTimer(); bool isbreakstarted;
            double start, breakstart = 0;
            Break:
            bitthreadevent[bit].WaitOne();
            isbreakstarted = false;
            timer.Restart();
            switch (bitwave[bit])
            {
                case BitWave.HighLow:
                    timer.Timeout(bitlatency_ms[bit] + bitphase[bit] * (bithighdur_ms[bit] + bitlowdur_ms[bit]));
                    while (true)
                    {
                        pport.SetBit(bit);
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
                                    pport.SetBit(bit, false);
                                    goto Break;
                                }
                            }
                        }

                        pport.SetBit(bit, false);
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
                case BitWave.PoissonSpike:
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

                            pport.SetBit(bit);
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
                                        pport.SetBit(bit, false);
                                        goto Break;
                                    }
                                }
                            }
                            pport.SetBit(bit, false);
                        }
                    }
            }
        }

        void _BitWave(object p)
        {
            ThreadBitWave((uint)p);
        }
    }
}