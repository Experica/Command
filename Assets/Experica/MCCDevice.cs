/*
MCCDevice.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using MccDaq;

namespace Experica
{
    /// <summary>
    /// Measurement Computing Device
    /// </summary>
    public class MCCDevice : IGPIO
    {
        #region IDisposable
        int disposecount = 0;

        ~MCCDevice()
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

        MccBoard DaqBoard;
        ErrorInfo ErrorInfo;
        DigitalPortType DPortType;
        readonly object apilock = new();

        public MCCDevice(string devicename = "1208FS", DigitalPortType dport = DigitalPortType.FirstPortA, IODirection direction = IODirection.Output)
        {
            for (var BoardNum = 0; BoardNum < 99; BoardNum++)
            {
                DaqBoard = new MccBoard(BoardNum);
                if (DaqBoard.BoardName.Contains(devicename))
                {
                    Found = true;
                    break;
                }
            }

            if (Found)
            {
                DConfig(dport, direction);
            }
            else
            {
                Debug.LogWarning($"MCC Device: {devicename} Not Found in System. Please Run MCC `InstaCal` to Check.");
            }
        }

        public void DConfig(DigitalPortType DPort, IODirection direction)
        {
            lock (apilock)
            {
                DPortType = DPort;
                ErrorInfo = DaqBoard.DConfigPort(DPortType, direction == IODirection.Output ? DigitalPortDirection.DigitalOut : DigitalPortDirection.DigitalIn);
                if (ErrorInfo.Value != ErrorInfo.ErrorCode.NoErrors) { Debug.LogError(ErrorInfo.Message); }
            }
        }

        public byte In()
        {
            lock (apilock)
            {
                ErrorInfo = DaqBoard.DIn(DPortType, out ushort value);
                if (ErrorInfo.Value != ErrorInfo.ErrorCode.NoErrors) { Debug.LogError(ErrorInfo.Message); }
                return Convert.ToByte(value);
            }
        }

        public void Out(byte value)
        {
            lock (apilock)
            {
                ErrorInfo = DaqBoard.DOut(DPortType, value);
                if (ErrorInfo.Value != ErrorInfo.ErrorCode.NoErrors) { Debug.LogError(ErrorInfo.Message); }
            }
        }

        public void BitOut(int bit, bool value)
        {
            lock (apilock)
            {
                ErrorInfo = DaqBoard.DBitOut(DPortType, bit, value ? DigitalLogicState.High : DigitalLogicState.Low);
                if (ErrorInfo.Value != ErrorInfo.ErrorCode.NoErrors) { Debug.LogError(ErrorInfo.Message); }
            }
        }

        public void BitPulse(int bit, double duration_ms, double delay_ms = 0, bool ispositivepulse = true)
        {
            throw new NotImplementedException();
        }

        public bool Found { get; }

        public double MaxFreq => 1e4;
    }
}