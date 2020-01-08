using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class DataService
    {
        static DataService()
        {
            Area = new AreaCache();
        }

        public static AreaCache Area { get; }
        public static AuthorizedDMCache AuthorizedDM { get; } = new AuthorizedDMCache();
        public static BankCache Bank { get; } = new BankCache();
        public static BankItemCache BankItem { get; } = new BankItemCache();
        public static DownloadCache Download { get; } = new DownloadCache();
        public static GameTopicCache GameTopic { get; } = new GameTopicCache();
        public static GuildTaskCache GuildTask { get; } = new GuildTaskCache();
        public static MessageCache Message { get; } = new MessageCache();
        public static PCBaseCache PCBase { get; } = new PCBaseCache();
        public static PCBaseStructureCache PCBaseStructure { get; } = new PCBaseStructureCache();
        public static PCBaseStructureItemCache PCBaseStructureItem { get; } = new PCBaseStructureItemCache();
        public static PCCustomEffectCache PCCustomEffect { get; } = new PCCustomEffectCache();
        public static PCImpoundedItemCache PCImpoundedItem { get; } = new PCImpoundedItemCache();
        public static PCMarketListingCache PCMarketListing { get; } = new PCMarketListingCache();
        public static PCObjectVisibilityCache PCObjectVisibility { get; } = new PCObjectVisibilityCache();
        public static PCOverflowItemCache PCOverflowItem { get; } = new PCOverflowItemCache();
        public static PCPerkRefundCache PCPerkRefund { get; } = new PCPerkRefundCache();
        public static PCQuestItemProgressCache PCQuestItemProgress { get; } = new PCQuestItemProgressCache();
        public static PCQuestKillTargetProgressCache PCQuestKillTargetProgress { get; } = new PCQuestKillTargetProgressCache();
        public static PCQuestStatusCache PCQuestStatus { get; } = new PCQuestStatusCache();
        public static PlayerCache Player { get; } = new PlayerCache();
        public static ServerConfigurationCache ServerConfiguration { get; } = new ServerConfigurationCache();

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
        }

        public static void RunMigration()
        {
            Console.WriteLine("Starting DB migration...");
            List<IDataMigration> migrationScripts = 
                Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => typeof(IDataMigration).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract)
                .Select(Activator.CreateInstance)
                .OrderBy(o => ((IDataMigration)o).Version)
                .Cast<IDataMigration>()
                .ToList();

            const string ConfigKey = "ServerConfiguration:1";

            var serverConfig =
                NWNXRedis.Exists(ConfigKey)
                    ? JsonConvert.DeserializeObject<ServerConfiguration>(NWNXRedis.Get(ConfigKey))
                    : new ServerConfiguration()
                    {
                        ID = 1,
                        ServerName = "Star Wars: Legends of the Old Republic",
                        MessageOfTheDay = "Welcome to SWLOR!"
                    };

            foreach (var migrationScript in migrationScripts)
            {
                if (serverConfig.DataVersion < migrationScript.Version)
                {
                    Console.WriteLine($"Applying DB version #{migrationScript.Version}...");
                    migrationScript.Up();
                    serverConfig.DataVersion = migrationScript.Version;
                    Console.WriteLine($"DB version #{migrationScript.Version} applied successfully!");
                }
            }

            NWNXRedis.Set(ConfigKey, JsonConvert.SerializeObject(serverConfig));
        }
    }
}
