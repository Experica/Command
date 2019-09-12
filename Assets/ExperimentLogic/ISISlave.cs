/*
OIMaster.cs is part of the Experica.
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
using System.Linq;

namespace Experica
{
    public class ISISlave : ConditionTestLogic
    {
        protected int condidx;
        protected bool start, go, issizeadjusted;
        protected double reversetime;
        protected Vector3 sizebeforeadjust;

        protected override void OnStart()
        {
            //recorder = new RippleRecorder();
        }

        protected override void OnStartExperiment()
        {
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("ReverseTime", false);
            var fss = ex.GetParam("FullScreenSize");
            if (fss != null && fss.Convert<bool>())
            {
                issizeadjusted = true;
                sizebeforeadjust = (Vector3)GetEnvActiveParam("Size");
                var hh = envmanager.maincamera_scene.orthographicSize;
                var hw = hh * envmanager.maincamera_scene.aspect;
                SetEnvActiveParam("Size", new Vector3(2.1f * hw, 2.1f * hh, 1));
            }
        }

        protected override void OnExperimentStopped()
        {
            SetEnvActiveParam("Visible", false);
            SetEnvActiveParam("ReverseTime", false);
            if (issizeadjusted)
            {
                SetEnvActiveParam("Size", sizebeforeadjust);
                issizeadjusted = false;
            }
        }


    }
}