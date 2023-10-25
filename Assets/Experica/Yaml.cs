/*
Yaml.cs is part of the Experica.
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
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Experica
{
    /// <summary>
    /// for object, the runtime type should be serialized also in tags,
    /// however the consuming library usually lacks tag support(YAML.jl),
    /// so tags not serialized yet.
    /// 
    /// without type info, object will be deserialized as string, but it is 
    /// reasonable to parse string to conventional types, such as int, float or bool, etc.
    /// </summary>
    public class YamlTypeConverter : IYamlTypeConverter
    {
        Type TVector2 = typeof(Vector2);
        Type TVector3 = typeof(Vector3);
        Type TVector4 = typeof(Vector4);
        Type TColor = typeof(Color);
        Type TString = typeof(string);

        public bool Accepts(Type type)
        {
            if (type == TVector2 || type == TVector3 || type == TVector4 || type == TColor)
            {
                return true;
            }
            return false;
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var v = ((Scalar)parser.Current).Value.Convert(TString,type);
            parser.MoveNext();
            return v;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(value.Convert<string>(type)));
        }
    }

    public static class Yaml
    {
        static ISerializer serializer;
        static IDeserializer deserializer;

        static Yaml()
        {
            var c = new YamlTypeConverter();
            // The default behaviour is to emit public fields and public properties, here we exclude fields and use explicit [YamlIgnore] to exclude specific property.
            serializer = new SerializerBuilder().DisableAliases().IgnoreFields().WithTypeConverter(c).Build();
            deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().IgnoreFields().WithTypeConverter(c).Build();
        }

        public static void WriteYamlFile<T>(this string path, T data)
        {
            File.WriteAllText(path, data.SerializeYaml());
        }

        public static string SerializeYaml<T>(this T data)
        {
            return serializer.Serialize(data);
        }

        public static T ReadYamlFile<T>(this string path)
        {
            return File.ReadAllText(path).DeserializeYaml<T>();
        }

        public static T DeserializeYaml<T>(this string data)
        {
            return deserializer.Deserialize<T>(data);
        }
    }
}