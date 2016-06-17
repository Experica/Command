using System.Collections.Generic;
using MsgPack;
using MsgPack.Serialization;

namespace VLab
{
    public static class MsgPackSerializer
    {
        public static MessagePackSerializer<Experiment> ExSerializer;
        public static MessagePackSerializer<List<object>> ListObjectSerializer;

        static MsgPackSerializer()
        {
            ExSerializer = GetDefault<Experiment>();
            ListObjectSerializer = GetDefault<List<object>>();
        }

        public static MessagePackSerializer<T> GetDefault<T>()
        {
            return SerializationContext.Default.GetSerializer<T>();
        }

    }
}