/*
ExperimentSession.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experica.Command
{
    /// <summary>
    /// Holds all information that define an experiment session
    /// </summary>
    public class ExperimentSession
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Designer { get; set; } = "";
        public string Experimenter { get; set; } = "";
        public string Log { get; set; } = "";

        public string LogicPath { get; set; } = "";
        public double ReadyWait { get; set; } = 5000;
        public double StopWait { get; set; } = 5000;
        public Dictionary<string, object> Param { get; set; } = new Dictionary<string, object>();

        public bool NotifyExperimenter { get; set; } = true;
        public bool IsFullScreen { get; set; } = false;
        public bool IsFullViewport { get; set; } = false;
        public bool IsGuideOn { get; set; } = true;
    }
}