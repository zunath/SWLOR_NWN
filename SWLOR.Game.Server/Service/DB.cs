using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the persistence layer. The logic lives in <see cref="IDBService"/>; the event handler stays
    /// here and every member forwards to the container's instance.
    /// </summary>
    public static class DB
    {
        private static IDBService Instance => FacadeAccessor.Resolve<IDBService>();

        /// <summary>Updates one unindexed property without deserializing legacy fields in the entity.</summary>
        internal static void SetUnindexedJsonProperty<T>(string id, string propertyName, string json)
            where T : EntityBase
        {
            Instance.SetUnindexedJsonProperty<T>(id, propertyName, json);
        }

        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void Load()
        {
            Instance.Load();
        }

        public static RedisValue StreamAdd(
            RedisKey key,
            NameValueEntry[] streamPairs,
            int? maxLength = null,
            bool useApproximateMaxLength = false)
        {
            return Instance.StreamAdd(key, streamPairs, maxLength, useApproximateMaxLength);
        }

        public static Task<RedisValue> StreamAddAsync(
            RedisKey key,
            NameValueEntry[] streamPairs,
            int? maxLength = null,
            bool useApproximateMaxLength = false)
        {
            return Instance.StreamAddAsync(key, streamPairs, maxLength, useApproximateMaxLength);
        }

        /// <summary>Stores a specific object in the database by its Id.</summary>
        public static void Set<T>(T entity)
            where T : EntityBase
        {
            Instance.Set(entity);
        }

        /// <summary>
        /// Increases on every Set, Delete, and partial write of the entity type. Services can keep data
        /// derived from that type across frames and discard it when the version changes.
        /// </summary>
        public static long GetWriteVersion<T>()
            where T : EntityBase
        {
            return Instance.GetWriteVersion<T>();
        }

        /// <summary>Retrieves a specific object in the database by an arbitrary key.</summary>
        public static T Get<T>(string id)
            where T : EntityBase
        {
            return Instance.Get<T>(id);
        }

        /// <summary>
        /// Retrieves the raw JSON of the object in the database by a given prefix and key.
        /// This can be useful for data migrations and one-time actions at server load.
        /// Do not use this for normal game play as it is slow.
        /// </summary>
        public static string GetRawJson<T>(string id)
        {
            return Instance.GetRawJson<T>(id);
        }

        /// <summary>Returns true if an entry with the specified key exists.</summary>
        public static bool Exists<T>(string id)
            where T : EntityBase
        {
            return Instance.Exists<T>(id);
        }

        /// <summary>Deletes an entry by a specified key.</summary>
        public static void Delete<T>(string id)
            where T : EntityBase
        {
            Instance.Delete<T>(id);
        }

        /// <summary>Escapes tokens used in Redis queries.</summary>
        public static string EscapeTokens(string str)
        {
            return Instance.EscapeTokens(str);
        }

        /// <summary>Searches the Redis DB for records matching the query criteria.</summary>
        public static IEnumerable<T> Search<T>(DBQuery<T> query)
            where T : EntityBase
        {
            return Instance.Search(query);
        }

        /// <summary>Returns the ids of the entities a query matches without reading the entities themselves.</summary>
        public static IEnumerable<string> SearchIds<T>(DBQuery<T> query)
            where T : EntityBase
        {
            return Instance.SearchIds(query);
        }

        /// <summary>Searches the Redis DB for raw JSON records matching the query criteria.</summary>
        public static IEnumerable<string> SearchRawJson<T>(DBQuery<T> query)
            where T : EntityBase
        {
            return Instance.SearchRawJson(query);
        }

        /// <summary>Searches the Redis DB for the number of records matching the query criteria.</summary>
        public static long SearchCount<T>(DBQuery<T> query)
            where T : EntityBase
        {
            return Instance.SearchCount(query);
        }
    }
}
