using Newtonsoft.Json;
using NReJSON;
using StackExchange.Redis;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>The serializer NReJSON uses for every JSON.SET and JSON.GET of the database (Newtonsoft defaults).</summary>
    internal class DBJsonSerializer : ISerializerProxy
    {
        public TResult Deserialize<TResult>(RedisResult serializedValue)
        {
            return JsonConvert.DeserializeObject<TResult>(serializedValue.ToString());
        }

        public string Serialize<TObjectType>(TObjectType obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
