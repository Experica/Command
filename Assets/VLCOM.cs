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

}