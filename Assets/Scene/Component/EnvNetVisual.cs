/*
EnvNetVisual.cs is part of the Experica.
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
using UnityEngine.VFX;
using System.Collections.Generic;
using UnityEngine.Networking;
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class EnvNetVisual : NetworkBehaviour
    {
        [SyncVar(hook = "onvisible")]
        public bool Visible = true;
        [SyncVar(hook = "onposition")]
        public Vector3 Position = Vector3.zero;
        [SyncVar(hook = "onpositionoffset")]
        public Vector3 PositionOffset = Vector3.zero;

        protected Renderer renderer;
        protected VisualEffect visualeffect;
#if COMMAND
        NetManager netmanager;
#endif

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            renderer = gameObject.GetComponent<Renderer>();
            visualeffect = gameObject.GetComponent<VisualEffect>();
#if COMMAND
            netmanager = FindObjectOfType<NetManager>();
#endif
        }

        void Start()
        {
            OnStart();
        }

        protected virtual void OnStart()
        {
        }

        void onvisible(bool v)
        {
            OnVisible(v);
        }
        protected virtual void OnVisible(bool v)
        {
            renderer.enabled = v;
            Visible = v;
        }

        void onposition(Vector3 p)
        {
            OnPosition(p);
        }
        protected virtual void OnPosition(Vector3 p)
        {
            transform.localPosition = p + PositionOffset;
            Position = p;
        }

        void onpositionoffset(Vector3 poffset)
        {
            OnPositionOffset(poffset);
        }
        protected virtual void OnPositionOffset(Vector3 poffset)
        {
            transform.localPosition = Position + poffset;
            PositionOffset = poffset;
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