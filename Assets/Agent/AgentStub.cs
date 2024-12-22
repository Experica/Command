/*
NetworkController.cs is part of the Experica.
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
using System;
using System.Threading;
using Ice;
using System.Threading.Tasks;

namespace Experica.Command
{
    public class AgentStub : AgentDisp_
    {
        public AppManager appmgr;
        Communicator communicator;

        public AgentStub(AppManager appManager)
        {
            appmgr = appManager;
        }

        public override bool getEnvBool(string name, Current current = null)
        {
            return GetEnvParamAtUnityMainThread<bool>(name);
        }

        public override bool setEnvBool(string name, bool value, Current current = null)
        {
            return SetEnvParamAtUnityMainThread(name, value);
        }

        public override float getEnvFloat(string name, Current current = null)
        {
            return GetEnvParamAtUnityMainThread<float>(name);
        }

        public override bool setEnvFloat(string name, float value, Current current = null)
        {
            return SetEnvParamAtUnityMainThread(name, value);
        }

        T GetEnvParamAtUnityMainThread<T>(string name)
        {
            var ar = Task<T>.Factory.StartNew(() => appmgr.exmgr.el.GetEnvParam<T>(name), CancellationToken.None, TaskCreationOptions.None, appmgr.unitymainthreadscheduler);
            return ar.Result;
        }

        bool SetEnvParamAtUnityMainThread(string name, object value)
        {
            var ar = Task<bool>.Factory.StartNew(() => appmgr.exmgr.el.SetEnvParam(name, value), CancellationToken.None, TaskCreationOptions.None, appmgr.unitymainthreadscheduler);
            return ar.Result;
        }

        public void StartStopAgentStub(bool isstart)
        {
            if (isstart)
            {
                if (communicator == null)
                {
                    communicator = Util.initialize();
                    var adapter = communicator.createObjectAdapterWithEndpoints("Experica", $"default -h localhost -p 8888");
                    adapter.add(this, Util.stringToIdentity("Agent"));
                    adapter.activate();
                }
            }
            else
            {
                if (communicator != null)
                {
                    communicator.Dispose();
                    communicator = null;
                }
            }

        }

    }
}