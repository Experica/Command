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
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;

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
        int dataaddress;
        public int DataAddress { get { lock (apilock) { return dataaddress; } } set { lock (apilock) { dataaddress = value; } } }
        public int StatusAddress { get { lock (apilock) { return dataaddress + 1; } } }
        public int ControlAddress { get { lock (apilock) { return dataaddress + 2; } } }
        IODirection datamode;
        public IODirection DataMode
        {
            get { lock (apilock) { return datamode; } }
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

        int datavalue;
        readonly object apilock = new object();

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
                return Inpout.Input8((ushort)dataaddress);
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
                Inpout.Output16((ushort)dataaddress, (ushort)data);
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
                var v = Convert.ToString(In(), 2).PadLeft(16, '0');
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
                        var v = Convert.ToString(In(), 2).PadLeft(16, '0');
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
            timer.TimeoutMillisecond(duration_ms);
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
}