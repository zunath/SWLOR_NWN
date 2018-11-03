
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using Attribute = SWLOR.Game.Server.Data.Entity.Attribute;
using BaseStructureType = SWLOR.Game.Server.Data.Entity.BaseStructureType;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;
using PCBaseType = SWLOR.Game.Server.Data.Entity.PCBaseType;
using PerkExecutionType = SWLOR.Game.Server.Data.Entity.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        private delegate IEnumerable<object> ObjectBuildDelegate(object key = null);

        public ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private string _connectionString;
        private readonly Dictionary<Type, ObjectBuildDelegate> _enumerableBuildProcedures;
        private readonly Dictionary<Type, Dictionary<object, object>> _cache;
        private bool _cacheInitialized;
        
        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
            _enumerableBuildProcedures = new Dictionary<Type, ObjectBuildDelegate>();
            _cache = new Dictionary<Type, Dictionary<object, object>>();

            AddBuildProcedures();
        }

        private void AddBuildProcedures()
        {
            // Objects with children need to be built manually.
            // Whenever GetAll of one of the following types is called,
            // the appropriate build process will run based on the specified type.
            _enumerableBuildProcedures.Add(typeof(Area), BuildAreas);
            _enumerableBuildProcedures.Add(typeof(Bank), BuildBanks);
            _enumerableBuildProcedures.Add(typeof(LootTable), BuildLootTables);
            _enumerableBuildProcedures.Add(typeof(Data.Entity.Perk), BuildPerks);
            _enumerableBuildProcedures.Add(typeof(Quest), BuildQuests);
            //_enumerableBuildProcedures.Add(typeof(PCBase), BuildPCBases);
            //_enumerableBuildProcedures.Add(typeof(PCMigration), BuildPCMigrations);
            //_enumerableBuildProcedures.Add(typeof(PCQuestStatus), BuildPCQuestStatuses);
            //_enumerableBuildProcedures.Add(typeof(PlayerCharacter), BuildPlayerCharacters);
            _enumerableBuildProcedures.Add(typeof(Skill), BuildSkills);
            _enumerableBuildProcedures.Add(typeof(Spawn), BuildSpawns);
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

            if(initializeCache)
                InitializeCache();
        }

        /// <summary>
        /// Retrieves all objects in frequently accessed data from the database and stores them into the cache.
        /// This should only be called one time at initial load.
        /// </summary>
        private void InitializeCache()
        {
            if (_cacheInitialized) return;

            GetAll<ApartmentBuilding>();
            GetAll<Area>(); // Potential size issue due to AreaWalkmeshes (child property)
            GetAll<Association>();
            GetAll<Attribute>();
            GetAll<AuthorizedDM>();
            GetAll<Bank>(); // Potential size issue due to BankItems (child property)
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
            GetAll<DiscordChatQueue>();
            GetAll<DMRoleDomain>();
            GetAll<EnmityAdjustmentRule>();
            GetAll<FameRegion>();
            GetAll<GrowingPlant>();
            GetAll<ItemType>();
            GetAll<KeyItem>();
            GetAll<KeyItemCategory>();
            GetAll<LootTable>();
            GetAll<Data.Entity.Mod>();
            GetAll<NPCGroup>();
            GetAll<PCBase>();
            GetAll<PCBaseType>();
            GetAll<Data.Entity.Perk>();
            GetAll<PerkCategory>();
            GetAll<PerkExecutionType>();
            GetAll<Plant>();
            GetAll<Quest>();
            GetAll<ServerConfiguration>();
            GetAll<Skill>();
            GetAll<SkillCategory>();
            GetAll<Spawn>();
            GetAll<SpawnObjectType>();





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
            SubmitDataChange(new DatabaseAction(data, actionType));
        }
        
        private T GetFromCache<T>(object key)
        {
            if(!_cache.TryGetValue(typeof(T), out var cachedSet))
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
        {
            if (!_cache.TryGetValue(typeof(T), out var cachedSet))
            {
                cachedSet = new Dictionary<object, object>();
                _cache.Add(typeof(T), cachedSet);
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
        {
            if (!_cache.ContainsKey(typeof(T))) return;

            var cachedSet = _cache[typeof(T)];

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
            where T: class, ICacheable
        {
            var cached = GetFromCache<T>(id);
            if (cached != null)
                return cached;

            // Some types require manual building of child properties.
            if (_enumerableBuildProcedures.ContainsKey(typeof(T)))
            {
                object retrieved = _enumerableBuildProcedures[typeof(T)].Invoke(id);
                cached = (T) retrieved;
                SetIntoCache<T>(id, cached);
            }
            // All others can be simply retrieved by ID.
            else
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    cached = connection.Get<T>(id);
                    SetIntoCache<T>(id, cached);
                }
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
            where T: class, ICacheable
        {
            // Cache already built. Return everything that's cached so far.
            if(_cache.ContainsKey(typeof(T)))
            {
                var cacheSet = _cache[typeof(T)];
                return cacheSet.Cast<T>();
            }

            IEnumerable<T> results;

            // Some types require manual building of child properties.
            if (_enumerableBuildProcedures.ContainsKey(typeof(T)))
            {
                IEnumerable<object> result = _enumerableBuildProcedures[typeof(T)].Invoke();
                results = result.Cast<T>();
            }
            // All others can be simply retrieved by ID.
            else
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    results = connection.GetAll<T>();
                }
            }

            // Locate a Key or ExplicitKey attribute on this type. These are Dapper attributes which determine if the key
            // is auto-generated (Key) or manually set (ExplicitKey) on the entity.
            // We will reuse these attributes for identifying cache items.
            var properties = typeof(T).GetProperties();
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
                throw new NullReferenceException("Unable to find a Key or ExplicitKey attribute on the entity provided (" + typeof(T) + "). Make sure you add the Key attribute for a database-generated IDs or an ExplicitKey attribute for manually-set IDs.");
            }

            foreach (var result in results)
            {
                object id = propertyWithKey.GetValue(result);
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

        private IEnumerable<Area> BuildAreas(object key = null)
        {
            var areaDictionary = new Dictionary<string, Area>();
            var banks = StoredProcedure<Area, AreaWalkmesh, Area>(
                    "GetAreas",
                    (area, item) =>
                    {
                        if (!areaDictionary.TryGetValue(area.AreaID, out var a))
                        {
                            a = area;
                            areaDictionary.Add(a.AreaID, a);
                        }

                        if (item != null)
                        {
                            a.AreaWalkmeshes.Add(item);
                        }

                        return a;
                    }, "AreaWalkmeshID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return banks;
        }
        
        private IEnumerable<Bank> BuildBanks(object key = null)
        {
            var bankDictionary = new Dictionary<int, Bank>();
            var banks = StoredProcedure<Bank, BankItem, Bank>(
                    "GetBanks",
                    (bank, item) =>
                    {
                        if (!bankDictionary.TryGetValue(bank.BankID, out var b))
                        {
                            b = bank;
                            bankDictionary.Add(b.BankID, b);
                        }

                        if (item != null)
                        {
                            b.BankItems.Add(item);
                        }

                        return b;
                    }, "BankItemID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return banks;

        }

        private IEnumerable<LootTable> BuildLootTables(object key = null)
        {
            var ltDictionary = new Dictionary<int, LootTable>();
            var lootTables = StoredProcedure<LootTable, LootTableItem, LootTable>(
                    "GetLootTables",
                    (table, item) =>
                    {
                        if (!ltDictionary.TryGetValue(table.LootTableID, out var lt))
                        {
                            lt = table;
                            ltDictionary.Add(lt.LootTableID, lt);
                        }

                        lt.LootTableItems.Add(item);
                        return lt;
                    }, "LootTableItemID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return lootTables;
        }

        private IEnumerable<Data.Entity.Perk> BuildPerks(object key = null)
        {
            var perkDictionary = new Dictionary<int, Data.Entity.Perk>();
            var perkLevelDictionary = new Dictionary<int, PerkLevel>();
            var perkLevelSkillRequirementDictionary = new Dictionary<int, PerkLevelSkillRequirement>();
            var perkLevelQuestRequirementDictionary = new Dictionary<int, PerkLevelQuestRequirement>();

            var perks = StoredProcedure<Data.Entity.Perk, PerkLevel, PerkLevelSkillRequirement, PerkLevelQuestRequirement, Data.Entity.Perk>(
                "GetPerks",
                (perk, perkLevel, skillReq, questReq) =>
                {
                    // Process the perk
                    if (!perkDictionary.TryGetValue(perk.PerkID, out var p))
                    {
                        p = perk;
                        perkDictionary.Add(p.PerkID, p);
                    }

                    // Process perk levels
                    if (!perkLevelDictionary.TryGetValue(perkLevel.PerkLevelID, out var pl))
                    {
                        pl = perkLevel;
                        perkLevelDictionary.Add(pl.PerkLevelID, pl);
                    }

                    if (skillReq != null)
                    {
                        // Process the perk skill requirements
                        if (!perkLevelSkillRequirementDictionary.TryGetValue(skillReq.PerkLevelSkillRequirementID, out var sr))
                        {
                            sr = skillReq;

                            // Add to both the result dictionary as well as the perk level.
                            perkLevelSkillRequirementDictionary.Add(sr.PerkLevelSkillRequirementID, sr);
                            pl.PerkLevelSkillRequirements.Add(sr);
                        }
                    }

                    if (questReq != null)
                    {
                        // Process the perk quest requirements
                        if (!perkLevelQuestRequirementDictionary.TryGetValue(questReq.PerkLevelQuestRequirementID, out var qr))
                        {
                            qr = questReq;

                            // Add to both the result dictionary as well as the perk level.
                            perkLevelQuestRequirementDictionary.Add(qr.PerkLevelQuestRequirementID, qr);
                            pl.PerkLevelQuestRequirements.Add(qr);
                        }
                    }

                    // Add the perk level info to the perk and return it.
                    p.PerkLevels.Add(pl);
                    return p;
                }, "PerkLevelID,PerkLevelSkillRequirementID,PerkLevelQuestRequirementID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return perks;
        }

        private IEnumerable<Quest> BuildQuests(object key = null)
        {
            var questDictionary = new Dictionary<int, Quest>();
            var questStateDictionary = new Dictionary<int, QuestState>();
            var questKillTargetListDictionary = new Dictionary<int, QuestKillTargetList>();
            var questPrerequisiteDictionary = new Dictionary<int, QuestPrerequisite>();
            var questRequiredItemListDictionary = new Dictionary<int, QuestRequiredItemList>();
            var questRequiredKeyItemListDictionary = new Dictionary<int, QuestRequiredKeyItemList>();
            var questRewardItemDictionary = new Dictionary<int, QuestRewardItem>();
            
            var perks = StoredProcedure<Quest, QuestState, QuestKillTargetList, QuestRequiredItemList, QuestPrerequisite, QuestRequiredKeyItemList, QuestRewardItem, Quest>(
                "GetQuests",
                (quest, questState, killTarget, requiredItem, prereq, requiredKeyItem, rewardItem) =>
                {
                    // Process the quest
                    if (!questDictionary.TryGetValue(quest.QuestID, out var q))
                    {
                        q = quest;

                        questDictionary.Add(q.QuestID, q);
                    }

                    // Process quest state
                    if (!questStateDictionary.TryGetValue(questState.QuestStateID, out var qs))
                    {
                        qs = questState;
                        questStateDictionary.Add(qs.QuestStateID, qs);
                    }

                    // Process kill targets
                    if (killTarget != null)
                    {
                        if (!questKillTargetListDictionary.TryGetValue(killTarget.QuestKillTargetListID, out var kt))
                        {
                            kt = killTarget;
                            questKillTargetListDictionary.Add(kt.QuestKillTargetListID, kt);
                            qs.QuestKillTargetLists.Add(kt);
                        }
                    }

                    // Process required items
                    if (requiredItem != null)
                    {
                        if (!questRequiredItemListDictionary.TryGetValue(requiredItem.QuestRequiredItemListID, out var ri))
                        {
                            ri = requiredItem;

                            questRequiredItemListDictionary.Add(ri.QuestRequiredItemListID, ri);
                            qs.QuestRequiredItemLists.Add(ri);
                        }
                    }

                    // Process prerequisite quests
                    if (prereq != null)
                    {
                        if (!questPrerequisiteDictionary.TryGetValue(prereq.QuestPrerequisiteID, out var pq))
                        {
                            pq = prereq;

                            questPrerequisiteDictionary.Add(pq.QuestPrerequisiteID, pq);
                            q.QuestPrerequisites.Add(pq);
                        }
                    }

                    // Process required key items
                    if (requiredKeyItem != null)
                    {
                        if (!questRequiredKeyItemListDictionary.TryGetValue(requiredKeyItem.QuestRequiredKeyItemID, out var rki))
                        {
                            rki = requiredKeyItem;

                            questRequiredKeyItemListDictionary.Add(rki.QuestRequiredKeyItemID, rki);
                            qs.QuestRequiredKeyItemLists.Add(rki);
                        }
                    }

                    // Process reward items
                    if (rewardItem != null)
                    {
                        if (!questRewardItemDictionary.TryGetValue(rewardItem.QuestRewardItemID, out var rw))
                        {
                            rw = rewardItem;

                            questRewardItemDictionary.Add(rw.QuestRewardItemID, rw);
                            q.QuestRewardItems.Add(rw);
                        }
                    }

                    q.QuestStates.Add(qs);
                    return q;
                }, "QuestStateID,QuestKillTargetListID,QuestRequiredItemListID,QuestPrerequisiteID,QuestRequiredKeyItemID,QuestRewardItemID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return perks;
        }

        //private IEnumerable<PCBase> BuildPCBases(object key = null)
        //{

        //}

        private IEnumerable<Skill> BuildSkills(object key = null)
        {
            var skillDictionary = new Dictionary<int, Skill>();
            var skills = StoredProcedure<Skill, SkillXPRequirement, Skill>(
                    "GetSkills",
                    (skill, xpReq) =>
                    {
                        if (!skillDictionary.TryGetValue(skill.SkillID, out var s))
                        {
                            s = skill;
                            skillDictionary.Add(s.SkillID, s);
                        }

                        s.SkillXPRequirements.Add(xpReq);
                        return s;
                    }, "SkillXPRequirementID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return skills;
        }


        private IEnumerable<Spawn> BuildSpawns(object key = null)
        {
            var spawnDictionary = new Dictionary<int, Spawn>();
            var spawns = StoredProcedure<Spawn, SpawnObject, Spawn>(
                    "GetSpawns",
                    (spawn, obj) =>
                    {
                        if (!spawnDictionary.TryGetValue(spawn.SpawnID, out var s))
                        {
                            s = spawn;
                            spawnDictionary.Add(s.SpawnID, s);
                        }

                        s.SpawnObjects.Add(obj);
                        return s;
                    }, "SpawnObjectID", key == null ? null : new SqlParameter("@Key", key))
                .Distinct()
                .ToList();

            return spawns;
        }
    }
}
