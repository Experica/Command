/*
VLSerialPort.cs is part of the VLAB project.
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
using System.Runtime.InteropServices;

namespace VLab
{
    public class SerialPort : IDisposable
    {
        bool disposed = false;
        public System.IO.Ports.SerialPort serialport;
        public string receiveddata = "";
        SerialDataReceivedEventHandler DataReceivedEventHandler;
        SerialErrorReceivedEventHandler ErrorReceivedEventHandler;
        SerialPinChangedEventHandler PinChangedEventHandler;

        public SerialPort(string portname = "COM1", int baudrate = 9600, Parity parity = Parity.None, int databits = 8, StopBits stopbits = StopBits.One,
            Handshake handshake = Handshake.None, int readtimeout = System.IO.Ports.SerialPort.InfiniteTimeout,
            int writetimeout = System.IO.Ports.SerialPort.InfiniteTimeout, string newline = "\n", bool isevent = false)
        {
            serialport = new System.IO.Ports.SerialPort(portname, baudrate, parity, databits, stopbits);
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

        ~SerialPort()
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
                Close();
                serialport.Dispose();
                disposed = true;
            }
        }

        public bool IsPortExist()
        {
            var hr = false;
            foreach (var n in System.IO.Ports.SerialPort.GetPortNames())
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

        public void DiscardInBuffer()
        {
            serialport.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            serialport.DiscardOutBuffer();
        }

        public string Read()
        {
            string data = "";
            if (!serialport.IsOpen)
            {
                Open();
            }
            if (serialport.IsOpen)
            {
                var nb = serialport.BytesToRead;
                if (nb > 0)
                {
                    byte[] databyte = new byte[nb];
                    serialport.Read(databyte, 0, nb);
                    data = serialport.Encoding.GetString(databyte);
                }
            }
            receiveddata += data;
            return receiveddata;
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
}