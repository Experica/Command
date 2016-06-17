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
        public void RpcNotifyCondTestData(string name,byte[] value)
        {
        }

        [ClientRpc]
        public void RpcNotifyAnalysis()
        {
        }

        /// <summary>
        /// whenever a client connected, server will try to spwan this network object if it exists to the client.
        /// but we want this object only talk to VLabAnalysis clients, save time and bandwidth, so when a new
        /// connection established, we check if the connection is to a relevent client,
        /// if not, excluded it from observers of this object.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return uicontroller. netmanager.IsConnectionPeerType(conn, VLPeerType.VLabAnalysis);
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
            var isrebuild = false;
            var cs = uicontroller.netmanager.GetPeerTypeConnection(VLPeerType.VLabAnalysis);
            if(cs.Count>0)
            {
                foreach (var c in cs)
                {
                    observers.Add(c);
                }
                isrebuild = true;
            }
            return isrebuild;
        }
    }
}