/*
EnvironmentManager.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace VLab
{
    public class EnvironmentManager
    {
        public Scene scene;
        public Camera maincamera;
        public Action<string, object> OnNotifyUI;
        public Dictionary<string, GameObject> sceneobj = new Dictionary<string, GameObject>();
        public Dictionary<string, NetworkBehaviour> sceneobj_net = new Dictionary<string, NetworkBehaviour>();
        public Dictionary<string, PropertyAccess> net_syncvar = new Dictionary<string, PropertyAccess>();

        public List<string> activenet = new List<string>();

        public void AddScene(string scenename)
        {
            scene = SceneManager.GetSceneByName(scenename);
            UpdateScene();
        }

        public void UpdateScene()
        {
            sceneobj.Clear();
            sceneobj_net.Clear();
            net_syncvar.Clear();
            activenet.Clear();
            maincamera = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                sceneobj[go.name] = go;
                ParseSceneObject(go);
            }
        }

        public void ParseSceneObject(GameObject go)
        {
            if (go.tag == "MainCamera")
            {
                maincamera = go.GetComponent<Camera>();
            }
            foreach (var nb in go.GetComponents<NetworkBehaviour>())
            {
                var nbname = nb.GetType().Name + "@" + go.name;
                sceneobj_net[nbname] = nb;
                ParseSceneObjectNet(nb, nbname);
                if (nb.isActiveAndEnabled)
                {
                    activenet.Add(nbname);
                }
            }
            for (var i = 0; i < go.transform.childCount; i++)
            {
                ParseSceneObject(go.transform.GetChild(i).gameObject);
            }
        }

        public void ParseSceneObjectNet(NetworkBehaviour nb, string nbname)
        {
            var nbtype = nb.GetType();
            var fs = nbtype.GetFields();
            foreach (var f in fs)
            {
                if (f.IsDefined(typeof(SyncVarAttribute), true))
                {
                    net_syncvar[f.Name + "@" + nbname] = new PropertyAccess(nbtype, "Network" + f.Name);
                }
            }
        }

        public void SetParams(Dictionary<string, object> envparam)
        {
            foreach (var p in envparam.Keys)
            {
                SetParam(p, envparam[p]);
            }
        }

        public void SetParams(Dictionary<string, object> envparam, string forobjname)
        {
            foreach (var p in envparam.Keys)
            {
                var tail = p.LastAtSplitTail();
                if (tail != null && tail == forobjname)
                {
                    SetParam(p, envparam[p]);
                }
            }
        }

        public void SetParam(string name, object value,bool notifyui=false)
        {
            var atidx = name.IndexOf('@');
            if (atidx > 0)
            {
                if (net_syncvar.ContainsKey(name))
                {
                    SetParam(sceneobj_net[name.Substring(atidx + 1)], net_syncvar[name], value,name,notifyui);
                }
            }
            else
            {
                foreach (var sn in sceneobj_net.Keys.ToList())
                {
                    var pname = name + "@" + sn;
                    if (net_syncvar.ContainsKey(pname))
                    {
                        SetParam(sceneobj_net[sn], net_syncvar[pname], value,pname,notifyui);
                    }
                }
            }
        }

        public void SetParam(NetworkBehaviour nb, PropertyAccess p, object value,string fullname="",bool notifyui=false)
        {
            object v = value.Convert(p.Type);
            p.Setter(nb, v);
            if(notifyui && OnNotifyUI!=null)
            {
                OnNotifyUI(fullname, v);
            }
        }

        public void ForcePushParams()
        {
            foreach (var sn in sceneobj_net.Values)
            {
                sn.SetDirtyBit(uint.MaxValue);
            }
        }

        public void ForcePushParams(string forobjname)
        {
            foreach (var sn in sceneobj_net.Values)
            {
                if (sn.gameObject.name == forobjname)
                {
                    sn.SetDirtyBit(uint.MaxValue);
                }
            }
        }

        public Dictionary<string, object> GetParams()
        {
            var envparam = new Dictionary<string, object>();
            foreach (var p in net_syncvar.Keys)
            {
                envparam[p] = GetParam(p);
            }
            return envparam;
        }

        public object GetParam(string name)
        {
            var atidx = name.IndexOf('@');
            if (atidx > 0)
            {
                if (net_syncvar.ContainsKey(name))
                {
                    return GetParam(sceneobj_net[name.Substring(atidx + 1)], net_syncvar[name]);
                }
            }
            else
            {
                foreach (var sn in sceneobj_net.Keys)
                {
                    var pname = name + "@" + sn;
                    if (net_syncvar.ContainsKey(pname))
                    {
                        return GetParam(sceneobj_net[sn], net_syncvar[pname]);
                    }
                }
            }
            return null;
        }

        public static object GetParam(NetworkBehaviour nb, PropertyAccess p)
        {
            return p.Getter(nb);
        }

        public void SetActiveParam(string name, object value,bool notifyui=true)
        {
            string pn, nn;
            if (name.FirstAtSplit(out pn, out nn))
            {
                if (activenet.Contains(nn) && net_syncvar.ContainsKey(name))
                {
                    SetParam(sceneobj_net[nn], net_syncvar[name], value,name,notifyui);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(pn))
                {
                    foreach (var asn in activenet)
                    {
                        var pname = pn + "@" + asn;
                        if (net_syncvar.ContainsKey(pname))
                        {
                            SetParam(sceneobj_net[asn], net_syncvar[pname], value,pname,notifyui);
                        }
                    }
                }
            }
        }

        public object GetActiveParam(string name)
        {
            string pn, nn;
            if (name.FirstAtSplit(out pn, out nn))
            {
                if (activenet.Contains(nn) && net_syncvar.ContainsKey(name))
                {
                    return GetParam(sceneobj_net[nn], net_syncvar[name]);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(pn))
                {
                    foreach (var asn in activenet)
                    {
                        var pname = pn + "@" + asn;
                        if (net_syncvar.ContainsKey(pname))
                        {
                            return GetParam(sceneobj_net[asn], net_syncvar[pname]);
                        }
                    }
                }
            }
            return null;
        }

        public bool ContainsActiveParam(string name, out string fullname)
        {
            string pn, nn;
            if (name.FirstAtSplit(out pn, out nn))
            {
                fullname = name;
                return activenet.Contains(nn);
            }
            else
            {
                fullname = pn;
                if (!string.IsNullOrEmpty(pn))
                {
                    foreach (var asn in activenet)
                    {
                        var pname = pn + '@' + asn;
                        if (net_syncvar.ContainsKey(pname))
                        {
                            fullname = pname;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ContainsActiveParam(string name)
        {
            string fullname;
            return ContainsActiveParam(name, out fullname);
        }

        public bool ContainsParam(string name, out string fullname)
        {
            string pn, nn;
            if (name.FirstAtSplit(out pn, out nn))
            {
                fullname = name;
                return net_syncvar.ContainsKey(name);
            }
            else
            {
                fullname = pn;
                if (!string.IsNullOrEmpty(pn))
                {
                    foreach (var sn in sceneobj_net.Keys)
                    {
                        var pname = pn + '@' + sn;
                        if (net_syncvar.ContainsKey(pname))
                        {
                            fullname = pname;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ContainsParam(string name)
        {
            string fullname;
            return ContainsParam(name, out fullname);
        }

    }
}

