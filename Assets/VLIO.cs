// --------------------------------------------------------------
// VLIO.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.Threading;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Runtime.InteropServices;
using LibUsbDotNet;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace VLab
{
    public class YamlVLabConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            if (type == typeof(Vector3) || type == typeof(Color))
            {
                return true;
            }
            return false;
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var v = ((Scalar)parser.Current).Value.Convert( type);
            parser.MoveNext();
            return v;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            string v = value.Convert<string>();
            emitter.Emit(new Scalar(v));
        }
    }

    public static class Yaml
    {
        static Serializer serializer = new Serializer(SerializationOptions.DisableAliases);
        static Deserializer deserializer = new Deserializer();

        static Yaml()
        {
            serializer.RegisterTypeConverter(new YamlVLabConverter());
            deserializer.RegisterTypeConverter(new YamlVLabConverter());
        }

        public static void WriteYaml<T>(string path, T data)
        {
            File.WriteAllText(path, Serialize(data));
        }

        public static string Serialize<T>(T data)
        {
            var s = new StringBuilder();
            serializer.Serialize(new StringWriter(s), data);
            return s.ToString();
        }

        public static T ReadYaml<T>(string path)
        {
            using (var s = new StringReader(File.ReadAllText(path)))
            {
                return deserializer.Deserialize<T>(s);
            }
        }

        public static T Deserialize<T>(string data)
        {
            return deserializer.Deserialize<T>(new StringReader(data));
        }
    }

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

        public void BitPulse(int bit = 0, double duration = 0.001)
        {
            var timer = new VLTimer();
            SetBit(bit);
            timer.Countdown(duration);
            SetBit(bit, false);
        }

        void _BitPulse(object p)
        {
            var param = (List<object>)p;
            BitPulse((int)param[0], (double)param[1]);
        }

        public void ThreadBitPulse(int bit = 0, double duration = 0.001)
        {
            var t = new Thread(new ParameterizedThreadStart(_BitPulse));
            t.Start(new List<object>() { bit, duration });
        }

        public void BitsPulse(int[] bits, double[] durations)
        {
            if (bits != null && durations != null)
            {
                var bs = bits.Distinct().ToArray();
                if (bs.Count() == durations.Length)
                {
                    for (var i = 0; i < bs.Count(); i++)
                    {
                        BitPulse(bs[i], durations[i]);
                    }
                }
            }
        }

        public void ThreadBitsPulse(int[] bits, double[] durations)
        {
            if (bits != null && durations != null)
            {
                var bs = bits.Distinct().ToArray();
                if (bs.Count() == durations.Length)
                {
                    for (var i = 0; i < bs.Count(); i++)
                    {
                        ThreadBitPulse(bs[i], durations[i]);
                    }
                }
            }
        }
    }

    public class COM : IDisposable
    {
        public SerialPort serialport;
        public string receiveddata = "";
        SerialDataReceivedEventHandler DataReceivedEventHandler;
        SerialErrorReceivedEventHandler ErrorReceivedEventHandler;
        SerialPinChangedEventHandler PinChangedEventHandler;

        public COM(string portname = "COM1", int baudrate = 9600, Parity parity = Parity.None, int databits = 8, StopBits stopbits = StopBits.One,
            Handshake handshake = Handshake.None, int readtimeout = SerialPort.InfiniteTimeout, int writetimeout = SerialPort.InfiniteTimeout, string newline = "\n", bool isevent = false)
        {
            serialport = new SerialPort(portname, baudrate, parity, databits, stopbits);
            serialport.Handshake = handshake;
            serialport.ReadTimeout = readtimeout;
            serialport.WriteTimeout = writetimeout;
            serialport.NewLine = newline;

            if (isevent)
            {
                DataReceivedEventHandler = new SerialDataReceivedEventHandler(DataReceived);
                ErrorReceivedEventHandler = new SerialErrorReceivedEventHandler(ErrorReceived);
                PinChangedEventHandler = new SerialPinChangedEventHandler(PinChanged);
                serialport.DataReceived += DataReceivedEventHandler;
                serialport.ErrorReceived += ErrorReceivedEventHandler;
                serialport.PinChanged += PinChangedEventHandler;
            }
        }

        ~COM()
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
            Close();
            if (disposing)
            {
                serialport.Dispose();
            }
        }

        public bool IsPortExist()
        {
            var hr = false;
            foreach (var n in SerialPort.GetPortNames())
            {
                if (serialport.PortName == n)
                {
                    hr = true;
                    break;
                }
            }
            if (!hr)
            {
                Debug.Log(serialport.PortName + " does not exist.");
            }
            return hr;
        }

        public void Open()
        {
            if (IsPortExist())
            {
                if (!serialport.IsOpen)
                {
                    serialport.Open();
                }
            }
        }

        public void Close()
        {
            if (serialport.IsOpen)
            {
                serialport.Close();
            }
        }

        public string Read()
        {
            var nb = serialport.BytesToRead;
            byte[] databyte = new byte[nb];
            string data = "";
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                serialport.Read(databyte, 0, nb);
                serialport.DiscardInBuffer();
                data = serialport.Encoding.GetString(databyte);
            }
            return data;
        }

        public string ReadLine()
        {
            string data = "";
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                data = serialport.ReadLine();
                serialport.DiscardInBuffer();
            }
            return data;
        }

        public void Write(string data)
        {
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                serialport.Write(data);
            }
        }

        public void Write(byte[] data)
        {
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                serialport.Write(data, 0, data.Length);
            }
        }

        public void WriteLine(string data)
        {
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                serialport.WriteLine(data);
            }
        }

        protected virtual void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            receiveddata = serialport.ReadExisting();
        }

        protected virtual void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            switch (e.EventType)
            {
                case SerialError.Frame:
                    Debug.Log("Frame Error.");
                    break;
                case SerialError.Overrun:
                    Debug.Log("Buffer Overrun.");
                    break;
                case SerialError.RXOver:
                    Debug.Log("Input Overflow.");
                    break;
                case SerialError.RXParity:
                    Debug.Log("Input Parity Error.");
                    break;
                case SerialError.TXFull:
                    Debug.Log("Output Full.");
                    break;
            }
        }

        protected virtual void PinChanged(object sender, SerialPinChangedEventArgs e)
        {

        }
    }

    public class USB
    {

    }
}