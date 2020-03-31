/*
MFIO.cs is part of the Experica.
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
//using MccDaq;

namespace Experica
{
    public interface IMFIO : IDisposable
    {

    }

    /// <summary>
    /// Measurement Computing Device
    /// </summary>
    //public class MCCDevice : IGPIO, IMFIO
    //{
    //    #region IDisposable
    //    int disposecount = 0;

    //    ~MCCDevice()
    //    {
    //        Dispose(false);
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (1 == Interlocked.Exchange(ref disposecount, 1))
    //        {
    //            return;
    //        }
    //        if (disposing)
    //        {
    //        }
    //    }
    //    #endregion
    //    MccBoard DaqBoard;
    //    ErrorInfo ULStat;
    //    DigitalPortType lastconfigDport;

    //    public MCCDevice(string devicename = "1208FS", int port = 10, byte direction = byte.MaxValue)
    //    {
    //        for (var BoardNum = 0; BoardNum < 99; BoardNum++)
    //        {

    //            DaqBoard = new MccBoard(BoardNum);
    //            if (DaqBoard.BoardName.Contains(devicename))
    //            {
    //                Found = true;
    //                break;
    //            }
    //        }

    //        if (Found == false)
    //        {
    //            Debug.LogWarning($"No {devicename} Found in System. Please Run InstaCal to Check.");
    //        }
    //        else
    //        {
    //            Config(port, direction);
    //        }
    //    }

    //    public void Config(int Port, byte Direction = byte.MaxValue)
    //    {
    //        lastconfigDport = (DigitalPortType)Port;
    //        ULStat = DaqBoard.DConfigPort(lastconfigDport, DigitalPortDirection.DigitalOut);
    //        if (ULStat.Value != 0)
    //        {
    //            Debug.LogWarning(ULStat.Message);
    //        }
    //    }

    //    public bool Found { get; }

    //    public void Out(byte value)
    //    {
    //        ULStat = DaqBoard.DOut(lastconfigDport, value);
    //        if (ULStat.Value != 0) Debug.LogWarning(ULStat.Message);
    //    }

    //    public void BitOut(int bit, bool value)
    //    {
    //        ULStat = DaqBoard.DBitOut(lastconfigDport, bit + 1, value ? DigitalLogicState.High : DigitalLogicState.Low);
    //        if (ULStat.Value != 0) Debug.LogWarning(ULStat.Message);
    //    }

    //    public void BitPulse(int bit, double duration_ms = 1)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public byte In()
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}