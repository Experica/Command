﻿/*
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
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using System.Threading;

namespace Experica.Command
{
    public class NetworkController : MonoBehaviour
    {
        //public Dictionary<int, Dictionary<string, object>> peerinfo = new Dictionary<int, Dictionary<string, object>>();
        //public GameObject vlabanalysismanagerprefab, vlabcontrolmanagerprefab;
        //HashSet<int> envconnid = new HashSet<int>();

        public AppManager appmgr;

        /// <summary>
        /// NetworkManager and NetworkController have same lifetime(as the same GameObject components),
        /// so we register events only once here when NetworkManager has initialized.
        /// </summary>
        void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnPreShutdown += OnPreShutdown;
            }
        }

        void OnPreShutdown()
        {
            var el = appmgr.exmgr.el;
            if (el != null && el.envmgr.TryGetParams(out Dictionary<string, object> ps))
            {
                el.ex.EnvParam = ps;
            }
        }

        public bool StartHostServer(bool ishost = true)
        {
            bool isstart = false;
            var nm = NetworkManager.Singleton;
            if (nm != null && !nm.IsListening)
            {
                isstart = ishost ? nm.StartHost() : nm.StartServer();
                if (isstart)
                {
                    nm.SceneManager.OnLoadEventCompleted += NetworkSceneManager_OnLoadEventCompleted;
                }
            }
            return isstart;
        }

        void OnClientConnectedCallback(ulong obj)
        {
            // When a client connected and synchronized, we force all NetworkVariables update to trigger OnValueChanged callbacks on the client.
            appmgr.exmgr.el.envmgr.RefreshParams();
        }

        void NetworkSceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            appmgr.exmgr.el.envmgr.ParseScene(sceneName);
            appmgr.exmgr.el.OnSceneReady(clientsCompleted);
            appmgr.exmgr.el.OnPlayerReady();
            appmgr.OnEnvLoadCompleted();
        }

        public void Shutdown(bool cleanscene = true)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening)
            {
                nm.SceneManager.OnLoadEventCompleted -= NetworkSceneManager_OnLoadEventCompleted;
                nm.Shutdown();
                if (cleanscene)
                {
                    // Network SceneManager will not exist when NetworkManager shutdown, so here we use UnityEngine's SceneManager to clean any loaded scene by loading an Empty scene
                    SceneManager.LoadScene(Base.EmptyScene, LoadSceneMode.Single);
                    // Clear previous scene info
                    appmgr.exmgr.el.envmgr.Clear();
                    appmgr.ui.ClearEnv();
                    appmgr.ui.ClearView();
                }
            }
        }

        public void LoadScene(string scene, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                var status = nm.SceneManager.LoadScene(scene, mode);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"Failed to load {scene} with a {nameof(SceneEventProgressStatus)}: {status}");
                }
            }
        }

        public void UnLoadScene(Scene scene)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                var status = nm.SceneManager.UnloadScene(scene);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"Failed to unload {scene} with a {nameof(SceneEventProgressStatus)}: {status}");
                }
            }
        }

        //public bool IsPeerTypeConnected(PeerType peertype, int[] excludeconns)
        //{
        //    foreach (var cid in peerinfo.Keys.Except(excludeconns))
        //    {
        //        var pi = peerinfo[cid];
        //        var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
        //        if (pi != null && pi.ContainsKey(strkey) && (PeerType)pi[strkey] == peertype)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //public List<NetworkConnection> GetPeerTypeConnection(PeerType peertype)
        //{
        //    var peertypeconnection = new List<NetworkConnection>();
        //    foreach (var c in NetworkServer.connections)
        //    {
        //        if (IsConnectionPeerType(c, peertype))
        //        {
        //            peertypeconnection.Add(c);
        //        }
        //    }
        //    return peertypeconnection;
        //}

        //public bool IsConnectionPeerType(NetworkConnection conn, PeerType peertype)
        //{
        //    if (conn == null) { return false; }
        //    var cid = conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
        //    return (peerinfo.ContainsKey(cid) && peerinfo[cid].ContainsKey(strkey) && (PeerType)peerinfo[cid][strkey] == peertype);
        //}

        ///// <summary>
        ///// Prepare server to handle all kinds of client messages.
        ///// </summary>
        //public override void OnStartServer()
        //{
        //    base.OnStartServer();
        //    NetworkServer.RegisterHandler(MsgType.PeerType, new NetworkMessageDelegate(PeerTypeHandler));
        //    NetworkServer.RegisterHandler(MsgType.AspectRatio, new NetworkMessageDelegate(AspectRatioHandler));
        //    NetworkServer.RegisterHandler(MsgType.EndSyncFrame, new NetworkMessageDelegate(EndSyncFrameHandler));
        //    envconnid.Clear();
        //}

        ///// <summary>
        ///// Peertype message is the first message received whenever a new client is connected.
        ///// </summary>
        ///// <param name="netMsg">The NetworkMessage Recieved</param>
        //void PeerTypeHandler(NetworkMessage netMsg)
        //{
        //    var pt = (PeerType)netMsg.ReadMessage<IntegerMessage>().value;
        //    if (LogFilter.logDebug)
        //    {
        //        Debug.Log("Receive PeerType Message: " + pt.ToString());
        //    }
        //    var connid = netMsg.conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.PeerType);
        //    if (!peerinfo.ContainsKey(connid))
        //    {
        //        peerinfo[connid] = new Dictionary<string, object>();
        //    }
        //    peerinfo[connid][strkey] = pt;

        //    if (pt == PeerType.Environment)
        //    {
        //        envconnid.Add(connid);
        //        uicontroller.SyncCurrentDisplayCLUT(new List<NetworkConnection> { netMsg.conn });
        //    }

        //    // if there are VLabAnalysis already connected, then VLabAnalysisManager is already there
        //    // and server will automatically spwan scene and network objects(including VLabAnalysisManager) to 
        //    // newly conneted client. if not, then this is the first time a VLabAnalysis client connected,
        //    // so we need to create a new instance of VLabAnalysisManager and spwan to all clients,
        //    // this may include VLabEnvironment, but since they doesn't register for the VLabAnalysisManager prefab,
        //    // they will spawn nothing.
        //    if ((pt == PeerType.Analysis) && (uicontroller.alsmanager == null))
        //    {
        //        SpwanVLAnalysisManager();
        //    }
        //}




    }
}