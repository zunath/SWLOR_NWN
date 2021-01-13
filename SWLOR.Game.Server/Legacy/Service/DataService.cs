using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Dapper;
using Dapper.Contrib.Extensions;
using MySqlConnector;
using SWLOR.Game.Server.Legacy.Caching;
using SWLOR.Game.Server.Legacy.Data.Contracts;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class DataService
    {
        public static ConcurrentQueue<DatabaseAction> DataQueue { get; }
        public static string SWLORConnectionString { get; }
        public static MySqlConnection Connection { get; private set; }

        public static AreaCache Area { get; } = new AreaCache();
        public static LootTableCache LootTable { get; } = new LootTableCache();
        public static MessageCache Message { get; } = new MessageCache();
        public static PCPerkCache PCPerk { get; } = new PCPerkCache();
        public static PCQuestStatusCache PCQuestStatus { get; } = new PCQuestStatusCache();
        public static PCSkillCache PCSkill { get; } = new PCSkillCache();
        public static PCSkillPoolCache PCSkillPool { get; } = new PCSkillPoolCache();
        public static PerkCache Perk { get; } = new PerkCache();
        public static PerkFeatCache PerkFeat { get; } = new PerkFeatCache();
        public static PerkLevelCache PerkLevel { get; } = new PerkLevelCache();
        public static PerkLevelQuestRequirementCache PerkLevelQuestRequirement { get; } = new PerkLevelQuestRequirementCache();
        public static PerkLevelSkillRequirementCache PerkLevelSkillRequirement { get; } = new PerkLevelSkillRequirementCache();
        public static PlayerCache Player { get; } = new PlayerCache();
        public static SkillCache Skill { get; } = new SkillCache();
        public static SkillCategoryCache SkillCategory { get; } = new SkillCategoryCache();
        public static SpawnObjectCache SpawnObject { get; } = new SpawnObjectCache();


        static DataService()
        {
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            DataQueue = new ConcurrentQueue<DatabaseAction>();

            var ip = Environment.GetEnvironmentVariable("MYSQL_SERVER_IP_ADDRESS") ?? string.Empty;
            var user = Environment.GetEnvironmentVariable("MYSQL_SERVER_USERNAME") ?? string.Empty;
            var password = Environment.GetEnvironmentVariable("MYSQL_SERVER_PASSWORD") ?? string.Empty;
            var database = Environment.GetEnvironmentVariable("MYSQL_SERVER_DATABASE") ?? string.Empty;
            uint.TryParse(Environment.GetEnvironmentVariable("MYSQL_SERVER_PORT"), out var port);

            SWLORConnectionString = new MySqlConnectionStringBuilder()
            {
                Server = ip,
                Port = port,
                Database = database,
                UserID = user,
                Password = password,
                ConnectionTimeout = 60 // 30 seconds
            }.ToString();
        }

        public static void Initialize(bool initializeCache)
        {
            Connection = new MySqlConnection(SWLORConnectionString);
        }

        private static void LoadCache<T>()
            where T : class, IEntity
        {
            var sw = new Stopwatch();
            sw.Start();

            var entities = Connection.GetAll<T>();
            foreach (var entity in entities)
            {
                MessageHub.Instance.Publish(new OnCacheObjectSet<T>(entity));
            }

            sw.Stop();
            Console.WriteLine("Loaded Cache: " + typeof(T).Name + " (" + sw.ElapsedMilliseconds + "ms)");
        }

        private static void SetIntoCache<T>(T entity)
            where T: class, IEntity
        {
            MessageHub.Instance.Publish(new OnCacheObjectSet<T>(entity));
        }

        private static void RemoveFromCache<T>(T entity)
            where T: class, IEntity
        {
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<T>(entity));
        }

        
        /// <summary>
        /// PC Market Listings should only be loaded into the cache if they:
        /// 1.) Haven't been sold already
        /// 2.) Haven't been removed by the seller.
        /// This method will retrieve these specific records and store them into the cache.
        /// Should be called in the InitializeCache() method one time.
        /// </summary>
        private static void LoadPCMarketListingCache()
        {
            const string Sql = "SELECT * FROM PCMarketListing WHERE DateSold IS NULL AND DateRemoved IS NULL";

            var results = Connection.Query<PCMarketListing>(Sql);

            foreach (var result in results)
            {
                MessageHub.Instance.Publish(new OnCacheObjectSet<PCMarketListing>(result));
            }
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data directly from the database immediately afterwards.
        /// However, data in the cache will be up to date as soon as a value is changed.
        /// </summary>
        /// <param name="data">The data to submit for processing</param>
        /// <param name="actionType">The type (Insert, Update, Delete, etc.) of change to make.</param>
        public static void SubmitDataChange<T>(T data, DatabaseActionType actionType)
            where T: class, IEntity
        {
            if(data == null) throw new ArgumentNullException(nameof(data));

            if (actionType == DatabaseActionType.Insert || actionType == DatabaseActionType.Update)
            {
                SetIntoCache(data);
            }
            else if (actionType == DatabaseActionType.Delete)
            {
                RemoveFromCache(data);
            }

            DataQueue.Enqueue(new DatabaseAction(data, actionType));
        }
    }
}
