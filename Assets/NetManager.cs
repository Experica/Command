/*
NetManager.cs is part of the Experica.
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
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;
using System.Linq;

namespace Experica.Command
{
    public class NetManager : NetworkManager
    {
        public UIController uicontroller;
        public Dictionary<int, Dictionary<string, object>> peerinfo = new Dictionary<int, Dictionary<string, object>>();
        public GameObject analysismanagerprefab, analysismanager, controlmanagerprefab;

        public bool IsPeerTypeConnected(PeerType peertype, int[] excludeconns)
        {
            foreach (var cid in peerinfo.Keys.Except(excludeconns))
            {
                var pi = peerinfo[cid]; var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
                if (pi != null && pi.ContainsKey(strkey) && (PeerType)pi[strkey] == peertype)
                {
                    return true;
                }
            }
            return false;
        }

        public List<NetworkConnection> GetPeerTypeConnection(PeerType peertype)
        {
            var peertypeconnection = new List<NetworkConnection>();
            foreach (var c in NetworkServer.connections)
            {
                if (IsConnectionPeerType(c, peertype))
                {
                    peertypeconnection.Add(c);
                }
            }
            return peertypeconnection;
        }

        public bool IsConnectionPeerType(NetworkConnection conn, PeerType peertype)
        {
            var cid = conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
            return (peerinfo.ContainsKey(cid) && peerinfo[cid].ContainsKey(strkey) && (PeerType)peerinfo[cid][strkey] == peertype);
        }

        /// <summary>
        /// Prepare server to handle all kinds of client messages.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (LogFilter.logDebug)
            {
                Debug.Log("Register PeerType Message Handler.");
            }
            NetworkServer.RegisterHandler(MsgType.PeerType, new NetworkMessageDelegate(PeerTypeHandler));
            if (LogFilter.logDebug)
            {
                Debug.Log("Register AspectRatio Message Handler.");
            }
            NetworkServer.RegisterHandler(MsgType.AspectRatio, new NetworkMessageDelegate(AspectRatioHandler));
        }

        /// <summary>
        /// Peertype message is the first message received whenever a new client is connected.
        /// </summary>
        /// <param name="netMsg"></param>
        void PeerTypeHandler(NetworkMessage netMsg)
        {
            var pt = (PeerType)netMsg.ReadMessage<IntegerMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive PeerType Message: " + pt.ToString());
            }
            var connid = netMsg.conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
            if (!peerinfo.ContainsKey(connid))
            {
                peerinfo[connid] = new Dictionary<string, object>();
            }
            peerinfo[connid][strkey] = pt;
            // if there is Analysis already connected, then AnalysisManager is already there
            // and server will automatically spwan scene and network objects(including AnalysisManager) to 
            // newly conneted client. if not, then this is the first time a Analysis client connected,
            // so we need to create a new instance of AnalysisManager and spwan to all clients,
            // this may include Environment, but since they doesn't register for the AnalysisManager prefab,
            // they will spawn nothing.
            if ((pt == PeerType.Analysis) && (uicontroller.alsmanager == null))
            {
                SpawnAnalysisManager();
            }
        }

        public void SpawnAnalysisManager()
        {
            GameObject go = Instantiate(analysismanagerprefab);
            var am = go.GetComponent<AnalysisManager>();
            am.uicontroller = uicontroller;
            uicontroller.alsmanager = am;
            go.name = "AnalysisManager";
            go.transform.SetParent(transform, false);

            NetworkServer.Spawn(go);
            analysismanager = go;
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject go = Instantiate(controlmanagerprefab);
            var ctrl = go.GetComponent<ControlManager>();
            ctrl.uicontroller = uicontroller;
            uicontroller.ctrlmanager = ctrl;
            go.name = "ControlManager";
            go.transform.SetParent(transform, false);

            NetworkServer.AddPlayerForConnection(conn, go, playerControllerId);
        }

        void AspectRatioHandler(NetworkMessage netMsg)
        {
            var r = netMsg.ReadMessage<FloatMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive AspectRatio Message: " + r.ToString());
            }
            var connid = netMsg.conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.AspectRatio);
            if (!peerinfo.ContainsKey(connid))
            {
                peerinfo[connid] = new Dictionary<string, object>();
            }
            peerinfo[connid][strkey] = r;
            uicontroller.OnAspectRatioMessage(r);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            if (peerinfo.ContainsKey(conn.connectionId))
            {
                PeerType pt = (PeerType)peerinfo[conn.connectionId]["PeerType"];
                if (pt == PeerType.Analysis)
                {
                    Destroy(analysismanager);
                    uicontroller.alsmanager = null;
                }
                peerinfo.Remove(conn.connectionId);
            }
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            uicontroller.OnServerSceneChanged(sceneName);
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            base.OnServerReady(conn);
            uicontroller.exmanager.el.envmanager.ForcePushParams();
        }
    }
}
