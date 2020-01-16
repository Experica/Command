/*
DynamicShowroom.cs is part of the Experica.
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
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Linq;
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class DynamicShowroom : NetworkBehaviour
    {
        [SyncVar(hook = "onshow")]
        public EnvironmentObject Show;
        [SyncVar(hook ="onmarker")]
        public bool Marker;

        public GameObject marker;
        Dictionary<EnvironmentObject, GameObject> items = new Dictionary<EnvironmentObject, GameObject>();
        Dictionary<NetworkHash128, EnvironmentObject> assetidtoid = new Dictionary<NetworkHash128, EnvironmentObject>();
        Dictionary<NetworkHash128, GameObject> prefabs = new Dictionary<NetworkHash128, GameObject>();

#if COMMAND
        UIController uicontroller;
#endif

        void Awake()
        {
#if COMMAND
            uicontroller = FindObjectOfType<UIController>();
#endif
#if ENVIRONMENT
            RegisterSpawnHandler();
#endif
        }

        void RegisterSpawnHandler()
        {
            foreach (var n in typeof(EnvironmentObject).GetValue())
            {
                if (n == "None")
                {
                    continue;
                }
                var prefab = Resources.Load<GameObject>(n);
                var assetid = prefab.GetComponent<NetworkIdentity>().assetId;
                prefabs[assetid] = prefab;
                assetidtoid[assetid] = n.Convert<EnvironmentObject>();
                ClientScene.RegisterSpawnHandler(assetid, new SpawnDelegate(SpawnHandler), new UnSpawnDelegate(UnSpawnHandler));
            }
        }

        GameObject SpawnHandler(Vector3 position, NetworkHash128 assetId)
        {
            var go = Instantiate(prefabs[assetId]);
            go.transform.SetParent(transform);
            var id = assetidtoid[assetId];
            go.name = id.ToString();
            items[id] = go;

            SetAllItemActiveExceptOtherWise(id, false);
            return go;
        }

        void UnSpawnHandler(GameObject spawned)
        {
            Destroy(spawned);
        }

        void onmarker(bool ismarker)
        {
            OnMarker(ismarker);
        }
        public virtual void OnMarker(bool ismarker)
        {
            if(ismarker)
            {
                marker.SetActive(true);
            }
            else
            {
                marker.SetActive(false);
            }
            Marker = ismarker;
#if COMMAND
            uicontroller.exmanager.el.envmanager.UpdateScene();
            uicontroller.envpanel.UpdateEnv(uicontroller.exmanager.el.envmanager);
#endif
        }

        void onshow(EnvironmentObject id)
        {
            OnShow(id);
        }
        public virtual void OnShow(EnvironmentObject id)
        {
            if (id == EnvironmentObject.None)
            {
                SetAllItemActive(false);
#if COMMAND
                uicontroller.exmanager.el.envmanager.UpdateScene();
#endif
            }
            else
            {
                if (items.ContainsKey(id))
                {
                    SetAllItemActiveExceptOtherWise(id, false);
#if COMMAND
                    uicontroller.exmanager.el.envmanager.UpdateScene();
#endif
                }
                else
                {
#if COMMAND
                    var go = LoadItem(id);
                    uicontroller.exmanager.el.envmanager.UpdateScene();
                    uicontroller.exmanager.el.envmanager.SetParams(uicontroller.exmanager.el.ex.EnvParam, go.name);
                    uicontroller.exmanager.InheritEnv(go.name);
                    NetworkServer.Spawn(go);
                    uicontroller.exmanager.el.envmanager.ForcePushParams();
#endif
                }
            }
            Show = id;
#if COMMAND
            uicontroller.envpanel.UpdateEnv(uicontroller.exmanager.el.envmanager);
#endif
        }

        GameObject LoadItem(EnvironmentObject id)
        {
            var go = Instantiate(Resources.Load<GameObject>(id.ToString()));
            go.transform.SetParent(transform);
            go.name = id.ToString();
            items[id] = go;

            SetAllItemActiveExceptOtherWise(id, false);
            return go;
        }

        void SetItemActive(EnvironmentObject id, bool isactive)
        {
            if (items.ContainsKey(id))
            {
                items[id].SetActive(isactive);
            }
        }

        void SetAllItemActive(bool isactive)
        {
            foreach (var i in items.Values)
            {
                i.SetActive(isactive);
            }
        }

        void SetAllItemActiveExcept(EnvironmentObject id, bool isactive)
        {
            foreach (var i in items.Keys)
            {
                if (i != id)
                {
                    items[i].SetActive(isactive);
                }
            }
        }

        void SetAllItemActiveExceptOtherWise(EnvironmentObject id, bool isactive)
        {
            foreach (var i in items.Keys)
            {
                if (i != id)
                {
                    items[i].SetActive(isactive);
                }
                else
                {
                    items[i].SetActive(!isactive);
                }
            }
        }

#if COMMAND
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return uicontroller.netmanager.IsConnectionPeerType(conn, PeerType.Environment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var vcs = uicontroller.netmanager.GetPeerTypeConnection(PeerType.Environment);
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