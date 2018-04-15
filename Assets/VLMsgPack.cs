﻿/*
VLMsgPack.cs is part of the VLAB project.
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

namespace VLab
{
    public static class VLMsgPack
    {
        public static MessagePackSerializer<Experiment> ExSerializer;
        public static MessagePackSerializer<List<int>> ListIntSerializer;
        public static MessagePackSerializer<List<List<Dictionary<string, double>>>> ListCONDSTATESerializer;

        static VLMsgPack()
        {
            ExSerializer = MessagePackSerializer.Get<Experiment>();
            ListIntSerializer = MessagePackSerializer.Get<List<int>>();
            ListCONDSTATESerializer = MessagePackSerializer.Get<List<List<Dictionary<string, double>>>>();
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
}