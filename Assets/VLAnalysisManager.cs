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
        public void RpcNotifyExperiment(byte[] ex)
        {
        }

        [ClientRpc]
        public void RpcNotifyCondTestData(string name,byte[] value)
        {
        }

        [ClientRpc]
        public void RpcAnalysis()
        {
        }

        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return uicontroller. netmanager.IsConnectionPeerType(conn, VLPeerType.VLabAnalysis);
        }

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