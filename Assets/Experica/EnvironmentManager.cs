/*
EnvironmentManager.cs is part of the Experica.
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
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace Experica
{
    public class EnvironmentManager
    {
        Scene scene;
        public Camera maincamera_scene;
        public Dictionary<string, GameObject> sceneobject = new Dictionary<string, GameObject>();
        public Dictionary<string, NetworkBehaviour> networkbehaviour_sceneobject = new Dictionary<string, NetworkBehaviour>();
        public Dictionary<string, PropertyAccess> syncvar_nb_so = new Dictionary<string, PropertyAccess>();
        public Dictionary<string, MethodAccess> clientrpc_nb_so = new Dictionary<string, MethodAccess>();

        public List<string> active_networkbehaviour = new List<string>();
        public Action<string, object> OnNotifyUI;

        public void ParseScene(string scenename)
        {
            ParseScene(SceneManager.GetSceneByName(scenename));
        }

        public void ParseScene(Scene scene)
        {
            if (scene.IsValid())
            {
                this.scene = scene;
                ParseScene();
            }
            else
            {
                Debug.LogError($"Scene: {scene.name} Is Not Valid.");
            }
        }

        public void ParseScene()
        {
            sceneobject.Clear();
            networkbehaviour_sceneobject.Clear();
            syncvar_nb_so.Clear();
            clientrpc_nb_so.Clear();
            active_networkbehaviour.Clear();
            maincamera_scene = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                ParseSceneObject(go);
            }
        }

        public void ParseSceneObject(GameObject go, string parent = null)
        {
            var goname = string.IsNullOrEmpty(parent) ? go.name : go.name + "$" + parent;
            sceneobject[goname] = go;
            if (go.tag == "MainCamera")
            {
                maincamera_scene = go.GetComponent<UnityEngine.Camera>();
            }
            foreach (var nb in go.GetComponents<NetworkBehaviour>())
            {
                var nbname = nb.GetType().Name + "@" + goname;
                networkbehaviour_sceneobject[nbname] = nb;
                ParseNetworkBehaviour(nb, nbname);
                if (nb.isActiveAndEnabled)
                {
                    active_networkbehaviour.Add(nbname);
                }
            }
            for (var i = 0; i < go.transform.childCount; i++)
            {
                ParseSceneObject(go.transform.GetChild(i).gameObject, goname);
            }
        }

        public void ParseNetworkBehaviour(NetworkBehaviour nb, string nbname)
        {
            // var nbtype = nb.GetType();
            // foreach (var f in nbtype.GetFields())
            // {
            //     if (f.IsDefined(typeof(SyncVarAttribute), true))
            //     {
            //         syncvar_nb_so[f.Name + "@" + nbname] = new PropertyAccess(nbtype, "Network" + f.Name);
            //     }
            // }
            // foreach (var m in nbtype.GetMethods())
            // {
            //     if (m.Name.StartsWith("CallRpc"))
            //     {
            //         clientrpc_nb_so[m.Name.Substring(4) + "@" + nbname] = new MethodAccess(nbtype, m.Name);
            //     }
            // }
        }

        public void UpdateActiveNetworkBehaviour()
        {
            foreach (var nbname in networkbehaviour_sceneobject.Keys)
            {
                if (networkbehaviour_sceneobject[nbname].isActiveAndEnabled)
                {
                    if (!active_networkbehaviour.Contains(nbname))
                    {
                        active_networkbehaviour.Add(nbname);
                    }
                }
                else
                {
                    if (active_networkbehaviour.Contains(nbname))
                    {
                        active_networkbehaviour.Remove(nbname);
                    }
                }
            }
        }

        public object InvokeActiveRPC(string name, object[] param, bool notifyui = false)
        {
            object r = null;
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (active_networkbehaviour.Contains(nbsoname) && clientrpc_nb_so.ContainsKey(fullname))
                {
                    r = InvokeRPC(networkbehaviour_sceneobject[nbsoname], clientrpc_nb_so[fullname], param, fullname, notifyui);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in active_networkbehaviour)
                    {
                        var fname = name + "@" + nbso;
                        if (clientrpc_nb_so.ContainsKey(fname))
                        {
                            r = InvokeRPC(networkbehaviour_sceneobject[nbso], clientrpc_nb_so[fname], param, fname, notifyui);
                        }
                    }
                }
            }
            return r;
        }

        public object InvokeRPC(string name, object[] param, bool notifyui = false)
        {
            object r = null;
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (clientrpc_nb_so.ContainsKey(fullname))
                {
                    r = InvokeRPC(networkbehaviour_sceneobject[nbsoname], clientrpc_nb_so[fullname], param, fullname, notifyui);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in networkbehaviour_sceneobject.Keys.ToArray())
                    {
                        var fname = name + "@" + nbso;
                        if (clientrpc_nb_so.ContainsKey(fname))
                        {
                            r = InvokeRPC(networkbehaviour_sceneobject[nbso], clientrpc_nb_so[fname], param, fname, notifyui);
                        }
                    }
                }
            }
            return r;
        }

        object InvokeRPC(NetworkBehaviour nb, MethodAccess m, object[] param, string fullname = "", bool notifyui = false)
        {
            object r = m.Call(nb, param);
            if (notifyui && OnNotifyUI != null && !string.IsNullOrEmpty(fullname))
            {
                OnNotifyUI(fullname, r);
            }
            return r;
        }

        public void SetParams(Dictionary<string, object> envparam, bool notifyui = false)
        {
            foreach (var p in envparam.Keys)
            {
                SetParam(p, envparam[p], notifyui);
            }
        }

        public void SetParams(Dictionary<string, object> envparam, string forsceneobjectname, bool notifyui = false)
        {
            foreach (var p in envparam.Keys)
            {
                var tail = p.LastSplitTail();
                if (tail != null && tail == forsceneobjectname)
                {
                    SetParam(p, envparam[p], notifyui);
                }
            }
        }

        public void SetActiveParam(string name, object value, bool notifyui = true)
        {
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (active_networkbehaviour.Contains(nbsoname) && syncvar_nb_so.ContainsKey(fullname))
                {
                    SetParam(networkbehaviour_sceneobject[nbsoname], syncvar_nb_so[fullname], fullname, value, notifyui);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in active_networkbehaviour)
                    {
                        var fname = name + "@" + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            SetParam(networkbehaviour_sceneobject[nbso], syncvar_nb_so[fname], fname, value, notifyui);
                        }
                    }
                }
            }
        }

        public void SetParam(string name, object value, bool notifyui = false)
        {
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (syncvar_nb_so.ContainsKey(fullname))
                {
                    SetParam(networkbehaviour_sceneobject[nbsoname], syncvar_nb_so[fullname], fullname, value, notifyui);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in networkbehaviour_sceneobject.Keys.ToArray())
                    {
                        var fname = name + "@" + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            SetParam(networkbehaviour_sceneobject[nbso], syncvar_nb_so[fname], fname, value, notifyui);
                        }
                    }
                }
            }
        }

        void SetParam(NetworkBehaviour nb, PropertyAccess p, string fullname, object value, bool notifyui = false)
        {
            object v = value.Convert(p.Type);
            p.Setter(nb, v);
            if (notifyui)
            {
                OnNotifyUI?.Invoke(fullname, v);
            }
        }

        public void ForcePushParams()
        {
            foreach (var nb in networkbehaviour_sceneobject.Values)
            {
                // nb.SetDirtyBit(uint.MaxValue);
            }
        }

        public void ForcePushParams(string forsceneobjectname)
        {
            foreach (var nbname in networkbehaviour_sceneobject.Keys)
            {
                var tail = nbname.FirstSplitTail();
                if (tail != null && tail == forsceneobjectname)
                {
                    // networkbehaviour_sceneobject[nbname].SetDirtyBit(uint.MaxValue);
                }
            }
        }

        public float? MainViewportHeight
        {
            get 
            { 
                if (maincamera_scene == null) { return null; }
                return maincamera_scene.orthographicSize * 2f;
            }
        }

        public float? MainViewportWidth
        {
            get 
            {
                if (maincamera_scene == null) { return null; }
                return maincamera_scene.orthographicSize * 2f * maincamera_scene.aspect;
            }
        }

        public Dictionary<string, object> GetParams()
        {
            var envparam = new Dictionary<string, object>();
            foreach (var p in syncvar_nb_so.Keys)
            {
                envparam[p] = GetParam(p);
            }
            return envparam;
        }

        public Dictionary<string, object> GetActiveParams(bool withshortname = false)
        {
            var envparam = new Dictionary<string, object>();
            foreach (var p in syncvar_nb_so.Keys)
            {
                var v = GetActiveParam(p);
                if (v != null) envparam[p] = v;
            }
            if (withshortname)
            {
                var shortnames = envparam.Keys.Select(i => i.FirstSplitHead());
                if (shortnames.Distinct().Count() == envparam.Keys.Count)
                {
                    envparam = envparam.ToDictionary(kv => kv.Key.FirstSplitHead(), kv => kv.Value);
                }
            }
            return envparam;
        }

        public object GetActiveParam(string name)
        {
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (active_networkbehaviour.Contains(nbsoname) && syncvar_nb_so.ContainsKey(fullname))
                {
                    return GetParam(networkbehaviour_sceneobject[nbsoname], syncvar_nb_so[fullname]);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in active_networkbehaviour)
                    {
                        var fname = name + "@" + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            return GetParam(networkbehaviour_sceneobject[nbso], syncvar_nb_so[fname]);
                        }
                    }
                }
            }
            return null;
        }

        public object GetParam(string name)
        {
            if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
            {
                if (syncvar_nb_so.ContainsKey(fullname))
                {
                    return GetParam(networkbehaviour_sceneobject[nbsoname], syncvar_nb_so[fullname]);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in networkbehaviour_sceneobject.Keys)
                    {
                        var fname = name + "@" + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            return GetParam(networkbehaviour_sceneobject[nbso], syncvar_nb_so[fname]);
                        }
                    }
                }
            }
            return null;
        }

        object GetParam(NetworkBehaviour nb, PropertyAccess p)
        {
            return p.Getter(nb);
        }

        public bool ContainsActiveParam(string name, out string fullname)
        {
            if (name.IsEnvParamFullName(out _, out string nbsoname, out fullname))
            {
                return active_networkbehaviour.Contains(nbsoname) && syncvar_nb_so.ContainsKey(fullname);
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in active_networkbehaviour)
                    {
                        var fname = name + '@' + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            fullname = fname;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ContainsActiveParam(string name)
        {
            return ContainsActiveParam(name, out _);
        }

        public bool ContainsParam(string name, out string fullname)
        {
            if (name.IsEnvParamFullName(out _, out _, out fullname))
            {
                return syncvar_nb_so.ContainsKey(fullname);
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (var nbso in networkbehaviour_sceneobject.Keys)
                    {
                        var fname = name + '@' + nbso;
                        if (syncvar_nb_so.ContainsKey(fname))
                        {
                            fullname = fname;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ContainsParam(string name)
        {
            return ContainsParam(name, out _);
        }
    }
}

