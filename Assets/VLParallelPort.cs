/*
VLParallelPort.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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

namespace VLab
{
    public class Inpout
    {
        [DllImport("inpoutx64.dll", EntryPoint = "IsInpOutDriverOpen")]
        public static extern int IsInpOutDriverOpen();
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        public static extern void Out8(ushort PortAddress, byte Data);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        public static extern byte Inp8(ushort PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUshort")]
        public static extern void Out16(ushort PortAddress, ushort Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUshort")]
        public static extern ushort Inp16(ushort PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUlong")]
        public static extern void Out64(ulong PortAddress, ulong Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUlong")]
        public static extern ulong Inp64(ulong PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "GetPhysLong")]
        public static extern int GetPhysLong(ref byte PortAddress, ref uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "SetPhysLong")]
        public static extern int SetPhysLong(ref byte PortAddress, uint Data);

        public Inpout()
        {
            try
            {
                if (IsInpOutDriverOpen() == 0)
                {
                    Debug.Log("Unable to open Inpoutx64 driver.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
    }

    public enum ParallelPortDataMode
    {
        Input,
        Output
    }

    public class ParallelPort : Inpout
    {
        public int dataaddress;
        public int statusaddress { get { return dataaddress + 1; } }
        public int controladdress { get { return dataaddress + 2; } }
        private ParallelPortDataMode datamode;
        public ParallelPortDataMode DataMode
        {
            get { return datamode; }
            set
            {
                lock (lockobj)
                {

                    Out8((ushort)controladdress, (byte)(value == ParallelPortDataMode.Input ? 0x01 << 5 : 0x00));
                }
                datamode = value;
            }
        }
        private int valuecache;
        private object lockobj = new object();

        public ParallelPort(int dataaddress = 0xC010, ParallelPortDataMode datamode = ParallelPortDataMode.Output)
        {
            this.dataaddress = dataaddress;
            DataMode = datamode;
        }

        public byte Inp()
        {
            if (DataMode == ParallelPortDataMode.Output)
            {
                DataMode = ParallelPortDataMode.Input;
            }
            lock (lockobj)
            {
                return Inp8((ushort)dataaddress);
            }
        }

        public void Out(int data)
        {
            if (DataMode == ParallelPortDataMode.Input)
            {
                DataMode = ParallelPortDataMode.Output;
            }
            lock (lockobj)
            {
                Out16((ushort)dataaddress, (ushort)data);
            }
        }

        public void Out(byte data)
        {
            if (DataMode == ParallelPortDataMode.Input)
            {
                DataMode = ParallelPortDataMode.Output;
            }
            lock (lockobj)
            {
                Out8((ushort)dataaddress, data);
            }
        }

        public void SetBit(int bit = 0, bool value = true)
        {
            var t = value ? (1 << bit) : ~(1 << bit);
            lock (lockobj)
            {
                valuecache = value ? valuecache | t : valuecache & t;
                Out(valuecache);
            }
        }

        public void SetBits(int[] bits, bool[] values)
        {
            if (bits != null && values != null)
            {
                var bs = bits.Distinct().ToArray();
                if (bs.Count() == values.Length)
                {
                    lock (lockobj)
                    {
                        for (var i = 0; i < bs.Count(); i++)
                        {
                            var t = values[i] ? (1 << bs[i]) : ~(1 << bs[i]);
                            valuecache = values[i] ? valuecache | t : valuecache & t;
                        }
                        Out(valuecache);
                    }
                }
            }
        }

        public bool GetBit(int bit = 0)
        {
            var t = Convert.ToString(Inp(), 2).PadLeft(16, '0');
            return t[15 - bit] == '1' ? true : false;
        }

        public bool[] GetBits(int[] bits)
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
                        vs.Add(t[15 - b] == '1' ? true : false);
                    }
                }
            }
            return vs.ToArray();
        }

        public void BitPulse(int bit = 0, double duration_ms = 1)
        {
            var timer = new VLTimer();
            SetBit(bit);
            timer.Countdown(duration_ms);
            SetBit(bit, false);
        }

        void _BitPulse(object p)
        {
            var param = (List<object>)p;
            BitPulse((int)param[0], (double)param[1]);
        }

        public void ThreadBitPulse(int bit = 0, double duration_ms = 1)
        {
            var t = new Thread(new ParameterizedThreadStart(_BitPulse));
            t.Start(new List<object>() { bit, duration_ms });
        }

        public void BitsPulse(int[] bits, double[] durations_ms)
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

        public void ThreadBitsPulse(int[] bits, double[] durations_ms)
        {
            if (bits != null && durations_ms != null)
            {
                var bs = bits.Distinct().ToArray();
                if (bs.Count() == durations_ms.Length)
                {
                    for (var i = 0; i < bs.Count(); i++)
                    {
                        ThreadBitPulse(bs[i], durations_ms[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Square wave can be reliably delivered upon 10kHz
    /// </summary>
    public class ParallelPortSquareWave
    {
        ParallelPort pport;
        public ParallelPort PPort
        {
            get { lock (lockobj) { return pport; } }
            set { lock (lockobj) { pport = value; } }
        }
        ConcurrentDictionary<int, Thread> bitthread = new ConcurrentDictionary<int, Thread>();
        ConcurrentDictionary<int, ManualResetEvent> bitthreadevent = new ConcurrentDictionary<int, ManualResetEvent>();
        ConcurrentDictionary<int, bool> bitthreadbreak = new ConcurrentDictionary<int, bool>();

        public ConcurrentDictionary<int, double> bitlatency_ms = new ConcurrentDictionary<int, double>();
        public ConcurrentDictionary<int, double> bithighdur_ms = new ConcurrentDictionary<int, double>();
        public ConcurrentDictionary<int, double> bitlowdur_ms = new ConcurrentDictionary<int, double>();
        ConcurrentDictionary<int, double> _bitfreq = new ConcurrentDictionary<int, double>();
        object lockobj = new object();

        public ParallelPortSquareWave(ParallelPort pp)
        {
            pport = pp;
        }

        public void SetBitFreq(int bit, double freq)
        {
            _bitfreq[bit] = freq;
            var halfcycle = (1 / Math.Max(0.001, freq)) * 1000 / 2;
            bithighdur_ms[bit] = halfcycle;
            bitlowdur_ms[bit] = halfcycle;
        }

        public double GetBitFreq(int bit)
        {
            return _bitfreq[bit];
        }

        public void Start(params int[] bs)
        {
            var vbs = bitlatency_ms.Keys.Intersect(bs).ToArray();
            var vbn = vbs.Length;
            if (vbn > 0)
            {
                foreach (var b in vbs)
                {
                    if (!bitthread.ContainsKey(b))
                    {
                        bitthread[b] = new Thread(_BitSquareWave);
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

        public void Stop(params int[] bs)
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

        public void BitSquareWave(int bit)
        {
            var timer = new VLTimer(); bool isbreakstarted;
            double start, breakstart = 0; double highdur, lowdur, latency;
        Break:
            bitthreadevent[bit].WaitOne();
            isbreakstarted = false;
            highdur = bithighdur_ms[bit];
            lowdur = bitlowdur_ms[bit];
            latency = bitlatency_ms[bit];
            timer.Restart();
            timer.Countdown(latency);
            while (true)
            {
                PPort.SetBit(bit);
                start = timer.ElapsedMillisecond;
                while ((timer.ElapsedMillisecond - start) < highdur)
                {
                    if (bitthreadbreak[bit])
                    {
                        if (!isbreakstarted)
                        {
                            breakstart = timer.ElapsedMillisecond;
                            isbreakstarted = true;
                        }
                        if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= latency)
                        {
                            PPort.SetBit(bit, false);
                            goto Break;
                        }
                    }
                }

                PPort.SetBit(bit, false);
                start = timer.ElapsedMillisecond;
                while ((timer.ElapsedMillisecond - start) < lowdur)
                {
                    if (bitthreadbreak[bit])
                    {
                        if (!isbreakstarted)
                        {
                            breakstart = timer.ElapsedMillisecond;
                            isbreakstarted = true;
                        }
                        if (isbreakstarted && timer.ElapsedMillisecond - breakstart >= latency)
                        {
                            goto Break;
                        }
                    }
                }
            }
        }

        void _BitSquareWave(object p)
        {
            BitSquareWave((int)p);
        }
    }
}