using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NRediSearch;
using NReJSON;
using StackExchange.Redis;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    internal static class DB
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

        [NWNEventHandler("mod_preload")]
        public static void Load()
        {
            _appSettings = ApplicationSettings.Get();
            _multiplexer = ConnectionMultiplexer.Connect(_appSettings.RedisIPAddress);
            LoadEntities();

            // Runs at the end of every main loop. Clears out all data retrieved during this cycle.
            Entrypoints.OnScriptContextEnd += () =>
            {
                _cachedEntities.Clear();
            };
        }

        /// <summary>
        /// Processes the Redis Search index with the latest changes.
        /// </summary>
        /// <param name="entity"></param>
        private static void ProcessIndex(EntityBase entity)
        {
            var type = entity.GetType();

            // Drop any existing index
            try
            {
                // FT.DROPINDEX is used here in lieu of DropIndex() as it does not cause all documents to be lost.
                _multiplexer.GetDatabase().Execute("FT.DROPINDEX", type.Name);
                Console.WriteLine($"Dropped index for {type}");
            }
            catch
            {
                Console.WriteLine($"Index does not exist for type {type}");
            }

            // Build the schema based on the IndexedAttribute associated to properties.
            var schema = new Schema();
            var indexedProperties = new List<string>();

            foreach (var prop in type.GetProperties())
            {
                if (prop.GetCustomAttribute(typeof(IndexedAttribute)) != null)
                {
                    if (prop.PropertyType == typeof(int) ||
                        prop.PropertyType == typeof(int?) ||
                        prop.PropertyType == typeof(ulong) ||
                        prop.PropertyType == typeof(ulong?) ||
                        prop.PropertyType == typeof(long) ||
                        prop.PropertyType == typeof(long?))
                    {
                        schema.AddNumericField(prop.Name);
                    }
                    else
                    {
                        schema.AddTextField(prop.Name);
                    }

                    indexedProperties.Add(prop.Name);
                }

            }

            // Cache the indexed properties for quick look-up later.
            _indexedPropertiesByName[type] = indexedProperties;

            // Create the index.
            if (schema.Fields.Count > 0)
            {
                _searchClientsByType[type].CreateIndex(schema, new Client.ConfiguredIndexOptions());
                Console.WriteLine($"Created index for {type}");
            }
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
                _keyPrefixByType[type] = entity.KeyPrefix;
                
                // Register the search client.
                _searchClientsByType[type] = new Client(type.Name, _multiplexer.GetDatabase());
                ProcessIndex(entity);

                Console.WriteLine($"Registered type '{entity.GetType()}' using key prefix {entity.KeyPrefix}");
            }
        }

        /// <summary>
        /// Stores a specific object in the database by an arbitrary key.
        /// </summary>
        /// <typeparam name="T">The type of data to store</typeparam>
        /// <param name="entity">The data to store.</param>
        /// <param name="key">The arbitrary key to set this object under.</param>
        /// <param name="keyPrefixOverride">If null, the key prefix defined on the entity will be used. Otherwise, this value will be used as the prefix.</param>
        public static void Set<T>(string key, T entity, string keyPrefixOverride = null)
            where T : EntityBase
        {
            var type = typeof(T);
            if (string.IsNullOrWhiteSpace(keyPrefixOverride))
            {
                keyPrefixOverride = _keyPrefixByType[type];
            }

            var data = JsonConvert.SerializeObject(entity);
            var indexKey = $"Index:{keyPrefixOverride}:{key}";
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

            _searchClientsByType[type].ReplaceDocument(indexKey, indexData);
            _multiplexer.GetDatabase().JsonSet($"{keyPrefixOverride}:{key}", data);
            _cachedEntities[key] = entity;
        }

        /// <summary>
        /// Retrieves a specific object in the database by an arbitrary key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <param name="key">The arbitrary key the data is stored under</param>
        /// <param name="keyPrefixOverride">If null, the key prefix defined on the entity will be used. Otherwise, this value will be used as the prefix.</param>
        /// <returns>The object stored in the database under the specified key</returns>
        public static T Get<T>(string key, string keyPrefixOverride = null)
            where T: EntityBase
        {
            if (string.IsNullOrWhiteSpace(keyPrefixOverride))
            {
                keyPrefixOverride = _keyPrefixByType[typeof(T)];
            }

            if (_cachedEntities.ContainsKey(key))
            {
                return (T)_cachedEntities[key];
            }
            else
            {
                RedisValue data;

                using (new Profiler("RedisGet"))
                {
                    data = _multiplexer.GetDatabase().JsonGet($"{keyPrefixOverride}:{key}").ToString();
                }

                if (string.IsNullOrWhiteSpace(data))
                    return default;

                using (new Profiler("Deserialization"))
                {
                    return JsonConvert.DeserializeObject<T>(data);
                }
            }
        }

        /// <summary>
        /// Returns true if an entry with the specified key exists.
        /// Returns false if not.
        /// </summary>
        /// <param name="key">The key of the entity.</param>
        /// <param name="keyPrefixOverride">If null, the key prefix defined on the entity will be used. Otherwise, this value will be used as the prefix.</param>
        /// <returns>true if found, false otherwise.</returns>
        public static bool Exists<T>(string key, string keyPrefixOverride = null)
            where T : EntityBase
        {
            if (keyPrefixOverride == null)
            {
                keyPrefixOverride = _keyPrefixByType[typeof(T)];
            }

            if (_cachedEntities.ContainsKey(key))
                return true;
            else
                return _multiplexer.GetDatabase().KeyExists($"{keyPrefixOverride}:{key}");
        }

        /// <summary>
        /// Deletes an entry by a specified key.
        /// </summary>
        /// <typeparam name="T">The type of entity to delete.</typeparam>
        /// <param name="key">The key of the entity</param>
        /// <param name="keyPrefixOverride">If null, the key prefix defined on the entity will be used. Otherwise, this value will be used as the prefix.</param>
        public static void Delete<T>(string key, string keyPrefixOverride = null)
            where T: EntityBase
        {
            if (keyPrefixOverride == null)
            {
                keyPrefixOverride = _keyPrefixByType[typeof(T)];
            }

            var indexKey = $"Index:{keyPrefixOverride}:{key}";
            _searchClientsByType[typeof(T)].DeleteDocument(indexKey);
            _multiplexer.GetDatabase().JsonDelete($"{keyPrefixOverride}:{key}");
            _cachedEntities.Remove(key);
        }

        private static string EscapeTokens(string str)
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
                .Replace(">", "\\>");
        }

        public static IEnumerable<T> Search<T>(string fieldName, string search, string fieldName2 = "", string search2 = "", string fieldName3 = "", string search3 = "")
            where T: EntityBase
        {
            // Escape special characters
            search = EscapeTokens(search);

            var query = $"@{fieldName}:{search}";


            if (!string.IsNullOrWhiteSpace(fieldName2) && !string.IsNullOrWhiteSpace(search2))
            {
                search2 = EscapeTokens(search2);
                query += $" @{fieldName2}:{search2}";
            }

            if (!string.IsNullOrWhiteSpace(fieldName3) && !string.IsNullOrWhiteSpace(search3))
            {
                search3 = EscapeTokens(search3);
                query += $" @{fieldName3}:{search3}";
            }

            var result = _searchClientsByType[typeof(T)].Search(new Query(query));
            
            foreach (var doc in result.Documents)
            {
                // Remove the 'Index:' prefix.
                var recordId = doc.Id.Remove(0, 6);
                yield return _multiplexer.GetDatabase().JsonGet<T>(recordId);
            }
        }
    }
}
