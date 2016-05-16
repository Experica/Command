// --------------------------------------------------------------
// Timer.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using System.Diagnostics;

public class Timer : Stopwatch
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
