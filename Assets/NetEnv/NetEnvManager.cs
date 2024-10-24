/*
NetEnvManager.cs is part of the Experica.
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
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using System.Linq;
using Experica;
using Fasterflect;
using System.Xml.Linq;

namespace Experica.NetEnv
{
    public class NetEnvManager : INetEnv
    {
        public List<INetEnvCamera> MainCamera { get; private set; } = new();
        Dictionary<string, Dictionary<string, Dictionary<string, NetworkVariableSource>>> go_nb_nv = new();
        Dictionary<string, Dictionary<string, Dictionary<string, MethodAccess>>> go_nb_rpc = new();
        Dictionary<string, GameObject> go = new();
        Dictionary<string, GameObject> active_go = new();



        public Scene Scene { get; private set; }

        public void ParseScene(string sceneName)
        {
            ParseScene(SceneManager.GetSceneByName(sceneName));
        }

        public void ParseScene(Scene scene)
        {
            Scene = scene;
            ParseScene();
        }

        public void ParseScene()
        {
            if (Scene.IsValid())
            {
                go.Clear();
                active_go.Clear();
                go_nb_nv.Clear();
                go_nb_rpc.Clear();
                MainCamera.Clear();

                foreach (var rgo in Scene.GetRootGameObjects())
                {
                    ParseGameObject(rgo);
                }
            }
            else { Debug.LogError($"Parse Invalid Scene: {Scene.name}."); }
        }

        public void ParseGameObject(GameObject cgo, string parent = null)
        {
            var goname = string.IsNullOrEmpty(parent) ? cgo.name : cgo.name + "~" + parent;
            go[goname] = cgo;
            if (cgo.activeInHierarchy) { active_go[goname] = cgo; }
            Dictionary<string, Dictionary<string, NetworkVariableSource>> nb_nv = new();
            foreach (var nb in cgo.GetComponents<NetworkBehaviour>())
            {
                var nbtype = nb.GetType();
                ParseNetworkBehaviour(nb, nbtype, out Dictionary<string, NetworkVariableSource> nv);
                if (nv.Count > 0)
                {
                    nb_nv[nbtype.Name] = nv;
                    if (nbtype.Implements<INetEnvCamera>() && cgo.CompareTag("MainCamera"))
                    {
                        MainCamera.Add((INetEnvCamera)nb);
                    }
                }
            }
            if (nb_nv.Count > 0) { go_nb_nv[goname] = nb_nv; }

            for (var i = 0; i < cgo.transform.childCount; i++)
            {
                ParseGameObject(cgo.transform.GetChild(i).gameObject, goname);
            }
        }

        public void ParseNetworkBehaviour(NetworkBehaviour nb, Type nbtype, out Dictionary<string, NetworkVariableSource> nv)
        {
            nv = new();
            foreach (var f in nbtype.GetFields())
            {
                if (f.FieldType.BaseType == typeof(NetworkVariableBase))
                {
                    nv[f.Name] = new((NetworkVariableBase)f.GetValue(nb));
                }
            }
            //foreach (var m in nbtype.GetMethods())
            //{
            //    if (m.Name.StartsWith("CallRpc"))
            //    {
            //        clientrpc_nb_so[m.Name.Substring(4) + "@" + nbname] = new MethodAccess(nbtype, m.Name);
            //    }
            //}
        }

        //public void UpdateActiveNetworkBehaviour()
        //{
        //    foreach (var nbname in nb_go.Keys)
        //    {
        //        if (nb_go[nbname].isActiveAndEnabled)
        //        {
        //            if (!active_networkbehaviour.Contains(nbname))
        //            {
        //                active_networkbehaviour.Add(nbname);
        //            }
        //        }
        //        else
        //        {
        //            if (active_networkbehaviour.Contains(nbname))
        //            {
        //                active_networkbehaviour.Remove(nbname);
        //            }
        //        }
        //    }
        //}

        //public object InvokeActiveRPC(string name, object[] param, bool notifyui = false)
        //{
        //    object r = null;
        //    if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
        //    {
        //        if (active_networkbehaviour.Contains(nbsoname) && clientrpc_nb_so.ContainsKey(fullname))
        //        {
        //            r = InvokeRPC(nb_go[nbsoname], clientrpc_nb_so[fullname], param, fullname, notifyui);
        //        }
        //    }
        //    else
        //    {
        //        if (!string.IsNullOrEmpty(name))
        //        {
        //            foreach (var nbso in active_networkbehaviour)
        //            {
        //                var fname = name + "@" + nbso;
        //                if (clientrpc_nb_so.ContainsKey(fname))
        //                {
        //                    r = InvokeRPC(nb_go[nbso], clientrpc_nb_so[fname], param, fname, notifyui);
        //                }
        //            }
        //        }
        //    }
        //    return r;
        //}

        //public object InvokeRPC(string name, object[] param, bool notifyui = false)
        //{
        //    object r = null;
        //    if (name.IsEnvParamFullName(out _, out string nbsoname, out string fullname))
        //    {
        //        if (clientrpc_nb_so.ContainsKey(fullname))
        //        {
        //            r = InvokeRPC(nb_go[nbsoname], clientrpc_nb_so[fullname], param, fullname, notifyui);
        //        }
        //    }
        //    else
        //    {
        //        if (!string.IsNullOrEmpty(name))
        //        {
        //            foreach (var nbso in nb_go.Keys.ToArray())
        //            {
        //                var fname = name + "@" + nbso;
        //                if (clientrpc_nb_so.ContainsKey(fname))
        //                {
        //                    r = InvokeRPC(nb_go[nbso], clientrpc_nb_so[fname], param, fname, notifyui);
        //                }
        //            }
        //        }
        //    }
        //    return r;
        //}

        object InvokeRPC(NetworkBehaviour nb, MethodAccess m, object[] param, string fullname = "", bool notifyui = false)
        {
            object r = m.Call(nb, param);
            //if (notifyui && OnNotifyUI != null && !string.IsNullOrEmpty(fullname))
            //{
            //    OnNotifyUI(fullname, r);
            //}
            return r;
        }






        public void SetActiveParams(Dictionary<string, object> envparam) => SetParams(envparam, true);

        public void SetParams(Dictionary<string, object> envparam, bool active = false)
        {
            foreach (var p in envparam.Keys)
            {
                SetParam(p, envparam[p], active);
            }
        }

        public void SetActiveParamsByGameObject(Dictionary<string, object> envparam, string goName) => SetParamsByGameObject(envparam, goName, true);

        public void SetParamsByGameObject(Dictionary<string, object> envparam, string goName, bool active = false)
        {
            if (envparam == null || envparam.Count == 0) { return; }
            if (envparam.Keys.First().SplitEnvParamFullName(out _))
            {
                foreach (var p in envparam.Keys)
                {
                    p.SplitEnvParamFullName(out var ns);
                    if (ns[2] == goName)
                    {
                        SetParamByFullName(ns[0], ns[1], ns[2], envparam[p], active);
                    }
                }
            }
            else
            {
                foreach (var p in envparam.Keys)
                {
                    SetParamByGameObject(p, goName, envparam[p], active);
                }
            }
        }

        public void SetActiveParam(string nvORfullName, object value) => SetParam(nvORfullName, value, true);

        public void SetParam(string nvORfullName, object value, bool active = false)
        {
            var ps = GetParamSource(nvORfullName, active);
            if (ps == null) { return; }
            ps.Value = value.Convert(ps.Type);
        }

        public void SetActiveParamByGameObject(string nvName, string goName, object value) => SetParamByGameObject(nvName, goName, value, true);

        public void SetParamByGameObject(string nvName, string goName, object value, bool active = false)
        {
            var ps = GetParamSourceByGameObject(nvName, goName, active);
            if (ps == null) { return; }
            ps.Value = value.Convert(ps.Type);
        }

        public void SetActiveParamByFullName(string nvName, string nbName, string goName, object value) => SetParamByFullName(nvName, nbName, goName, value, true);

        public void SetParamByFullName(string nvName, string nbName, string goName, object value, bool active = false)
        {
            var ps = GetParamSourceByFullName(nvName, nbName, goName, active);
            if (ps == null) { return; }
            ps.Value = value.Convert(ps.Type);
        }


        public void RefreshParamsByGameObject(string goName, bool active = false)
        {
            var nbnv = active ? (active_go.ContainsKey(goName) ? go_nb_nv[goName] : null) : (go_nb_nv.ContainsKey(goName) ? go_nb_nv[goName] : null);
            if (nbnv == null) { Debug.LogError($"Can not find{(active ? " active" : "")} GameObject by {goName}, skip param refreshing."); return; }
            foreach (var nv in nbnv.Values)
            {
                foreach (var p in nv.Values)
                {
                    p.NotifyNetworkValue();
                }
            }
        }

        public void RefreshParams(bool active = false)
        {
            var nbnvs = active ? active_go.Keys.Select(i => go_nb_nv[i]) : go_nb_nv.Values;
            foreach (var nb_nv in nbnvs)
            {
                foreach (var nv in nb_nv.Values)
                {
                    foreach (var p in nv.Values)
                    {
                        p.NotifyNetworkValue();
                    }
                }
            }
        }


        public Dictionary<string, object> GetParamsByGameObject(string goName, bool tryReduceFullName = false)
        {
            if (!go_nb_nv.ContainsKey(goName))
            {
                Debug.LogError($"Can not find gameobject: {goName}");
                return null;
            }

            var envparam = new Dictionary<string, object>();
            var nb_nv = go_nb_nv[goName];
            foreach (var nbName in nb_nv.Keys)
            {
                var nv = nb_nv[nbName];
                foreach (var nvName in nv.Keys)
                {
                    envparam[string.Join('@', nvName, nbName, goName)] = nv[nvName].Value;
                }
            }
            if (tryReduceFullName)
            {
                var nvNames = envparam.Keys.Select(i => i.FirstSplitHead());
                if (nvNames.Distinct().Count() == envparam.Count)
                {
                    envparam = new(nvNames.Zip(envparam.Values, (k, v) => KeyValuePair.Create(k, v)));
                }
            }
            return envparam;
        }

        public Dictionary<string, NetworkVariableSource> GetParamSourcesByGameObject(string goName, bool tryReduceFullName = false)
        {
            if (!go_nb_nv.ContainsKey(goName))
            {
                Debug.LogError($"Can not find gameobject: {goName}");
                return null;
            }

            var envps = new Dictionary<string, NetworkVariableSource>();
            var nb_nv = go_nb_nv[goName];
            foreach (var nbName in nb_nv.Keys)
            {
                var nv = nb_nv[nbName];
                foreach (var nvName in nv.Keys)
                {
                    envps[string.Join('@', nvName, nbName, goName)] = nv[nvName];
                }
            }
            if (tryReduceFullName)
            {
                var nvNames = envps.Keys.Select(i => i.FirstSplitHead());
                if (nvNames.Distinct().Count() == envps.Count)
                {
                    envps = new(nvNames.Zip(envps.Values, (k, v) => KeyValuePair.Create(k, v)));
                }
            }
            return envps;
        }

        public Dictionary<string, object> GetActiveParams(bool tryReduceFullName = false) => GetParams(tryReduceFullName, true);

        public Dictionary<string, object> GetParams(bool tryReduceFullName = false, bool active = false)
        {
            var envparam = new Dictionary<string, object>();
            var gonames = active ? active_go.Keys.ToArray() : go_nb_nv.Keys.ToArray();
            foreach (var goName in gonames)
            {
                var nb_nv = go_nb_nv[goName];
                foreach (var nbName in nb_nv.Keys)
                {
                    var nv = nb_nv[nbName];
                    foreach (var nvName in nv.Keys)
                    {
                        envparam[string.Join('@', nvName, nbName, goName)] = nv[nvName].Value;
                    }
                }
            }
            if (tryReduceFullName)
            {
                var nvNames = envparam.Keys.Select(i => i.FirstSplitHead());
                if (nvNames.Distinct().Count() == envparam.Count)
                {
                    envparam = new(nvNames.Zip(envparam.Values, (k, v) => KeyValuePair.Create(k, v)));
                }
            }
            return envparam;
        }

        public Dictionary<string, NetworkVariableSource> GetActiveParamSources(bool tryReduceFullName = false) => GetParamSources(tryReduceFullName, true);

        public Dictionary<string, NetworkVariableSource> GetParamSources(bool tryReduceFullName = false, bool active = false)
        {
            var envps = new Dictionary<string, NetworkVariableSource>();
            var gonames = active ? active_go.Keys.ToArray() : go_nb_nv.Keys.ToArray();
            foreach (var goName in gonames)
            {
                var nb_nv = go_nb_nv[goName];
                foreach (var nbName in nb_nv.Keys)
                {
                    var nv = nb_nv[nbName];
                    foreach (var nvName in nv.Keys)
                    {
                        envps[string.Join('@', nvName, nbName, goName)] = nv[nvName];
                    }
                }
            }
            if (tryReduceFullName)
            {
                var nvNames = envps.Keys.Select(i => i.FirstSplitHead());
                if (nvNames.Distinct().Count() == envps.Count)
                {
                    envps = new(nvNames.Zip(envps.Values, (k, v) => KeyValuePair.Create(k, v)));
                }
            }
            return envps;
        }


        public T GetActiveParam<T>(string nvORfullName) => GetParam<T>(nvORfullName, true);

        public T GetParam<T>(string nvORfullName, bool active = false)
        {
            var nv = GetParamSource(nvORfullName, active);
            if (nv == null)
            {
                Debug.LogWarning($"Using default value of {typeof(T)} : {default}.");
                return default;
            }
            else { return nv.GetValue<T>(); }
        }

        public object GetActiveParam(string nvORfullName) => GetParam(nvORfullName, true);

        public object GetParam(string nvORfullName, bool active = false)
        {
            return GetParamSource(nvORfullName, active)?.Value;
        }

        public NetworkVariable<T> GetNetworkVariable<T>(string nvORfullName, bool active = false)
        {
            return GetParamSource(nvORfullName, active)?.NetworkVariable<T>();
        }

        public NetworkVariableSource GetParamSource(string nvORfullName, bool active = false)
        {
            if (string.IsNullOrEmpty(nvORfullName)) { return null; }
            if (nvORfullName.SplitEnvParamFullName(out var ns))
            {
                return GetParamSourceByFullName(ns[0], ns[1], ns[2], active);
            }
            else
            {
                var nbnvs = active ? active_go.Keys.Select(i => go_nb_nv[i]) : go_nb_nv.Values;
                foreach (var nb_nv in nbnvs)
                {
                    foreach (var nv in nb_nv.Values)
                    {
                        if (nv.ContainsKey(nvORfullName))
                        {
                            return nv[nvORfullName];
                        }
                    }
                }
                Debug.LogError($"Can not find{(active ? " active" : "")} NetworkVariable by {nvORfullName}@?@?");
                return null;
            }
        }


        public T GetActiveParamByGameObject<T>(string nvName, string goName) => GetParamByGameObject<T>(nvName, goName, true);

        public T GetParamByGameObject<T>(string nvName, string goName, bool active = false)
        {
            var nv = GetParamSourceByGameObject(nvName, goName, active);
            if (nv == null)
            {
                Debug.LogWarning($"Using default value of {typeof(T)} : {default}.");
                return default;
            }
            else { return nv.GetValue<T>(); }
        }

        public object GetActiveParamByGameObject(string nvName, string goName) => GetParamByGameObject(nvName, goName, true);

        public object GetParamByGameObject(string nvName, string goName, bool active = false)
        {
            return GetParamSourceByGameObject(nvName, goName, active)?.Value;
        }

        public NetworkVariable<T> GetNetworkVariableByGameObject<T>(string nvName, string goName, bool active = false)
        {
            return GetParamSourceByGameObject(nvName, goName, active)?.NetworkVariable<T>();
        }

        public NetworkVariableSource GetParamSourceByGameObject(string nvName, string goName, bool active = false)
        {
            if (active ? active_go.ContainsKey(goName) : go_nb_nv.ContainsKey(goName))
            {
                var nb_nv = go_nb_nv[goName];
                foreach (var nv in nb_nv.Values)
                {
                    if (nv.ContainsKey(nvName))
                    {
                        return nv[nvName];
                    }
                }
            }
            Debug.LogError($"Can not find{(active ? " active" : "")} NetworkVariable by {nvName}@?@{goName}");
            return null;
        }


        public T GetActiveParamByFullName<T>(string nvName, string nbName, string goName) => GetParamByFullName<T>(nvName, nbName, goName, true);

        public T GetParamByFullName<T>(string nvName, string nbName, string goName, bool active = false)
        {
            var nv = GetParamSourceByFullName(nvName, nbName, goName, active);
            if (nv == null)
            {
                Debug.LogWarning($"Using default value of {typeof(T)} : {default}.");
                return default;
            }
            else { return nv.GetValue<T>(); }
        }

        public object GetActiveParamByFullName(string nvName, string nbName, string goName) => GetParamByFullName(nvName, nbName, goName, true);

        public object GetParamByFullName(string nvName, string nbName, string goName, bool active = false)
        {
            return GetParamSourceByFullName(nvName, nbName, goName, active)?.Value;
        }

        public NetworkVariable<T> GetNetworkVariableByFullName<T>(string nvName, string nbName, string goName, bool active = false)
        {
            return GetParamSourceByFullName(nvName, nbName, goName, active)?.NetworkVariable<T>();
        }

        public NetworkVariableSource GetParamSourceByFullName(string nvName, string nbName, string goName, bool active = false)
        {
            if (active ? active_go.ContainsKey(goName) : go_nb_nv.ContainsKey(goName))
            {
                var nb_nv = go_nb_nv[goName];
                if (nb_nv.ContainsKey(nbName))
                {
                    var nv = nb_nv[nbName];
                    if (nv.ContainsKey(nvName))
                    {
                        return nv[nvName];
                    }
                }
            }
            Debug.LogError($"Can not find{(active ? " active" : "")} NetworkVariable by {nvName}@{nbName}@{goName}");
            return null;
        }


        public bool ContainsActiveParam(string nvORfullName, out string nvName, out string nbName, out string goName) => ContainsParam(nvORfullName, out nvName, out nbName, out goName, true);

        public bool ContainsParam(string nvORfullName, out string nvName, out string nbName, out string goName, bool active = false)
        {
            nvName = nbName = goName = null;
            if (string.IsNullOrEmpty(nvORfullName)) { return false; }
            if (nvORfullName.SplitEnvParamFullName(out var ns))
            {
                nvName = ns[0]; nbName = ns[1]; goName = ns[2];
                return ContainsParamByFullName(nvName, nbName, goName, active);
            }
            else
            {
                var gonames = active ? active_go.Keys.ToArray() : go_nb_nv.Keys.ToArray();
                foreach (var goname in gonames)
                {
                    var nb_nv = go_nb_nv[goname];
                    foreach (var nbname in nb_nv.Keys)
                    {
                        var nv = nb_nv[nbname];
                        if (nv.ContainsKey(nvORfullName))
                        {
                            nvName = nvORfullName; nbName = nbname; goName = goname;
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool ContainsActiveParamByGameObject(string nvName, string goName, out string nbName) => ContainsParamByGameObject(nvName, goName, out nbName, true);

        public bool ContainsParamByGameObject(string nvName, string goName, out string nbName, bool active = false)
        {
            nbName = null;
            if (active ? active_go.ContainsKey(goName) : go_nb_nv.ContainsKey(goName))
            {
                var nb_nv = go_nb_nv[goName];
                foreach (var nbname in nb_nv.Keys)
                {
                    var nv = nb_nv[nbname];
                    if (nv.ContainsKey(nvName))
                    {
                        nvName = nbname;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ContainsActiveParamByFullName(string nvName, string nbName, string goName) => ContainsParamByFullName(nvName, nbName, goName, true);

        public bool ContainsParamByFullName(string nvName, string nbName, string goName, bool active = false)
        {
            if (active ? active_go.ContainsKey(goName) : go_nb_nv.ContainsKey(goName))
            {
                var nb_nv = go_nb_nv[goName];
                if (nb_nv.ContainsKey(nbName))
                {
                    var nv = nb_nv[nbName];
                    if (nv.ContainsKey(nvName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public GameObject GetActiveGameObjectByFullName(string fullName) => GetGameObjectByFullName(fullName, true);

        public GameObject GetGameObjectByFullName(string fullName, bool active = false)
        {
            if (active)
            {
                if (active_go.ContainsKey(fullName)) { return active_go[fullName]; }
                else { Debug.LogError($"Can not find active GameObject by FullName: {fullName}."); return null; }
            }
            else
            {
                if (go.ContainsKey(fullName)) { return go[fullName]; }
                else { Debug.LogError($"Can not find GameObject by FullName: {fullName}."); return null; }
            }
        }

        public string[] GetGameObjectFullNames(bool active = false) { return active ? active_go.Keys.ToArray() : go.Keys.ToArray(); }


        public ScaleGrid SpawnScaleGrid(INetEnvCamera c, string name = null)
        {
            var nb = Spawn<ScaleGrid>("Assets/NetEnv/Object/ScaleGrid.prefab", name, c.gameObject.transform);
            if (nb == null) { return null; }
            //nb.transform.localPosition = new(0, 0, c.FarPlane - c.NearPlane);
            nb.Position.Value = Vector3.zero;
            c.OnCameraChange += nb.UpdateView;
            nb.UpdateView(c);
            return nb;
        }

        public Cross SpawnCross(string name = null)
        {
            if (!"Assets/NetEnv/Object/Cross.prefab".QueryPrefab(out GameObject crossprefab)) { return null; }
            var go = GameObject.Instantiate(crossprefab);
            if (!string.IsNullOrEmpty(name)) { go.name = name; }
            go.GetComponent<NetworkObject>().Spawn(true);
            var cross = go.GetComponent<Cross>();
            return cross;
        }

        public Circle SpawnCircle(Vector3 size, Color color, Vector3 position = default, float width = 0.1f, string name = null, Transform parent = null)
        {
            var nb = Spawn<Circle>("Assets/NetEnv/Object/Circle.prefab", name, parent);
            if (nb == null) { return null; }
            nb.Position.Value = position;
            nb.Size.Value = size;
            nb.Color.Value = color;
            nb.Width.Value = width;
            return nb;
        }

        public Dot SpawnDot()
        {
            if (!"Assets/NetEnv/Object/Dot.prefab".QueryPrefab(out GameObject dotprefab)) { return null; }
            var go = GameObject.Instantiate(dotprefab);
            go.GetComponent<NetworkObject>().Spawn(true);
            return go.GetComponent<Dot>();
        }

        public DotTrail SpawnDotTrail(Vector3 size, Color color, Vector3 position = default, float trailwidthscale = 0.5f, string name = null, Transform parent = null)
        {
            var nb = Spawn<DotTrail>("Assets/NetEnv/Object/DotTrail.prefab", name, parent);
            if (nb == null) { return null; }
            nb.Position.Value = position;
            nb.Size.Value = size;
            nb.Color.Value = color;
            nb.TrailWidthScale.Value = trailwidthscale;
            return nb;
        }

        public T Spawn<T>(string addressprefab, string name = null, Transform parent = null, bool destroyWithScene = true)
        {
            if (!addressprefab.QueryPrefab(out GameObject prefab)) { Debug.LogError($"Can not find Prefab at address: {addressprefab}."); return default; }
            var go = parent == null ? GameObject.Instantiate(prefab) : GameObject.Instantiate(prefab, parent);
            if (!string.IsNullOrEmpty(name)) { go.name = name; }
            go.GetComponent<NetworkObject>().Spawn(destroyWithScene);
            return go.GetComponent<T>();
        }
    }
}

