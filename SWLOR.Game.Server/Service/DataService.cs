
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        private delegate IEnumerable<object> ObjectBuildDelegate();

        public ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private string _connectionString;
        private readonly Dictionary<Type, ObjectBuildDelegate> _enumerableBuildProcedures;

        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
            _enumerableBuildProcedures = new Dictionary<Type, ObjectBuildDelegate>();

            // Objects with children need to be built manually.
            // Whenever GetAll of one of the following types is called,
            // the appropriate build process will run based on the specified type.
            _enumerableBuildProcedures.Add(typeof(LootTable), BuildLootTables);
            _enumerableBuildProcedures.Add(typeof(Data.Perk), BuildPerks);
            _enumerableBuildProcedures.Add(typeof(Quest), BuildQuests);
            _enumerableBuildProcedures.Add(typeof(Skill), BuildSkills);
            _enumerableBuildProcedures.Add(typeof(Spawn), BuildSpawns);
        }

        public void Initialize()
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS"),
                InitialCatalog = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE"),
                UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
            }.ToString();
        }

        public void Initialize(string ip, string database, string user, string password)
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = database,
                UserID = user,
                Password = password
            }.ToString();
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data immediately afterwards.
        /// </summary>
        public void SubmitDataChange(DatabaseAction action)
        {
            DataQueue.Enqueue(action);
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data immediately afterwards.
        /// </summary>
        /// <param name="data">The data to submit for processing</param>
        /// <param name="actionType">The type (Insert, Update, Delete, etc.) of change to make.</param>
        public void SubmitDataChange(IEntity data, DatabaseActionType actionType)
        {
            SubmitDataChange(new DatabaseAction(data, actionType));
        }

        /// <summary>
        /// Returns a single entity of a given type from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get<T>(object id)
            where T: class, IEntity
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Get<T>(id);
            }
        }

        /// <summary>
        /// Returns all entities of a given type from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>()
            where T: class, IEntity
        {
            if (_enumerableBuildProcedures.ContainsKey(typeof(T)))
            {
                IEnumerable<object> result = _enumerableBuildProcedures[typeof(T)].Invoke();
                return result.Cast<T>();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.GetAll<T>();
            }
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

        public IEnumerable<TResult> StoredProcedure<T1, T2, TResult>(string procedureName, Func<T1, T2, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, TResult>(string procedureName, Func<T1, T2, T3, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, TResult>(string procedureName, Func<T1, T2, T3, T4, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }
        
        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, T5, T6, T7, TResult>(string procedureName, Func<T1, T2, T3, T4, T5, T6, T7, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
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
        
        private IEnumerable<LootTable> BuildLootTables()
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
                    }, "LootTableItemID")
                .Distinct()
                .ToList();

            return lootTables;
        }

        private IEnumerable<Data.Perk> BuildPerks()
        {
            var perkDictionary = new Dictionary<int, Data.Perk>();
            var perkLevelDictionary = new Dictionary<int, PerkLevel>();
            var perkLevelSkillRequirementDictionary = new Dictionary<int, PerkLevelSkillRequirement>();
            var perkLevelQuestRequirementDictionary = new Dictionary<int, PerkLevelQuestRequirement>();

            var perks = StoredProcedure<Data.Perk, PerkLevel, PerkLevelSkillRequirement, PerkLevelQuestRequirement, Data.Perk>(
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
                }, "PerkLevelID,PerkLevelSkillRequirementID,PerkLevelQuestRequirementID")
                .Distinct()
                .ToList();

            return perks;
        }

        private IEnumerable<Quest> BuildQuests()
        {
            var questDictionary = new Dictionary<int, Data.Quest>();
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
                }, "QuestStateID,QuestKillTargetListID,QuestRequiredItemListID,QuestPrerequisiteID,QuestRequiredKeyItemID,QuestRewardItemID")
                .Distinct()
                .ToList();

            return perks;
        }

        private IEnumerable<Skill> BuildSkills()
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
                    }, "SkillXPRequirementID")
                .Distinct()
                .ToList();

            return skills;
        }


        private IEnumerable<Spawn> BuildSpawns()
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
                    }, "SpawnObjectID")
                .Distinct()
                .ToList();

            return spawns;
        }
    }
}
