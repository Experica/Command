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

    public class ParallelPort : Inpout
    {
        public int address;

        public ParallelPort(int address = 0x378)
        {
            this.address = address;
        }

        public int Inp()
        {
            return Inp16((ushort)address);
        }

        public byte InpByte()
        {
            return Inp8((ushort)address);
        }

        public void Out(int data)
        {
            Out16((ushort)address, (ushort)data);
        }

        public void OutByte(byte data)
        {
            Out8((ushort)address, data);
        }

        public void SetBit(int bit = 0, bool value = true)
        {
            var t = value ? Math.Pow(2.0, bit) : 0;
            Out((int)t);
        }

        public void SetBits(int[] bits, bool[] values)
        {
            if (bits != null && values != null)
            {
                var bs = bits.Distinct().ToArray();
                if (bs.Count() == values.Length)
                {
                    var t = 0.0;
                    for (var i = 0; i < bs.Count(); i++)
                    {
                        t += values[i] ? Math.Pow(2.0, bs[i]) : 0;
                    }
                    Out((int)t);
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
        ConcurrentDictionary<int, Thread> bitthread=new ConcurrentDictionary<int, Thread>();
        ConcurrentDictionary<int, ManualResetEvent> bitthreadevent = new ConcurrentDictionary<int, ManualResetEvent>();
        ConcurrentDictionary<int, bool> bitthreadbreak = new ConcurrentDictionary<int, bool>();
        public ConcurrentDictionary<int, float> bitfreq = new ConcurrentDictionary<int, float>();
        object lockobj = new object();

        public ParallelPortSquareWave(ParallelPort pp)
        {
            pport = pp;
        }

        public void Start(params KeyValuePair<int,float>[] bf)
        {
            foreach(var kv in bf)
            {
                bitfreq[kv.Key] = kv.Value;
            }
            Start(bf.Select(i => i.Key).ToArray());
        }

        public void Start(params int[] bs)
        {
            var vbs = bitfreq.Keys.Intersect(bs).ToArray();
            var vbn = vbs.Length;
            if (vbn > 0)
            {
                foreach(var b in vbs)
                {
                    if (!bitthread.ContainsKey(b))
                    {
                        bitthread[b] = new Thread(_BitSquareWave);
                        bitthreadevent[b] = new ManualResetEvent(false);
                    }
                    bitthreadbreak[b] = false;
                }
                foreach(var b in vbs)
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
                foreach(var b in vbs)
                {
                    bitthreadevent[b].Reset();
                    bitthreadbreak[b] = true;
                }
            }
        }

        public void BitSquareWave(int bit)
        {
            var timer = new VLTimer();
            double start,end;float halfcycle;
            Break:
            bitthreadevent[bit].WaitOne();
            halfcycle = (1 / Mathf.Max(0.001f, bitfreq[bit])) / 2;
            timer.Restart();
            while (true)
            {
                PPort.SetBit(bit);
                start = timer.ElapsedSecond;
                end = timer.ElapsedSecond;
                while ((end - start) < halfcycle)
                {
                    if(bitthreadbreak[bit])
                    {
                        PPort.SetBit(bit, false);
                        goto Break;
                    }
                    end = timer.ElapsedSecond;
                }

                PPort.SetBit(bit, false);
                start = timer.ElapsedSecond;
                end = timer.ElapsedSecond;
                while ((end - start) < halfcycle)
                {
                    if (bitthreadbreak[bit])
                    {
                        goto Break;
                    }
                    end = timer.ElapsedSecond;
                }
            }
        }

        void _BitSquareWave(object p)
        {
            BitSquareWave((int)p);
        }
    }
}