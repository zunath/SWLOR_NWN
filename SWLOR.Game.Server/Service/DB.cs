using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NRediSearch;
using NReJSON;
using StackExchange.Redis;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    public static class DB
    {
        internal class JsonSerializer: ISerializerProxy
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

        /// <summary>
        /// Updates one unindexed property without deserializing legacy fields in the entity.
        /// </summary>
        internal static void SetUnindexedJsonProperty<T>(string id, string propertyName, string json)
            where T : EntityBase
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property == null || property.GetCustomAttribute<IndexedAttribute>() != null)
                throw new ArgumentException("A declared, unindexed entity property is required.", nameof(propertyName));

            var result = _multiplexer.GetDatabase().JsonSet($"{_keyPrefixByType[typeof(T)]}:{id}",
                json, path: $".{propertyName}", setOption: SetOption.SetOnlyIfExists);
            if (!result.IsSuccess)
                throw new InvalidOperationException($"Could not update {typeof(T).Name} {id}.{propertyName}.");
            _cachedEntities.Remove(id);
        }

        private static ApplicationSettings _appSettings;
        private static readonly Dictionary<Type, string> _keyPrefixByType = new();
        private static readonly Dictionary<Type, Client> _searchClientsByType = new();
        private static readonly Dictionary<Type, List<string>> _indexedPropertiesByName = new();
        private static ConnectionMultiplexer _multiplexer;
        private static readonly Dictionary<string, EntityBase> _cachedEntities = new();

        static DB()
        {
            NReJSONSerializer.SerializerProxy = new JsonSerializer();
        }

        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void Load()
        {
            _appSettings = ApplicationSettings.Get();

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                // NRediSearch decodes search results using the RESP2 array layout.
                Protocol = RedisProtocol.Resp2,
                EndPoints = { _appSettings.RedisIPAddress }
            };

            _multiplexer = ConnectionMultiplexer.Connect(options);

            Console.WriteLine($"Waiting for database connection. If this takes longer than 10 minutes, there's a problem.");
            DBStartupWait.Until(() => _multiplexer.IsConnected, "database connection", TimeSpan.FromMinutes(10));
            Console.WriteLine($"Database connection established.");

            LoadEntities();

            // Runs at the end of every main loop. Clears out all data retrieved during this cycle.
            ServerManager.OnScriptContextEnd += () =>
            {
                _cachedEntities.Clear();
            };

            // CLI tools also use this class and don't have access to the NWN context.
            // Perform an environment variable check to ensure we're in the game server context before executing the event.
            var context = Environment.GetEnvironmentVariable("GAME_SERVER_CONTEXT");
            if (!string.IsNullOrWhiteSpace(context) && context.ToLower() == "true")
                ExecuteScript("db_loaded", OBJECT_SELF);
        }

        public static RedisValue StreamAdd(
            RedisKey key,
            NameValueEntry[] streamPairs,
            int? maxLength = null,
            bool useApproximateMaxLength = false)
        {
            return _multiplexer.GetDatabase().StreamAdd(
                key,
                streamPairs,
                maxLength: maxLength,
                useApproximateMaxLength: useApproximateMaxLength);
        }

        public static Task<RedisValue> StreamAddAsync(
            RedisKey key,
            NameValueEntry[] streamPairs,
            int? maxLength = null,
            bool useApproximateMaxLength = false)
        {
            return _multiplexer.GetDatabase().StreamAddAsync(
                key,
                streamPairs,
                maxLength: maxLength,
                useApproximateMaxLength: useApproximateMaxLength);
        }

        /// <summary>
        /// Makes sure the entity's search index matches the current schema. The index is only rebuilt
        /// when its definition changed or the live index is not the one this code creates.
        /// </summary>
        /// <param name="entity"></param>
        private static void ProcessIndex(EntityBase entity)
        {
            var type = entity.GetType();
            var indexedProperties = type.GetProperties()
                .Where(prop => prop.GetCustomAttribute(typeof(IndexedAttribute)) != null)
                .ToList();

            // Cache the indexed properties for quick look-up later.
            _indexedPropertiesByName[type] = indexedProperties.Select(prop => prop.Name).ToList();

            var createArgs = SearchIndexDefinition.BuildCreateArgs(
                type.Name,
                _keyPrefixByType[type],
                indexedProperties.Select(prop => (prop.Name, SearchIndexDefinition.IsNumeric(prop.PropertyType))));
            var fingerprint = SearchIndexDefinition.Fingerprint(createArgs);

            if (IsIndexCurrent(type, fingerprint))
            {
                Console.WriteLine($"Index for {type} is current");
                WaitForReindexing(type);
                return;
            }

            DropIndex(type);
            _multiplexer.GetDatabase().Execute("FT.CREATE", createArgs.ToArray());
            Console.WriteLine($"Created index for {type}");
            WaitForReindexing(type);
            SyncIndexEntries(type);

            // Stored last so an interrupted rebuild runs again on the next boot.
            _multiplexer.GetDatabase().StringSet(SearchIndexDefinition.FingerprintKey(type.Name), fingerprint);
        }

        private static bool IsIndexCurrent(Type type, string fingerprint)
        {
            var stored = _multiplexer.GetDatabase().StringGet(SearchIndexDefinition.FingerprintKey(type.Name));
            if (stored != fingerprint)
                return false;

            // A build without prefixes (for example after a rollback) leaves a stale index behind a matching fingerprint.
            var prefixes = GetLiveIndexPrefixes(type.Name);
            return prefixes != null && prefixes.Contains(SearchIndexDefinition.KeyPrefix(_keyPrefixByType[type]));
        }

        private static List<string> GetLiveIndexPrefixes(string indexName)
        {
            RedisResult[] info;
            try
            {
                info = (RedisResult[])_multiplexer.GetDatabase().Execute("FT.INFO", indexName);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Unknown Index name", StringComparison.InvariantCultureIgnoreCase))
                    Console.WriteLine($"Could not read index {indexName}. Exception: {ex.ToMessageAndCompleteStacktrace()}");
                return null;
            }

            for (var i = 0; i + 1 < info.Length; i += 2)
            {
                if ((string)info[i] != "index_definition")
                    continue;

                var definition = (RedisResult[])info[i + 1];
                for (var j = 0; j + 1 < definition.Length; j += 2)
                {
                    if ((string)definition[j] == "prefixes")
                        return ((RedisResult[])definition[j + 1]).Select(prefix => (string)prefix).ToList();
                }
            }

            return new List<string>();
        }

        private static void DropIndex(Type type)
        {
            try
            {
                // FT.DROPINDEX is used here in lieu of DropIndex() as it does not cause all documents to be lost.
                _multiplexer.GetDatabase().Execute("FT.DROPINDEX", type.Name);
                Console.WriteLine($"Dropped index for {type}");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"Index does not exist for type {type}.");
                }
                else
                {
                    Console.WriteLine($"Issue dropping index for type {type}. Exception: {ex.ToMessageAndCompleteStacktrace()}");
                }
            }
        }

        /// <summary>
        /// Rewrites search hashes from the stored records so they hold every currently indexed field,
        /// and removes search hashes whose record no longer exists.
        /// </summary>
        private static void SyncIndexEntries(Type type)
        {
            var db = _multiplexer.GetDatabase();
            var client = _searchClientsByType[type];
            var keyPrefix = _keyPrefixByType[type];
            var indexKeyPrefix = SearchIndexDefinition.KeyPrefix(keyPrefix);
            var records = 0;
            var added = 0;
            var updated = 0;
            var removed = 0;
            var unreadable = 0;

            foreach (var batch in ScanKeys($"{keyPrefix}:*").Chunk(500))
            {
                var jsonReads = batch.Select(key => db.ExecuteAsync("JSON.GET", key)).ToArray();
                var entryReads = batch.Select(key => db.HashGetAllAsync(indexKeyPrefix + key.Substring(keyPrefix.Length + 1))).ToArray();

                for (var index = 0; index < batch.Length; index++)
                {
                    string json;
                    try
                    {
                        var result = jsonReads[index].Result;
                        if (result.IsNull)
                            continue;
                        json = result.ToString();
                    }
                    catch (Exception)
                    {
                        // Not a JSON record (another key family sharing the prefix).
                        continue;
                    }

                    records++;
                    var indexKey = indexKeyPrefix + batch[index].Substring(keyPrefix.Length + 1);
                    Dictionary<string, RedisValue> expected;
                    try
                    {
                        expected = BuildIndexData(type, (EntityBase)JsonConvert.DeserializeObject(json, type));
                    }
                    catch (Exception ex)
                    {
                        unreadable++;
                        Console.WriteLine($"Could not rebuild search entry {indexKey}. Exception: {ex.ToMessageAndCompleteStacktrace()}");
                        continue;
                    }

                    var existing = entryReads[index].Result;
                    if (EntryMatches(existing, expected))
                        continue;

                    client.ReplaceDocument(indexKey, expected);
                    if (existing.Length == 0)
                        added++;
                    else
                        updated++;
                }
            }

            foreach (var batch in ScanKeys(indexKeyPrefix + "*").Chunk(500))
            {
                var recordChecks = batch.Select(key => db.KeyExistsAsync(key.Substring("Index:".Length))).ToArray();
                for (var index = 0; index < batch.Length; index++)
                {
                    if (recordChecks[index].Result)
                        continue;

                    client.DeleteDocument(batch[index]);
                    removed++;
                }
            }

            Console.WriteLine($"Synced search entries for {type}: {records} records, {added} added, {updated} updated, {removed} orphans removed, {unreadable} unreadable.");
        }

        private static bool EntryMatches(HashEntry[] existing, Dictionary<string, RedisValue> expected)
        {
            if (existing.Length != expected.Count)
                return false;

            return existing.All(entry =>
                expected.TryGetValue(entry.Name, out var value) &&
                value.ToString() == entry.Value.ToString());
        }

        private static IEnumerable<string> ScanKeys(string pattern)
        {
            var cursor = "0";
            do
            {
                var result = (RedisResult[])_multiplexer.GetDatabase().Execute("SCAN", cursor, "MATCH", pattern, "COUNT", "1000");
                cursor = (string)result[0];
                foreach (var key in (RedisResult[])result[1])
                {
                    yield return (string)key;
                }
            } while (cursor != "0");
        }

        private static void WaitForReindexing(Type type)
        {
            Console.WriteLine($"Waiting for Redis to complete indexing of: {type}");
            DBStartupWait.Until(() =>
            {
                try
                {
                    // If there is a lot of data or the machine is slow, this command can time out.
                    // Ignore when this happens and retry the command in 100ms.
                    var info = _searchClientsByType[type].GetInfo();
                    return double.TryParse(info["percent_indexed"], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var indexed) && indexed >= 1d;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during indexing: {ex.ToMessageAndCompleteStacktrace()}");
                    return false;
                }

            }, $"Redis index {type.Name}", TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// When initialized, the assembly will be searched for all implementations of the EntityBase object.
        /// The KeyPrefix value of each of these will be stored into a dictionary for quick retrievals later.
        /// This is intended to abstract key building away from the consumer of this class.
        /// </summary>
        private static void LoadEntities()
        {
            var entityInstances = typeof(EntityBase)
                .Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EntityBase)) && !t.IsAbstract && !t.IsGenericType)
                .Select(t => (EntityBase)Activator.CreateInstance(t));

            foreach (var entity in entityInstances)
            {
                var type = entity.GetType();
                // Register the type by itself first.
                _keyPrefixByType[type] = type.Name;

                // Register the search client.
                _searchClientsByType[type] = new Client(type.Name, _multiplexer.GetDatabase());
                ProcessIndex(entity);

                Console.WriteLine($"Registered type '{entity.GetType()}' using key prefix {type.Name}");
            }
        }

        /// <summary>
        /// Stores a specific object in the database by its Id.
        /// </summary>
        /// <typeparam name="T">The type of data to store</typeparam>
        /// <param name="entity">The data to store.</param>
        public static void Set<T>(T entity)
            where T : EntityBase
        {
            var type = typeof(T);
            var data = JsonConvert.SerializeObject(entity);

            var keyPrefix = _keyPrefixByType[type];
            var indexKey = SearchIndexDefinition.KeyPrefix(keyPrefix) + entity.Id;
            var indexData = BuildIndexData(type, entity);
            _searchClientsByType[type].ReplaceDocument(indexKey, indexData);
            _multiplexer.GetDatabase().JsonSet($"{keyPrefix}:{entity.Id}", data);
            _cachedEntities[entity.Id] = entity;
        }

        /// <summary>
        /// Builds the search hash fields for an entity. Used by Set and by the index sync so both write identical entries.
        /// </summary>
        private static Dictionary<string, RedisValue> BuildIndexData(Type type, EntityBase entity)
        {
            var indexData = new Dictionary<string, RedisValue>();

            foreach (var prop in _indexedPropertiesByName[type])
            {
                var property = type.GetProperty(prop);
                var value = property?.GetValue(entity);

                if (value != null)
                {
                    // Convert enums to numeric values
                    if (property.PropertyType.IsEnum)
                        value = (int) value;

                    if (property.PropertyType == typeof(Guid))
                    {
                        value = EscapeTokens(((Guid) value).ToString());
                    }

                    if (property.PropertyType == typeof(string))
                    {
                        value = EscapeTokens((string) value);
                    }

                    indexData[prop] = (dynamic)value;
                }
            }

            return indexData;
        }

        /// <summary>
        /// Retrieves a specific object in the database by an arbitrary key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <param name="id">The arbitrary key the data is stored under</param>
        /// <returns>The object stored in the database under the specified key</returns>
        public static T Get<T>(string id)
            where T: EntityBase
        {
            var keyPrefix = _keyPrefixByType[typeof(T)];
            if (_cachedEntities.ContainsKey(id))
            {
                return (T)_cachedEntities[id];
            }
            else
            {
                RedisValue data = _multiplexer.GetDatabase().JsonGet($"{keyPrefix}:{id}").ToString();

                if (string.IsNullOrWhiteSpace(data))
                    return default;

                var entity = JsonConvert.DeserializeObject<T>(data);
                _cachedEntities[id] = entity;

                return entity;
            }
        }

        /// <summary>
        /// Retrieves the raw JSON of the object in the database by a given prefix and key.
        /// This can be useful for data migrations and one-time actions at server load.
        /// Do not use this for normal game play as it is slow.
        /// </summary>
        /// <param name="id">The arbitrary key the data is stored under</param>
        /// <returns>The raw json stored in the database under the specified key</returns>
        public static string GetRawJson<T>(string id)
        {
            var keyPrefix = _keyPrefixByType[typeof(T)];
            RedisValue data = _multiplexer.GetDatabase().JsonGet($"{keyPrefix}:{id}").ToString();

            if (string.IsNullOrWhiteSpace(data))
                return string.Empty;

            return data.ToString();
        }

        /// <summary>
        /// Returns true if an entry with the specified key exists.
        /// Returns false if not.
        /// </summary>
        /// <param name="id">The key of the entity.</param>
        /// <returns>true if found, false otherwise.</returns>
        public static bool Exists<T>(string id)
            where T : EntityBase
        {
            var keyPrefix = _keyPrefixByType[typeof(T)];
            if (_cachedEntities.ContainsKey(id))
                return true;
            else
                return _multiplexer.GetDatabase().KeyExists($"{keyPrefix}:{id}");
        }

        /// <summary>
        /// Deletes an entry by a specified key.
        /// </summary>
        /// <typeparam name="T">The type of entity to delete.</typeparam>
        /// <param name="id">The key of the entity</param>
        public static void Delete<T>(string id)
            where T: EntityBase
        {
            var keyPrefix = _keyPrefixByType[typeof(T)];
            var indexKey = SearchIndexDefinition.KeyPrefix(keyPrefix) + id;
            _searchClientsByType[typeof(T)].DeleteDocument(indexKey);
            _multiplexer.GetDatabase().JsonDelete($"{keyPrefix}:{id}");
            _cachedEntities.Remove(id);
        }

        /// <summary>
        /// Escapes tokens used in Redis queries.
        /// </summary>
        /// <param name="str">The string to escape</param>
        /// <returns>A string containing escaped tokens.</returns>
        public static string EscapeTokens(string str)
        {
            return str
                .Replace("@", "\\@")
                .Replace("!", "\\!")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("|", "\\|")
                .Replace("-", "\\-")
                .Replace("=", "\\=")
                .Replace(">", "\\>")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"");
        }

        /// <summary>
        /// Searches the Redis DB for records matching the query criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>An enumerable of entities matching the criteria.</returns>
        public static IEnumerable<T> Search<T>(DBQuery<T> query)
            where T: EntityBase
        {
            var result = SearchIndex(query);

            foreach (var doc in result.Documents)
            {
                // Remove the 'Index:' prefix.
                var recordId = doc.Id.Remove(0, 6);
                yield return _multiplexer.GetDatabase().JsonGet<T>(recordId);
            }
        }

        /// <summary>
        /// Searches the Redis DB for raw JSON records matching the query criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>An enumerable of raw json values matching the criteria.</returns>
        public static IEnumerable<string> SearchRawJson<T>(DBQuery<T> query)
            where T: EntityBase
        {
            var result = SearchIndex(query);

            foreach (var doc in result.Documents)
            {
                // Remove the 'Index:' prefix.
                var recordId = doc.Id.Remove(0, 6);
                yield return _multiplexer.GetDatabase().JsonGet(recordId).ToString();
            }
        }

        /// <summary>
        /// Searches the Redis DB for the number of records matching the query criteria.
        /// This only retrieves the number of records. Use Search() if you need the actual results.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>The number of records matching the query criteria.</returns>
        public static long SearchCount<T>(DBQuery<T> query)
            where T: EntityBase
        {
            var result = SearchIndex(query, true);

            return result.TotalResults;
        }

        private static SearchResult SearchIndex<T>(DBQuery<T> query, bool countsOnly = false)
            where T: EntityBase
        {
            var client = _searchClientsByType[typeof(T)];
            var searchQuery = query.BuildQuery(countsOnly);
            // Retry the read itself, never the caller's settlement/refund workflow or its writes.
            return DBSearchRetry.Execute(() => client.Search(searchQuery), typeof(T).Name);
        }
    }
}
