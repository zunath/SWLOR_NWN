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
        public static BankItemCache BankItem { get; } = new BankItemCache();
        public static GuildTaskCache GuildTask { get; } = new GuildTaskCache();
        public static MessageCache Message { get; } = new MessageCache();
        public static PCBaseCache PCBase { get; } = new PCBaseCache();
        public static PCBaseStructureCache PCBaseStructure { get; } = new PCBaseStructureCache();
        public static PCCustomEffectCache PCCustomEffect { get; } = new PCCustomEffectCache();
        public static PCImpoundedItemCache PCImpoundedItem { get; } = new PCImpoundedItemCache();
        public static PCMarketListingCache PCMarketListing { get; } = new PCMarketListingCache();
        public static PCQuestStatusCache PCQuestStatus { get; } = new PCQuestStatusCache();
        public static PlayerCache Player { get; } = new PlayerCache();
        public static ServerConfigurationCache ServerConfiguration { get; } = new ServerConfigurationCache();

        private static void SetIntoCache<T>(T entity)
            where T: class, IEntity
        {
        }

        public static void Set<T>(T data)
            where T : class, IEntity
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            MessageHub.Instance.Publish(new OnCacheObjectSet<T>(data));
        }

        public static void Delete<T>(T data)
            where T: class, IEntity
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            MessageHub.Instance.Publish(new OnCacheObjectDeleted<T>(data));
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
