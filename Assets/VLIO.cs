using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Runtime.InteropServices;

public class Yaml {

	public static void WriteYaml<T>(string path, T data)
    {
        var serializer = new Serializer();
        var s = new StringBuilder();
        serializer.Serialize(new StringWriter(s), data);
        File.WriteAllText(path, s.ToString());
    }

    public static T ReadYaml<T>(string path)
    {
        using (var s = new StringReader(File.ReadAllText(path)))
        {
            var deserializer = new Deserializer();
            return deserializer.Deserialize<T>(s);
        }
    }
}

public class Inpout
{
    [DllImport("inpoutx64.dll")]
    public static extern uint IsInpOutDriverOpen();
    [DllImport("inpoutx64.dll")]
    public static extern void Out32(short PortAddress, short Data);
    [DllImport("inpoutx64.dll")]
    public static extern short Inp32(short PortAddress);

    [DllImport("inpoutx64.dll")]
    public static extern void DlPortWritePortUshort(short PortAddress, ushort Data);
    [DllImport("inpoutx64.dll")]
    public static extern ushort DlPortReadPortUshort(short PortAddress);

    [DllImport("inpoutx64.dll")]
    public static extern void DlPortWritePortUlong(int PortAddress, uint Data);
    [DllImport("inpoutx64.dll")]
    public static extern uint DlPortReadPortUlong(int PortAddress);

    [DllImport("inpoutx64.dll")]
    public static extern bool GetPhysLong(ref int PortAddress, ref uint Data);
    [DllImport("inpoutx64.dll")]
    public static extern bool SetPhysLong(ref int PortAddress, ref uint Data);

    public Inpout()
    {
        try
        {
            if(IsInpOutDriverOpen()==0)
            {
                Debug.Log("Unable to open Inpoutx64 driver.");
            }
        }
        catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }
}

public class ParallelPort : Inpout
{
    public short address;
    private bool isdataoutput;
    public bool IsDataOutput
    {
        get { return isdataoutput; }
        set
        {
            var t = value ? 0x0 : 0xFF;
            Out(0x37A, (short)t);
            isdataoutput = value;
        }
    }
    public ParallelPort(short address = 0x378, bool isdataoutput = true)
    {
        this.address = address;
        IsDataOutput = isdataoutput;
    }

    public short Inp(short address)
    {
        return Inp32(address);
    }
    public short Inp()
    {
        return Inp(address);
    }
    public byte InpByte(short address)
    {
        var t = BitConverter.GetBytes(Inp(address));
        return t[0];
    }
    public byte InpByte()
    {
        return InpByte(address);
    }

    public void Out(short address, short data)
    {
        Out32(address, data);
    }
    public void Out(short data)
    {
        Out(address, data);
    }

    public void SetDataBit(short address = 0x378, int bit = 0, bool value = true)
    {
        var t = value ? Math.Pow(2.0, bit) : 0;
        Out(address, (short)t);
    }
    public void SetDataBits(int[] bits, bool[] values, short address = 0x378)
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
                Out(address, (short)t);
            }
        }
    }

    public bool GetDataBit(short address = 0x378, int bit = 0)
    {
        var t = Convert.ToString(InpByte(address), 2).PadLeft(8, '0');
        return t[7 - bit] == '1' ? true : false;
    }
    public bool[] GetDataBits(int[] bits, short address = 0x378)
    {
        var vs = new List<bool>();
        if (bits != null)
        {
            var bs = bits.Distinct().ToArray();
            if (bs.Count() != 0)
            {
                var t = Convert.ToString(InpByte(address), 2).PadLeft(8, '0');
                foreach (var b in bs)
                {
                    vs.Add(t[7 - b] == '1' ? true : false);
                }
            }
        }
        return vs.ToArray();
    }

    public void DataBitPulse(short address = 0x378, int bit = 0, double duration = 0.001)
    {
        var timer = new Timer();
        SetDataBit(address, bit);
        timer.Countdown(duration);
        SetDataBit(address, bit, false);
    }
    void _DataBitPulse(object p)
    {
        var param = (List<object>)p;
        DataBitPulse((short)param[0], (int)param[1], (double)param[2]);
    }
    public void ThreadDataBitPulse(short address = 0x378, int bit = 0, double duration = 0.001)
    {
        var t = new Thread(new ParameterizedThreadStart(_DataBitPulse));
        var p = new List<object>();
        p.Add(address);
        p.Add(bit);
        p.Add(duration);
        t.Start(p);
    }

    public void SDataBitsPulse(int[] bits, double[] durations, short address = 0x378)
    {
        if (bits != null && durations != null)
        {
            var bs = bits.Distinct().ToArray();
            if (bs.Count() == durations.Length)
            {
                for (var i = 0; i < bs.Count(); i++)
                {
                    DataBitPulse(address, bs[i], durations[i]);
                }
            }
        }
    }
    public void PDataBitsPulse(int[] bits, double[] durations, short address = 0x378)
    {
        if (bits != null && durations != null)
        {
            var bs = bits.Distinct().ToArray();
            if (bs.Count() == durations.Length)
            {
                for (var i = 0; i < bs.Count(); i++)
                {
                    ThreadDataBitPulse(address, bs[i], durations[i]);
                }
            }
        }
    }
}

public class USB
{

}