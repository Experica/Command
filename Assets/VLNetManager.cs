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

        void PeerTypeHandler(NetworkMessage netMsg)
        {
            var v = (VLPeerType)netMsg.ReadMessage<IntegerMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive PeerType Message: " + v.ToString());
            }
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
            //uicontroller.exmanager.el.envmanager.PushParams();
        }
    }
}
