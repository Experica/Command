/*
IRecord.cs is part of the Experica.
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
using System;
using UnityEngine;

namespace Experica
{
    public enum StimulationStatus
    {
        None,
        Stopped,
        Stimulating,
        Paused
    }

    public struct DCPulse
    {
        /// <summary>
        /// DCPulse Amplitude (μA)
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// Duration of DCPulse (ms)
        /// </summary>
        public float Duration;
    }

    public struct ACPulse
    {
        /// <summary>
        /// ACPulse Amplitude (μA)
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// Duration of ACPulse (ms)
        /// </summary>
        public float Duration;
        /// <summary>
        /// SinWave Frequency (Hz)
        /// </summary>
        public float Frequency;
        /// <summary>
        /// SinWave Phase [0, 1]
        /// </summary>
        public float Phase;
        /// <summary>
        /// Period of the SinWave (ms)
        /// </summary>
        public float Period => 1000f / Frequency;
    }

    public struct SWPulse
    {
        /// <summary>
        /// SWPulse Amplitude (μA)
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// Duration of SWPulse (ms)
        /// </summary>
        public float Duration;
        /// <summary>
        /// SquareWave Frequency (Hz)
        /// </summary>
        public float Frequency;
        /// <summary>
        /// SquareWave Duty [0, 1]
        /// </summary>
        public float Duty;
        /// <summary>
        /// Period of the SquareWave (ms)
        /// </summary>
        public float Period => 1000f / Frequency;
        /// <summary>
        /// Duration of Pulse Phase (ms)
        /// </summary>
        public float PulseWidth => Duty * Period;
    }

    public struct BiPhasicPulse
    {
        /// <summary>
        /// Pulse Amplitude (μA) for 1st phase, 2nd phase flips to -Amplitude
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// Number of Pulses [0...]
        /// </summary>
        public int NumOfPulse;
        /// <summary>
        /// Time before Pulse (ms)
        /// </summary>
        public float PrePulseDelay;
        /// <summary>
        /// Duration of Pulse Phase (ms)
        /// </summary>
        public float PulseWidth;
        /// <summary>
        /// Delay between Phases (ms)
        /// </summary>
        public float InterPhaseInterval;
        /// <summary>
        /// Time after Pulse (ms)
        /// </summary>
        public float SufPulseDelay;

        /// <summary>
        /// Duration of a BiPhasicPulse (ms)
        /// </summary>
        public float Period => PrePulseDelay + 2 * PulseWidth + InterPhaseInterval + SufPulseDelay;
        /// <summary>
        /// Pulsing Frequency (Hz)
        /// </summary>
        public float Frequency
        {
            get { return 1000 / Period; }
            set
            {
                var p = 1000 / value;
                var d = p - PrePulseDelay - 2 * PulseWidth - InterPhaseInterval;
                if (d < 0) { Debug.LogWarning($"Frequency is higher than current BiPhasicPulse can fit, ignore setting Frequency = {value}Hz."); }
                else { SufPulseDelay = d; }
            }
        }
        /// <summary>
        /// Duration of the BiPhasicPulses (ms)
        /// </summary>
        public float Duration => NumOfPulse * Period;
        /// <summary>
        /// Charges of Pulse Phase (μC)
        /// </summary>
        public float PhaseCharge => Amplitude * PulseWidth / 1000;
    }

    public interface IStimulation : IDisposable
    {
        StimulationStatus StimulationStatus { get; }
        bool SetDCPulse(int Channel, DCPulse DCPulse);
        bool SetACPulse(int Channel, ACPulse ACPulse);
        bool SetSWPulse(int Channel, SWPulse SWPulse);
        bool SetBiPhasicPulse(int Channel, BiPhasicPulse BiPhasicPulse);
        bool StartStimulation();
        bool StopStimulation();
        bool StartImpedance();
        bool StopImpedance();
    }
}
