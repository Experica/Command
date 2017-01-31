/*
Showroom.cs is part of the VLAB project.
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

namespace VLab
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

#if VLAB
        VLUIController uicontroller;
#endif

        void Awake()
        {
#if VLAB
            uicontroller = FindObjectOfType<VLUIController>();
#endif
            foreach (var n in typeof(EnvironmentObject).GetValue().Except(new List<string> { "None" }))
            {
                var t = transform.FindChild(n);
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
            if (ismarker)
            {
                marker.SetActive(true);
            }
            else
            {
                marker.SetActive(false);
            }
            Marker = ismarker;
#if VLAB
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
            }
            else
            {
                if (items.ContainsKey(id))
                {
                    SetAllItemActiveExceptOtherWise(id, false);
                }
            }
            Show = id;
#if VLAB
            uicontroller.exmanager.el.envmanager.UpdateScene();
            uicontroller.envpanel.UpdateEnv(uicontroller.exmanager.el.envmanager);
#endif
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

#if VLAB
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return uicontroller.netmanager.IsConnectionPeerType(conn, VLPeerType.VLabEnvironment);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var vcs = uicontroller.netmanager.GetPeerTypeConnection(VLPeerType.VLabEnvironment);
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