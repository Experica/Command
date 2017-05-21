/*
VLNetManager.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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

namespace VLab
{
    public class VLNetManager : NetworkManager
    {
        public VLUIController uicontroller;
        public Dictionary<int, Dictionary<string, object>> peerinfo = new Dictionary<int, Dictionary<string, object>>();


        public bool IsPeerTypeConnected(VLPeerType peertype, int[] excludeconns)
        {
            foreach(var cid in peerinfo.Keys.Except(excludeconns))
            {
                var pi = peerinfo[cid];var strkey = VLMsgType.MsgTypeToString(VLMsgType.PeerType);
                if(pi!=null&&pi.ContainsKey(strkey)&&(VLPeerType)pi[strkey]==peertype)
                {
                    return true;
                }
            }
            return false;
        }

        public List<NetworkConnection> GetPeerTypeConnection(VLPeerType peertype)
        {
            var peertypeconnection = new List<NetworkConnection>();
            foreach(var c in NetworkServer.connections)
            {
                if(IsConnectionPeerType(c,peertype))
                {
                    peertypeconnection.Add(c);
                }
            }
            return peertypeconnection;
        }

        public bool IsConnectionPeerType(NetworkConnection conn,VLPeerType peertype)
        {
            var cid = conn.connectionId;var strkey = VLMsgType.MsgTypeToString(VLMsgType.PeerType);
            return (peerinfo.ContainsKey(cid) && peerinfo[cid].ContainsKey(strkey) && (VLPeerType)peerinfo[cid][strkey] == peertype);
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
            NetworkServer.RegisterHandler(VLMsgType.PeerType, new NetworkMessageDelegate(PeerTypeHandler));
            if (LogFilter.logDebug)
            {
                Debug.Log("Register AspectRatio Message Handler.");
            }
            NetworkServer.RegisterHandler(VLMsgType.AspectRatio, new NetworkMessageDelegate(AspectRatioHandler));
        }

        /// <summary>
        /// Peertype message is the first message received whenever a new client is connected.
        /// </summary>
        /// <param name="netMsg"></param>
        void PeerTypeHandler(NetworkMessage netMsg)
        {
            var pt = (VLPeerType)netMsg.ReadMessage<IntegerMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive PeerType Message: " + pt.ToString());
            }
            var connid = netMsg.conn.connectionId;var strkey = VLMsgType.MsgTypeToString(VLMsgType.PeerType);
            if (!peerinfo.ContainsKey(connid))
            {
                peerinfo[connid] = new Dictionary<string, object>();
            }
            peerinfo[connid][strkey] = pt;
            // if there are VLabAnalysis already connected, then VLabAnalysisManager is already there
            // and server will automatically spwan scene and network objects(including VLabAnalysisManager) to 
            // newly conneted client. if not, then this is the first time a VLabAnalysis client connected,
            // so we need to create a new instance of VLabAnalysisManager and spwan to all clients,
            // this may include VLabEnvironment, but since they doesn't register for the VLabAnalysisManager prefab,
            // they will spawn nothing.
            if ((pt == VLPeerType.VLabAnalysis)&& (uicontroller.alsmanager==null))
            {
                SpwanVLAnalysis(netMsg.conn);
            }
        }

        public void SpwanVLAnalysis(NetworkConnection conn)
        {
            GameObject goa = Instantiate(Resources.Load<GameObject>("VLAnalysisManager"));
            var als = goa.GetComponent<VLAnalysisManager>();
            als.uicontroller = uicontroller;
            uicontroller.alsmanager = als;
            goa.name = "VLAnalysisManager";
            goa.transform.SetParent(transform, false);

            NetworkServer.Spawn(goa);

            GameObject goc = Instantiate(Resources.Load<GameObject>("VLControlManager"));
            var ctrl = goc.GetComponent<VLControlManager>();
            ctrl.uicontroller = uicontroller;
            uicontroller.ctrlmanager = ctrl;
            goc.name = "VLControlManager";
            goc.transform.SetParent(transform, false);

            //NetworkServer.Spawn(goc);
            NetworkServer.SpawnWithClientAuthority(goc, conn);
        }

        void AspectRatioHandler(NetworkMessage netMsg)
        {
            var r = netMsg.ReadMessage<FloatMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive AspectRatio Message: " + r.ToString());
            }
            var connid = netMsg.conn.connectionId;var strkey = VLMsgType.MsgTypeToString(VLMsgType.AspectRatio);
            if(!peerinfo.ContainsKey(connid))
            {
                peerinfo[connid] = new Dictionary<string, object>();
            }
            peerinfo[connid][strkey] = r;
            uicontroller.OnAspectRatioMessage(r);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            peerinfo.Remove(conn.connectionId);
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
