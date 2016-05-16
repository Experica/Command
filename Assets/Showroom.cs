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

        void onitemid(ItemID id)
        {
            OnItemID(id);
        }
        public virtual void OnItemID(ItemID id)
        {
            if (id == ItemID.None)
            {
                SetAllItemActive(false);
            }
            else
            {
                if (items.ContainsKey(id))
                {
                    SetAllItemActiveExceptOtherWise(id, false);
                }
                else
                {
#if VLAB
                    LoadItem(id);
#endif
                }
            }
            itemid = id;
#if VLAB
            uicontroller.exmanager.el.envmanager.Update();
            uicontroller.exmanager.el.envmanager.SetEnvParam(uicontroller.exmanager.el.ex.envparam);

            uicontroller.UpdateEnv();
#endif
        }

        void LoadItem(ItemID id)
        {
            var go = Instantiate(Resources.Load<GameObject>(id.ToString()));
            go.transform.SetParent(transform);
            go.name = id.ToString();
            items[id] = go;

            NetworkServer.Spawn(go);
            SetAllItemActiveExceptOtherWise(id, false);
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