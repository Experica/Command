using System.Collections.Generic;
using MsgPack;
using MsgPack.Serialization;

namespace VLab
{
    public static class MsgPackSerializer
    {
        public static MessagePackSerializer<Experiment> ExSerializer;
        public static MessagePackSerializer<List<object>> ListObjectSerializer;
        public static MessagePackSerializer<List<int>> ListIntSerializer;

        static MsgPackSerializer()
        {
            ExSerializer = GetDefault<Experiment>();
            ListObjectSerializer = GetDefault<List<object>>();
            ListIntSerializer = GetDefault<List<int>>();
        }

        public static MessagePackSerializer<T> GetDefault<T>()
        {
            return SerializationContext.Default.GetSerializer<T>();
        }

    }
}