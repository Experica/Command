/*
Timer.cs is part of the Experica.
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
using System.Diagnostics;
using System;

namespace Experica
{
    public class Timer : Stopwatch
    {
        public bool IsFrameTime = false;

        double ftatstop = 0;
        bool ftrunning = false;
        double ft0 = 0;

        public Timer(bool frametime = false)
        {
            IsFrameTime = frametime;
        }

        public new bool IsRunning
        {
            get
            {
                if (IsFrameTime) { return ftrunning; }
                else { return base.IsRunning; }
            }
        }

        public new void Start()
        {
            if (IsFrameTime)
            {
                ft0 = Time.timeAsDouble - ftatstop;
                ftrunning = true;
            }
            else { base.Start(); }
        }

        public new void Stop()
        {
            if (IsFrameTime)
            {
                ftatstop = Time.timeAsDouble - ft0;
                ftrunning = false;
            }
            else { base.Stop(); }
        }

        public new void Reset()
        {
            if (IsFrameTime)
            {
                ftatstop = 0;
                ftrunning = false;
            }
            else { base.Reset(); }
        }

        public new void Restart()
        {
            if (IsFrameTime)
            {
                ft0 = Time.timeAsDouble;
                ftrunning = true;
            }
            else { base.Restart(); }
        }

        public double ElapsedSecond
        {
            get { return IsFrameTime ? (ftrunning ? Time.timeAsDouble - ft0 : ftatstop) : Elapsed.TotalSeconds; }
        }

        public double ElapsedMillisecond
        {

            get { return ElapsedSecond * 1000; }
        }

        public double ElapsedMinute
        {
            get { return ElapsedSecond / 60; }
        }

        public double ElapsedHour
        {
            get { return ElapsedSecond / 3600; }
        }

        public void TimeoutSecond(double timeout_s)
        {
            var start = Time.realtimeSinceStartupAsDouble;
            while ((Time.realtimeSinceStartupAsDouble - start) < timeout_s)
            {
            }
        }

        public void TimeoutMillisecond(double timeout_ms)
        {
            TimeoutSecond(timeout_ms / 1000);
        }

        public TimeoutResult<Tout> TimeoutSecond<Tin, Tout>(Func<Tin, Tout> function, Tin argument, double timeout_s) where Tout : class
        {
            var start = Time.realtimeSinceStartupAsDouble;
            while ((Time.realtimeSinceStartupAsDouble - start) < timeout_s)
            {
                if (function != null)
                {
                    var r = function(argument);
                    if (r != null)
                    {
                        return new TimeoutResult<Tout>() { Result = r, ElapsedMillisecond = (Time.realtimeSinceStartupAsDouble - start) * 1000 };
                    }
                }
            }
            return new TimeoutResult<Tout>() { Result = null, ElapsedMillisecond = (Time.realtimeSinceStartupAsDouble - start) * 1000 };
        }

        public TimeoutResult<Tout> TimeoutMillisecond<Tin, Tout>(Func<Tin, Tout> function, Tin argument, double timeout_ms) where Tout : class
        {
            return TimeoutSecond(function, argument, timeout_ms / 1000);
        }
    }

    public class TimeoutResult<T> where T : class
    {
        public T Result;
        public double ElapsedMillisecond;
    }
}