/*
Experica.cs is part of the Experica.
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
using System;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Mail;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Interpolation;
using Fasterflect;
using Unity.Properties;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if COMMAND
using System.Windows.Forms;
using MethodInvoker = Fasterflect.MethodInvoker;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Experica
{
    /// <summary>
    /// wrap and cache of fast delegate to reflected property
    /// </summary>
    public class Property
    {
        public Type Type { get; }
        public string Name { get; }
        public MemberGetter Getter { get; }
        public MemberSetter Setter { get; }

        public Property(Type type, string name, MemberGetter getter, MemberSetter setter)
        {
            Type = type; Name = name; Getter = getter; Setter = setter;
        }
        public Property(PropertyInfo info)
        {
            Type = info.PropertyType;
            Name = info.Name;
            Getter = info.DelegateForGetPropertyValue();
            Setter = info.DelegateForSetPropertyValue();
        }
    }

    /// <summary>
    /// wrap and cache of fast delegate to reflected method
    /// </summary>
    public class Method
    {
        public Type ReturnType { get; }
        public string Name { get; }
        public Type[] ParamType { get; }
        public MethodInvoker Invoker { get; }

        public Method(Type returntype, string name, MethodInvoker invoker, params Type[] paramtype)
        {
            ReturnType = returntype; Name = name; Invoker = invoker; ParamType = paramtype;
        }
        public Method(MethodInfo info)
        {
            ReturnType = info.ReturnType;
            Name = info.Name;
            Invoker = info.DelegateForCallMethod();
            ParamType = info.GetParameters().Select(i => i.ParameterType).ToArray();
        }
    }

    /// <summary>
    /// UI datasource wrapper of reflected property for data binding
    /// </summary>
    /// <typeparam name="TContainer">Type of object on which the property is reflected</typeparam>
    public class PropertySource<TContainer> : INotifyBindablePropertyChanged, IDataSource<object>
    {
        public string Name => Property.Name;
        public Type Type => Property.Type;
        Property Property;
        TContainer Container;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public PropertySource(TContainer container, Property property) { Container = container; Property = property; }

        public PropertySource(TContainer container, Type propertytype, string propertyname)
        {
            Container = container;
            var containertype = container.GetType();
            var containertypename = containertype.ToString();

            if (!containertypename.QueryProperty(propertyname, out Property))
            {
                Property = new Property(propertytype, propertyname, containertype.DelegateForGetPropertyValue(propertyname), containertype.DelegateForSetPropertyValue(propertyname));
                containertypename.StoreProperty(propertyname, Property);
            }
        }

        [CreateProperty]
        public object Value
        {
            get { return Property.Getter(Container); }
            set { Property.Setter(Container, value); Notify(); }
        }
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        public void NotifyValue() { Notify("Value"); }
        public void SetValueWithoutNotify(object value) { Property.Setter(Container, value); }

        public T GetValue<T>() { return Value.Convert<T>(Type); }
        public void SetValue<T>(T value) { Value = value.Convert(typeof(T), Type); }
        public void SetValueWithoutNotify<T>(T value) { Property.Setter(Container, value.Convert(typeof(T), Type)); }
    }

    /// <summary>
    /// UI datasource wrapper of Dictionary<String, TValue> for data binding
    /// </summary>
    /// <typeparam name="TValue">Value Type of Dictionary</typeparam>
    public class DictSource<TValue> : INotifyBindablePropertyChanged, IDataSource<TValue>
    {
        public string Name { get; }
        public Type Type => Value.GetType();
        Dictionary<string, TValue> Container;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public DictSource(Dictionary<string, TValue> container, string name)
        {
            Container = container; Name = name;
        }

        [CreateProperty]
        public TValue Value
        {
            get { return Container[Name]; }
            set { Container[Name] = value; Notify(); }
        }
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        public void NotifyValue() { Notify("Value"); }
        public void SetValueWithoutNotify(TValue value) { Container[Name] = value; }

        public T GetValue<T>() { return Value.Convert<T>(); }
        public void SetValue<T>(T value) { Value = value.Convert<TValue>(typeof(T)); }
        public void SetValueWithoutNotify<T>(T value) { Container[Name] = value.Convert<TValue>(typeof(T)); }
    }

    /// <summary>
    /// UI datasource wrapper of reflected property for data binding
    /// </summary>
    public class PropertySource : INotifyBindablePropertyChanged, IDataSource<object>
    {
        public string Name => Property.Name;
        public Type Type => Property.Type;
        Property Property;
        object Container;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public PropertySource(object container, Property property) { Container = container; Property = property; }

        public PropertySource(object container, Type propertytype, string propertyname)
        {
            Container = container;
            var containertype = container.GetType();
            var containertypename = containertype.ToString();

            if (!containertypename.QueryProperty(propertyname, out Property))
            {
                Property = new Property(propertytype, propertyname, containertype.DelegateForGetPropertyValue(propertyname), containertype.DelegateForSetPropertyValue(propertyname));
                containertypename.StoreProperty(propertyname, Property);
            }
        }

        [CreateProperty]
        public object Value
        {
            get { return Property.Getter(Container); }
            set { Property.Setter(Container, value); Notify(); }
        }
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        public void NotifyValue() { Notify("Value"); }
        public void SetValueWithoutNotify(object value) { Property.Setter(Container, value); }

        public T GetValue<T>() { return Value.Convert<T>(Type); }
        public void SetValue<T>(T value) { Value = value.Convert(typeof(T), Type); }
        public void SetValueWithoutNotify<T>(T value) { Property.Setter(Container, value.Convert(typeof(T), Type)); }
    }

    /// <summary>
    /// DataSource that expose only one DataPath of name: "Value"
    /// </summary>
    /// <typeparam name="T">Type of Exposed Value</typeparam>
    public interface IDataSource<T>
    {
        public string Name { get; }
        public Type Type { get; }
        public T Value { get; set; }
        public void NotifyValue();
        public void SetValueWithoutNotify(T value);
    }

    /// <summary>
    /// For it's derived class, provide reflected property access and UI datasource wrapper
    /// </summary>
    public abstract class DataClass
    {
        public Dictionary<string, object> ExtendParam { get; set; } = new();
        Dictionary<string, PropertySource<DataClass>> properties = new();
        Dictionary<string, DictSource<object>> extendproperties = new();

        /// <summary>
        /// cache and wrap all property access in datasource for UI binding
        /// </summary>
        public DataClass()
        {
            var dtype = GetType();
            var dtypename = dtype.ToString();
            start:
            if (dtypename.QueryProperties(out var ps))
            {
                properties = ps.ToDictionary(kv => kv.Key, kv => new PropertySource<DataClass>(this, kv.Value));
            }
            else 
            {
                foreach (var p in dtype.GetProperties())
                {
                    dtypename.StoreProperty(p.Name, new Property(p));
                }
                goto start;
            }
        }


        /// <summary>
        /// Try set value for properties or ExtendParam via datasource wrapper(will notify UI for value change)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetParam(string name, object value)
        {
            if (properties.ContainsKey(name))
            {
                var p = properties[name];
                p.Value = value.Convert(p.Type);
                return true;
            }
            if (extendproperties.ContainsKey(name))
            {
                extendproperties[name].Value = value;
                return true;
            }
            Debug.LogError($"Param: {name} not found in {GetType().Name} or its ExtendParam");
            return false;
        }

        /// <summary>
        /// set ExtendParam value via datasource wrapper(will notify UI for value change)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetExtendProperty(string name, object value)
        {
            if (extendproperties.ContainsKey(name))
            {
                extendproperties[name].Value = value;
                return true;
            }
            Debug.LogError($"ExtendProperty: {name} not exist in {GetType().Name}.ExtendParam");
            return false;
        }

        /// <summary>
        /// set property value via datasource wrapper(will notify UI for value change)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetProperty(string name, object value)
        {
            if (properties.ContainsKey(name))
            {
                var p = properties[name];
                p.Value = value.Convert(p.Type);
                return true;
            }
            Debug.LogError($"Property: {name} not defined in {GetType().Name}");
            return false;
        }

        public T GetParam<T>(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name].GetValue<T>();
            }
            if (extendproperties.ContainsKey(name))
            {
                return extendproperties[name].GetValue<T>();
            }
            Debug.LogError($"Param: {name} not found in {GetType().Name} or its ExtendParam, return default value of {typeof(T)} : {default}.");
            return default;
        }

        /// <summary>
        /// Try get value from properties or ExtendParam via datasource wrapper
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetParam(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name].Value;
            }
            if (extendproperties.ContainsKey(name))
            {
                return extendproperties[name].Value;
            }
            Debug.LogError($"Param: {name} not found in {GetType().Name} or its ExtendParam");
            return null;
        }

        public T GetExtendProperty<T>(string name)
        {
            if (extendproperties.ContainsKey(name))
            {
                return extendproperties[name].GetValue<T>();
            }
            Debug.LogError($"ExtendProperty: {name} not exist in {GetType().Name}.ExtendParam, return default value of {typeof(T)} : {default}.");
            return default;
        }

        /// <summary>
        /// get ExtendParam value via datasource wrapper
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetExtendProperty(string name)
        {
            if (extendproperties.ContainsKey(name))
            {
                return extendproperties[name].Value;
            }
            Debug.LogError($"ExtendProperty: {name} not exist in {GetType().Name}.ExtendParam");
            return null;
        }

        public T GetProperty<T>(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name].GetValue<T>();
            }
            Debug.LogError($"Property: {name} not defined in {GetType().Name}, return default value of {typeof(T)} : {default}.");
            return default;
        }

        /// <summary>
        /// get property value via datasource wrapper
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetProperty(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name].Value;
            }
            Debug.LogError($"Property: {name} not defined in {GetType().Name}");
            return null;
        }

        public bool ContainsParam(string name) => properties.ContainsKey(name) || extendproperties.ContainsKey(name);
        public bool ContainsExtendProperty(string name) => extendproperties.ContainsKey(name);
        public bool ContainsProperty(string name) => properties.ContainsKey(name);
        public Dictionary<string, PropertySource<DataClass>> Properties() => properties;
        public Dictionary<string, DictSource<object>> ExtendProperties() => extendproperties;

        /// <summary>
        /// delete name:value in ExtendParam and its datasource wrapper
        /// </summary>
        /// <param name="name"></param>
        public void RemoveExtendProperty(string name)
        {
            extendproperties.Remove(name);
            ExtendParam.Remove(name);
        }

        /// <summary>
        /// add name:value in ExtendParam and its datasource wrapper
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public DictSource<object> AddExtendProperty(string name, object value)
        {
            ExtendParam[name] = value;
            DictSource<object> source = new(ExtendParam, name);
            extendproperties[name] = source;
            return source;
        }

        /// <summary>
        /// wrap all ExtendParam in datasource for UI binding
        /// </summary>
        public void RefreshExtendProperties() => extendproperties = ExtendParam.ToDictionary(kv => kv.Key, kv => new DictSource<object>(ExtendParam, kv.Key));

    }


    public class MethodAccess
    {
        public MethodInvoker Call { get; }
        public string Name { get; }

        public MethodAccess(string n, MethodInvoker m)
        {
            Name = n;
            Call = m;
        }

        public MethodAccess(Type reflectedtype, string methodname)
        {
            Name = methodname;
            var minfo = reflectedtype.GetMethod(methodname);
            Call = reflectedtype.DelegateForCallMethod(methodname, minfo.GetParameters().Select(i => i.ParameterType).ToArray());
        }
    }

    public enum SampleMethod
    {
        Manual,
        Ascending,
        Descending,
        UniformWithReplacement,
        UniformWithoutReplacement
    }

    public enum CONDTESTPARAM
    {
        CondIndex,
        CondRepeat,
        TrialIndex,
        TrialRepeat,
        BlockIndex,
        BlockRepeat,
        Event,
        SyncEvent,
        CONDSTATE,
        TRIALSTATE,
        BLOCKSTATE,
        TASKSTATE,
        CYCLE
    }

    public enum DataFormat
    {
        YAML,
        EX
    }

    public interface INetEnv
    {
        public Scene Scene { get; }
        public void SetParam(string nvORfullName, object value, bool active = false);
    }

    public enum FactorDesignMethod
    {
        Linear,
        Power,
        Log10,
        Log2
    }

    public class ImageSet
    {
        public Texture2D[] Images = Array.Empty<Texture2D>();
        public Color MeanColor = Color.gray;
    }

    public class MPIS<T> where T : struct
    {
        public int[] ImageSize;
        public float[] MeanColor;
        public T[][] Images;
    }

    public static class Experica
    {
        public const uint ExperimentVersion = 3;
        public const uint CommandConfigVersion = 0;
        public const string EmptyScene = "Empty";
        static Dictionary<string, Dictionary<string, List<object>>> colordata = new();

        static Dictionary<string, Dictionary<string, Matrix<float>>> colormatrix = new();

        // Plants of the Unit Cube defined by a point and a corresponding normal, used for intersection of line and six faces of the Unit Cube
        static Vector<float>[] UnitOriginCubePoints = new[] { CreateVector.Dense(3, 0f), CreateVector.Dense(3, 0f), CreateVector.Dense(3, 0f),
                                                              CreateVector.Dense(3, 1f),CreateVector.Dense(3, 1f),CreateVector.Dense(3, 1f)};
        static Vector<float>[] UnitOriginCubeNormals = new[] { CreateVector.Dense(new[] { 1f, 0f, 0f }), CreateVector.Dense(new[] { 0f, 1f, 0f }), CreateVector.Dense(new[] { 0f, 0f, 1f }),
                                                               CreateVector.Dense(new[] { 1f, 0f, 0f }), CreateVector.Dense(new[] { 0f, 1f, 0f }), CreateVector.Dense(new[] { 0f, 0f, 1f })};

        static readonly object apilock = new object();

        static HashSet<Type> NumericTypes = new()
        {
            typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),
            typeof(int),typeof(uint),typeof(long),typeof(ulong),
            typeof(float),typeof(double),typeof(decimal)
        };
        static Type TObject = typeof(object), TString = typeof(string), TBool = typeof(bool), TInt = typeof(int), TUInt = typeof(uint), TFloat = typeof(float), TDouble = typeof(double),
            TVector2 = typeof(Vector2), TVector3 = typeof(Vector3), TVector4 = typeof(Vector4), TColor = typeof(Color), TFixString512 = typeof(FixedString512Bytes),
            TListT = typeof(List<>), TDictTT = typeof(Dictionary<,>), TListObject = typeof(List<object>), TDictObjectObject = typeof(Dictionary<object, object>), TArray = typeof(Array);

        #region ImageSets
        static Dictionary<string, ImageSet> imagesets = new();
        public static bool QueryImageSet(this string imagesetname, out ImageSet imgset, bool reload = false)
        {
            if (!reload && imagesets.ContainsKey(imagesetname))
            {
                imgset = imagesets[imagesetname];
                return true;
            }
            imgset = imagesetname.LoadImageSet();
            if (imgset == null)
            {
                imgset = new();
                return false;
            }
            else
            {
                imagesets[imagesetname] = imgset;
                return true;
            }
        }

        public static ImageSet LoadImageSet(this string imagesetname, string rootdir = "Data", string ext = ".mpis")
        {
            if (string.IsNullOrEmpty(imagesetname)) { return null; }
            var file = Path.Combine(rootdir, imagesetname + ext);
            if (!File.Exists(file)) { Debug.LogError($"ImageSet File: {file} Not Exist."); return null; }
            var eltype = Path.GetExtension(imagesetname);
            if (string.IsNullOrEmpty(eltype)) { Debug.LogError($"Incomplete ImageSet Name: {imagesetname}, with no data format extension."); return null; }

            if (eltype == ".UInt8")
            {
                MPIS<byte> data;
                using (var fs = File.OpenRead(file))
                {
                    data = fs.DeserializeMsgPack<MPIS<byte>>();
                }

                int w, h, nch; int[] ci;
                if (data.ImageSize.Length == 2)
                {
                    nch = 1; h = data.ImageSize[0]; w = data.ImageSize[1]; ci = new int[3] { 0, 0, 0 };
                }
                else
                {
                    nch = data.ImageSize[0]; h = data.ImageSize[1]; w = data.ImageSize[2]; ci = new int[3] { 0, 1, 2 };
                }

                var mcolor = new Color(data.MeanColor[0], data.MeanColor[1], data.MeanColor[2], 1);
                var imgset = new Texture2D[data.Images.Length];
                for (var i = 0; i < data.Images.Length; i++)
                {
                    var img = data.Images[i];
                    var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true, true);
                    var ps = t.GetRawTextureData<Color32>();
                    for (var j = 0; j < w * h; j++)
                    {
                        ps[j] = new(img[ci[0] + nch * j], img[ci[1] + nch * j], img[ci[2] + nch * j], 255);
                    }
                    t.Apply();
                    imgset[i] = t;
                }
                return new() { Images = imgset, MeanColor = mcolor };
            }

            return null;
        }
        #endregion

        #region Registry of reflections for property and method of a type
        static Dictionary<string, Dictionary<string, Property>> Properties = new();
        static Dictionary<string, Dictionary<string, Method>> Methods = new();
        public static bool QueryProperties(this string containertypename, out Dictionary<string, Property> properties)
        {
            if (Properties.TryGetValue(containertypename, out properties)) { return true; }
            else { properties = null; return false; }
        }
        public static bool QueryProperty(this string containertypename, string propertyname, out Property property)
        {
            if (Properties.ContainsKey(containertypename))
            {
                var ps = Properties[containertypename];
                if (ps.ContainsKey(propertyname))
                {
                    property = ps[propertyname];
                    return true;
                }
                else { property = null; return false; }
            }
            else
            {
                Properties[containertypename] = new();
                property = null; return false;
            }
        }
        public static bool ContainsProperty(this string containertypename, string propertyname)
        {
            return Properties.ContainsKey(containertypename) && Properties[containertypename].ContainsKey(propertyname);
        }
        public static bool QueryMethod(this string containertypename, string methodname, out Method method)
        {
            if (Methods.ContainsKey(containertypename))
            {
                var ms = Methods[containertypename];
                if (ms.ContainsKey(methodname))
                {
                    method = ms[methodname];
                    return true;
                }
                else { method = null; return false; }
            }
            else
            {
                Methods[containertypename] = new();
                method = null; return false;
            }
        }
        public static bool ContainsMethod(this string containertypename, string methodname)
        {
            return Methods.ContainsKey(containertypename) && Methods[containertypename].ContainsKey(methodname);
        }
        public static void StoreProperty(this string containertypename, string propertyname, Property property)
        {
            if (!Properties.ContainsKey(containertypename))
            {
                Properties[containertypename] = new();
            }
            Properties[containertypename][propertyname] = property;
        }
        public static void StoreMethod(this string containertypename, string methodname, Method method)
        {
            if (!Methods.ContainsKey(containertypename))
            {
                Methods[containertypename] = new();
            }
            Methods[containertypename][methodname] = method;
        }
        #endregion

        #region Addressable Assets
        public static Dictionary<string, AsyncOperationHandle<GameObject>> addressprefab = new();

        public static bool QueryPrefab(this string address, out GameObject go)
        {
            if (addressprefab.ContainsKey(address))
            {
                var handle = addressprefab[address];
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    go = handle.Result; return true;
                }
                else
                {
                    addressprefab.Remove(address); Addressables.Release(handle);
                    Debug.LogError($"Failed to Load Prefab: {address}.");
                    go = null; return false;
                }
            }
            else
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(address);
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    addressprefab[address] = handle;
                    go = handle.Result; return true;
                }
                else
                {
                    Addressables.Release(handle); Debug.LogError($"Failed to Load Prefab: {address}.");
                    go = null; return false;
                }
            }
        }

        public static LineRenderer AddLine(this Vector3[] positions, string name = null, Transform parent = null, bool loop = false)
        {
            if (!"Assets/NetEnv/Object/Line.prefab".QueryPrefab(out GameObject lineprefab)) { return null; }
            var go = parent == null ? GameObject.Instantiate(lineprefab) : GameObject.Instantiate(lineprefab, parent);
            if (!string.IsNullOrEmpty(name)) { go.name = name; }
            var lr = go.GetComponent<LineRenderer>();
            lr.positionCount = positions.Length;
            lr.loop = loop;
            lr.SetPositions(positions);
            return lr;
        }

        public static LineRenderer AddXLine(float radius = 0.5f, string name = null, Transform parent = null)
        {
            return AddLine(new[] { new Vector3(-radius, 0, 0), new Vector3(radius, 0, 0) }, name, parent);
        }

        public static LineRenderer AddYLine(float radius = 0.5f, string name = null, Transform parent = null)
        {
            return AddLine(new[] { new Vector3(0, -radius, 0), new Vector3(0, radius, 0) }, name, parent);
        }

        public static LineRenderer AddCircle(float radius = 0.5f, float deltadegree = 2f, string name = null, Transform parent = null)
        {
            var ps = new Vector3[Mathf.FloorToInt(360 / deltadegree)];
            for (int i = 0; i < ps.Length; i++)
            {
                var d = Mathf.Deg2Rad * i * deltadegree;
                ps[i] = new Vector3(radius * Mathf.Cos(d), radius * Mathf.Sin(d), 0);
            }
            return AddLine(ps, name, parent, true);
        }

        #endregion


#if COMMAND
        static IRecorder spikeglxrecorder, ripplerecorder, imagerrecorder = null;
#endif

        public static bool IsNumeric(this Type type)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
        }

        public static IList<T> AsList<T>(this object o) => o as IList<T>;
        public static IList AsList(this object o) => o as IList;

        #region Convert between Types
        // Here, we try parsing string on all build-in types of YAML, plus commonly used UnityEngine types. We begin on scaler types, then container types,
        // since we allow long vector to be parsed as short vector, so we need to search descendingly to prevent e.g. vector4 be parsed as vector2
        static Type[] tryparsestringfortypes = new Type[] { TBool, TFloat, TColor, TVector3, TVector2, TListObject, TDictObjectObject };
        public static object TryParse(this string value)
        {
            if (string.IsNullOrEmpty(value)) { return value; }
            foreach (var T in tryparsestringfortypes)
            {
                var v = value.Convert(TString, T);
                if (v != null) { return v; }
            }
            return value;
        }

        public static T Convert<T>(this object value)
        {
            return (T)Convert(value, value.GetType(), typeof(T));
        }

        public static T Convert<T>(this object value, Type TValue)
        {
            return (T)Convert(value, TValue, typeof(T));
        }

        public static object Convert(this object value, Type T)
        {
            if (value == null) { return value; }
            return Convert(value, value.GetType(), T);
        }

        /// <summary>
        /// Try convert between types
        /// </summary>
        /// <param name="value"></param>
        /// <param name="TValue">Type of the value</param>
        /// <param name="T">Type we want the value to be converted to</param>
        /// <param name="floatfmt">String format when convert float value (float gives 9 significant digits, since most of our data don't need full precision, here we use 5 significant digits.)</param>
        /// <returns>The converted value, or null if unsuccessful</returns>
        public static object Convert(this object value, Type TValue, Type T, string floatfmt = "G5")
        {
            lock (apilock)
            {
                if (value == null || TValue == T) { return value; }
                object cvalue = null; bool isfallback = false;

                // try all to all conversion
                if (TValue == TFixString512)
                {
                    var v = (FixedString512Bytes)value;
                    return v.ToString().Convert(TString, T);
                }
                else if (TValue == TFloat)
                {
                    var v = (float)value;
                    if (T == TString)
                    {
                        return v.ToString(floatfmt);
                    }
                    else { isfallback = true; }
                }
                else if (TValue == TVector2)
                {
                    var v = (Vector2)value;
                    if (T == TString)
                    {
                        return v.x.ToString(floatfmt) + " " + v.y.ToString(floatfmt);
                    }
                    else if (T.IsSubclassOf(TArray))
                    {
                        var et = T.GetElementType();
                        var av = Array.CreateInstance(et, 2);
                        av.SetValue(v.x.Convert(TFloat, et), 0);
                        av.SetValue(v.y.Convert(TFloat, et), 1);
                        return av;
                    }
                }
                else if (TValue == TVector3)
                {
                    var v = (Vector3)value;
                    if (T == TString)
                    {
                        return string.Join(" ", Enumerable.Range(0, 3).Select(i => v[i].ToString(floatfmt)));
                    }
                    else if (T.IsSubclassOf(TArray))
                    {
                        var et = T.GetElementType();
                        var av = Array.CreateInstance(et, 3);
                        av.SetValue(v.x.Convert(TFloat, et), 0);
                        av.SetValue(v.y.Convert(TFloat, et), 1);
                        av.SetValue(v.z.Convert(TFloat, et), 2);
                        return av;
                    }
                }
                else if (TValue == TVector4)
                {
                    var v = (Vector4)value;
                    if (T == TString)
                    {
                        return string.Join(" ", Enumerable.Range(0, 4).Select(i => v[i].ToString(floatfmt)));
                    }
                    else if (T == TColor)
                    {
                        return new Color(v.x, v.y, v.z, v.w);
                    }
                    else if (T.IsSubclassOf(TArray))
                    {
                        var et = T.GetElementType();
                        var av = Array.CreateInstance(et, 4);
                        av.SetValue(v.x.Convert(TFloat, et), 0);
                        av.SetValue(v.y.Convert(TFloat, et), 1);
                        av.SetValue(v.z.Convert(TFloat, et), 2);
                        av.SetValue(v.w.Convert(TFloat, et), 3);
                        return av;
                    }
                }
                else if (TValue == TColor)
                {
                    var v = (Color)value;
                    if (T == TString)
                    {
                        return string.Join(" ", Enumerable.Range(0, 4).Select(i => v[i].ToString(floatfmt)));
                    }
                    else if (T == TVector4)
                    {
                        return new Vector4(v.r, v.g, v.b, v.a);
                    }
                    else if (T.IsSubclassOf(TArray))
                    {
                        var et = T.GetElementType();
                        var av = Array.CreateInstance(et, 4);
                        av.SetValue(v.r.Convert(TFloat, et), 0);
                        av.SetValue(v.g.Convert(TFloat, et), 1);
                        av.SetValue(v.b.Convert(TFloat, et), 2);
                        av.SetValue(v.a.Convert(TFloat, et), 3);
                        return av;
                    }
                }
                else if (TValue == TString)
                {
                    var str = (string)value;
                    if (T == TBool)
                    {
                        if (bool.TryParse(str, out bool v)) { cvalue = v; }
                    }
                    else if (T == TUInt)
                    {
                        if (uint.TryParse(str, out uint v)) { cvalue = v; }
                    }
                    else if (T == TInt)
                    {
                        if (int.TryParse(str, out int v)) { cvalue = v; }
                    }
                    else if (T == TFloat)
                    {
                        if (float.TryParse(str, out float v)) { cvalue = v; }
                    }
                    else if (T == TDouble)
                    {
                        if (double.TryParse(str, out double v)) { cvalue = v; }
                    }
                    else if (T == TVector2)
                    {
                        var n = 2;
                        var vs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (vs.Length >= n) // here allow e.g. Vector4 as Vector2
                        {
                            var fvs = new float[n];
                            if (Enumerable.Range(0, n).Select(i => float.TryParse(vs[i], out fvs[i])).All(i => i))
                            {
                                cvalue = new Vector2(fvs[0], fvs[1]);
                            }
                        }
                    }
                    else if (T == TVector3)
                    {
                        var n = 3;
                        var vs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (vs.Length >= n)
                        {
                            var fvs = new float[n];
                            if (Enumerable.Range(0, n).Select(i => float.TryParse(vs[i], out fvs[i])).All(i => i))
                            {
                                cvalue = new Vector3(fvs[0], fvs[1], fvs[2]);
                            }
                        }
                    }
                    else if (T == TVector4)
                    {
                        var n = 4;
                        var vs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (vs.Length >= n)
                        {
                            var fvs = new float[n];
                            if (Enumerable.Range(0, n).Select(i => float.TryParse(vs[i], out fvs[i])).All(i => i))
                            {
                                cvalue = new Vector4(fvs[0], fvs[1], fvs[2], fvs[3]);
                            }
                        }
                    }
                    else if (T == TColor)
                    {
                        var n = 4;
                        var vs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (vs.Length >= n)
                        {
                            var fvs = new float[n];
                            if (Enumerable.Range(0, n).Select(i => float.TryParse(vs[i], out fvs[i])).All(i => i))
                            {
                                cvalue = new Color(fvs[0], fvs[1], fvs[2], fvs[3]);
                            }
                        }
                    }
                    else if (T.IsEnum)
                    {
                        if (Enum.TryParse(T, str, out object v)) { cvalue = v; }
                    }
                    else if (T == TFixString512)
                    {
                        cvalue = new FixedString512Bytes(str);
                    }
                    else if (T.IsValueType) // for all other value-types
                    {
                        isfallback = true;
                    }
                    else // for all other non-value-types, we assume the string value is its yaml serialization(List, Dict, Classes, etc.)
                    {
                        try { cvalue = str.DeserializeYaml(T); }
                        catch (Exception ex) { /*Debug.LogException(ex);*/ }
                    }
                }
                else if (TValue.IsGenericType && TValue.GetGenericTypeDefinition() == TListT)
                {
                    if (T.IsGenericType && T.GetGenericTypeDefinition() == TListT)
                    {
                        var TT = T.GetGenericArguments()[0];
                        var v = Activator.CreateInstance(T).AsList();
                        foreach (var i in value.AsList())
                        {
                            v.Add(i.Convert(TT));
                        }
                        cvalue = v;
                    }
                    else if (T == TVector2)
                    {
                        var list = value.AsList();
                        var n = 2;
                        if (list.Count >= n)
                        {
                            var fvs = Enumerable.Range(0, n).Select(i => list[i].Convert(TFloat)).ToArray();
                            if (fvs.All(i => i != null))
                            {
                                cvalue = new Vector2((float)fvs[0], (float)fvs[1]);
                            }
                        }
                    }
                    else if (T == TVector3)
                    {
                        var list = value.AsList();
                        var n = 3;
                        if (list.Count >= n)
                        {
                            var fvs = Enumerable.Range(0, n).Select(i => list[i].Convert(TFloat)).ToArray();
                            if (fvs.All(i => i != null))
                            {
                                cvalue = new Vector3((float)fvs[0], (float)fvs[1], (float)fvs[2]);
                            }
                        }
                    }
                    else if (T == TVector4)
                    {
                        var list = value.AsList();
                        var n = 4;
                        if (list.Count >= n)
                        {
                            var fvs = Enumerable.Range(0, n).Select(i => list[i].Convert(TFloat)).ToArray();
                            if (fvs.All(i => i != null))
                            {
                                cvalue = new Vector4((float)fvs[0], (float)fvs[1], (float)fvs[2], (float)fvs[3]);
                            }
                        }
                    }
                    else if (T == TColor)
                    {
                        var list = value.AsList();
                        var n = 4;
                        if (list.Count >= n)
                        {
                            var fvs = Enumerable.Range(0, n).Select(i => list[i].Convert(TFloat)).ToArray();
                            if (fvs.All(i => i != null))
                            {
                                cvalue = new Color((float)fvs[0], (float)fvs[1], (float)fvs[2], (float)fvs[3]);
                            }
                        }
                    }
                    else if (T == TString)
                    {
                        var list = value.AsList();
                        cvalue = '[' + string.Join(", ", Enumerable.Range(0, list.Count).Select(i => list[i].Convert(TString))) + ']';
                    }
                }
                else // other Types of value
                {
                    if (T == TString)
                    {
                        cvalue = value.SerializeYaml();
                    }
                    else { isfallback = true; }
                }

                if (isfallback)
                {
                    try { cvalue = System.Convert.ChangeType(value, T); }
                    catch (Exception ex) { /*Debug.LogException(ex);*/ }
                }

                return cvalue;
            }
        }
        #endregion

        public static List<int> Permutation(this System.Random rng, int maxexclusive)
        {
            var seq = Enumerable.Repeat(-1, maxexclusive).ToList();
            int i, j;
            for (i = 0; i < maxexclusive; i++)
            {
                do
                {
                    j = rng.Next(maxexclusive);
                }
                while (seq[j] >= 0);
                seq[j] = i;
            }
            return seq;
        }

        public static List<T> Shuffle<T>(this System.Random rng, List<T> seq)
        {
            return rng.Permutation(seq.Count).Select(i => seq[i]).ToList();
        }

        public static void Scale01(this List<double> data)
        {
            if (data == null || data.Count < 2) return;
            var min = data.Min();
            var max = data.Max();
            var range = max - min;
            for (var i = 0; i < data.Count; i++)
            {
                data[i] = (data[i] - min) / range;
            }
        }

        public static void CopyDirectory(this string sourceDirectory, string targetDirectory, string excludeExt = ". ")
        {
            DirectoryInfo dirSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo dirTarget = new DirectoryInfo(targetDirectory);

            CopyDirectory(dirSource, dirTarget, excludeExt);
        }

        public static void CopyDirectory(DirectoryInfo source, DirectoryInfo target, string excludeExt = ". ")
        {
            Directory.CreateDirectory(target.FullName);

            foreach (FileInfo fi in source.GetFiles().Where(i => !i.Extension.StartsWith(excludeExt)))
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo sourceSubDir in source.GetDirectories())
            {
                DirectoryInfo targetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                CopyDirectory(sourceSubDir, targetSubDir, excludeExt);
            }
        }

        public static Dictionary<string, string> GetDefinationFiles(this string indir, string definationtype, string ext = ".yaml", bool createdir = true)
        {
            if (Directory.Exists(indir))
            {
                var files = Directory.GetFiles(indir, $"*{ext}", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    Debug.Log($"{definationtype} Defination Directory: \"{indir}\" Is Empty.");
                }
                else
                {
                    Dictionary<string, string> deffiles = new();
                    foreach (var f in files)
                    {
                        deffiles[Path.GetFileNameWithoutExtension(f)] = f;
                    }
                    return deffiles;
                }
            }
            else
            {
                Debug.Log($"{definationtype} Defination Directory: \"{indir}\" Not Exist{(createdir ? ", Create the Directory" : "")}.");
                if (createdir) { Directory.CreateDirectory(indir); }
            }
            return null;
        }

        //public static Dictionary<string, List<object>> ResolveConditionReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    return cond.ResolveCondFactorReference(param).ResolveCondLevelReference(param);
        //}

        ///// <summary>
        ///// Replace all factor values with known reference in experiment parameters
        ///// </summary>
        ///// <param name="cond"></param>
        ///// <param name="param"></param>
        ///// <returns></returns>
        //public static Dictionary<string, List<object>> ResolveCondFactorReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    foreach (var f in cond.Keys.ToList())
        //    {
        //        if (f.Count() > 1 && f.First() == '$')
        //        {
        //            var rf = f.Substring(1);
        //            if (param.ContainsKey(rf) && param[rf] != null && param[rf].Type.IsList())
        //            {
        //                var fl = cond[f]; fl.Clear();
        //                foreach (var i in (IEnumerable)param[rf].Value)
        //                {
        //                    fl.Add(i);
        //                }
        //                cond.Remove(f);
        //                cond[rf] = fl;
        //            }
        //        }
        //    }
        //    return cond;
        //}

        ///// <summary>
        ///// Replace factor values with known reference in experiment parameter
        ///// </summary>
        ///// <param name="cond"></param>
        ///// <param name="param"></param>
        ///// <returns></returns>
        //public static Dictionary<string, List<object>> ResolveCondLevelReference(this Dictionary<string, List<object>> cond, Dictionary<string, Param> param)
        //{
        //    foreach (var f in cond.Keys)
        //    {
        //        for (var i = 0; i < cond[f].Count; i++)
        //        {
        //            if (cond[f][i].GetType() == typeof(string))
        //            {
        //                var v = (string)cond[f][i];
        //                if (v.Count() > 1 && v.First() == '$')
        //                {
        //                    var r = v.Substring(1);
        //                    if (param.ContainsKey(r) && param[r] != null)
        //                    {
        //                        cond[f][i] = param[r].Value;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return cond;
        //}

#if COMMAND
        public static IRecorder GetSpikeGLXRecorder(string host = "localhost", int port = 4142)
        {
            if (spikeglxrecorder == null)
            {
                var r = new SpikeGLXRecorder(host, port);
                if (r.IsConnected) { r.SetRecordingBeep(); spikeglxrecorder = r; }
                return spikeglxrecorder;
            }
            else
            {
                if (spikeglxrecorder.GetType() == typeof(SpikeGLXRecorder))
                {
                    var r = spikeglxrecorder as SpikeGLXRecorder;
                    r.Disconnect();
                    if (r.Connect(host, port))
                    { r.SetRecordingBeep(); }
                    else { spikeglxrecorder = null; }
                    return spikeglxrecorder;
                }
                else
                {
                    spikeglxrecorder.Dispose();
                    spikeglxrecorder = null;
                    return GetSpikeGLXRecorder(host, port);
                }
            }
        }

        public static IRecorder GetImagerRecorder(string host = "localhost", int port = 10000)
        {
            if (imagerrecorder == null)
            {
                var r = new ImagerRecorder(host, port);
                if (r.IsConnected) { imagerrecorder = r; }
                return imagerrecorder;
            }
            else
            {
                if (imagerrecorder.GetType() == typeof(ImagerRecorder))
                {
                    var r = imagerrecorder as ImagerRecorder;
                    r.Disconnect();
                    if (!r.Connect(host, port)) { imagerrecorder = null; }
                    return imagerrecorder;
                }
                else
                {
                    imagerrecorder.Dispose();
                    imagerrecorder = null;
                    return GetImagerRecorder(host, port);
                }
            }
        }

        #region Condition
        public static bool ValidateFactorDesign(this Dictionary<string, object> design)
        {
            if (design.ContainsKey("Start") && design["Start"] != null && design.ContainsKey("N") && design["N"] != null && design.ContainsKey("Method") && design["Method"] != null)
            {
                var T = design["Start"].GetType();
                if ((design.ContainsKey("Step") && design["Step"] != null && T == design["Step"].GetType()) || (design.ContainsKey("Stop") && design["Stop"] != null && T == design["Stop"].GetType()))
                {
                    if (design["Method"].GetType() == typeof(string) && Enum.TryParse((string)design["Method"], out FactorDesignMethod method))
                    {
                        design["Method"] = method;
                        return true;
                    }
                }
            }
            return false;
        }

        public static Dictionary<string, List<object>> ProcessFactorDesign(this Dictionary<string, List<object>> conddesign)
        {
            foreach (var f in conddesign.Keys.ToArray())
            {
                var fd = conddesign[f];
                if (fd.Count == 2 && fd[0].GetType() == typeof(string) && (string)fd[0] == "FactorDesign" && fd[1].GetType() == typeof(Dictionary<object, object>))
                {
                    var design = ((Dictionary<object, object>)fd[1]).ToDictionary(kv => (string)kv.Key, kv => kv.Value);
                    if (design.ValidateFactorDesign())
                    {
                        conddesign[f] = design.FactorLevelOfDesign();
                    }
                }
            }
            return conddesign;
        }

        public static float[] Range(this FactorDesignMethod method, float start, int n, float? step, float? stop)
        {
            if (step.HasValue)
            {
                var s = step.Value;
                return method switch
                {
                    FactorDesignMethod.Power => Enumerable.Range(0, n).Select(i => 1 + i * s).Select(i => Mathf.Pow(start, i)).ToArray(),
                    _ => Enumerable.Range(0, n).Select(i => start + i * s).ToArray(),
                };
            }
            else if (stop.HasValue)
            {
                var s = stop.Value;
                return method switch
                {
                    FactorDesignMethod.Log10 => Generate.LogSpacedMap(n, start, s, i => (float)i).ToArray(),
                    _ => Generate.LinearSpacedMap(n, start, s, i => (float)i).ToArray(),
                };
            }
            return null;
        }

        public static List<object> FactorLevelOfDesign(this Dictionary<string, object> design)
        {
            var Start = design["Start"]; var N = design["N"]; var T = Start.GetType();
            var Method = (FactorDesignMethod)design["Method"];
            bool? OrthoCombine = design.ContainsKey("OrthoCombine") ? design["OrthoCombine"].Convert<bool>() : null;
            var Stop = design.ContainsKey("Stop") ? design["Stop"] : null;
            var Step = design.ContainsKey("Step") ? design["Step"] : null;
            List<object> ls = new();

            if (T.IsNumeric())
            {
                ls = Method.Range(Start.Convert<float>(T), N.Convert<int>(), Step?.Convert<float>(T), Stop?.Convert<float>(T)).Select(i => (object)i).ToList();
            }
            else if (T == typeof(Vector3))
            {
                var b = (Vector3)Start;
                var n = N.Convert<int[]>();
                var s = Step?.Convert<Vector3>(T);
                var e = Stop?.Convert<Vector3>(T);

                var xl = Method.Range(b.x, n[0], s?.x, e?.x);
                var yl = Method.Range(b.y, n[1], s?.y, e?.y);
                var zl = Method.Range(b.z, n[2], s?.z, e?.z);

                if (OrthoCombine.HasValue && OrthoCombine.Value)
                {
                    for (var xi = 0; xi < xl.Length; xi++)
                    {
                        for (var yi = 0; yi < yl.Length; yi++)
                        {
                            for (var zi = 0; zi < zl.Length; zi++)
                            {
                                ls.Add(new Vector3(xl[xi], yl[yi], zl[zi]));
                            }
                        }
                    }
                }
                else
                {
                    for (var xi = 0; xi < xl.Length; xi++)
                    {
                        ls.Add(new Vector3(xl[xi], yl[0], zl[0]));
                    }
                    for (var yi = 0; yi < yl.Length; yi++)
                    {
                        ls.Add(new Vector3(xl[0], yl[yi], zl[0]));
                    }
                    for (var zi = 0; zi < zl.Length; zi++)
                    {
                        ls.Add(new Vector3(xl[0], yl[0], zl[zi]));
                    }
                    ls = ls.Distinct().ToList();
                }
            }
            else if (T == typeof(Color))
            {
                var b = (Color)Start;
                var n = N.Convert<int[]>();
                var s = Step?.Convert<Color>(T);
                var e = Stop?.Convert<Color>(T);

                var rl = Method.Range(b.r, n[0], s?.r, e?.r);
                var gl = Method.Range(b.g, n[1], s?.g, e?.g);
                var bl = Method.Range(b.b, n[2], s?.b, e?.b);
                var al = Method.Range(b.a, n[3], s?.a, e?.a);

                if (OrthoCombine.HasValue && OrthoCombine.Value)
                {
                    for (var ri = 0; ri < rl.Length; ri++)
                    {
                        for (var gi = 0; gi < gl.Length; gi++)
                        {
                            for (var bi = 0; bi < bl.Length; bi++)
                            {
                                for (var ai = 0; ai < al.Length; ai++)
                                {
                                    ls.Add(new Color(rl[ri], gl[gi], bl[bi], al[ai]));
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (var ri = 0; ri < rl.Length; ri++)
                    {
                        ls.Add(new Color(rl[ri], gl[0], bl[0], al[0]));
                    }
                    for (var gi = 0; gi < gl.Length; gi++)
                    {
                        ls.Add(new Color(rl[0], gl[gi], bl[0], al[0]));
                    }
                    for (var bi = 0; bi < bl.Length; bi++)
                    {
                        ls.Add(new Color(rl[0], gl[0], bl[bi], al[0]));
                    }
                    for (var ai = 0; ai < al.Length; ai++)
                    {
                        ls.Add(new Color(rl[0], gl[0], bl[0], al[ai]));
                    }
                    ls = ls.Distinct().ToList();
                }
            }

            return ls;
        }

        public static Dictionary<string, List<object>> ProcessOrthoCombineFactor(this Dictionary<string, List<object>> cond)
        {
            if (cond.ContainsKey("OrthoCombineFactor") && cond["OrthoCombineFactor"].Count == 0)
            {
                return cond.OrthoCombineFactor();
            }
            return cond;
        }

        public static Dictionary<string, List<object>> OrthoCombineFactor(this Dictionary<string, List<object>> fsls)
        {
            foreach (var f in fsls.Keys.ToArray())
            {
                if (fsls[f].Count == 0)
                {
                    fsls.Remove(f);
                }
            }

            var fn = fsls.Count;
            if (fn < 2) { Debug.LogWarning($"Only {fn} Factor, Skip OrthoCombineFactor ..."); return fsls; }

            var cond = new Dictionary<string, List<object>>();
            var fs = fsls.Keys.ToArray();
            int[] irn = new int[fn];
            int[] fln = new int[fn];
            int cn = 1;
            for (var i = 0; i < fn; i++)
            {
                var n = fsls[fs[i]].Count;
                fln[i] = n;
                cn *= n;
                if (i == 0) { irn[i] = 1; }
                else { irn[i] = fln[i - 1] * irn[i - 1]; }
            }

            for (var i = 0; i < fn; i++)
            {
                List<object> ir = new();
                for (var l = 0; l < fln[i]; l++)
                {
                    for (var r = 0; r < irn[i]; r++)
                    {
                        ir.Add(fsls[fs[i]][l]);
                    }
                }
                var orn = cn / ir.Count;
                List<object> or = new();
                for (var r = 0; r < orn; r++)
                {
                    or.AddRange(ir);
                }
                cond[fs[i]] = or;
            }
            return cond;
        }

        //public static Type GetFactorValueType(this string factorname)
        //{
        //    switch (factorname)
        //    {
        //        case "Luminance":
        //        case "Contrast":
        //        case "Diameter":
        //        case "SpatialFreq":
        //        case "SpatialPhase":
        //        case "TemporalFreq":
        //        case "Ori":
        //        case "OriOffset":
        //        case "Ori_Final":
        //        case "Speed":
        //            return typeof(float);
        //        case "Rotation":
        //        case "RotationOffset":
        //        case "Rotation_Final":
        //        case "Position":
        //        case "PositionOffset":
        //        case "Position_Final":
        //            return typeof(Vector3);
        //        case "Color":
        //        case "BGColor":
        //            return typeof(Color);
        //        default:
        //            return null;
        //    }
        //}

        public static Dictionary<string, IList> FinalizeFactorValue(this Dictionary<string, List<object>> cond)
        {
            if (cond == null) { return null; }
            var final = new Dictionary<string, IList>();
            foreach (var f in cond.Keys)
            {
                var fvt = cond[f][0].GetType();
                var fvs = Activator.CreateInstance(typeof(List<>).MakeGenericType(fvt)).AsList();
                cond[f].ForEach(i => fvs.Add(i.Convert(fvt)));
                final[f] = fvs;
            }
            return final;
        }

        public static Dictionary<string, IList> CondGroup(this Dictionary<string, IList> cond, List<string> groupingfactor, out List<List<int>> gi)
        {
            var n = cond.Values.First().Count;
            var group = new Dictionary<string, IList>();
            foreach (var f in groupingfactor)
            {
                var t = cond[f][0].GetType();

                var l = Activator.CreateInstance(typeof(List<>).MakeGenericType(t)).AsList();
                l.Add(cond[f][0]);
                group[f] = l;
            }
            gi = new List<List<int>>() { new List<int>() { 0 } };

            bool isequal = true;
            for (var i = 1; i < n; i++)
            {
                for (var j = 0; j < gi.Count; j++)
                {
                    isequal = true;
                    foreach (var f in groupingfactor)
                    {
                        isequal &= Equals(cond[f][i], group[f][j]); // compare value, instead of ref (==)
                    }
                    if (isequal)
                    {
                        gi[j].Add(i); break;
                    }
                }
                if (!isequal)
                {
                    gi.Add(new List<int>() { i });
                    foreach (var f in groupingfactor)
                    {
                        group[f].Add(cond[f][i]);
                    }
                }
            }
            return group;
        }
        #endregion
        //public static string GetAddresses(this string experimenter, CommandConfig config)
        //{
        //    string addresses = null;
        //    if (string.IsNullOrEmpty(experimenter)) return addresses;
        //    var al = experimenter.Split(',', ';').Where(i => config.ExperimenterAddress.ContainsKey(i)).Select(i => config.ExperimenterAddress[i]).ToArray();
        //    if (al != null && al.Length > 0)
        //    {
        //        addresses = string.Join(",", al);
        //    }
        //    return addresses;
        //}

        //public static ILaser GetLaser(this string lasername, CommandConfig config)
        //{
        //    switch (lasername)
        //    {
        //        case "luxx473":
        //            return new Omicron(config.SerialPort0);
        //        case "mambo594":
        //            return new Cobolt(config.SerialPort1);
        //    }
        //    return null;
        //}

        public static Assembly CompileFile(this string sourcepath)
        {
            return File.ReadAllText(sourcepath).Compile();
        }

        public static Assembly Compile(this string source)
        {
            // currently not really needed, so desable them


            //var sourcetree = CSharpSyntaxTree.ParseText(source);
            //var compilation = CSharpCompilation.Create("sdfsdf")
            //    .AddReferences()
            //    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            //                     .WithOptimizationLevel(OptimizationLevel.Release))
            //    .AddSyntaxTrees(sourcetree);
            //using (var asm = new MemoryStream())
            //{
            //    var emitresult = compilation.Emit(asm);
            //    if (emitresult.Success)
            //    {
            //        return Assembly.Load(asm.GetBuffer());
            //    }
            //}
            return null;
        }

        public static string OpenFile(string title = "Open File ...")
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = title,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "File (*.yaml;*.cs)|*.yaml;*.cs|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static string SaveFile(string title = "Save File ...")
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = title,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "File (*.yaml;*.cs)|*.yaml;*.cs|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static string ChooseDir(string title = "Choose Directory ...")
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = title;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        public static bool YesNoDialog(string msg = "Yes or No?")
        {
            if (MessageBox.Show(msg, "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                return true;
            }
            return false;
        }

        public static void WarningDialog(string msg = "This is a Warning.")
        {
            MessageBox.Show(msg, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static Display GetDisplay(this string displayid, Dictionary<string, Display> displays)
        {
            if (!string.IsNullOrEmpty(displayid) && displays != null && displays.ContainsKey(displayid))
            {
                return displays[displayid];
            }
            Debug.LogWarning($"Display ID: {displayid} can not be found.");
            return null;
        }

        //public static double? DisplayLatency(this string displayid, Dictionary<string, Display> displays)
        //{
        //    var d = displayid.GetDisplay(displays);
        //    if (d != null && d.Latency >= 0) { return d.Latency; }
        //    return null;
        //}

        //public static double? DisplayResponseTime(this string displayid, Dictionary<string, Display> displays)
        //{
        //    var d = displayid.GetDisplay(displays);
        //    if (d != null)
        //    {
        //        var r = Math.Max(d.RiseLag, d.FallLag);
        //        if (r >= 0) { return r; }
        //    }
        //    return null;
        //}

        //public static double? DisplayLatencyPlusResponseTime(this string displayid, Dictionary<string, Display> displays)
        //{
        //    var d = displayid.GetDisplay(displays);
        //    if (d != null)
        //    {
        //        return Math.Max(0, d.Latency) + Math.Max(0, Math.Max(d.RiseLag, d.FallLag));
        //    }
        //    return null;
        //}

        public static double GammaFunc(double x, double gamma, double a = 1, double c = 0)
        {
            return a * Math.Pow(x, gamma) + c;
        }

        public static double CounterGammaFunc(double x, double gamma, double a = 1, double c = 0)
        {
            return a * Math.Pow(x, 1 / gamma) + c;
        }

        public static bool GammaFit(double[] x, double[] y, out double gamma, out double amp, out double cons)
        {
            gamma = 0; amp = 0; cons = 0;
            try
            {
                var param = Fit.Curve(x, y, (g, a, c, i) => GammaFunc(i, g, a, c), 1, 1, 0);
                gamma = param.Item1; amp = param.Item2; cons = param.Item3;
                return true;
            }
            catch (Exception) { }
            return false;
        }

        //public static bool SplineFit(double[] x, double[] y, out IInterpolation spline, DisplayFitType fittype = DisplayFitType.LinearSpline)
        //{
        //    spline = null;
        //    try
        //    {
        //        switch (fittype)
        //        {
        //            case DisplayFitType.LinearSpline:
        //                spline = Interpolate.Linear(x, y);
        //                return true;
        //            case DisplayFitType.CubicSpline:
        //                spline = Interpolate.CubicSpline(x, y);
        //                return true;
        //        }
        //        return false;
        //    }
        //    catch (Exception) { }
        //    return false;
        //}

        /// <summary>
        /// Get Independent R,G,B channel measurement
        /// </summary>
        /// <param name="m"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isnormalize"></param>
        /// <param name="issort"></param>
        public static void GetRGBIntensityMeasurement(this Dictionary<string, List<object>> m, out Dictionary<string, double[]> x, out Dictionary<string, double[]> y, bool isnormalize = false, bool issort = false)
        {
            var colors = m["Color"].Convert<List<Color>>();
            var intensities = m["Y"].Convert<List<double>>();

            var rs = new List<double>(); var gs = new List<double>(); var bs = new List<double>();
            var rys = new List<double>(); var gys = new List<double>(); var bys = new List<double>();
            for (var j = 0; j < colors.Count; j++)
            {
                var c = colors[j]; var i = intensities[j];
                if (c.r == 0 && c.g == 0 && c.b == 0)
                {
                    rs.Add(c.r);
                    rys.Add(i);
                    gs.Add(c.g);
                    gys.Add(i);
                    bs.Add(c.b);
                    bys.Add(i);
                }
                else
                {
                    if (c.g == 0 && c.b == 0)
                    {
                        rs.Add(c.r);
                        rys.Add(i);
                    }
                    if (c.r == 0 && c.b == 0)
                    {
                        gs.Add(c.g);
                        gys.Add(i);
                    }
                    if (c.r == 0 && c.g == 0)
                    {
                        bs.Add(c.b);
                        bys.Add(i);
                    }
                }
            }
            if (issort)
            {
                Sorting.Sort(rs, rys); Sorting.Sort(gs, gys); Sorting.Sort(bs, bys);
            }
            if (isnormalize)
            {
                rys.Scale01(); gys.Scale01(); bys.Scale01();
            }
            x = new Dictionary<string, double[]>() { { "R", rs.ToArray() }, { "G", gs.ToArray() }, { "B", bs.ToArray() } };
            y = new Dictionary<string, double[]>() { { "R", rys.ToArray() }, { "G", gys.ToArray() }, { "B", bys.ToArray() } };
        }

        public static void GetRGBSpectralMeasurement(this Dictionary<string, List<object>> m, out Dictionary<string, double[]> x, out Dictionary<string, double[][]> yi, out Dictionary<string, double[][]> y)
        {
            var colors = m["Color"].Convert<List<Color>>();
            var wls = m["WL"].Convert<List<double[]>>();
            var wlis = m["Spectral"].Convert<List<double[]>>();

            var rs = new List<double>(); var gs = new List<double>(); var bs = new List<double>();
            var rwls = new List<double[]>(); var gwls = new List<double[]>(); var bwls = new List<double[]>();
            var rwlis = new List<double[]>(); var gwlis = new List<double[]>(); var bwlis = new List<double[]>();
            for (var j = 0; j < colors.Count; j++)
            {
                var c = colors[j]; var wl = wls[j]; var wli = wlis[j];
                if (c.r == 0 && c.g == 0 && c.b == 0)
                {
                    rs.Add(c.r);
                    rwls.Add(wl);
                    rwlis.Add(wli);
                    gs.Add(c.g);
                    gwls.Add(wl);
                    gwlis.Add(wli);
                    bs.Add(c.b);
                    bwls.Add(wl);
                    bwlis.Add(wli);
                }
                else
                {
                    if (c.g == 0 && c.b == 0)
                    {
                        rs.Add(c.r);
                        rwls.Add(wl);
                        rwlis.Add(wli);
                    }
                    if (c.r == 0 && c.b == 0)
                    {
                        gs.Add(c.g);
                        gwls.Add(wl);
                        gwlis.Add(wli);
                    }
                    if (c.r == 0 && c.g == 0)
                    {
                        bs.Add(c.b);
                        bwls.Add(wl);
                        bwlis.Add(wli);
                    }
                }
            }
            x = new Dictionary<string, double[]>() { { "R", rs.ToArray() }, { "G", gs.ToArray() }, { "B", bs.ToArray() } };
            yi = new Dictionary<string, double[][]> { { "R", rwls.ToArray() }, { "G", gwls.ToArray() }, { "B", bwls.ToArray() } };
            y = new Dictionary<string, double[][]>() { { "R", rwlis.ToArray() }, { "G", gwlis.ToArray() }, { "B", bwlis.ToArray() } };
        }

        public static Texture3D GenerateRGBGammaCLUT(double rgamma, double ggamma, double bgamma, double ra, double ga, double ba, double rc, double gc, double bc, int n)
        {
            var xx = Generate.LinearSpaced(n, 0, 1);
            var riy = Generate.Map(xx, i => (float)CounterGammaFunc(i, rgamma, ra, rc));
            var giy = Generate.Map(xx, i => (float)CounterGammaFunc(i, ggamma, ga, gc));
            var biy = Generate.Map(xx, i => (float)CounterGammaFunc(i, bgamma, ba, bc));

            var clut = new Texture3D(n, n, n, TextureFormat.RGB24, false);
            for (var r = 0; r < n; r++)
            {
                for (var g = 0; g < n; g++)
                {
                    for (var b = 0; b < n; b++)
                    {
                        clut.SetPixel(r, g, b, new Color(riy[r], giy[g], biy[b]));
                    }
                }
            }
            clut.Apply();
            return clut;
        }

        public static Texture3D GenerateRGBSplineCLUT(IInterpolation rii, IInterpolation gii, IInterpolation bii, int n)
        {
            var xx = Generate.LinearSpaced(n, 0, 1);
            var riy = Generate.Map(xx, i => (float)rii.Interpolate(i));
            var giy = Generate.Map(xx, i => (float)gii.Interpolate(i));
            var biy = Generate.Map(xx, i => (float)bii.Interpolate(i));

            var clut = new Texture3D(n, n, n, TextureFormat.RGB24, false);
            for (var r = 0; r < n; r++)
            {
                for (var g = 0; g < n; g++)
                {
                    for (var b = 0; b < n; b++)
                    {
                        clut.SetPixel(r, g, b, new Color(riy[r], giy[g], biy[b]));
                        //clut.SetPixel(r, g, b, new Color(riy[r].sRGBEncode(), giy[g].sRGBEncode(), biy[b].sRGBEncode()));
                        //clut.SetPixel(r, g, b, new Color((float)r /(n-1), (float)g /(n-1), (float)b /(n-1)));
                        //clut.SetPixel(r, g, b, new Color(((float)r / (n - 1)).sRGBEncode(), ((float)g / (n - 1)).sRGBEncode(), ((float)b / (n - 1)).sRGBEncode()));
                    }
                }
            }
            clut.Apply();
            return clut;
        }

        public static float sRGBEncode(this float x)
        {
            return x <= 0.0031308f ? 12.92f * x : 1.055f * Mathf.Pow(x, 1f / 2.4f) - 0.055f;
        }

        public static float sRGBDecode(this float x)
        {
            return x <= 0.04045f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
        }

        /// <summary>
        /// Prepare Color Look-Up Table based on display R,G,B intensity measurement
        /// </summary>
        /// <param name="display"></param>
        /// <param name="forceprepare"></param>
        /// <returns></returns>
        //public static bool PrepareCLUT(this Display display, bool forceprepare = false)
        //{
        //    if (display.CLUT != null && !forceprepare) { return true; }
        //    var m = display.IntensityMeasurement;
        //    if (m == null || m.Count == 0) { return false; }

        //    Dictionary<string, double[]> x, y;
        //    switch (display.FitType)
        //    {
        //        case DisplayFitType.Gamma:
        //            m.GetRGBIntensityMeasurement(out x, out y, false, true);
        //            double rgamma, ra, rc, ggamma, ga, gc, bgamma, ba, bc;
        //            GammaFit(x["R"], y["R"], out rgamma, out ra, out rc);
        //            GammaFit(x["G"], y["G"], out ggamma, out ga, out gc);
        //            GammaFit(x["B"], y["B"], out bgamma, out ba, out bc);
        //            display.CLUT = GenerateRGBGammaCLUT(rgamma, ggamma, bgamma, ra, ga, ba, rc, gc, bc, display.CLUTSize);
        //            break;
        //        case DisplayFitType.LinearSpline:
        //        case DisplayFitType.CubicSpline:
        //            m.GetRGBIntensityMeasurement(out x, out y, true, true);
        //            IInterpolation rii, gii, bii;
        //            SplineFit(y["R"], x["R"], out rii, display.FitType);
        //            SplineFit(y["G"], x["G"], out gii, display.FitType);
        //            SplineFit(y["B"], x["B"], out bii, display.FitType);
        //            if (rii != null && gii != null && bii != null)
        //            {
        //                display.CLUT = GenerateRGBSplineCLUT(rii, gii, bii, display.CLUTSize);
        //            }
        //            break;
        //    }
        //    return display.CLUT == null ? false : true;
        //}
#endif


        /// <summary>
        /// Get a unique file incremental index that fits in the file name pattern within a dir.
        /// Index is supposed to be the last part before file extension in pattern: *_{index}.*
        /// </summary>
        /// <param name="filepattern"></param>
        /// <param name="indir"></param>
        /// <param name="searchoption"></param>
        /// <returns></returns>
        public static int SearchIndexForNewFile(this string filepattern, string indir, SearchOption searchoption = SearchOption.AllDirectories)
        {
            int i = 0;
            if (Directory.Exists(indir))
            {
                var fs = Directory.GetFiles(indir, filepattern, searchoption);
                if (fs.Length > 0)
                {
                    var ns = new List<int>();
                    foreach (var f in fs)
                    {
                        var s = f.LastIndexOf('_') + 1;
                        var e = f.LastIndexOf('.') - 1;
                        if (int.TryParse(f.Substring(s, e - s + 1), out int n))
                        {
                            ns.Add(n);
                        }
                    }
                    if (ns.Count > 0) { i = ns.Max() + 1; }
                }
            }
            return i;
        }

        #region NetEnv ParamName Parsing
        public static bool SplitEnvParamFullName(this string fullName, out string[] ns, char separator = '@', int count = 3)
        {
            ns = fullName.Split(separator, count, StringSplitOptions.RemoveEmptyEntries);
            if (ns.Length < count)
            {
                return false;
            }
            return true;
        }

        public static bool FirstSplit(this string name, out string head, out string tail, string del = "@")
        {
            head = tail = null;
            if (string.IsNullOrEmpty(name)) { return false; }
            var n = del.Length;
            var i = name.IndexOf(del);
            if (i == 0)
            {
                tail = name.Substring(n);
                return true;
            }
            else if (i > 0)
            {
                head = name.Substring(0, i);
                tail = name.Substring(i + n);
                return true;
            }
            else
            {
                head = name;
                return false;
            }
        }

        public static string FirstSplitHead(this string name, string del = "@")
        {
            name.FirstSplit(out string head, out _, del);
            return head;
        }

        public static string FirstSplitTail(this string name, string del = "@")
        {
            name.FirstSplit(out _, out string tail, del);
            return tail;
        }

        public static bool LastSplit(this string name, out string head, out string tail, string del = "@")
        {
            head = tail = null;
            if (string.IsNullOrEmpty(name)) { return false; }
            var n = del.Length;
            var i = name.LastIndexOf(del);
            if (i == 0)
            {
                tail = name.Substring(n);
                return true;
            }
            else if (i > 0)
            {
                head = name.Substring(0, i);
                tail = name.Substring(i + n);
                return true;
            }
            else
            {
                head = name;
                return false;
            }
        }

        public static string LastSplitHead(this string name, string del = "@")
        {
            name.LastSplit(out string head, out _, del);
            return head;
        }

        public static string LastSplitTail(this string name, string del = "@")
        {
            name.LastSplit(out _, out string tail, del);
            return tail;
        }
        #endregion

        /// <summary>
        /// Luminance span based on average luminance and michelson contrast(symmatric min and max luminance)
        /// </summary>
        /// <param name="luminance"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        public static float LuminanceSpan(float luminance, float contrast)
        {
            return 2 * luminance * contrast;
        }

        /// <summary>
        /// Symmatric scale between mincolor and maxcolor
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="minc"></param>
        /// <param name="maxc"></param>
        /// <param name="sminc"></param>
        /// <param name="smaxc"></param>
        public static void ScaleColor(this float scale, Color minc, Color maxc, out Color sminc, out Color smaxc)
        {
            var mc = (minc + maxc) / 2;
            var dmc = maxc - mc;
            sminc = new Color(mc.r - dmc.r * scale, mc.g - dmc.g * scale, mc.b - dmc.b * scale, minc.a);
            smaxc = new Color(mc.r + dmc.r * scale, mc.g + dmc.g * scale, mc.b + dmc.b * scale, maxc.a);
        }

        public static Vector3 RotateZCCW(this Vector3 v, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v;
        }

        public static string[] ValidStrings(params string[] ss)
        {
            var r = new List<string>();
            if (ss.Length > 0)
            {
                foreach (var s in ss)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        r.Add(s);
                    }
                }
            }
            return r.ToArray();
        }

        public static bool IsFollowEnvCrossInheritRule(this Dictionary<string, Dictionary<string, List<string>>> rule, string to, string from, string param)
        {
            if (rule.ContainsKey(to))
            {
                var fp = rule[to];
                if (fp.ContainsKey(from) && fp[from].Contains(param))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsEnvCrossInheritTo(this Dictionary<string, Dictionary<string, List<string>>> rule, string to)
        {
            return rule.ContainsKey(to);
        }

        public static void Mail(this string to, string subject = "", string body = "")
        {
            if (string.IsNullOrEmpty(to)) return;
            var smtp = new SmtpClient() { Host = "smtp.gmail.com", Port = 587, EnableSsl = true, Credentials = new NetworkCredential("vlabsys@gmail.com", "Experica$y$tem") };
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            smtp.Send("vlabsys@gmail.com", to, subject, body);
        }

        /// <summary>
        /// Load textures from a AssetBundle
        /// </summary>
        /// <param name="imagesetname"></param>
        /// <returns></returns>
        public static Dictionary<string, Texture2D> LoadTextures(this string imagesetname)
        {
            if (string.IsNullOrEmpty(imagesetname)) return null;
            var file = Path.Combine(UnityEngine.Application.streamingAssetsPath, imagesetname);
            if (File.Exists(file))
            {
                var isab = AssetBundle.LoadFromFile(file);
                var ins = isab.GetAllAssetNames().Select(i => Path.GetFileNameWithoutExtension(i));
                if (ins != null && ins.Count() > 0)
                {
                    var imgset = new Dictionary<string, Texture2D>();
                    foreach (var n in ins)
                    {
                        imgset[n] = isab.LoadAsset<Texture2D>(n);
                    }
                    return imgset;
                }
                else
                {
                    Debug.LogWarning($"Image Data: {file} Empty.");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"Image Data: {file} Not Found.");
                return null;
            }
        }




        public static Dictionary<string, Texture2D> Load(this string imageset, int startidx = 0, int numofimg = 10)
        {
            if (string.IsNullOrEmpty(imageset)) return null;
            var imgs = new Dictionary<string, Texture2D>();

            //Addressables.LoadAssetsAsync

            for (var i = startidx; i < numofimg + startidx; i++)
            {
                var img = Resources.Load<Texture2D>(imageset + "/" + i);
                if (img != null)
                {
                    imgs[i.ToString()] = img;
                }
            }
            return imgs;
        }

        public static Texture2DArray LoadImageSet(this string imgsetdir, int startidx, int numofimg, bool forcereload = false)
        {
            if (string.IsNullOrEmpty(imgsetdir)) return null;
            Texture2DArray imgarray;
            if (!forcereload)
            {
                imgarray = Resources.Load<Texture2DArray>(imgsetdir + ".asset");
                if (imgarray != null) return imgarray;
            }
            var img = Resources.Load<Texture2D>(imgsetdir + "/" + startidx);
            if (img == null) return null;

            imgarray = new Texture2DArray(img.width, img.height, numofimg + startidx, img.format, false);
            imgarray.SetPixels(img.GetPixels(), startidx);
            for (var i = startidx + 1; i < numofimg + startidx; i++)
            {
                img = Resources.Load<Texture2D>(imgsetdir + "/" + i);
                if (img != null)
                {
                    imgarray.SetPixels(img.GetPixels(), i);
                }
            }
            imgarray.Apply();
            return imgarray;
        }

        public static Dictionary<string, List<object>> GetColorData(this string Display_ID, bool forceload = false)
        {
            if (!forceload && colordata.ContainsKey(Display_ID))
            {
                return colordata[Display_ID];
            }
            var file = Path.Combine("Data", Display_ID, "colordata.yaml");
            if (!File.Exists(file))
            {
                // generate colordata
            }
            if (File.Exists(file))
            {
                var data = Yaml.ReadYamlFile<Dictionary<string, List<object>>>(file);
                var cm = new Dictionary<string, Matrix<float>>();
                foreach (var k in data.Keys)
                {
                    if (k.Contains("To") && data[k].Count == 16)
                    {
                        cm[k] = CreateMatrix.DenseOfColumnMajor(4, 4, data[k].Select(i => i.Convert<float>()));
                    }
                }
                if (cm.Count > 0)
                {
                    colormatrix[Display_ID] = cm;
                }
                if (data.Count > 0)
                {
                    colordata[Display_ID] = data;
                    return data;
                }
                else
                {
                    Debug.LogWarning("Color Data Empty.");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"Color Data: {file} Not Found.");
                return null;
            }
        }

        /// <summary>
        /// Intersection point of a line and a plane.
        /// points of a line are defined as a direction(Dₗ) through a point(Pₗ) : P = Pₗ + λDₗ , where λ is a scaler
        /// points of a plane are defined as a plane through a point(Pₚ) and with normal vector(Nₚ) : Nₚᵀ(P - Pₚ) = 0 , where Nᵀ is the transpose of N
        /// return point of intersection on direction
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static Vector<float> IntersectLinePlane(Vector<float> pl, Vector<float> dl, Vector<float> pp, Vector<float> np)
        {
            var nptdl = np.PointwiseMultiply(dl).Sum(); // Nₚ'*Dₗ
            if (nptdl == 0f) { return null; } // line on/parallel the plane
            var lam = np.PointwiseMultiply(pp - pl).Sum() / nptdl; // λ = Nₚ'*(Pₚ - Pₗ) / NₚᵀDₗ
            if (lam < 0f) { return null; } // intersection point at opposite direction
            return pl + lam * dl;
        }

        /// <summary>
        /// Intersection point of a line and the six faces of the unit cube with origin as a vertex and three axies as edges.
        /// points of a line are defined as a direction(Dₗ) through a point(Pₗ)
        /// return intersection point on direction
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static Vector<float> IntersectLineUnitOriginCube(Vector<float> pl, Vector<float> dl)
        {
            for (var i = 0; i < 6; i++)
            {
                var p = IntersectLinePlane(pl, dl, UnitOriginCubePoints[i], UnitOriginCubeNormals[i]);
                if (p != null && p.AsArray().Select(j => j >= -1.192e-7f && j <= 1 + 1.192e-7f).All(j => j)) // check if all are within 0-1 with rounding error[eps(float(1))]
                {
                    return p;
                }
            }
            return null;
        }

        public static Color DKLIsoLum(this float angle, float lum, string displayid)
        {
            if (colormatrix.ContainsKey(displayid))
            {
                var cm = colormatrix[displayid];
                if (cm.ContainsKey("DKLToRGB"))
                {
                    var DKLToRGB = cm["DKLToRGB"];
                    var d = Matrix4x4.Rotate(Quaternion.Euler(angle, 0, 0)).MultiplyVector(Vector3.up);
                    var cd = DKLToRGB.Multiply(CreateVector.Dense(new[] { d.x, d.y, d.z, 0f })).SubVector(0, 3);
                    var c = IntersectLineUnitOriginCube(DKLToRGB.Multiply(CreateVector.Dense(new[] { lum, 0f, 0f, 1f })).SubVector(0, 3), cd);
                    if (c != null) { return new Color(Mathf.Clamp01(c.At(0)), Mathf.Clamp01(c.At(1)), Mathf.Clamp01(c.At(2)), 1f); }
                }
            }
            return Color.gray;
        }

        public static Color DKLIsoSLM(this float angle, float scone, string displayid)
        {
            if (colormatrix.ContainsKey(displayid))
            {
                var cm = colormatrix[displayid];
                if (cm.ContainsKey("DKLToRGB"))
                {
                    var DKLToRGB = cm["DKLToRGB"];
                    var d = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)).MultiplyVector(Vector3.down);
                    var cd = DKLToRGB.Multiply(CreateVector.Dense(new[] { d.x, d.y, d.z, 0f })).SubVector(0, 3);
                    var c = IntersectLineUnitOriginCube(DKLToRGB.Multiply(CreateVector.Dense(new[] { 0f, 0f, scone, 1f })).SubVector(0, 3), cd);
                    if (c != null) { return new Color(Mathf.Clamp01(c.At(0)), Mathf.Clamp01(c.At(1)), Mathf.Clamp01(c.At(2)), 1f); }
                }
            }
            return Color.gray;
        }

        public static Color DKLIsoLM(this float angle, float lmcone, string displayid)
        {
            if (colormatrix.ContainsKey(displayid))
            {
                var cm = colormatrix[displayid];
                if (cm.ContainsKey("DKLToRGB"))
                {
                    var DKLToRGB = cm["DKLToRGB"];
                    var d = Matrix4x4.Rotate(Quaternion.Euler(0, angle, 0)).MultiplyVector(Vector3.forward);
                    var cd = DKLToRGB.Multiply(CreateVector.Dense(new[] { d.x, d.y, d.z, 0f })).SubVector(0, 3);
                    var c = IntersectLineUnitOriginCube(DKLToRGB.Multiply(CreateVector.Dense(new[] { 0f, lmcone, 0f, 1f })).SubVector(0, 3), cd);
                    if (c != null) { return new Color(Mathf.Clamp01(c.At(0)), Mathf.Clamp01(c.At(1)), Mathf.Clamp01(c.At(2)), 1f); }
                }
            }
            return Color.gray;
        }



        public static byte[] Compress(this byte[] data)
        {
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(this byte[] data)
        {
            var input = new MemoryStream(data);
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        /// <summary>
        /// Get Condition Duration(ms) for constant number of frames, if frame rate is fixed.
        /// </summary>
        /// <param name="targetdur_ms">duration to be closest to with constant number of frames</param>
        /// <param name="framerate"></param>
        /// <param name="isint">if integer duration millisecond</param>
        /// <returns></returns>
        public static double GetCondDur(this double targetdur_ms, double framerate, bool isint = true)
        {
            var t = 1000.0 / framerate;
            var d = (Math.Round(targetdur_ms / t) - 0.8) * t;
            if (isint) { d = Math.Round(d); }
            return d;
        }

        public static void Save(this string filepath, object obj, bool rmext = false)
        {
            var ext = Path.GetExtension(filepath);
            var file = rmext ? Path.ChangeExtension(filepath, null) : filepath;
            switch (ext)
            {
                case ".EX":
                case ".ex":
                    throw new NotImplementedException();
                    break;
                case ".YAML":
                case ".yaml":
                    file.WriteYamlFile(obj);
                    break;
                default:
                    Debug.LogWarning($"Saving format: \"{ext}\" not supported.");
                    break;
            }
        }
    }
}