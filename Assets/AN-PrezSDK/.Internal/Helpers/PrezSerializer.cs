using System;

namespace AfterNow.PrezSDK.Internal.Helpers
{
    public static class PrezSerializer
    {
        internal static Func<object, string> Serialize;
        internal static Func<string, Type, object> Deserialize;

        public static void Initialize(Func<object, string> serialize, Func<string, Type, object> deserialize)
        {
            Serialize = serialize;
            Deserialize = deserialize;
        }

        internal static T DeserializeObject<T>(string json)
        {
            return (T)Deserialize(json, typeof(T));
        }

        internal static string SerializeObject(object value)
        {
            return Serialize(value);
        }
    }
}