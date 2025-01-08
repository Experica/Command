/*
Showroom.cs is part of the Experica.
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
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Experica.NetEnv
{
    public class Showroom : NetworkBehaviour
    {
        public NetworkVariable<NetEnvObject> Show = new(NetEnvObject.None);

        NetworkObject no;
        dynamic appmgr;

#if COMMAND
        void Awake()
        {
            appmgr = GameObject.FindWithTag("AppManager").GetComponent("AppManager");
        }


        public override void OnNetworkSpawn()
        {
            Show.OnValueChanged += OnShow;
            // since default of `Show` is None, so here we don't need to spawn any NetEnvObject but still has complete spawned state
        }

        public override void OnNetworkDespawn()
        {
            Show.OnValueChanged -= OnShow;
        }

        protected virtual void OnShow(NetEnvObject p, NetEnvObject c)
        {
            if (p == c) { return; }
            NetEnvManager envmgr = appmgr.exmgr.el.envmgr;
            if (no != null) { envmgr.Despawn(no); no = null; }
            if (c != NetEnvObject.None)
            {
                no = envmgr.Spawn<NetworkObject>($"Assets/NetEnv/Object/{c}.prefab"); // spawn under root
                envmgr.ParseGameObject(no.gameObject);
                // Try init EnvParam for this object
                envmgr.SetParamsByGameObject(appmgr.exmgr.el.ex.EnvParam, no.gameObject.name);
                // Try inherit from current history
                appmgr.exmgr.InheritEnv();
            }
            appmgr.ui.UpdateEnv();
        }
#endif
    }
}