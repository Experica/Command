using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class BackGround : NetworkBehaviour
    {
        [SyncVar(hook ="OnBGColor")]
        public Color BGColor = Color.gray;
        [SyncVar(hook ="OnBGSize")]
        public Vector3 BGSize = new Vector3(180,180,1);

        public Renderer renderer;
#if COMMAND
        NetManager netmanager;
#endif

        void Awake()
        {
            renderer = gameObject.GetComponent<Renderer>();
#if COMMAND
            netmanager = FindObjectOfType<NetManager>();
#endif
        }

        void OnBGColor(Color bg)
        {
            renderer.material.SetColor("col", bg);
            BGColor = bg;
        }

        void OnBGSize(Vector3 s)
        {
            transform.localScale = s;
            BGSize = s;
        }

#if COMMAND
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return netmanager.IsConnectionPeerType(conn, PeerType.Environment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var vcs = netmanager.GetPeerTypeConnection(PeerType.Environment);
            if (vcs.Count > 0)
            {
                foreach (var c in vcs)
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