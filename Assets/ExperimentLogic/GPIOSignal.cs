﻿/*
GPIOSignal.cs is part of the Experica.
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
using Experica;
using Experica.Command;

public class GPIOSignal : ExperimentLogic
{
    protected GPIOWave gpiowave;

    protected override void OnStartExperiment()
    {
        var gpioname = GetExParam<string>("GPIO");
        switch (gpioname)
        {
            case "ParallelPort":
                // On Average 5kHz
                gpio = new ParallelPort(Config.ParallelPort0);
                break;
            case "FTDI":
                // On Average 2kHz
                //gpio = new FTDIGPIO();
                break;
            case "1208FS":
                // On Average 500Hz
                gpio = new MCCDevice();
                break;
        }
        if (gpio == null) { Debug.LogWarning($"No Valid GPIO From {gpioname}."); return; }

        gpiowave = new GPIOWave(gpio);
        var ch = GetExParam<int>("Ch");
        var wavetype = GetExParam<string>("WaveType");
        var freq = GetExParam<double>("Freq");
        var duty = GetExParam<double>("Duty");
        var rate = GetExParam<double>("FiringRate");
        switch (wavetype)
        {
            case "PWM":
                var w = new PWMWave();
                w.SetWave(freq, duty);
                gpiowave.SetBitWave(ch, wavetype, w);
                break;
            case "PoissonSpike":
                gpiowave.SetBitWave(ch, wavetype, new PoissonSpikeWave(spikerate_sps: rate));
                gpiowave.SetBitWave(ch+1, wavetype, new PoissonSpikeWave(spikerate_sps: 2 * rate));
                break;
        }
        gpiowave.StartAll();
    }

    protected override void OnExperimentStopped()
    {
        gpiowave?.StopAll();
        base.OnExperimentStopped();
    }
}
