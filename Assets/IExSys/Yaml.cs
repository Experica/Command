/*
VLYaml.cs is part of the VLAB project.
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
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace IExSys
{
    public class YamlVLabConverter : IYamlTypeConverter
    {
        Type TVector2 = typeof(Vector2);
        Type TVector3 = typeof(Vector3);
        Type TVector4 = typeof(Vector4);
        Type TColor = typeof(Color);

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
            var o = ((Scalar)parser.Current).Value.Convert(type);
            parser.MoveNext();
            return o;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(value.Convert<string>()));
        }
    }

    public static class Yaml
    {
        static Serializer serializer;
        static Deserializer deserializer;

        static Yaml()
        {
            var yamlvlabconverter = new YamlVLabConverter();
            serializer = new SerializerBuilder().DisableAliases().EmitDefaults().WithTypeConverter(yamlvlabconverter).Build();
            deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithTypeConverter(yamlvlabconverter).Build();
        }

        public static void WriteYamlFile<T>(this string path, T data)
        {
            File.WriteAllText(path, SerializeYaml(data));
        }

        public static string SerializeYaml<T>(this T data)
        {
            return serializer.Serialize(data);
        }

        public static T ReadYamlFile<T>(this string path)
        {
            return deserializer.Deserialize<T>(File.ReadAllText(path));
        }

        public static T DeserializeYaml<T>(this string data)
        {
            return deserializer.Deserialize<T>(data);
        }
    }
}