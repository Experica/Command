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
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Transports.UTP;

namespace Experica.Command
{
    public class NetworkController : MonoBehaviour
    {
        //public Dictionary<int, Dictionary<string, object>> peerinfo = new Dictionary<int, Dictionary<string, object>>();
        //public GameObject vlabanalysismanagerprefab, vlabcontrolmanagerprefab;
        //HashSet<int> envconnid = new HashSet<int>();
        //int nenvsyncframe;

        public UIController uicontroller;
        public UnityTransport utp;

        public bool StartHostServer(bool ishost = true)
        {
            utp.SetConnectionData("127.0.0.0", 8800, "0.0.0.0");
            bool isstarted = false;
            if (ishost)
            {
                isstarted = NetworkManager.Singleton.StartHost();
            }
            else
            {
                isstarted = NetworkManager.Singleton.StartServer();
            }

            if (isstarted)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
            }
            return isstarted;
        }

        void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            uicontroller.OnSceneLoadEventCompleted(sceneName);
        }

        void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public void LoadScene(string scene, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var status = NetworkManager.Singleton.SceneManager.LoadScene(scene, mode);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"Failed to load {scene} with a {nameof(SceneEventProgressStatus)}: {status}");
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

        //public void SpwanVLAnalysisManager()
        //{
        //    GameObject go = Instantiate(vlabanalysismanagerprefab);
        //    var am = go.GetComponent<AnalysisManager>();
        //    am.uicontroller = uicontroller;
        //    uicontroller.alsmanager = am;
        //    go.name = "VLAnalysisManager";
        //    go.transform.SetParent(transform, false);

        //    NetworkServer.Spawn(go);
        //}

        //public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
        //{
        //    GameObject go = Instantiate(Resources.Load<GameObject>("VLControlManager"));
        //    var ctrl = go.GetComponent<ControlManager>();
        //    ctrl.uicontroller = uicontroller;
        //    uicontroller.ctrlmanager = ctrl;
        //    go.name = "VLControlManager";
        //    go.transform.SetParent(transform, false);

        //    NetworkServer.AddPlayerForConnection(conn, go, playerControllerId);
        //}

        //void AspectRatioHandler(NetworkMessage netMsg)
        //{
        //    //var r = netMsg.ReadMessage<FloatMessage>().value;
        //    var r = float.Parse(netMsg.ReadMessage<StringMessage>().value);
        //    if (LogFilter.logDebug)
        //    {
        //        Debug.Log("Receive AspectRatio Message: " + r.ToString());
        //    }
        //    var connid = netMsg.conn.connectionId; var strkey = MsgType.MsgTypeToString(MsgType.AspectRatio);
        //    if (!peerinfo.ContainsKey(connid))
        //    {
        //        peerinfo[connid] = new Dictionary<string, object>();
        //    }
        //    peerinfo[connid][strkey] = r;
        //    uicontroller.OnAspectRatioMessage(r);
        //}

        ///// <summary>
        ///// send BeginSyncFrame Msg before lateupdate where syncvars being batched by UNET
        ///// </summary>
        //public void BeginSyncFrame()
        //{
        //    if (envconnid.Count > 0)
        //    {
        //        foreach (var id in envconnid)
        //        {
        //            NetworkServer.SendToClient(id, MsgType.BeginSyncFrame, new EmptyMessage());
        //        }
        //        // mark task in SyncFrameManager lateupdate(of which the script execution order later than UNET) to end SyncFrame Msg structure
        //        uicontroller.syncmanager.endingsyncframe = true;
        //    }
        //}

        //public void EndSyncFrame()
        //{
        //    if (envconnid.Count > 0)
        //    {
        //        nenvsyncframe = envconnid.Count;
        //        foreach (var id in envconnid)
        //        {
        //            NetworkServer.SendToClient(id, MsgType.EndSyncFrame, new EmptyMessage());
        //        }
        //        uicontroller.exmanager.el.issyncingframe = true;
        //        uicontroller.exmanager.el.SyncFrameOnTime = Time.realtimeSinceStartupAsDouble;
        //    }
        //}

        //void EndSyncFrameHandler(NetworkMessage netMsg)
        //{
        //    nenvsyncframe--;
        //    if (nenvsyncframe == 0)
        //    {
        //        uicontroller.exmanager.el.issyncingframe = false;
        //    }
        //}

        ///// <summary>
        ///// Called on the server when a client disconnects. Removes the connections.
        ///// </summary>
        ///// <param name="conn">The connection to remove from the server</param>
        //public override void OnServerDisconnect(NetworkConnection conn)
        //{
        //    base.OnServerDisconnect(conn);
        //    peerinfo.Remove(conn.connectionId);
        //    envconnid.Remove(conn.connectionId);
        //}

        //public override void OnServerReady(NetworkConnection conn)
        //{
        //    base.OnServerReady(conn);
        //    // when a client loaded scene and been spawned, refresh all syncvars in the scene
        //    uicontroller.ForcePushEnv();
        //}
    }
}