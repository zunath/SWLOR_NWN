
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
using QuestType = SWLOR.Game.Server.Data.Entity.QuestType;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        public ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private string _connectionString;
        private bool _cacheInitialized;

        public Dictionary<Type, Dictionary<object, object>> Cache { get; }

        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
            Cache = new Dictionary<Type, Dictionary<object, object>>();

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
            GetAll<ChatChannel>();
            GetAll<ClientLogEventType>();
            GetAll<ComponentType>();
            GetAll<CooldownCategory>();
            GetAll<CraftBlueprint>();
            GetAll<CraftBlueprintCategory>();
            GetAll<CraftDevice>();
            GetAll<Data.Entity.CustomEffect>();
            GetAll<CustomEffectCategory>();
            GetAll<DiscordChatQueue>();
            GetAll<DMRole>();
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
            //GetAll<PCCooldown>();
            //GetAll<PCCraftedBlueprint>();
            //GetAll<PCCustomEffect>();
            //GetAll<PCImpoundedItem>();
            //GetAll<PCKeyItem>();
            //GetAll<PCMapPin>();
            //GetAll<PCMapProgression>();
            //GetAll<PCObjectVisibility>();
            //GetAll<PCOutfit>();
            //GetAll<PCOverflowItem>();
            //GetAll<PCPerk>();
            //GetAll<PCPerkRefund>();
            //GetAll<PCQuestItemProgress>();
            //GetAll<PCQuestKillTargetProgress>();
            //GetAll<PCQuestStatus>();
            //GetAll<PCRegionalFame>();
            //GetAll<PCSearchSite>();
            //GetAll<PCSearchSiteItem>();
            //GetAll<PCSkill>();
            GetAll<Data.Entity.Perk>();
            GetAll<PerkCategory>();
            GetAll<PerkExecutionType>();
            GetAll<PerkLevel>();
            GetAll<PerkLevelQuestRequirement>();
            GetAll<PerkLevelSkillRequirement>();
            GetAll<Plant>();
            GetAll<Player>(); // Load all player data as it's referenced in other systems even if player isn't online.
            GetAll<Quest>();
            GetAll<QuestKillTarget>();
            GetAll<QuestPrerequisite>();
            GetAll<QuestRequiredItem>();
            GetAll<QuestRequiredKeyItem>();
            GetAll<QuestRewardItem>();
            GetAll<QuestState>();
            GetAll<QuestType>();
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

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var multi = connection.QueryMultiple("GetPlayerData", new {PlayerID = player.GlobalID}, commandType: CommandType.StoredProcedure))
                {
                    foreach(var item in multi.Read<PCCooldown>().ToList())
                        SetIntoCache<PCCooldown>(item.ID, item);
                    foreach (var item in multi.Read<PCCraftedBlueprint>().ToList())
                        SetIntoCache<PCCraftedBlueprint>(item.ID, item);

                    foreach (var item in multi.Read<PCCustomEffect>().ToList())
                        SetIntoCache<PCCustomEffect>(item.ID, item);
                    foreach (var item in multi.Read<PCImpoundedItem>().ToList())
                        SetIntoCache<PCImpoundedItem>(item.ID, item);
                    foreach (var item in multi.Read<PCKeyItem>().ToList())
                        SetIntoCache<PCKeyItem>(item.ID, item);
                    foreach (var item in multi.Read<PCMapPin>().ToList())
                        SetIntoCache<PCMapPin>(item.ID, item);
                    foreach (var item in multi.Read<PCMapProgression>().ToList())
                        SetIntoCache<PCMapProgression>(item.ID, item);
                    foreach (var item in multi.Read<PCObjectVisibility>().ToList())
                        SetIntoCache<PCObjectVisibility>(item.ID, item);

                    var outfit = multi.Read<PCOutfit>().SingleOrDefault();

                    if(outfit != null)
                        SetIntoCache<PCOutfit>(outfit.PlayerID, outfit);

                    foreach (var item in multi.Read<PCOverflowItem>().ToList())
                        SetIntoCache<PCOverflowItem>(item.ID, item);
                    foreach (var item in multi.Read<PCPerk>().ToList())
                        SetIntoCache<PCPerk>(item.ID, item);
                    foreach (var item in multi.Read<PCQuestItemProgress>().ToList())
                        SetIntoCache<PCQuestItemProgress>(item.ID, item);
                    foreach (var item in multi.Read<PCQuestKillTargetProgress>().ToList())
                        SetIntoCache<PCQuestKillTargetProgress>(item.ID, item);
                    foreach (var item in multi.Read<PCQuestStatus>().ToList())
                        SetIntoCache<PCQuestStatus>(item.ID, item);
                    foreach (var item in multi.Read<PCRegionalFame>().ToList())
                        SetIntoCache<PCRegionalFame>(item.ID, item);
                    foreach (var item in multi.Read<PCSearchSite>().ToList())
                        SetIntoCache<PCSearchSite>(item.ID, item);
                    foreach (var item in multi.Read<PCSearchSiteItem>().ToList())
                        SetIntoCache<PCSearchSiteItem>(item.ID, item);

                    foreach(var item in multi.Read<PCSkill>().ToList())
                        SetIntoCache<PCSkill>(item.ID, item);
                }
            }

            Get<Player>(player.GlobalID);
        }

        /// <summary>
        /// Removes a player's cached data. Be sure to call this ONLY on the OnClientLeave event.
        /// </summary>
        /// <param name="player"></param>
        public void RemoveCachedPlayerData(NWPlayer player)
        {
            if (!player.IsPlayer) return;

            Guid id = player.GlobalID;
            
            foreach(var item in Where<PCCooldown>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCCooldown>(item.ID);
            foreach (var item in Where<PCCraftedBlueprint>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCCraftedBlueprint>(item.ID);
            foreach (var item in Where<PCCustomEffect>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCCustomEffect>(item.ID);
            foreach (var item in Where<PCImpoundedItem>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCImpoundedItem>(item.ID);
            foreach (var item in Where<PCKeyItem>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCKeyItem>(item.ID);
            foreach (var item in Where<PCMapPin>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCMapPin>(item.ID);
            foreach (var item in Where<PCMapProgression>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCMapProgression>(item.ID);
            foreach (var item in Where<PCObjectVisibility>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCObjectVisibility>(item.ID);
            foreach (var item in Where<PCOutfit>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCOutfit>(item.PlayerID);
            foreach (var item in Where<PCOverflowItem>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCOverflowItem>(item.ID);
            foreach (var item in Where<PCPerk>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCPerk>(item.ID);
            foreach (var item in Where<PCPerkRefund>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCPerkRefund>(item.ID);
            foreach (var item in Where<PCQuestItemProgress>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCQuestItemProgress>(item.ID);
            foreach (var item in Where<PCQuestKillTargetProgress>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCQuestKillTargetProgress>(item.ID);
            foreach (var item in Where<PCQuestStatus>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCQuestStatus>(item.ID);
            foreach (var item in Where<PCRegionalFame>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCRegionalFame>(item.ID);
            foreach (var item in Where<PCSearchSite>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCSearchSite>(item.ID);
            foreach (var item in Where<PCSearchSiteItem>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCSearchSiteItem>(item.ID);
            foreach(var item in Where<PCSkill>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCSkill>(item.ID);
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
            if (!Cache.TryGetValue(typeof(T), out var cachedSet))
            {
                cachedSet = new Dictionary<object, object>();
                Cache.Add(typeof(T), cachedSet);
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

            if (!Cache.TryGetValue(type, out var cachedSet))
            {
                cachedSet = new Dictionary<object, object>();
                Cache.Add(type, cachedSet);
            }

            // Safety check to ensure all key types are the same for a given type.
            if (cachedSet.Count > 0)
            {
                var first = cachedSet.Keys.First();
                if (first.GetType() != key.GetType())
                {
                    throw new Exception("Cannot set key of type " + key.GetType() + " into cache because the established type is already defined as " + first.GetType());
                }
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
            if (!Cache.ContainsKey(type)) return;

            var cachedSet = Cache[type];

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
            // Cache already built. Return everything that's cached so far.
            if (Cache.ContainsKey(typeof(T)))
            {
                var cacheSet = Cache[typeof(T)];
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
            if (Cache.TryGetValue(typeof(T), out var set))
            {
                return new HashSet<T>(set.Values.Cast<T>());
            }

            // If there's no data in the database for that table, return an empty list.
            return new HashSet<T>();
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

        public HashSet<T> Where<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return new HashSet<T>(GetAll<T>().Where(predicate));
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
