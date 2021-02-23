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
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
#if COMMAND
using Experica.Command;
#endif

namespace Experica
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class Showroom : NetworkBehaviour
    {
        [SyncVar(hook = "onshow")]
        public EnvironmentObject Show = EnvironmentObject.None;
        [SyncVar(hook = "onmarker")]
        public bool Marker = true;

        public GameObject marker;
        Dictionary<EnvironmentObject, GameObject> items = new Dictionary<EnvironmentObject, GameObject>();

#if COMMAND
        UIController uicontroller;
#endif

        void Awake()
        {
#if COMMAND
            uicontroller = FindObjectOfType<UIController>();
#endif
            marker = GameObject.Find("Marker");
            foreach (var n in typeof(EnvironmentObject).GetValue().Except(new List<string> { "None" }))
            {
                var t = transform.Find(n);
                if (t != null)
                {
                    items[n.Convert<EnvironmentObject>()] = t.gameObject;
                }
            }
        }

        void onmarker(bool ismarker)
        {
            OnMarker(ismarker);
        }
        public virtual void OnMarker(bool ismarker)
        {
            marker.SetActive(ismarker);
            Marker = ismarker;
#if COMMAND
            if (ismarker)
            {
                NetworkServer.Spawn(marker);
                uicontroller.exmanager.el.envmanager.ForcePushParams(marker.name);
            }
            uicontroller.exmanager.el.envmanager.ParseScene();
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
            }
            else
            {
                if (items.ContainsKey(id))
                {
                    SetAllItemActiveExceptOtherWise(id, false);
                }
            }
            Show = id;
#if COMMAND
            uicontroller.exmanager.el.envmanager.ParseScene();
            uicontroller.envpanel.UpdateEnv(uicontroller.exmanager.el.envmanager);
#endif
        }

        void SetItemActive(EnvironmentObject id, bool isactive)
        {
            if (items.ContainsKey(id))
            {
                items[id].SetActive(isactive);
#if COMMAND
                if (isactive)
                {
                    NetworkServer.Spawn(items[id]);
                    uicontroller.exmanager.el.envmanager.ForcePushParams(items[id].name);
                }
#endif
            }
        }

        void SetAllItemActive(bool isactive)
        {
            foreach (var i in items.Values)
            {
                i.SetActive(isactive);
#if COMMAND
                if (isactive)
                {
                    NetworkServer.Spawn(i);
                    uicontroller.exmanager.el.envmanager.ForcePushParams(i.name);
                }
#endif
            }
        }

        void SetAllItemActiveExcept(EnvironmentObject id, bool isactive)
        {
            foreach (var i in items.Keys.Except(new List<EnvironmentObject> { id }))
            {
                items[i].SetActive(isactive);
#if COMMAND
                if (isactive)
                {
                    NetworkServer.Spawn(items[i]);
                    uicontroller.exmanager.el.envmanager.ForcePushParams(items[i].name);
                }
#endif
            }
        }

        void SetAllItemActiveExceptOtherWise(EnvironmentObject id, bool isactive)
        {
            var activestate = false;
            foreach (var i in items.Keys)
            {
                activestate = i != id ? isactive : !isactive;
                items[i].SetActive(activestate);
#if COMMAND
                if (activestate)
                {
                    NetworkServer.Spawn(items[i]);
                    uicontroller.exmanager.el.envmanager.ForcePushParams(items[i].name);
                }
#endif
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