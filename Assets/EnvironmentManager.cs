// --------------------------------------------------------------
// EnvironmentManager.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
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
        public Dictionary<string, GameObject> sceneobj = new Dictionary<string, GameObject>();
        public Dictionary<string, NetworkBehaviour> sceneobj_net = new Dictionary<string, NetworkBehaviour>();
        public Dictionary<string, PropertyInfo> net_syncvar = new Dictionary<string, PropertyInfo>();
        public List<string> activenet = new List<string>();
        public Camera maincamera;

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
            var nbs = go.GetComponents<NetworkBehaviour>();
            foreach (var nb in nbs)
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
                    net_syncvar[f.Name + "@" + nbname] = nbtype.GetProperty("Network" + f.Name);
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

        public void SetParamsForObject(Dictionary<string, object> envparam, string objname)
        {
            foreach (var p in envparam.Keys)
            {
                if (p.Contains(objname))
                {
                    SetParam(p, envparam[p]);
                }
            }
        }

        public void SetParam(string name, object value)
        {
            var atidx = name.IndexOf("@");
            if (atidx == -1)
            {
                foreach (var sname in sceneobj_net.Keys)
                {
                    var pname = name + "@" + sname;
                    if (net_syncvar.ContainsKey(pname))
                    {
                        SetParam(sceneobj_net[sname], net_syncvar[pname], value);
                    }
                    else
                    {
#if UNITY_EDITOR
                        //UnityEngine.Debug.Log("Param: " + name + " does not exist in " + sname);
#endif
                    }
                }
            }
            else
            {
                if (net_syncvar.ContainsKey(name))
                {
                    SetParam(sceneobj_net[name.Substring(atidx + 1)], net_syncvar[name], value);
                }
                else
                {
#if UNITY_EDITOR
                    //UnityEngine.Debug.Log("Param: " + name + " does not exist.");
#endif
                }
            }
        }

        public void SetParam(NetworkBehaviour nb, PropertyInfo p, object value)
        {
            p.SetValue(nb, VLConvert.Convert(value, p.PropertyType), null);
        }

        public void PushParams()
        {
            foreach (var s in sceneobj_net.Values)
            {
                s.SetDirtyBit(uint.MaxValue);
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
            var atidx = name.IndexOf("@");
            if (atidx == -1)
            {
                foreach (var sname in sceneobj_net.Keys)
                {
                    var pname = name + "@" + sname;
                    if (net_syncvar.ContainsKey(pname))
                    {
                        return GetParam(sceneobj_net[sname], net_syncvar[pname]);
                    }
                    else
                    {
#if UNITY_EDITOR
                        //UnityEngine.Debug.Log("Param: " + name + " does not exist in " + sname);
#endif
                    }
                }
                return null;
            }
            else
            {
                if (net_syncvar.ContainsKey(name))
                {
                    return GetParam(sceneobj_net[name.Substring(atidx + 1)], net_syncvar[name]);
                }
                else
                {
#if UNITY_EDITOR
                    //UnityEngine.Debug.Log("Param: " + name + " does not exist.");
#endif
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
            foreach (var anbname in activenet)
            {
                SetParam("visible" + "@" + anbname, isvisible);
            }
        }

        public bool isparamactive(string name)
        {
            var i = name.IndexOf("@");
            if(i>=0&&i<name.Length-1)
            {
                var pnet = name.Substring(i + 1);
                return activenet.Contains(pnet);
            }
            else
            {
                return false;
            }
        }

        public static string GetSyncVarName(string name)
        {
            var i = name.IndexOf("@");
            if(i>0)
            {
                return name.Substring(0, i);
            }
            else
            {
                return "";
            }
                
        }

    }
}

