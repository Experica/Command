using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading;
using MccDaq;

namespace Experica.Test
{
    public class MCTest
    {
        public MccBoard DaqBoard;
        public ErrorInfo ULStat;

        void config()
        {
            //First Lets make sure there's a USB-1208FS plugged in,
            short BoardNum;
            bool Boardfound = false;
            for (BoardNum = 0; BoardNum < 99; BoardNum++)
            {

                DaqBoard = new MccBoard(BoardNum);
                if (DaqBoard.BoardName.Contains("1208FS"))
                {
                    Boardfound = true;
                    DaqBoard.FlashLED();
                    break;
                }
            }

            if (Boardfound == false)
            {
                Debug.Log("No USB-1208FS found in system.  Please run InstaCal.");
            }
            else
            {
                //Configure all the digital bits to output, and initialize the array
                ULStat = DaqBoard.DConfigPort(DigitalPortType.FirstPortA, DigitalPortDirection.DigitalOut);
                if (ULStat.Value != 0) Debug.Log(ULStat.Message);

                ULStat = DaqBoard.DConfigPort(DigitalPortType.FirstPortB, DigitalPortDirection.DigitalOut);
                if (ULStat.Value != 0) Debug.Log(ULStat.Message);
            }
        }
        // A Test behaves as an ordinary method
        [Test]
        public void MCTestSimplePasses()
        {
            config();
            for (var i = 0; i < 10; i++)
            {
                ULStat = DaqBoard.DBitOut(DigitalPortType.FirstPortA,1, DigitalLogicState.High);
                //ULStat = DaqBoard.DOut(DigitalPortType.FirstPortA, byte.MaxValue);
                if (ULStat.Value != 0) Debug.Log(ULStat.Message);
                Thread.Sleep(500);
                ULStat = DaqBoard.DBitOut(DigitalPortType.FirstPortA, 1, DigitalLogicState.Low);
                //ULStat = DaqBoard.DOut(DigitalPortType.FirstPortA, byte.MinValue);
                if (ULStat.Value != 0) Debug.Log(ULStat.Message);
                Thread.Sleep(500);
            }
        }
    }
}
