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
using Unity.Collections;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Experica
{
    /// <summary>
    /// Convert between value and its text representation of Types that don't have built-in support/need special treatment, in YAML(Bool, Int, Float, List, Dict, etc.)
    /// 
    /// For `typeof(object)`, serialization would use runtime type of value, but deserialization don't know which specific
    /// type to use except `typeof(object)`. So the runtime type should be serialized in tag along with value,
    /// however it would add noise in the Yaml file, and the deserialization, probably in other languages, may not support tag
    /// or need extra work, so here Type tag is not serialized yet.
    /// 
    /// Without specific Type tag, deserialized value remains in string for `typeof(object)`, but it is 
    /// reasonable to try parsing string to common types such as bool, float or Vector3, etc.
    /// </summary>
    class YamlTypeConverter : IYamlTypeConverter
    {
        Type TVector2 = typeof(Vector2);
        Type TVector3 = typeof(Vector3);
        Type TVector4 = typeof(Vector4);
        Type TColor = typeof(Color);
        Type TFixString512 = typeof(FixedString512Bytes);

        Type TObject = typeof(object);


        public bool Accepts(Type type)
        {
            if (type == TVector2 || type == TVector3 || type == TVector4 || type == TColor || type == TFixString512 || type == TObject)
            {
                return true;
            }
            return false;
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var value = parser.Consume<Scalar>().Value;
            return type == TObject ? value.TryParse() : value.Convert(typeof(string), type);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            // here use the runtime type of value
            emitter.Emit(new Scalar(value.Convert<string>()));
        }
    }

    public static class Yaml
    {
        static ISerializer serializer;
        static IDeserializer deserializer;

        static Yaml()
        {
            var c = new YamlTypeConverter();
            // The default behaviour is to emit public fields and public properties, here we exclude fields and could use explicit [YamlIgnore] to exclude specific property if we want.
            serializer = new SerializerBuilder().DisableAliases().IgnoreFields().WithTypeConverter(c).WithIndentedSequences().Build();
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

        public static object ReadYamlFile(this string path, Type type)
        {
            return File.ReadAllText(path).DeserializeYaml(type);
        }

        public static T DeserializeYaml<T>(this string data)
        {
            return deserializer.Deserialize<T>(data);
        }

        public static object DeserializeYaml(this string data, Type type)
        {
            return deserializer.Deserialize(data, type);
        }
    }
}