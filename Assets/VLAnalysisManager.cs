/*
VLAnalysisManager.cs is part of the VLAB project.
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
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Linq;

namespace VLab
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class VLAnalysisManager : NetworkBehaviour
    {
        public VLUIController uicontroller;

        [ClientRpc]
        public void RpcNotifyStartExperiment()
        {
        }

        [ClientRpc]
        public void RpcNotifyStopExperiment()
        {
        }

        [ClientRpc]
        public void RpcNotifyPauseExperiment()
        {
        }

        [ClientRpc]
        public void RpcNotifyResumeExperiment()
        {
        }

        [ClientRpc]
        public void RpcNotifyExperiment(byte[] ex)
        {
        }

        [ClientRpc]
        public void RpcNotifyCondTest(CONDTESTPARAM name, byte[] value)
        {
        }

        [ClientRpc]
        public void RpcNotifyCondTestEnd(double time)
        {
        }

        /// <summary>
        /// whenever a client connected, server will try to spwan this network object to the client.
        /// but we want this object only talk to VLabAnalysis clients, save time and bandwidth, so when a new
        /// connection established, we check if the connection is to a relevent client,
        /// if not, excluded it from observers of this object.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            var b = uicontroller.netmanager.IsConnectionPeerType(conn, VLPeerType.VLabAnalysis);
            //if(b) GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
            return b;
        }

        /// <summary>
        /// whenever server spwan this object to all clients, there may be some other type of clients like VLabEnvironment
        /// marked as listening clients, to keep this object communicate with only VLabAnalysis clients, we rebuild observers
        /// for this object after spwan, exclude any other type of clients, so that any further communication is kept between
        /// VLab and VLabAnalysis.
        /// </summary>
        /// <param name="observers"></param>
        /// <param name="initialize"></param>
        /// <returns></returns>
        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var acs = uicontroller.netmanager.GetPeerTypeConnection(VLPeerType.VLabAnalysis);
            if (acs.Count > 0)
            {
                foreach (var c in acs)
                {
                    observers.Add(c);
                    //GetComponent<NetworkIdentity>().AssignClientAuthority(c);
                }
                return true;
            }
            return false;
        }
    }
}