// --------------------------------------------------------------
// EnvNet.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace VLab
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class EnvNet : NetworkBehaviour
    {
        [SyncVar(hook = "onvisible")]
        public bool visible = true;
        [SyncVar(hook = "onposition")]
        public Vector3 position = new Vector3();

        public new Renderer renderer;
#if VLAB
        VLNetManager netmanager;
#endif

        void Awake()
        {
            OnAwake();
        }
        public virtual void OnAwake()
        {
            renderer = gameObject.GetComponent<Renderer>();
#if VLAB
            netmanager = FindObjectOfType<VLNetManager>();
#endif
        }

        void onvisible(bool v)
        {
            OnVisible(v);
        }
        public virtual void OnVisible(bool v)
        {
            if (renderer != null)
            {
                renderer.enabled = v;
            }
            visible = v;
        }

        void onposition(Vector3 p)
        {
            OnPosition(p);
        }
        public virtual void OnPosition(Vector3 p)
        {
            transform.position = p;
            position = p;
        }

#if VLAB
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return netmanager.IsConnectionPeerType(conn, VLPeerType.VLabEnvironment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var isrebuild = false;
            var cs = netmanager.GetPeerTypeConnection(VLPeerType.VLabEnvironment);
            if (cs.Count > 0)
            {
                foreach (var c in cs)
                {
                    observers.Add(c);
                }
                isrebuild = true;
            }
            return isrebuild;
        }
#endif

    }
}