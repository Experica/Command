using Experica.Command;
using NUnit.Framework;
using QuanLan;
using System.Collections.Generic;
using UnityEngine;
using Experica;

namespace Experica.Test
{
    public class QuanLanTest
    {
        QuanLan_RS ql = new();

        [Test]
        public void QuanLanDevice()
        {
            string device_id = "390024350033";
            ql.Connect(device_id);
        }

        [Test]
        public void Stimulation()
        {
            var ch0 = new DCStimulationParams() { channel = 0 ,duration=10};
            ql.DCStimulation(ch0);
            var ch1 = new ACStimulationParams() { channel = 1 };
            ql.ACStimulation(ch1);
            var ch2 = new SquareWaveStimulationParams() { channel = 2 };
            ql.SquareWaveStimulation(ch2);
            var ch3 = new PulseStimulationParams() { channel = 3 };
            ql.PulseStimulation(ch3);

            ql.StartStimulation();
        }


    }
}