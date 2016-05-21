// --------------------------------------------------------------
// Laser.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using VLab;

public class Omicron
{
    COM com;
    public double MaxPower;

    public Omicron(string comname, int baudrate = 500000, double maxpower = 0.1)
    {
        com = new COM(portname: comname, baudrate: baudrate, newline: "\r");
        MaxPower = maxpower;
    }

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

    public double Power
    {
        set
        {
            PowerRatio = value / MaxPower;
        }
    }

    public double PowerRatio
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

public class Cobolt
{
    COM com;
    public double MaxPower;

    public Cobolt(string comname, int baudrate = 115200, double maxpower = 0.1)
    {
        com = new COM(portname: comname, baudrate: baudrate, newline: "\r");
        MaxPower = maxpower;
    }

    public double Power
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

    public double PowerRatio
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
