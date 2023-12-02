﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { _appSettings.RedisIPAddress }
            };

            _multiplexer = ConnectionMultiplexer.Connect(options);

            Console.WriteLine($"Waiting for database connection. If this takes longer than 10 minutes, there's a problem.");
            while (!_multiplexer.IsConnected)
            {
                // Spin
                Thread.Sleep(100);
            }
            Console.WriteLine($"Database connection established.");

            LoadEntities();

            // Runs at the end of every main loop. Clears out all data retrieved during this cycle.
            Internal.OnScriptContextEnd += () =>
            {
                _cachedEntities.Clear();
            };

            // CLI tools also use this class and don't have access to the NWN context.
            // Perform an environment variable check to ensure we're in the game server context before executing the event.
            var context = Environment.GetEnvironmentVariable("GAME_SERVER_CONTEXT");
            if (!string.IsNullOrWhiteSpace(context) && context.ToLower() == "true")
                ExecuteScript("db_loaded", OBJECT_SELF);
        }

        /// <summary>
        /// Processes the Redis Search index with the latest changes.
        /// </summary>
        /// <param name="entity"></param>
        private static void ProcessIndex(EntityBase entity)
        {
            var type = entity.GetType();
            
            // Build the schema based on the IndexedAttribute associated to properties.
            var schema = new Schema();
            var indexedProperties = new List<string>();

            foreach (var prop in type.GetProperties())
            {
                var attribute = prop.GetCustomAttribute(typeof(IndexedAttribute));
                if (attribute != null)
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


            if (schema.Fields.Count > 0)
            {
                // Update fields on existing index.
                if (DoesIndexExist(type))
                {
                    var info = _searchClientsByType[type].GetInfoParsed();

                    var fieldsToAdd = new List<Schema.Field>();

                    // Find new fields to index.
                    foreach (var field in schema.Fields)
                    {
                        if (info.Fields == null || !info.Fields.ContainsKey(field.Name))
                        {
                            Console.WriteLine($"New field found: {field.Name}");
                            fieldsToAdd.Add(field);
                        }
                    }

                    if (fieldsToAdd.Count > 0)
                    {
                        _searchClientsByType[type].AlterIndex(fieldsToAdd.ToArray());
                        Console.WriteLine($"Updated index for {type}.");
                    }
                    else
                    {
                        Console.WriteLine($"No changes found for index {type}.");
                    }
                    
                }
                // Create new index.
                else
                {
                    _searchClientsByType[type].CreateIndex(schema, new Client.ConfiguredIndexOptions());
                    Console.WriteLine($"Created index for {type}");
                }
            }


            WaitForReindexing(type);
        }

        private static bool DoesIndexExist(Type type)
        {
            try
            {
                _searchClientsByType[type].GetInfo();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return false;
                }

                // Got a different exception - throw.
                throw;
            }
        }

        private static void WaitForReindexing(Type type)
        {
            string indexing;

            Console.WriteLine($"Waiting for Redis to complete indexing of: {type}");
            do
            {
                Thread.Sleep(100);

                try
                {
                    // If there is a lot of data or the machine is slow, this command can time out.
                    // Ignore when this happens and retry the command in 100ms.
                    var info = _searchClientsByType[type].GetInfo();
                    indexing = info["percent_indexed"];
                }
                catch (Exception ex)
                {
                    indexing = "0";
                    Console.WriteLine($"Error during indexing: {ex.ToMessageAndCompleteStacktrace()}");
                }

            } while (indexing != "1");
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
            var indexKey = $"Index:{keyPrefix}:{entity.Id}";
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
            _multiplexer.GetDatabase().JsonSet($"{keyPrefix}:{entity.Id}", data);
            _cachedEntities[entity.Id] = entity;
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
            var indexKey = $"Index:{keyPrefix}:{id}";
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
            var result = _searchClientsByType[typeof(T)].Search(query.BuildQuery());

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
            var result = _searchClientsByType[typeof(T)].Search(query.BuildQuery());

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
            var result = _searchClientsByType[typeof(T)].Search(query.BuildQuery(true));

            return result.TotalResults;
        }
    }
}
