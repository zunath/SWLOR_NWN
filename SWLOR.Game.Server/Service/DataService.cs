
using Dapper;

using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Data;
using Attribute = SWLOR.Game.Server.Data.Entity.Attribute;
using BaseStructureType = SWLOR.Game.Server.Data.Entity.BaseStructureType;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;
using PCBaseType = SWLOR.Game.Server.Data.Entity.PCBaseType;
using PerkExecutionType = SWLOR.Game.Server.Data.Entity.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        public ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private string _connectionString;
        private readonly Dictionary<Type, Dictionary<object, object>> _cache;
        private bool _cacheInitialized;

        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
            _cache = new Dictionary<Type, Dictionary<object, object>>();

        }

        public void Initialize(bool initializeCache)
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS"),
                InitialCatalog = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE"),
                UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
            }.ToString();

            if (initializeCache)
                InitializeCache();
        }

        public void Initialize(string ip, string database, string user, string password, bool initializeCache)
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = database,
                UserID = user,
                Password = password
            }.ToString();

            if (initializeCache)
                InitializeCache();
        }

        /// <summary>
        /// Retrieves all objects in frequently accessed data from the database and stores them into the cache.
        /// This should only be called one time at initial load.
        /// </summary>
        private void InitializeCache()
        {
            if (_cacheInitialized) return;

            Console.WriteLine("Initializing the cache...");
            // Todo: We're caching everything right now to get up and running. Look into optimizations for PC objects and item storage later.
            GetAll<ApartmentBuilding>();
            // Note: Area and AreaWalkmesh get cached in the AreaService
            GetAll<Association>();
            GetAll<Attribute>();
            GetAll<AuthorizedDM>();
            GetAll<Bank>();
            GetAll<BankItem>();
            GetAll<BaseItemType>();
            GetAll<BaseStructure>();
            GetAll<BaseStructureType>();
            GetAll<BuildingStyle>();
            GetAll<Data.Entity.BuildingType>();
            GetAll<ChatChannelsDomain>();
            GetAll<ClientLogEventTypesDomain>();
            GetAll<ComponentType>();
            GetAll<CooldownCategory>();
            GetAll<CraftBlueprint>();
            GetAll<CraftBlueprintCategory>();
            GetAll<CraftDevice>();
            GetAll<Data.Entity.CustomEffect>();
            GetAll<CustomEffectCategory>();
            GetAll<DiscordChatQueue>();
            GetAll<DMRoleDomain>();
            GetAll<EnmityAdjustmentRule>();
            GetAll<FameRegion>();
            GetAll<GrowingPlant>();
            GetAll<ItemType>();
            GetAll<KeyItem>();
            GetAll<KeyItemCategory>();
            GetAll<LootTable>();
            GetAll<LootTableItem>();
            GetAll<Data.Entity.Mod>();
            GetAll<NPCGroup>();
            GetAll<PCBase>();
            GetAll<PCBasePermission>();
            GetAll<PCBaseStructure>();
            GetAll<PCBaseStructureItem>();
            GetAll<PCBaseStructurePermission>();
            GetAll<PCBaseType>();
            GetAll<PCCooldown>();
            GetAll<PCCraftedBlueprint>();
            GetAll<PCCustomEffect>();
            GetAll<PCImpoundedItem>();
            GetAll<PCKeyItem>();
            GetAll<PCMapPin>();
            GetAll<PCMapProgression>();
            GetAll<PCObjectVisibility>();
            GetAll<PCOutfit>();
            GetAll<PCOverflowItem>();
            GetAll<PCPerk>();
            GetAll<PCPerkRefund>();
            GetAll<PCQuestItemProgress>();
            GetAll<PCQuestKillTargetProgress>();
            GetAll<PCQuestStatus>();
            GetAll<PCRegionalFame>();
            GetAll<PCSearchSite>();
            GetAll<PCSearchSiteItem>();
            GetAll<PCSkill>();
            GetAll<Data.Entity.Perk>();
            GetAll<PerkCategory>();
            GetAll<PerkExecutionType>();
            GetAll<PerkLevel>();
            GetAll<PerkLevelQuestRequirement>();
            GetAll<PerkLevelSkillRequirement>();
            GetAll<Plant>();
            GetAll<PlayerCharacter>(); // todo: temporarily enabled to get up and running. We should *not* be loading all PCs and related data into the cache.
            GetAll<Quest>();
            GetAll<QuestKillTargetList>();
            GetAll<QuestPrerequisite>();
            GetAll<QuestRequiredItemList>();
            GetAll<QuestRequiredKeyItemList>();
            GetAll<QuestRewardItem>();
            GetAll<QuestState>();
            GetAll<QuestTypeDomain>();
            GetAll<ServerConfiguration>();
            GetAll<Skill>();
            GetAll<SkillCategory>();
            GetAll<SkillXPRequirement>();
            GetAll<Spawn>();
            GetAll<SpawnObject>();
            GetAll<SpawnObjectType>();
            Console.WriteLine("Cache initialized!");

            _cacheInitialized = true;
        }

        /// <summary>
        /// Caches a player's data. Be sure to call RemoveCachedPlayerData when the player exits the game.
        /// </summary>
        /// <param name="player"></param>
        public void CachePlayerData(NWPlayer player)
        {
            if (!player.IsPlayer) return;

            Get<PlayerCharacter>(player.GlobalID);
        }

        /// <summary>
        /// Removes a player's cached data. Be sure to call this ONLY on the OnClientLeave event.
        /// </summary>
        /// <param name="player"></param>
        public void RemoveCachedPlayerData(NWPlayer player)
        {
            if (!player.IsPlayer) return;

            DeleteFromCache<PlayerCharacter>(player.GlobalID);
        }
        
        /// <summary>
        /// Sends a request to change data into the database queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data directly from the database immediately afterwards.
        /// However, data in the cache will be up to date as soon as a value is changed.
        /// </summary>
        public void SubmitDataChange(DatabaseAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if(action.Data == null) throw new ArgumentNullException(nameof(action.Data));

            var actionType = action.Action;

            foreach(var item in action.Data)
            {
                if (actionType == DatabaseActionType.Insert || actionType == DatabaseActionType.Update)
                {
                    SetIntoCache(item.GetType(), GetEntityKey(item), item);
                }
                else if (actionType == DatabaseActionType.Delete)
                {
                    DeleteFromCache(item.GetType(), GetEntityKey(item));
                }
            }

            DataQueue.Enqueue(action);
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data directly from the database immediately afterwards.
        /// However, data in the cache will be up to date as soon as a value is changed.
        /// </summary>
        /// <param name="data">The data to submit for processing</param>
        /// <param name="actionType">The type (Insert, Update, Delete, etc.) of change to make.</param>
        public void SubmitDataChange(IEntity data, DatabaseActionType actionType)
        {
            if(data == null) throw new ArgumentNullException(nameof(data));

            if (actionType == DatabaseActionType.Insert || actionType == DatabaseActionType.Update)
            {
                SetIntoCache(data.GetType(), GetEntityKey(data), data);
            }
            else if (actionType == DatabaseActionType.Delete)
            {
                DeleteFromCache(data.GetType(), GetEntityKey(data));
            }

            DataQueue.Enqueue(new DatabaseAction(data, actionType));
        }

        private T GetFromCache<T>(object key)
            where T : IEntity
        {
            if (!_cache.TryGetValue(typeof(T), out var cachedSet))
            {
                cachedSet = new Dictionary<object, object>();
                _cache.Add(typeof(T), cachedSet);
            }

            if (cachedSet.TryGetValue(key, out object cachedObject))
            {
                return (T)cachedObject;
            }
            
            return default(T);
        }

        private void SetIntoCache<T>(object key, object value)
            where T : IEntity
        {
            SetIntoCache(typeof(T), key, value);
        }

        private void SetIntoCache(Type type, object key, object value)
        {
            if (!type.GetInterfaces().Contains(typeof(IEntity)))
                throw new ArgumentException("Only objects which implement " + nameof(IEntity) + " may be set into the cache.");

            if (!_cache.TryGetValue(type, out var cachedSet))
            {
                cachedSet = new Dictionary<object, object>();
                _cache.Add(type, cachedSet);
            }

            if (cachedSet.ContainsKey(key))
            {
                cachedSet[key] = value;
            }
            else
            {
                cachedSet.Add(key, value);
            }

        }

        private void DeleteFromCache<T>(object key)
            where T : IEntity
        {
            DeleteFromCache(typeof(T), key);
        }

        private void DeleteFromCache(Type type, object key)
        {
            if (!_cache.ContainsKey(type)) return;

            var cachedSet = _cache[type];

            if (cachedSet.ContainsKey(key))
            {
                cachedSet.Remove(key);
            }

        }

        /// <summary>
        /// Returns a single entity of a given type from the database or cache.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get<T>(object id)
            where T : class, IEntity
        {
            var cached = GetFromCache<T>(id);
            if (cached != null)
                return cached;

            using (var connection = new SqlConnection(_connectionString))
            {
                cached = connection.Get<T>(id);
                SetIntoCache<T>(id, cached);
            }

            return cached;
        }

        /// <summary>
        /// Returns all entities of a given type from the database or cache.
        /// Keep in mind that if you don't build the cache up-front (i.e: at load time) you will only retrieve records which have been cached so far.
        /// For example, if Get() is called first, then subsequent GetAll() will only retrieve that one object.
        /// To fix this, you should call GetAll() at load time to retrieve everything from the database for that object type.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>()
            where T : class, IEntity
        {
            // todo: temporarily disabled to get up and running
            //if (typeof(T) == typeof(PlayerCharacter))
            //{
            //    throw new ArgumentException("GetAll() is not permitted on PlayerCharacter objects.");
            //}
            
            // Cache already built. Return everything that's cached so far.
            if (_cache.ContainsKey(typeof(T)))
            {
                var cacheSet = _cache[typeof(T)];
                return cacheSet.Values.Cast<T>();
            }

            // Can't find anything in the cache so pull back the records from the database.
            IEnumerable<T> results;
            using (var connection = new SqlConnection(_connectionString))
            {
                results = connection.GetAll<T>();
            }
            
            // Add the records to the cache.
            foreach (var result in results)
            {
                object id = GetEntityKey(result);
                SetIntoCache<T>(id, result);
            }
            
            // Send back the results if we know they exist in the cache.
            if (_cache.TryGetValue(typeof(T), out var set))
            {
                return set.Values.Cast<T>();
            }

            // If there's no data in the database for that table, return an empty list.
            return new List<T>();
        }

        public T Single<T>()
            where T : class, IEntity
        {
            return GetAll<T>().Single();
        }

        public T Single<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return GetAll<T>().Single(predicate);
        }

        public T SingleOrDefault<T>()
            where T : class, IEntity
        {
            return GetAll<T>().SingleOrDefault();
        }

        public T SingleOrDefault<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return GetAll<T>().SingleOrDefault(predicate);
        }

        public IEnumerable<T> Where<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return GetAll<T>().Where(predicate);
        }

        private object GetEntityKey(IEntity entity)
        {
            // Locate a Key or ExplicitKey attribute on this type. These are Dapper attributes which determine if the key
            // is auto-generated (Key) or manually set (ExplicitKey) on the entity.
            // We will reuse these attributes for identifying cache items.
            var properties = entity.GetType().GetProperties();
            PropertyInfo propertyWithKey = null;

            foreach (var prop in properties)
            {
                var autoGenKey = prop.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault();
                if (autoGenKey != null)
                {
                    propertyWithKey = prop;
                    break;
                }

                var explicitKey = prop.GetCustomAttributes(typeof(ExplicitKeyAttribute), false).FirstOrDefault();
                if (explicitKey != null)
                {
                    propertyWithKey = prop;
                    break;
                }
            }

            if (propertyWithKey == null)
            {
                throw new NullReferenceException("Unable to find a Key or ExplicitKey attribute on the entity provided (" + entity.GetType() + "). Make sure you add the Key attribute for a database-generated IDs or an ExplicitKey attribute for manually-set IDs.");
            }

            return propertyWithKey.GetValue(entity);
        }

        public void StoredProcedure(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(BuildSQLQuery(procedureName, args), args);
            }
        }

        public IEnumerable<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, TResult>(string procedureName, Func<T1, T2, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, TResult>(string procedureName, Func<T1, T2, T3, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, TResult>(string procedureName, Func<T1, T2, T3, T4, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, T7, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, T7, TResult> map, string splitOn, SqlParameter arg)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure).SingleOrDefault();
            }
        }

        private static string BuildSQLQuery(string procedureName, params SqlParameter[] args)
        {
            string sql = procedureName;

            for (int x = 0; x < args.Length; x++)
            {
                sql += " " + args[x].ParameterName;

                if (x + 1 < args.Length) sql += ",";
            }

            return sql;
        }
    }
}
