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
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;

namespace Experica
{
    public static class Julia
    {
        static readonly string dll = "C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll";

        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string pathName);
        //[DllImport("libjulia",CallingConvention = CallingConvention.Cdecl)]
        //static extern void jl_init__threading();
        //[DllImport("libjulia",CallingConvention = CallingConvention.Cdecl)]
        //static extern IntPtr jl_eval_string(string jlcode);
        //[DllImport("libjulia",CallingConvention = CallingConvention.Cdecl)]
        //static extern IntPtr jl_exception_occurred();

        //[DllImport("libjulia",CallingConvention = CallingConvention.Cdecl)]
        //static extern IntPtr jl_typeof_str(IntPtr value);

        //[DllImport("libjulia",CallingConvention = CallingConvention.Cdecl)]
        //static extern void jl_atexit_hook(int a);
        

        [DllImport("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void jl_init__threading();
        [DllImport("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr jl_eval_string(string jlcode);
        [DllImport("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr jl_exception_occurred();

        [DllImport("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr jl_typeof_str(IntPtr value);

        [DllImport("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin\\libjulia.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void jl_atexit_hook(int a);




        static readonly object apilock = new object();

        static Julia()
        {
            try
            {
                lock (apilock)
                {
                    //SetDllDirectory("C:\\Users\\fff00\\AppData\\Local\\Julia-1.2.0\\bin");
                    jl_init__threading();
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }


        /// <summary>
        /// This function is used to simply execute a julia script, not load functions in any way. i.e. this function
        /// cannont be called then assumed the julia functions are loaded into memory to be called with another 
        /// jl_eval_string() call, this will lead to error. This function is only used with Juila 1.1.1 and will not
        /// work with older versions.
        /// </summary>
        /// <param name="juliaDir"></param>
        /// <param name="juliaScriptDir"></param>
        public static object Run()
        {
            object v = null;
            lock (apilock)
            {
                string script = @"Base.include(Base,raw""d:\Temp\test.jl"";)";

                //string script = @"Base.include(Base,raw""" + juliaScriptDir + @""";)";

                jl_eval_string(script);
                jl_eval_string("sqrt(4.0)");

                IntPtr exception = jl_exception_occurred();

                if (exception != (IntPtr)0x0)
                {
                    string exceptionString = Marshal.PtrToStringAnsi(jl_typeof_str(jl_exception_occurred()));
                    Debug.Log(exceptionString);
                }
            }
            return v;
        }

    }

}