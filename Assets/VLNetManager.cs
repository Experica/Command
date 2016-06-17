// --------------------------------------------------------------
// VLNetManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Types;
using System.Collections.Generic;

namespace VLab
{
    public class VLNetManager : NetworkManager
    {
        public VLUIController uicontroller;
        public Dictionary<int, Dictionary<string, object>> peerinfo = new Dictionary<int, Dictionary<string, object>>();

        public bool IsPeerTypeConnected(VLPeerType peertype)
        {
            foreach(var pi in peerinfo.Values)
            {
                if(pi!=null&&pi.ContainsKey("peertype")&&(VLPeerType)pi["peertype"]==peertype)
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
            var cid = conn.connectionId;
            return (peerinfo.ContainsKey(cid) && peerinfo[cid].ContainsKey("peertype") && (VLPeerType)peerinfo[cid]["peertype"] == peertype);
        }

        
        /// <summary>
        /// Prepare server to handle all kinds of client messages.
        /// </summary>
        public override void OnStartServer()
        {
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
        /// for every client, we maintain information about the unique connection and 
        /// peer, so we can talk to each one specificly.
        /// </summary>
        /// <param name="netMsg"></param>
        void PeerTypeHandler(NetworkMessage netMsg)
        {
            var v = (VLPeerType)netMsg.ReadMessage<IntegerMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive PeerType Message: " + v.ToString());
            }
            var ispeertypeconnected = IsPeerTypeConnected(v);
            var connid = netMsg.conn.connectionId;
            if (peerinfo.ContainsKey(connid))
            {
                peerinfo[connid]["peertype"] = v;
            }
            else
            {
                var info = new Dictionary<string, object>();
                info["peertype"] = v;
                peerinfo[connid] = info;
            }
            // whenever a VLabAnalysis client connected to VLab, it will seed peertype message, so
            // here the function is triggered.
            // if there are VLabAnalysis already connected, then VLabAnalysisManager is already there
            // so server will automatically spwan scene and network objects(including VLabAnalysisManager) to 
            // newly conneted client. if not, then this is the first time a VLabAnalysis client connected,
            // we then create a new instance and spwan to all clients(may include VLabEnvironment, but since
            // they don't register for the VLabAnalysisManager prefab, they will spawn nothing).
            if ((v == VLPeerType.VLabAnalysis)&& (!ispeertypeconnected))
            {
                SpwanVLAnalysisManager();
            }
        }

        public void SpwanVLAnalysisManager()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("VLabAnalysisManager"));
            var als = go.GetComponent<VLAnalysisManager>();
            als.uicontroller = uicontroller;
            uicontroller.alsmanager = als;
            go.name = "VLAnalysisManager";
            go.transform.parent = transform;
            
            NetworkServer.Spawn(go);
        }

        void AspectRatioHandler(NetworkMessage netMsg)
        {
            var v = netMsg.ReadMessage<FloatMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive AspectRatio Message: " + v.ToString());
            }
            var connid = netMsg.conn.connectionId;
            if (peerinfo.ContainsKey(connid))
            {
                peerinfo[connid]["aspectratio"] = v;
            }
            else
            {
                var info = new Dictionary<string, object>();
                info["aspectratio"] = v;
                peerinfo[connid] = info;
            }
            uicontroller.viewpanel.aspectratio = v;
            uicontroller.viewpanel.UpdateView();
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            //base.OnServerConnect(conn);
            //conn.SetChannelOption(0, ChannelOption.AllowFragmentation, 1);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            //base.OnClientConnect(conn);
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            Resources.UnloadUnusedAssets();
            base.OnServerSceneChanged(sceneName);
            
            uicontroller.exmanager.el.OnServerSceneChanged(sceneName);
            uicontroller.exmanager.InheritEnv();

            uicontroller.UpdateEnv();
            uicontroller.UpdateView();
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            NetworkServer.SpawnObjects();

            base.OnServerReady(conn);
            uicontroller.exmanager.el.envmanager.PushParams();
        }
    }
}
