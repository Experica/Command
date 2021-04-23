/*
MsgPack.cs is part of the Experica.
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
using System.Collections.Generic;
using MsgPack;
using MsgPack.Serialization;
using System.Linq;
using System;

namespace Experica
{
    public static class MsgPack
    {
#if COMMAND
        // currently not needed for online analysis, and it cause bugs in build not in editor
        // will change plugin from msgpack-cli to messagepack-for-csharp in the future
        //public static MessagePackSerializer<Experiment> ExSerializer;
#endif
        public static MessagePackSerializer<List<int>> ListIntSerializer;
        public static MessagePackSerializer<List<List<string>>> ListListStringSerializer;
        public static MessagePackSerializer<List<List<Dictionary<string, double>>>> ListListEventSerializer;
        public static MessagePackSerializer<ImageSet8> ImageSet8Serializer = MessagePackSerializer.Get<ImageSet8>();
        public static MessagePackSerializer<Dictionary<string, List<List<UInt32>>>> ImageSet32Serializer;

        static MsgPack()
        {
#if COMMAND
            //ExSerializer = MessagePackSerializer.Get<Experiment>();
#endif
            ListIntSerializer = MessagePackSerializer.Get<List<int>>();
            ListListStringSerializer = MessagePackSerializer.Get<List<List<string>>>();
            ListListEventSerializer = MessagePackSerializer.Get<List<List<Dictionary<string, double>>>>();
            ImageSet32Serializer = MessagePackSerializer.Get<Dictionary<string, List<List<UInt32>>>>();
        }

        public static object MsgPackObjectToObject(this object o)
        {
            if (o.GetType() == typeof(MessagePackObject))
            {
                return MsgPackObjectToObject((MessagePackObject)o);
            }
            return o;
        }

        public static object MsgPackObjectToObject(this MessagePackObject mpo)
        {
            if (mpo.IsArray || mpo.IsList)
            {
                return mpo.AsList().Select(i => i.MsgPackObjectToObject()).ToList();
            }
            else
            {
                return mpo.ToObject();
            }
        }

    }

    public class ImageSet8
    {
        public List<UInt16> ImageSize { get; set; } = new List<ushort>();
        public List<List<Byte>> Images { get; set; } = new List<List<byte>>();
    }
}