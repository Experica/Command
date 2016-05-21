// --------------------------------------------------------------
// VLTimer.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using System.Diagnostics;

namespace VLab
{
    public class VLTimer : Stopwatch
    {
        public double ElapsedS
        {
            get { return Elapsed.TotalSeconds; }
        }

        public double ElapsedMS
        {
            get { return Elapsed.TotalMilliseconds; }
        }

        public void ReStart()
        {
            Reset();
            Start();
        }

        public void Countdown(double durationms)
        {
            if (!IsRunning)
            {
                Start();
            }
            var start = ElapsedMS;
            var end = ElapsedMS;
            while ((end - start) < durationms)
            {
                end = ElapsedMS;
            }
        }
    }
}