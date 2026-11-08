using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The persistence layer: the Redis connection, the per-script-context entity cache, the search indexes and the reads
    /// and writes of the entities. The implementation is <c>DBService</c>; static callers reach the container's instance
    /// through <c>GameServices.DB</c>. A unit test hands a service an in-memory fake. Members have the signatures of the
    /// members of the removed static <c>DB</c> facade.
    /// </summary>
    public interface IDBService
    {
        /// <summary>Retrieves an object by its Id, or null when none is stored. Same as <c>DB.Get&lt;T&gt;</c>.</summary>
        T Get<T>(string id)
            where T : EntityBase;

        /// <summary>Stores an object under its Id. Same as <c>DB.Set&lt;T&gt;</c>.</summary>
        void Set<T>(T entity)
            where T : EntityBase;

        /// <summary>Deletes an entity by its Id. Same as <c>DB.Delete&lt;T&gt;</c>.</summary>
        void Delete<T>(string id)
            where T : EntityBase;

        /// <summary>True when an entity with the Id is stored, without reading it. Same as <c>DB.Exists&lt;T&gt;</c>.</summary>
        bool Exists<T>(string id)
            where T : EntityBase;

        /// <summary>Searches for the entities matching a query. Same as <c>DB.Search&lt;T&gt;</c> (the result is lazy).</summary>
        IEnumerable<T> Search<T>(DBQuery<T> query)
            where T : EntityBase;

        /// <summary>Counts the entities matching a query without reading them. Same as <c>DB.SearchCount&lt;T&gt;</c>.</summary>
        long SearchCount<T>(DBQuery<T> query)
            where T : EntityBase;

        /// <summary>Counts the writes of an entity type, so derived data can be discarded when it changes.</summary>
        long GetWriteVersion<T>()
            where T : EntityBase;

        /// <summary>Appends an entry to a Redis stream without waiting for the reply. Same as <c>DB.StreamAddAsync</c>.</summary>
        Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false);

        // The members below are what the static DB facade forwarded.

        /// <summary>Connects to Redis, registers the entity types and builds their search indexes. Same as <c>DB.Load</c>.</summary>
        void Load();

        /// <summary>Appends an entry to a Redis stream and waits for the reply. Same as <c>DB.StreamAdd</c>.</summary>
        RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false);

        /// <summary>The raw JSON stored under an id, or an empty string. Same as <c>DB.GetRawJson&lt;T&gt;</c>.</summary>
        string GetRawJson<T>(string id);

        /// <summary>The ids of the entities a query matches, without reading them. Same as <c>DB.SearchIds&lt;T&gt;</c>.</summary>
        IEnumerable<string> SearchIds<T>(DBQuery<T> query)
            where T : EntityBase;

        /// <summary>The raw JSON of the entities a query matches. Same as <c>DB.SearchRawJson&lt;T&gt;</c>.</summary>
        IEnumerable<string> SearchRawJson<T>(DBQuery<T> query)
            where T : EntityBase;

        /// <summary>Updates one unindexed property of a stored entity without reading the entity. Same as <c>DB.SetUnindexedJsonProperty&lt;T&gt;</c>.</summary>
        void SetUnindexedJsonProperty<T>(string id, string propertyName, string json)
            where T : EntityBase;

        /// <summary>Escapes the characters that are tokens in a Redis query. Same as <c>DB.EscapeTokens</c>.</summary>
        string EscapeTokens(string str);
    }
}
