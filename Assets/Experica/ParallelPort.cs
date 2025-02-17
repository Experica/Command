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
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace Experica
{
    /// <summary>
    /// Thread Safe Access to "inpoutx64" Windows Parallel Port Driver
    /// </summary>
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

        static readonly object apilock = new();

        static Inpout()
        {
            try
            {
                lock (apilock)
                {
                    if (IsInpOutDriverOpen() == 0)
                    {
                        Debug.Log("Unable to Open Parallel Port Driver: Inpoutx64.");
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

    /// <summary>
    /// Thread Safe Access to Parallel Port
    /// </summary>
    public class ParallelPort : IGPIO
    {
        #region IDisposable
        int disposecount = 0;

        ~ParallelPort()
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
        }
        #endregion

        public int DataAddress;
        public int StatusAddress => DataAddress + 1;
        public int ControlAddress => DataAddress + 2;
        IODirection datamode;
        public IODirection DataMode
        {
            get { return datamode; }
            set
            {
                lock (apilock)
                {
                    Inpout.Output8((ushort)ControlAddress, (byte)(value == IODirection.Input ? 0x01 << 5 : 0x00));
                    datamode = value;
                }
            }
        }

        public bool Found => true;

        public double MaxFreq => 1e4;

        int currentdataout;
        readonly object apilock = new();

        public ParallelPort(int dataaddress = 0xB010, IODirection datamode = IODirection.Output)
        {
            DataAddress = dataaddress;
            DataMode = datamode;
        }

        public byte In()
        {
            lock (apilock)
            {
                if (DataMode == IODirection.Output)
                {
                    DataMode = IODirection.Input;
                }
                return Inpout.Input8((ushort)DataAddress);
            }
        }

        public void Out(int data)
        {
            lock (apilock)
            {
                if (DataMode == IODirection.Input)
                {
                    DataMode = IODirection.Output;
                }
                Inpout.Output16((ushort)DataAddress, (ushort)data);
                currentdataout = data;
            }
        }

        public void Out(byte data)
        {
            lock (apilock)
            {
                if (DataMode == IODirection.Input)
                {
                    DataMode = IODirection.Output;
                }
                Inpout.Output8((ushort)DataAddress, data);
                currentdataout = data;
            }
        }

        public void BitOut(int bit = 0, bool value = true)
        {
            lock (apilock)
            {
                var v = value ? (0x01 << bit) : ~(0x01 << bit);
                Out(value ? currentdataout | v : currentdataout & v);
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
                        var data = currentdataout;
                        for (var i = 0; i < bs.Length; i++)
                        {
                            var v = values[i] ? (0x01 << bs[i]) : ~(0x01 << bs[i]);
                            data = values[i] ? data | v : data & v;
                        }
                        Out(data);
                    }
                    else { Debug.LogWarning($"Parallel Port Set Bits: {bits} and Values: {values} Do Not Match!"); }
                }
            }
        }

        public bool GetBit(int bit = 0)
        {
            lock (apilock)
            {
                return (((0x01 << bit) & In()) >> bit) == 1;
            }
        }

        public bool[] GetBits(int[] bits)
        {
            lock (apilock)
            {
                if (bits != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length > 0)
                    {
                        var vs = new bool[bs.Length];
                        var v = In();
                        for (var i = 0; i < bs.Length; i++)
                        {
                            vs[i] = (((0x01 << bs[i]) & v) >> bs[i]) == 1;
                        }
                        return vs;
                    }
                }
                return null;
            }
        }

        void _BitPulse(int bit = 0, double duration_ms = 1, double delay_ms = 0, bool ispositivepulse = true)
        {
            var timer = new Timer();
            if (delay_ms > 0)
            {
                timer.WaitMillisecond(delay_ms);
            }
            if (duration_ms > 0)
            {
                var startlevel = ispositivepulse ? true : false;
                BitOut(bit, startlevel);
                timer.WaitMillisecond(duration_ms);
                BitOut(bit, !startlevel);
            }
        }

        /// <summary>
        /// Async generate a pulse on a bit channel
        /// </summary>
        /// <param name="bit"></param>
        /// <param name="duration_ms"></param>
        /// <param name="delay_ms"></param>
        /// <param name="ispositivepulse"></param>
        public void BitPulse(int bit = 0, double duration_ms = 1, double delay_ms = 0, bool ispositivepulse = true)
        {
            lock (apilock)
            {
                var t = new Thread(() => _BitPulse(bit, duration_ms, delay_ms, ispositivepulse));
                t.Start();
            }
        }

        /// <summary>
        /// Async generate pulses on bit channels
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="durations_ms"></param>
        /// <param name="delays_ms"></param>
        /// <param name="ispositivepulses"></param>
        public void BitsPulse(int[] bits, double[] durations_ms, double[] delays_ms, bool[] ispositivepulses)
        {
            lock (apilock)
            {
                if (bits != null && durations_ms != null && delays_ms != null && ispositivepulses != null)
                {
                    var bs = bits.Distinct().ToArray();
                    if (bs.Length == durations_ms.Length && bs.Length == delays_ms.Length && bs.Length == ispositivepulses.Length)
                    {
                        for (var i = 0; i < bs.Length; i++)
                        {
                            BitPulse(bs[i], durations_ms[i], delays_ms[i], ispositivepulses[i]);
                        }
                    }
                }
            }
        }
    }
}