/*
VLTimer.cs is part of the VLAB project.
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
using System.Diagnostics;
using System;

namespace VLab
{
    public class VLTimer : Stopwatch
    {
        public double ElapsedSecond
        {
            get { return Elapsed.TotalSeconds; }
        }

        public double ElapsedMillisecond
        {

            get { return Elapsed.TotalMilliseconds; }
        }

        public void Timeout(double timeout_ms)
        {
            if (!IsRunning)
            {
                Start();
            }
            var start = ElapsedMillisecond;
            while ((ElapsedMillisecond - start) < timeout_ms)
            {
            }
        }

        public TimeoutResult Timeout<T>(Func<T, object> function, T argument, double timeout_ms = 1.0)
        {
            if (!IsRunning)
            {
                Start();
            }
            var start = ElapsedMillisecond;
            while ((ElapsedMillisecond - start) < timeout_ms)
            {
                if (function != null)
                {
                    var r = function(argument);
                    if (r != null)
                    {
                        return new TimeoutResult(r, ElapsedMillisecond - start);
                    }
                }
            }
            return new TimeoutResult(null, ElapsedMillisecond - start);
        }
    }

    public class TimeoutResult
    {
        public object Result;
        public double ElapsedMillisecond;

        public TimeoutResult(object result, double time)
        {
            Result = result;
            ElapsedMillisecond = time;
        }
    }
}