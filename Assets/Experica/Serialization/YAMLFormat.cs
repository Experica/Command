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
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using System.Runtime.Serialization;
using System.IO;

namespace Experica
{
    public class YamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Color))
            {
                return true;
            }
            return false;
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var o = ((Scalar)parser.Current).Value.Convert(type);
            parser.MoveNext();
            return o;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(value.Convert<string>()));
        }
    }

    public class YAMLFormat : IFormat
    {
        ISerializer serializer;
        IDeserializer deserializer;

        public YAMLFormat()
        {
            var yamlvlabconverter = new YamlTypeConverter();
            serializer = new SerializerBuilder().DisableAliases().EmitDefaults().IgnoreFields().WithTypeConverter(yamlvlabconverter).Build();
            deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().IgnoreFields().WithTypeConverter(yamlvlabconverter).Build();
        }

        //public static void WriteYamlFile<T>(this string path, T data)
        //{
        //    File.WriteAllText(path, serializer.Serialize(data));
        //}

        //public static T ReadYamlFile<T>(string path)
        //{
        //    return deserializer.Deserialize<T>(File.ReadAllText(path));
        //}

        //public static T DeserializeYaml<T>(string data)
        //{
        //    return deserializer.Deserialize<T>(data);
        //}

        public string Serialize<T>(T obj)
        {
            return serializer.Serialize(obj);
        }

        public T Deserialize<T>(string data)
        {
            return deserializer.Deserialize<T>(data);
        }
    }
}