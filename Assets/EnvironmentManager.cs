// --------------------------------------------------------------
// EnvironmentManager.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

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
        public Dictionary<string, GameObject> sceneobject = new Dictionary<string, GameObject>();
        public Dictionary<string, NetworkBehaviour> sceneobjectsync = new Dictionary<string, NetworkBehaviour>();
        public Dictionary<string, PropertyInfo> syncparam = new Dictionary<string, PropertyInfo>();
        public Camera maincamera;
        public List<string> activesync = new List<string>();

        public void AddScene(string scenename)
        {
            scene = SceneManager.GetSceneByName(scenename);
            Update();
        }

        public void Update()
        {
            sceneobject.Clear();
            sceneobjectsync.Clear();
            syncparam.Clear();
            activesync.Clear();
            foreach (var go in scene.GetRootGameObjects())
            {
                sceneobject[go.name] = go;
                ParseSceneObject(go);
            }
        }

        public void ParseSceneObject(GameObject go)
        {
            if (go.tag == "MainCamera")
            {
                maincamera = go.GetComponent<Camera>();
            }
            var nbs = go.GetComponents<NetworkBehaviour>();
            foreach (var nb in nbs)
            {
                var nbname = nb.GetType().Name + "@" + go.name;
                sceneobjectsync[nbname] = nb;
                ParseSceneObjectSync(nb, nbname);
                if (nb.isActiveAndEnabled)
                {
                    activesync.Add(nbname);
                }
            }
            for (var i = 0; i < go.transform.childCount; i++)
            {
                ParseSceneObject(go.transform.GetChild(i).gameObject);
            }
        }

        public void ParseSceneObjectSync(NetworkBehaviour nb, string nbname)
        {
            var nbtype = nb.GetType();
            var fs = nbtype.GetFields();
            foreach (var f in fs)
            {
                if (f.IsDefined(typeof(SyncVarAttribute), true))
                {
                    syncparam[f.Name + "@" + nbname] = nbtype.GetProperty("Network" + f.Name);
                }
            }
        }

        public void SetEnvParam(Dictionary<string, object> envparam)
        {
            foreach (var p in envparam.Keys)
            {
                SetParam(p, envparam[p]);
            }
        }

        public void SetParam(string name, object value)
        {
            var atidx = name.IndexOf("@");
            if (atidx == -1)
            {
                foreach (var sname in sceneobjectsync.Keys)
                {
                    var pname = name + "@" + sname;
                    if (syncparam.ContainsKey(pname))
                    {
                        SetParam(sceneobjectsync[sname], syncparam[pname], value);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Param: " + name + " does not exist in " + sname);
                    }
                }
            }
            else
            {
                if (syncparam.ContainsKey(name))
                {
                    SetParam(sceneobjectsync[name.Substring(atidx + 1)], syncparam[name], value);
                }
                else
                {
                    UnityEngine.Debug.Log("Param: " + name + " does not exist.");
                }
            }
        }

        public void SetParam(NetworkBehaviour nb, PropertyInfo p, object value)
        {
            p.SetValue(nb, VLConvert.Convert(value, p.PropertyType), null);
        }

        public void PushParams()
        {
            foreach (var s in sceneobjectsync.Values)
            {
                s.SetDirtyBit(uint.MaxValue);
            }
        }

        public Dictionary<string, object> GetEnvParam()
        {
            var envparam = new Dictionary<string, object>();
            foreach (var p in syncparam.Keys)
            {
                envparam[p] = GetParam(p);
            }
            return envparam;
        }

        public object GetParam(string name)
        {
            var atidx = name.IndexOf("@");
            if (atidx == -1)
            {
                foreach (var sname in sceneobjectsync.Keys)
                {
                    var pname = name + "@" + sname;
                    if (syncparam.ContainsKey(pname))
                    {
                        return GetParam(sceneobjectsync[sname], syncparam[pname]);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Param: " + name + " does not exist in " + sname);
                    }
                }
                return null;
            }
            else
            {
                if (syncparam.ContainsKey(name))
                {
                    return GetParam(sceneobjectsync[name.Substring(atidx + 1)], syncparam[name]);
                }
                else
                {
                    UnityEngine.Debug.Log("Param: " + name + " does not exist.");
                    return null;
                }
            }
        }

        public object GetParam(NetworkBehaviour nb, PropertyInfo p)
        {
            return p.GetValue(nb, null);
        }

        public void ActiveSyncVisible(bool isvisible)
        {
            foreach (var anbname in activesync)
            {
                SetParam("visible" + "@" + anbname, isvisible);
            }
        }

    }
}

