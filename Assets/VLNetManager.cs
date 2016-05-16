// --------------------------------------------------------------
// VLNetManager.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
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
        public Dictionary<int, VLPeerType> peertype = new Dictionary<int, VLPeerType>();

        public override void OnStartServer()
        {
            if (LogFilter.logDebug)
            {
                Debug.Log("Register PeerInfo Message Handler.");
            }
            NetworkServer.RegisterHandler(VLMsgType.PeerInfo, new NetworkMessageDelegate(PeerInfoHandler));
        }

        void PeerInfoHandler(NetworkMessage netMsg)
        {
            var v = (VLPeerType)netMsg.ReadMessage<IntegerMessage>().value;
            if (LogFilter.logDebug)
            {
                Debug.Log("Receive PeerInfo Message: " + v.ToString());
            }
            peertype[netMsg.conn.connectionId] = v;
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
            base.OnServerSceneChanged(sceneName);
            Resources.UnloadUnusedAssets();
            uicontroller.exmanager.el.UpdateScene(sceneName);
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
