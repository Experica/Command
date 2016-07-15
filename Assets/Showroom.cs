// --------------------------------------------------------------
// Showroom.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Linq;

namespace VLab
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class Showroom : NetworkBehaviour
    {
        public enum ItemID
        {
            None,
            Quad,
            GratingQuad
        }

        [SyncVar(hook = "onitemid")]
        public ItemID itemid;

        Dictionary<ItemID, GameObject> items = new Dictionary<ItemID, GameObject>();
        Dictionary<NetworkHash128, ItemID> assetidtoid = new Dictionary<NetworkHash128, ItemID>();
        Dictionary<NetworkHash128, GameObject> prefabs = new Dictionary<NetworkHash128, GameObject>();

#if VLAB
        VLUIController uicontroller;
#endif

        void Awake()
        {
#if VLAB
            uicontroller = GameObject.Find("VLUIController").GetComponent<VLUIController>();
#endif
#if VLABENVIRONMENT
            RegisterSpawnHandler();
#endif
        }

        void RegisterSpawnHandler()
        {
            var ns = Enum.GetNames(typeof(ItemID));
            for (var i = 1; i < ns.Length; i++)
            {
                var prefab = Resources.Load<GameObject>(ns[i]);
                var assetid = prefab.GetComponent<NetworkIdentity>().assetId;
                prefabs[assetid] = prefab;
                assetidtoid[assetid] = (ItemID)i;
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
        }

#if VLAB
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return uicontroller. netmanager.IsConnectionPeerType(conn, VLPeerType.VLabEnvironment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var isrebuild = false;
            var cs =uicontroller. netmanager.GetPeerTypeConnection(VLPeerType.VLabEnvironment);
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

        void onitemid(ItemID id)
        {
            OnItemID(id);
        }
        public virtual void OnItemID(ItemID id)
        {
            if (id == ItemID.None)
            {
                SetAllItemActive(false);
#if VLAB
                uicontroller.exmanager.el.envmanager.UpdateScene();
#endif
            }
            else
            {
                if (items.ContainsKey(id))
                {
                    SetAllItemActiveExceptOtherWise(id, false);
#if VLAB
                    uicontroller.exmanager.el.envmanager.UpdateScene();
#endif
                }
                else
                {
#if VLAB
                    var go = LoadItem(id);
                    uicontroller.exmanager.el.envmanager.UpdateScene();
                    uicontroller.exmanager.el.envmanager.SetParamsForObject(uicontroller.exmanager.el.ex.EnvParam,go.name);
                    uicontroller.exmanager.InheritEnv();
                    NetworkServer.Spawn(go);
#endif
                }
            }
            itemid = id;
#if VLAB
            uicontroller.UpdateEnv();
#endif
        }

        GameObject LoadItem(ItemID id)
        {
            var go = Instantiate(Resources.Load<GameObject>(id.ToString()));
            go.transform.SetParent(transform);
            go.name = id.ToString();
            items[id] = go;

            SetAllItemActiveExceptOtherWise(id, false);
            return go;
        }

        void SetItemActive(ItemID id, bool isactive)
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

        void SetAllItemActiveExcept(ItemID id, bool isactive)
        {
            foreach (var i in items.Keys)
            {
                if (i != id)
                {
                    items[i].SetActive(isactive);
                }
            }
        }

        void SetAllItemActiveExceptOtherWise(ItemID id, bool isactive)
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

    }
}