/*
PerspCamera.cs is part of the Experica.
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
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;
using System.Collections.Generic;

namespace Experica.NetEnv
{
    public class PerspCamera : NetworkBehaviour, INetEnvCamera
    {
        public float Height => throw new NotImplementedException();

        public float Width => throw new NotImplementedException();

        public float NearPlane => throw new NotImplementedException();

        public float FarPlane => throw new NotImplementedException();

        public ulong ClientID => throw new NotImplementedException();

        public Action<INetEnvCamera> OnCameraChange { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Camera Camera => throw new NotImplementedException();

        public HDAdditionalCameraData CameraHD => throw new NotImplementedException();

        public float Aspect => throw new NotImplementedException();

        public void AskReportRpc()
        {
            throw new NotImplementedException();
        }

        public void ReportRpc(string name, float value)
        {
            throw new NotImplementedException();
        }
    }
}