/*
Environment.cs is part of the Experica.
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

namespace Experica.NetEnv
{
    public enum NetEnvObject
    {
        None,
        Quad,
        GratingQuad,
        ImageQuad,
        ImageArrayQuad,
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

    public enum DisplayFitType
    {
        Gamma,
        LinearSpline,
        CubicSpline
    }

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

    public class NetworkVariableSource : INotifyBindablePropertyChanged,IDataSource<object>
    {
        public string Name => Property.Name;
        public Type Type => Property.Type;
        Property Property;
        Method Method;
        NetworkVariableBase NV;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public NetworkVariableSource(NetworkVariableBase nv)
        {
            NV = nv;
            var nvtype = nv.GetType(); // NetworkVariable<T>
            var propertytype = nvtype.GenericTypeArguments[0]; // T
            var propertyname = "Value";
            var methodname = "Set";
            var nvtypename = nvtype.ToString();
            
            if (!nvtypename.QueryProperty(propertyname, out Property))
            {
                Property = new Property(propertytype, propertyname, nvtype.DelegateForGetPropertyValue(propertyname), nvtype.DelegateForSetPropertyValue(propertyname));
                nvtypename.StoreProperty(propertyname, Property);
            }
            if (!nvtypename.QueryMethod(methodname, out Method))
            {
                Method = new Method(null, methodname, nvtype.DelegateForCallMethod(methodname, propertytype), propertytype);
                nvtypename.StoreMethod(methodname, Method);
            }
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

        public void NotifyValue()
        {
            Notify("Value");
        }

        public T GetValue<T>() { return Value.Convert<T>(Type); }
        public void SetValue<T>(T value) { Value = value.Convert(typeof(T), Type); }

        public NetworkVariable<T> NetworkVariable<T>()
        {
            if (typeof(T) == Type)
            { return (NetworkVariable<T>)NV; }
            else { Debug.LogError($"Can not downcast to NetworkVariable<{typeof(T)}>."); return null; }
        }

        public void Refresh() { Method.Invoker(NV, Value); }

        public NetworkBehaviour GetBehaviour() { return NV.GetBehaviour(); }
    }

    public interface INetEnvCamera
    {
        public GameObject gameObject { get; }
        public float Height { get; }
        public float Width { get; }
        public float Aspect { get; }
        public float NearPlane { get; }
        public float FarPlane { get; }
        public ulong ClientID { get; }
        public Action<INetEnvCamera> OnCameraChange { get; set; }
        public Camera Camera { get; }
        public HDAdditionalCameraData CameraHD { get; }
    }
}