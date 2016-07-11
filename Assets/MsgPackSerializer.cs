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
        public static MessagePackSerializer<List<List<Dictionary<string, double>>>> CONDSTATESerializer;

        static MsgPackSerializer()
        {
            ExSerializer = GetDefault<Experiment>();
            ListObjectSerializer = GetDefault<List<object>>();
            ListIntSerializer = GetDefault<List<int>>();
            CONDSTATESerializer = GetDefault<List<List<Dictionary<string, double>>>>();
        }

        public static MessagePackSerializer<T> GetDefault<T>()
        {
            return SerializationContext.Default.GetSerializer<T>();
        }

    }
}