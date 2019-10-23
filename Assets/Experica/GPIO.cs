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
using System;
using FTD2XX_NET;

namespace Experica
{
    public interface GPIO
    {

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
            var hr = timer.Timeout(x =>
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

    public class FTDIGPIO
    {
        FTDI FTD2XX;
        FTDI.FT_STATUS FTSTATUS;
        uint ndevice;
        FTDI.FT_DEVICE_INFO_NODE[] devices;

        uint NumBytesToSend = 0;
        uint NumBytesToRead = 0;
        uint NumBytesSent = 0;
        uint NumBytesRead = 0;
        byte[] outbuffer;
        byte[] inbuffer;

        public FTDIGPIO()
        {
            FTD2XX = new FTDI();
            outbuffer = new byte[16];
            inbuffer = new byte[16];

            if(FTD2XX.GetNumberOfDevices(ref ndevice) == FTDI.FT_STATUS.FT_OK)
            {
                if (ndevice>0)
                {
                    devices = new FTDI.FT_DEVICE_INFO_NODE[ndevice];
                   FTSTATUS= FTD2XX.GetDeviceList(devices);
                    FTSTATUS = FTD2XX.OpenByDescription(devices[0].Description);
                    Config();
                }
                else
                {
                    Debug.LogWarning("No FTDI Device Detected.");
                }
            }
            else
            {
                Debug.LogWarning("Can Not Detect FTDI Devices.");
            }
        }

        void Config(byte bitdirection=0xFF)
        {
            FTSTATUS |= FTD2XX.ResetDevice(); 
            FTSTATUS |= FTD2XX.SetTimeouts(5000, 5000);
            FTSTATUS |= FTD2XX.SetLatency(0);
            FTSTATUS |= FTD2XX.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00, 0x00);
            FTSTATUS |= FTD2XX.SetBitMode(0x00, 0x00); // Reset
            FTSTATUS |= FTD2XX.SetBitMode(bitdirection, 0x01); // Asyc Bit-Bang Mode    
            FTSTATUS |= FTD2XX.SetBaudRate(3000000);

            // Enable internal loop-back
            //Outputbuffer[NumBytesToSend++] = 0x84;
            //ftStatus = FTDIGPIO.Write(Outputbuffer, NumBytesToSend, ref NumBytesSent);
            //NumBytesToSend = 0; // Reset output buffer pointer

            //ftStatus = FTDIGPIO.GetRxBytesAvailable(ref NumBytesToRead);
            //if (NumBytesToRead!=0)
            //{
            //    Debug.LogError("Error - MPSSE receive buffer should be empty");
            //    FTDIGPIO.SetBitMode(0x00, 0x00);
            //    FTDIGPIO.Close();
            //}

            // Use 60MHz master clock (disable divide by 5)
            NumBytesToSend = 0;
            outbuffer[NumBytesToSend++] = 0x8A;
            // Turn off adaptive clocking (may be needed for ARM)
            outbuffer[NumBytesToSend++] = 0x97;
            // Disable three-phase clocking
            outbuffer[NumBytesToSend++] = 0x8D;

            FTSTATUS = FTD2XX.Write(outbuffer, NumBytesToSend, ref NumBytesSent);

            // Configure data bits low-byte of MPSSE port
            NumBytesToSend = 0;
            outbuffer[NumBytesToSend++] = 0x82;
            // Initial state all low
            outbuffer[NumBytesToSend++] = 0x00;
            // Direction all output 
            outbuffer[NumBytesToSend++] = 0xFF;
            FTSTATUS = FTD2XX.Write(outbuffer, NumBytesToSend, ref NumBytesSent);
        }

        void PinOut()
        {
            NumBytesToSend = 0;
            outbuffer[NumBytesToSend++] = 0x82;
            // Initial state all low
            outbuffer[NumBytesToSend++] = 0x00;
            // Direction all output 
            outbuffer[NumBytesToSend++] = 0xFF;
            FTSTATUS = FTD2XX.Write(outbuffer, NumBytesToSend, ref NumBytesSent);

        }

        public void Out(byte v)
        {
            outbuffer[0] = v;
            FTD2XX.Write(outbuffer, 1, ref NumBytesSent);
        }

        public byte In()
        {
            FTD2XX.Read(inbuffer, 1, ref NumBytesRead);
            return inbuffer[0];
        }

        void Close()
        {
            FTD2XX.SetBitMode(0x00, 0x00);
            FTD2XX.Close();
        }
    }
}