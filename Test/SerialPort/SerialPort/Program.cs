using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VLab;

namespace SerialPort
{
    class Program
    {
        static void Main(string[] args)
        {
            //var omicron = new Omicron("COM3", timeout_ms: 200);
            var cobolt = new Cobolt("COM4", timeout_ms: 200);
            //var gpio = new SerialGPIO("COM8",timeout_ms:2);

            //omicron.LaserOn();
            //Console.WriteLine("Press \"q\" to quit ...");
            //while (Console.ReadLine() != "q")
            //{
            //    Console.WriteLine("Power Ratio: " + omicron.PowerRatio * 100 + "%");
            //}
            //omicron.LaserOff();

            cobolt.LaserOn();
            Console.WriteLine("Press \"q\" to quit ...");
            while (Console.ReadLine() != "q")
            {
                cobolt.PowerRatio = 1f;
                Console.WriteLine("Power Ratio: " + cobolt.PowerRatio * 100 + "%");
            }
            cobolt.LaserOff();

            //Console.WriteLine("ver: "+gpio.Ver());
            //Console.WriteLine("DI0: "+gpio.Read(0));
            //Console.WriteLine("Press \"q\" to quit ...");
            //while (Console.ReadLine() != "q")
            //{
            //    Console.WriteLine(gpio.Read0_7());
            //}

            //omicron.Dispose();
            cobolt.Dispose();
            //gpio.Dispose();
        }
    }
}
