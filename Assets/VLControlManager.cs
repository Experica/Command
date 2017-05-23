using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace VLab
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class VLControlManager : NetworkBehaviour
    {
        public VLUIController uicontroller;

        [Command]
        public void CmdRF()
        {
            Debug.Log("Control back");
        }

#if VLAB
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            var b = uicontroller.netmanager.IsConnectionPeerType(conn, VLPeerType.VLabAnalysis);
            return b;
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var acs = uicontroller.netmanager.GetPeerTypeConnection(VLPeerType.VLabAnalysis);
            if (acs.Count > 0)
            {
                foreach (var c in acs)
                {
                    observers.Add(c);
                }
                return true;
            }
            return false;
        }
#endif

    }
}
