
using Dapper;

using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Data;
using Attribute = SWLOR.Game.Server.Data.Entity.Attribute;
using BaseStructureType = SWLOR.Game.Server.Data.Entity.BaseStructureType;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;
using PCBaseType = SWLOR.Game.Server.Data.Entity.PCBaseType;
using QuestType = SWLOR.Game.Server.Data.Entity.QuestType;

namespace SWLOR.Game.Server.Service
{
    public static class DataService
    {
        public static ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private static bool _cacheInitialized;
        public static string MasterConnectionString { get; }
        public static string SWLORConnectionString { get; private set; }
        public static Dictionary<Type, Dictionary<object, object>> Cache { get; }
        public static SqlConnection Connection { get; private set; }

        static DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
            Cache = new Dictionary<Type, Dictionary<object, object>>();

            var ip = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS");
            var user = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME");
            var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
            var database = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE");


            MasterConnectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = "MASTER",
                UserID = user,
                Password = password
            }.ToString();
            SWLORConnectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = database,
                UserID = user,
                Password = password
            }.ToString();

        }

        public static void Initialize(bool initializeCache)
        {
            Connection = new SqlConnection(SWLORConnectionString);

            if (initializeCache)
                InitializeCache();
        }

        public static void Initialize(string ip, string database, string user, string password, bool initializeCache)
        {
            SWLORConnectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = database,
                UserID = user,
                Password = password
            }.ToString();

            Connection = new SqlConnection(SWLORConnectionString);

            if (initializeCache)
                InitializeCache();
        }

        /// <summary>
        /// Retrieves all objects in frequently accessed data from the database and stores them into the cache.
        /// This should only be called one time at initial load.
        /// </summary>
        private static void InitializeCache()
        {
            if (_cacheInitialized) return;

            Console.WriteLine("Initializing the cache...");
            GetAll<ApartmentBuilding>();
            // Note: Area and AreaWalkmesh get cached in the AreaService
            GetAll<Association>();
            GetAll<Attribute>();
            GetAll<AuthorizedDM>();
            GetAll<Bank>();
            RegisterEmptyCacheSet<BankItem>();
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
            GetAll<DMRole>();
            GetAll<EnmityAdjustmentRule>();
            GetAll<FameRegion>();
            GetAll<Guild>();
            GetAll<GuildTask>();
            GetAll<GrowingPlant>();
            GetAll<ItemType>();
            GetAll<JukeboxSong>();
            GetAll<KeyItem>();
            GetAll<KeyItemCategory>();
            GetAll<LootTable>();
            GetAll<LootTableItem>();
            GetAll<MarketCategory>();
            GetAll<Message>();
            GetAll<NPCGroup>();
            GetAll<PCBase>();
            GetAll<PCBasePermission>();
            GetAll<PCBaseStructure>();
            GetAll<PCBaseStructureItem>();
            GetAll<PCBaseStructurePermission>();
            GetAll<PCBaseType>();
            LoadPCMarketListingCache();
            GetAll<SpaceStarport>();
            GetAll<SpaceEncounter>();

            RegisterEmptyCacheSet<PCCooldown>();
            RegisterEmptyCacheSet<PCCraftedBlueprint>();
            RegisterEmptyCacheSet<PCCustomEffect>();
            RegisterEmptyCacheSet<PCImpoundedItem>();
            RegisterEmptyCacheSet<PCGuildPoint>();
            RegisterEmptyCacheSet<PCKeyItem>();
            RegisterEmptyCacheSet<PCMapPin>();
            RegisterEmptyCacheSet<PCMapProgression>();
            RegisterEmptyCacheSet<PCObjectVisibility>();
            RegisterEmptyCacheSet<PCOutfit>();
            RegisterEmptyCacheSet<PCOverflowItem>();
            RegisterEmptyCacheSet<PCPerk>();
            RegisterEmptyCacheSet<PCQuestItemProgress>();
            RegisterEmptyCacheSet<PCQuestKillTargetProgress>();
            RegisterEmptyCacheSet<PCQuestStatus>();
            RegisterEmptyCacheSet<PCRegionalFame>();
            RegisterEmptyCacheSet<PCSearchSite>();
            RegisterEmptyCacheSet<PCSearchSiteItem>();
            RegisterEmptyCacheSet<PCSkill>();
            RegisterEmptyCacheSet<PCSkillPool>();
            RegisterEmptyCacheSet<PCPerkRefund>();

            GetAll<Data.Entity.Perk>();
            GetAll<PerkFeat>();
            GetAll<PerkCategory>();
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
            GetAll<Spawn>();
            GetAll<SpawnObject>();
            GetAll<SpawnObjectType>();
            GetAll<StructureMode>();
            Console.WriteLine("Cache initialized!");

            _cacheInitialized = true;
        }

        /// <summary>
        /// PC Market Listings should only be loaded into the cache if they:
        /// 1.) Haven't been sold already
        /// 2.) Haven't been removed by the seller.
        /// This method will retrieve these specific records and store them into the cache.
        /// Should be called in the InitializeCache() method.
        /// </summary>
        private static void LoadPCMarketListingCache()
        {
            const string Sql = "SELECT * FROM dbo.PCMarketListing WHERE DateSold IS NULL AND DateRemoved IS NULL";

            var results = Connection.Query<PCMarketListing>(Sql);
            
            foreach (var result in results)
            {
                object id = GetEntityKey(result);
                SetIntoCache<PCMarketListing>(id, result);
            }
        }

        /// <summary>
        /// Caches a player's data. Be sure to call RemoveCachedPlayerData when the player exits the game.
        /// </summary>
        /// <param name="player"></param>
        public static void CachePlayerData(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            
            Console.WriteLine("Starting CachePlayerData for ID = " + player.GlobalID);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (var multi = Connection.QueryMultiple("GetPlayerData", new { PlayerID = player.GlobalID }, commandType: CommandType.StoredProcedure))
            {
                foreach (var item in multi.Read<PCCooldown>())
                    SetIntoCache<PCCooldown>(item.ID, item);
                foreach (var item in multi.Read<PCCraftedBlueprint>())
                    SetIntoCache<PCCraftedBlueprint>(item.ID, item);

                foreach (var item in multi.Read<PCCustomEffect>())
                    SetIntoCache<PCCustomEffect>(item.ID, item);
                foreach (var item in multi.Read<PCImpoundedItem>())
                    SetIntoCache<PCImpoundedItem>(item.ID, item);
                foreach (var item in multi.Read<PCKeyItem>())
                    SetIntoCache<PCKeyItem>(item.ID, item);
                foreach (var item in multi.Read<PCMapPin>())
                    SetIntoCache<PCMapPin>(item.ID, item);
                foreach (var item in multi.Read<PCMapProgression>())
                    SetIntoCache<PCMapProgression>(item.ID, item);
                foreach (var item in multi.Read<PCObjectVisibility>())
                    SetIntoCache<PCObjectVisibility>(item.ID, item);

                var outfit = multi.Read<PCOutfit>().SingleOrDefault();

                if (outfit != null)
                    SetIntoCache<PCOutfit>(outfit.PlayerID, outfit);

                foreach (var item in multi.Read<PCOverflowItem>())
                    SetIntoCache<PCOverflowItem>(item.ID, item);
                foreach (var item in multi.Read<PCPerk>())
                    SetIntoCache<PCPerk>(item.ID, item);
                foreach (var item in multi.Read<PCQuestItemProgress>())
                    SetIntoCache<PCQuestItemProgress>(item.ID, item);
                foreach (var item in multi.Read<PCQuestKillTargetProgress>())
                    SetIntoCache<PCQuestKillTargetProgress>(item.ID, item);
                foreach (var item in multi.Read<PCQuestStatus>())
                    SetIntoCache<PCQuestStatus>(item.ID, item);
                foreach (var item in multi.Read<PCRegionalFame>())
                    SetIntoCache<PCRegionalFame>(item.ID, item);
                foreach (var item in multi.Read<PCSearchSite>())
                    SetIntoCache<PCSearchSite>(item.ID, item);
                foreach (var item in multi.Read<PCSearchSiteItem>())
                    SetIntoCache<PCSearchSiteItem>(item.ID, item);
                foreach (var item in multi.Read<PCSkill>())
                    SetIntoCache<PCSkill>(item.ID, item);
                foreach (var item in multi.Read<BankItem>())
                    SetIntoCache<BankItem>(item.ID, item);
                foreach (var item in multi.Read<PCSkillPool>())
                    SetIntoCache<PCSkillPool>(item.ID, item);
                foreach(var item in multi.Read<PCGuildPoint>())
                    SetIntoCache<PCGuildPoint>(item.ID, item);
            }
            sw.Stop();
            Console.WriteLine("CachePlayerData took " + sw.ElapsedMilliseconds + " ms to run. Player ID = " + player.GlobalID);

            Get<Player>(player.GlobalID);
        }

        /// <summary>
        /// Removes a player's cached data. Be sure to call this ONLY on the OnClientLeave event.
        /// </summary>
        /// <param name="player"></param>
        public static void RemoveCachedPlayerData(NWPlayer player)
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
            foreach(var item in Where<BankItem>(x => x.PlayerID == id).ToList())
                DeleteFromCache<BankItem>(item.ID);
            foreach(var item in Where<PCSkillPool>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCSkillPool>(item.ID);
            foreach(var item in Where<PCGuildPoint>(x => x.PlayerID == id).ToList())
                DeleteFromCache<PCGuildPoint>(item.ID);
        }

        /// <summary>
        /// Sends a request to change data into the database queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data directly from the database immediately afterwards.
        /// However, data in the cache will be up to date as soon as a value is changed.
        /// </summary>
        public static void SubmitDataChange(DatabaseAction action)
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
        public static void SubmitDataChange(IEntity data, DatabaseActionType actionType)
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

        private static T GetFromCache<T>(object key)
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

        private static void SetIntoCache<T>(object key, object value)
            where T : IEntity
        {
            SetIntoCache(typeof(T), key, value);
        }

        private static void SetIntoCache(Type type, object key, object value)
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

        private static void DeleteFromCache<T>(object key)
            where T : IEntity
        {
            DeleteFromCache(typeof(T), key);
        }

        private static void DeleteFromCache(Type type, object key)
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
        public static T Get<T>(object id)
            where T : class, IEntity
        {
            var cached = GetFromCache<T>(id);
            if (cached != null)
                return cached;

            cached = Connection.Get<T>(id);
            SetIntoCache<T>(id, cached);
            
            return cached;
        }

        private static void RegisterEmptyCacheSet<T>()
            where T: class, IEntity
        {
            if (Cache.ContainsKey(typeof(T)))
            {
                throw new Exception("Cannot register an empty cacheset because it already exists! Did you call " + nameof(RegisterEmptyCacheSet) + " more than once? Type = " + typeof(T));
            }

            var cachedSet = new Dictionary<object, object>();
            Cache.Add(typeof(T), cachedSet);
        }

        /// <summary>
        /// Returns all entities of a given type from the database or cache.
        /// Keep in mind that if you don't build the cache up-front (i.e: at load time) you will only retrieve records which have been cached so far.
        /// For example, if Get() is called first, then subsequent GetAll() will only retrieve that one object.
        /// To fix this, you should call GetAll() at load time to retrieve everything from the database for that object type.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetAll<T>()
            where T : class, IEntity
        {
            // Cache already built. Return everything that's cached so far.
            if (Cache.ContainsKey(typeof(T)))
            {
                var cacheSet = Cache[typeof(T)];
                return cacheSet.Values.Cast<T>();
            }

            // Can't find anything in the cache so pull back the records from the database.
            IEnumerable<T> results = Connection.GetAll<T>();
            
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

        public static T Single<T>()
            where T : class, IEntity
        {
            return GetAll<T>().Single();
        }

        public static T Single<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return GetAll<T>().Single(predicate);
        }

        public static T SingleOrDefault<T>()
            where T : class, IEntity
        {
            return GetAll<T>().SingleOrDefault();
        }

        public static T SingleOrDefault<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return GetAll<T>().SingleOrDefault(predicate);
        }

        public static HashSet<T> Where<T>(Func<T, bool> predicate)
            where T : class, IEntity
        {
            return new HashSet<T>(GetAll<T>().Where(predicate));
        }

        private static object GetEntityKey(IEntity entity)
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

        public static void StoredProcedure(string procedureName, params SqlParameter[] args)
        {
            Connection.Execute(BuildSQLQuery(procedureName, args), args);
        }

        public static IEnumerable<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args)
        {
            return Connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, TResult>(string procedureName, Func<T1, T2, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, T3, TResult>(string procedureName, Func<T1, T2, T3, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, TResult>(string procedureName, Func<T1, T2, T3, T4, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, T7, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, T7, TResult> map, string splitOn, SqlParameter arg)
        {
            return Connection.Query(procedureName, map, arg, splitOn: splitOn, commandType: CommandType.StoredProcedure);
        }

        public static T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args)
        {
            return Connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure).SingleOrDefault();
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
