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
        /// <summary>
        /// Using Frame Begin-Time Resolution (Unity `Time` only works in Unity Main Thread)
        /// </summary>
        public bool UnityFrameTime = false;
        bool running = false;
        double starttime = 0, totalelapsed = 0;

        public Timer(bool unityframetime = false)
        {
            UnityFrameTime = unityframetime;
        }

        public new bool IsRunning => UnityFrameTime ? running : base.IsRunning;

        public new void Start()
        {
            if (UnityFrameTime)
            {
                if (!running)
                {
                    starttime = Time.timeAsDouble;
                    running = true;
                }
            }
            else { base.Start(); }
        }

        public new void Stop()
        {
            if (UnityFrameTime)
            {
                if (running)
                {
                    totalelapsed += Time.timeAsDouble - starttime;
                    running = false;
                }
            }
            else { base.Stop(); }
        }

        public new void Reset()
        {
            if (UnityFrameTime)
            {
                totalelapsed = 0;
                running = false;
            }
            else { base.Reset(); }
        }

        public new void Restart()
        {
            if (UnityFrameTime)
            {
                totalelapsed = 0;
                starttime = Time.timeAsDouble;
                running = true;
            }
            else { base.Restart(); }
        }

        public double ElapsedSecond => UnityFrameTime ? (running ? (Time.timeAsDouble - starttime + totalelapsed) : totalelapsed) : Elapsed.TotalSeconds;

        public double ElapsedMillisecond => ElapsedSecond * 1000;

        public double ElapsedMinute => ElapsedSecond / 60;

        public double ElapsedHour => ElapsedSecond / 3600;

        public double ElapsedDay => ElapsedSecond / 86400;

        /// <summary>
        /// busy waiting for a period of real time
        /// </summary>
        /// <param name="timeout_s"></param>
        public void WaitSecond(double timeout_s)
        {
            // Unity `Time.realtimeSinceStartupAsDouble`use system timer which usually less precise than `Stopwatch`
            var isrunning = base.IsRunning;
            if (!isrunning) { base.Start(); }
            var start = Elapsed.TotalSeconds;
            while ((Elapsed.TotalSeconds - start) < timeout_s) { }
            if (!isrunning) { base.Stop(); }
        }

        /// <summary>
        /// busy waiting for a period of real time
        /// </summary>
        /// <param name="timeout_ms"></param>
        public void WaitMillisecond(double timeout_ms) => WaitSecond(timeout_ms / 1000);

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