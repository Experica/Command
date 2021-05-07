/*
GPIODigitalWave.cs is part of the Experica.
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

public class GPIODigitalWave : ExperimentLogic
{
    protected IGPIO gpio;
    protected GPIOWave gpiowave;

    protected override void OnStartExperiment()
    {
        var gpioname = (string)ex.GetParam("GPIO");
        var wavetype = ex.GetParam("WaveType").Convert<DigitalWaveType>();
        var freq = ex.GetParam("Freq").Convert<float>();
        switch (gpioname)
        {
            case "ParallelPort":
                // On Average 5kHz
                gpio = new ParallelPort(config.ParallelPort1);
                break;
            case "FTDI":
                // On Average 2kHz
                gpio = new FTDIGPIO();
                break;
            case "1208FS":
                // On Average 500Hz
                //gpio = new MCCDevice();
                break;
        }
        if (gpio != null)
        {
            gpiowave = new GPIOWave(gpio);
            switch (wavetype)
            {
                case DigitalWaveType.PWM:
                    gpiowave.SetBitWave(0, freq);
                    break;
                case DigitalWaveType.PoissonSpike:
                    gpiowave.SetBitWave(0, 50, 2, 2, 0, 0);
                    gpiowave.SetBitWave(1, 100, 2, 2, 0, 0);
                    break;
            }
            gpiowave.StartAll();
        }
        else
        {
            Debug.LogWarning("No Valid GPIO.");
        }
    }

    protected override void OnStopExperiment()
    {
        gpiowave?.StopAll();
        gpio?.Dispose();
    }
}
