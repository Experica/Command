/*
Julia.cs is part of the Experica.
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
using Ice;
using QuanLan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Experica
{
    public class QuanLan_RS : IStimulation, IRecorder
    {
        int disposecount = 0;
        readonly object apilock = new();
        QuanLanInterfacePrx QuanLanI;
        Communicator communicator;
        string DeviceID;

        public QuanLan_RS(string host = "LocalHost", uint port = 9999)
        {
            if (communicator == null) { communicator = Util.initialize(); }

            var obj = communicator.stringToProxy($"QuanLan:default -h {host} -p {port}");
            QuanLanI = QuanLanInterfacePrxHelper.checkedCast(obj);
            if (QuanLanI == null)
            {
                throw new ApplicationException($"Invalid QuanLan Proxy from Host: {host}, Port: {port}");
            }
        }

        ~QuanLan_RS()
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
            if (communicator != null)
            {
                communicator.Dispose();
                communicator = null;
                QuanLanI = null;
            }
        }

        public StimulationStatus StimulationStatus { get => throw new NotImplementedException(); }
        public string DataFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RecordPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public RecordStatus RecordStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AcquisitionStatus AcquisitionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Connect(string device_id = "390024350033", int timeout = 10)
        {
            var hr = QuanLanI.connectDevice(device_id, timeout);
            if (hr) { DeviceID = device_id; } else { Debug.LogError($"Can't find QuanLan device: {device_id}"); }
            return hr;
        }

        public bool DCStimulation(DCStimulationParams ps)
        {
            return QuanLanI.setDCStimulation(ps);
        }

        public bool ACStimulation(ACStimulationParams ps)
        {
            return QuanLanI.setACStimulation(ps);
        }

        public bool SquareWaveStimulation(SquareWaveStimulationParams ps)
        {
            return QuanLanI.setSquareWaveStimulation(ps);
        }

        public bool PulseStimulation(PulseStimulationParams ps)
        {
            return QuanLanI.setPulseStimulation(ps);
        }

        public bool StartStimulation() => QuanLanI.startStimulation();

        public bool StopStimulation() => QuanLanI.stopStimulation();

        public bool SetBiPhasicPulse(int Channel, BiPhasicPulse BiPhasicPulse)
        {
            var s = new PulseStimulationParams()
            {
                channel = Channel,
                current = BiPhasicPulse.Amplitude / 1000f,
                pulseWidth = Mathf.RoundToInt(BiPhasicPulse.PulseWidth * 1000),
                pulseInterval = Mathf.RoundToInt(BiPhasicPulse.InterPhaseInterval * 1000),
                pulseWidthRatio = 1f,
                duration = BiPhasicPulse.Duration / 1000f,
                frequency = BiPhasicPulse.Frequency
            };
            return PulseStimulation(s);
        }

        public bool StartImpedance() => QuanLanI.startImpedance();

        public bool StopImpedance() => QuanLanI.stopImpedance();

        public bool StartRecordAndAcquisite() => QuanLanI.startAcquisition();


        public bool StopAcquisiteAndRecord() => QuanLanI.stopAcquisition();


        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool ReadDigitalInput(out Dictionary<int, List<double>> dintime, out Dictionary<int, List<int>> dinvalue)
        {
            throw new NotImplementedException();
        }

        public bool SetDCPulse(int Channel, DCPulse DCPulse)
        {
            var s = new DCStimulationParams()
            {
                channel = Channel,
                current = DCPulse.Amplitude / 1000f,
                duration = DCPulse.Duration / 1000f,
            };
            return DCStimulation(s);
        }

        public bool SetACPulse(int Channel, ACPulse ACPulse)
        {
            var s = new ACStimulationParams()
            {
                channel = Channel,
                current = ACPulse.Amplitude / 1000f,
                duration = ACPulse.Duration / 1000f,
                frequency = ACPulse.Frequency,
                phase = Mathf.RoundToInt(ACPulse.Phase * 360f)
            };
            return ACStimulation(s);
        }

        public bool SetSWPulse(int Channel, SWPulse SWPulse)
        {
            var s = new SquareWaveStimulationParams()
            {
                channel = Channel,
                current = SWPulse.Amplitude / 1000f,
                duration = SWPulse.Duration / 1000f,
                frequency = SWPulse.Frequency,
                duty = SWPulse.Duty
            };
            return SquareWaveStimulation(s);
        }
    }

}