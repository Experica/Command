/*
NetEnv.cs is part of the Experica.
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
using System.Collections.Generic;
using Fasterflect;
using System;
using Unity.Netcode;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;
using Unity.Properties;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Linq;

namespace Experica.NetEnv
{
    public enum NetEnvObject
    {
        None,
        Quad,
        Grating,
        Plaid,
        ImageList,
        Dots
    }

    public enum MaskType
    {
        None,
        Disk,
        Gaussian,
        DiskFade
    }

    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public enum WaveType
    {
        Square,
        Sinusoidal,
        Triangle
    }

    public enum ColorSpace
    {
        RGB,
        HSL,
        XYZ,
        LMS,
        DKL,
        CAM
    }

    public enum ColorChannel
    {
        None,
        R,
        G,
        B,
        A,
        Each
    }

    public enum DisplayType
    {
        CRT,
        LCD,
        LED,
        Projector
    }

    //public enum DisplayFitType
    //{
    //    Gamma,
    //    LinearSpline,
    //    CubicSpline
    //}

    public class Display
    {
        public string ID { get; set; } = "";
        public DisplayType Type { get; set; } = DisplayType.LCD;
        public double Latency { get; set; } = 0;
        public double RiseLag { get; set; } = 0;
        public double FallLag { get; set; } = 0;
        public int CLUTSize { get; set; } = 32;
        public DisplayFitType FitType { get; set; } = DisplayFitType.LinearSpline;
        public Dictionary<string, List<object>> IntensityMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Dictionary<string, List<object>> SpectralMeasurement { get; set; } = new Dictionary<string, List<object>>();
        public Texture3D CLUT;
    }

    /// <summary>
    /// UI datasource wrapper of reflected "Value" property of NetworkVariable for UI data binding and network sync
    /// </summary>
    public class NetworkVariableSource : INotifyBindablePropertyChanged, IDataSource<object>
    {
        public string Name => Property.Name;
        public Type Type => Property.Type;
        Property Property;
        NetworkVariableBase NV;
        //Method Method;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public NetworkVariableSource(NetworkVariableBase nv, Property property) { NV = nv; Property = property; }
        public NetworkVariableSource(NetworkVariableBase nv)
        {
            NV = nv;
            var nvtype = nv.GetType(); // NetworkVariable<T>
            var nvtypename = nvtype.ToString();
            var propertytype = nvtype.GenericTypeArguments[0]; // T
            var propertyname = "Value";
            //var methodname = "Set";

            if (!nvtypename.QueryProperty(propertyname, out Property))
            {
                Property = new Property(propertytype, propertyname, nvtype.DelegateForGetPropertyValue(propertyname), nvtype.DelegateForSetPropertyValue(propertyname));
                nvtypename.StoreProperty(propertyname, Property);
            }
            //if (!nvtypename.QueryMethod(methodname, out Method))
            //{
            //    Method = new Method(null, methodname, nvtype.DelegateForCallMethod(methodname, propertytype), propertytype);
            //    nvtypename.StoreMethod(methodname, Method);
            //}
        }

        [CreateProperty]
        public object Value
        {
            get { return Property.Getter(NV); }
            set { Property.Setter(NV, value); if (NV.IsDirty()) { Notify(); } }
        }
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        public void NotifyValue() { Notify("Value"); }
        public void SetValueWithoutNotify(object value) { Property.Setter(NV, value); }

        public T GetValue<T>() { return Value.Convert<T>(Type); }
        public void SetValue<T>(T value) { Value = value.Convert(typeof(T), Type); }
        public void SetValueWithoutNotify<T>(T value) { Property.Setter(NV, value.Convert(typeof(T), Type)); }

        public NetworkVariable<T> NetworkVariable<T>()
        {
            if (typeof(T) == Type)
            { return (NetworkVariable<T>)NV; }
            else { Debug.LogError($"Can not downcast to NetworkVariable<{typeof(T)}>."); return null; }
        }

        public void NotifyNetworkValue()
        {
            NV.SetDirty(true);
            dynamic nv = NV;
            dynamic v = nv.Value;
            nv.OnValueChanged(v, v);

            ////Method.Invoker(NV, Value,Value);
        }

        public NetworkBehaviour GetBehaviour() { return NV.GetBehaviour(); }
    }

    public interface INetEnvPlayer
    {
        public ulong ClientID { get; set; }
        public void AskReportRpc();
        public void ReportRpc(string name, float value);
    }

    public interface INetEnvCamera : INetEnvPlayer
    {
        public GameObject gameObject { get; }
        public float Height { get; }
        public float Width { get; }
        public float Aspect { get; }
        public float NearPlane { get; }
        public float FarPlane { get; }
        public Action<INetEnvCamera> OnCameraChange { get; set; }
        public Camera Camera { get; }
        public HDAdditionalCameraData CameraHD { get; }
    }

    public enum NetVisibility
    {
        None,
        Single,
        All
    }

    public static class NetEnvBase
    {
        public static List<ulong> Observers(this NetworkObject no)
        {
            List<ulong> list = new();
            var obs = no.GetObservers();
            while (obs.MoveNext()) { list.Add(obs.Current); }
            return list;
        }

        public static bool IsNetworkHideFromAll(this NetworkObject no)
        {
            int count = 0;
            var obs = no.GetObservers();
            while (obs.MoveNext()) { count++; }
            return count == 0;
        }

        public static bool IsNetworkShowToAll(this NetworkObject no)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                int count = 0;
                var obs = no.GetObservers();
                while (obs.MoveNext()) { count++; }
                return count == nm.ConnectedClientsIds.Count;
            }
            return false;
        }

        public static void NetworkShowHideOnly(this NetworkObject no, ulong clientid, bool isshow)
        {
            if (isshow) { NetworkShowOnlyTo(no, clientid); } else { NetworkHideOnlyFrom(no, clientid); }
        }

        public static void NetworkShowHideAll(this NetworkObject no, bool isshow)
        {
            if (isshow) { NetworkShowToAll(no); } else { NetworkHideFromAll(no); }
        }

        public static void NetworkShowOnlyTo(this NetworkObject no, ulong clientid)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                var ss = no.Observers();
                var hs = nm.ConnectedClientsIds.Except(ss).ToArray();
                if (hs.Contains(clientid)) { no.NetworkShow(clientid); }
                foreach (var c in ss) { if (c != clientid) { no.NetworkHide(c); } }
            }
        }

        public static void NetworkShowToAll(this NetworkObject no)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                foreach (var c in nm.ConnectedClientsIds.Except(no.Observers()))
                {
                    no.NetworkShow(c);
                }
            }
        }

        public static void NetworkHideOnlyFrom(this NetworkObject no, ulong clientid)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                var ss = no.Observers();
                var hs = nm.ConnectedClientsIds.Except(ss).ToArray();
                if (ss.Contains(clientid)) { no.NetworkHide(clientid); }
                foreach (var c in hs) { if (c != clientid) { no.NetworkShow(c); } }
            }
        }

        public static void NetworkHideFromAll(this NetworkObject no)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                foreach (var c in no.Observers())
                {
                    no.NetworkHide(c);
                }
            }
        }
    }
}