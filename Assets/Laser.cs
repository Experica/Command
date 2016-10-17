/*
Laser.cs is part of the VLAB project.
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
using System;
using System.Linq;
using VLab;

public class Omicron : IDisposable
{
    private bool disposed = false;
    COM com;
    public float MaxPower;

    public Omicron(string comname, int baudrate = 500000, float maxpower = 0.1f)
    {
        com = new COM(portname: comname, baudrate: baudrate, newline: "\r");
        MaxPower = maxpower;
    }

    ~Omicron()
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
            com.Dispose();
            disposed = true;
        }
    }

    /// <summary>
    /// Turn on Laser in 3 seconds
    /// </summary>
    public void LaserOn()
    {
        com.WriteLine("?LOn");
    }

    public void LaserOff()
    {
        com.WriteLine("?LOf");
    }

    public void PowerOn()
    {
        com.WriteLine("?POn");
    }

    public void PowerOff()
    {
        com.WriteLine("?POf");
    }

    public float Power
    {
        set
        {
            PowerRatio = value / MaxPower;
        }
    }

    /// <summary>
    /// Power percentage change in 100ms
    /// </summary>
    public float PowerRatio
    {
        set
        {
            if (value >= 0 && value <= 1)
            {
                com.WriteLine("?SLP" + Convert.ToString((int)Math.Round(value * 0xFFF), 16).PadLeft(3, '0'));
            }
        }
    }
}

public class Cobolt : IDisposable
{
    private bool disposed = false;
    COM com;
    public float MaxPower;

    public Cobolt(string comname, int baudrate = 115200, float maxpower = 0.1f)
    {
        com = new COM(portname: comname, baudrate: baudrate, newline: "\r");
        MaxPower = maxpower;
    }

    ~Cobolt()
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
            com.Dispose();
            disposed = true;
        }
    }

    public float Power
    {
        set
        {
            com.WriteLine("p " + value.ToString());
        }
    }

    public void ClearFault()
    {
        com.WriteLine("cf");
    }

    public float PowerRatio
    {
        set
        {
            if (value >= 0 && value <= 1)
            {
                com.WriteLine("p " + (value * MaxPower).ToString());
            }
        }
    }
}
