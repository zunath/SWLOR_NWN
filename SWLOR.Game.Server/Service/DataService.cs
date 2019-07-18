using Dapper;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class DataService
    {
        public static ConcurrentQueue<DatabaseAction> DataQueue { get; }
        public static string MasterConnectionString { get; }
        public static string SWLORConnectionString { get; }
        public static SqlConnection Connection { get; private set; }

        public static ApartmentBuildingCache ApartmentBuilding { get; } = new ApartmentBuildingCache();
        public static AreaCache Area { get; } = new AreaCache();
        public static AreaWalkmeshCache AreaWalkmesh { get; } = new AreaWalkmeshCache();
        public static AssociationCache Association { get; } = new AssociationCache();
        public static AttributeCache Attribute { get; } = new AttributeCache();
        public static AuthorizedDMCache AuthorizedDM { get; } = new AuthorizedDMCache();
        public static BankCache Bank { get; } = new BankCache();
        public static BankItemCache BankItem { get; } = new BankItemCache();
        public static BaseItemTypeCache BaseItemType { get; } = new BaseItemTypeCache();
        public static BaseStructureCache BaseStructure { get; } = new BaseStructureCache();
        public static BaseStructureTypeCache BaseStructureType { get; } = new BaseStructureTypeCache();
        public static BugReportCache BugReport { get; } = new BugReportCache();
        public static BuildingStyleCache BuildingStyle { get; } = new BuildingStyleCache();
        public static BuildingTypeCache BuildingType { get; } = new BuildingTypeCache();
        public static ChatChannelCache ChatChannel { get; } = new ChatChannelCache();
        public static ChatLogCache ChatLog { get; } = new ChatLogCache();
        public static ClientLogEventCache ClientLogEvent { get; } = new ClientLogEventCache();
        public static ClientLogEventTypeCache ClientLogEventType { get; } = new ClientLogEventTypeCache();
        public static ComponentTypeCache ComponentType { get; } = new ComponentTypeCache();
        public static CooldownCategoryCache CooldownCategory { get; } = new CooldownCategoryCache();
        public static CraftBlueprintCache CraftBlueprint { get; } = new CraftBlueprintCache();
        public static CraftBlueprintCategoryCache CraftBlueprintCategory { get; } = new CraftBlueprintCategoryCache();
        public static CraftDeviceCache CraftDevice { get; } = new CraftDeviceCache();
        public static CustomEffectCache CustomEffect { get; } = new CustomEffectCache();
        public static CustomEffectCategoryCache CustomEffectCategory { get; } = new CustomEffectCategoryCache();
        public static DatabaseVersionCache DatabaseVersion { get; } = new DatabaseVersionCache();
        public static DMActionCache DMAction { get; } = new DMActionCache();
        public static DMActionTypeCache DMActionType { get; } = new DMActionTypeCache();
        public static DMRoleCache DMRole { get; } = new DMRoleCache();
        public static DownloadCache Download { get; } = new DownloadCache();
        public static EnmityAdjustmentRuleCache EnmityAdjustmentRule { get; } = new EnmityAdjustmentRuleCache();
        public static ErrorCache Error { get; } = new ErrorCache();
        public static FameRegionCache FameRegion { get; } = new FameRegionCache();
        public static GameTopicCache GameTopic { get; } = new GameTopicCache();
        public static GameTopicCategoryCache GameTopicCategory { get; } = new GameTopicCategoryCache();
        public static GuildCache Guild { get; } = new GuildCache();
        public static GuildTaskCache GuildTask { get; } = new GuildTaskCache();
        public static ItemTypeCache ItemType { get; } = new ItemTypeCache();
        public static JukeboxSongCache JukeboxSong { get; } = new JukeboxSongCache();
        public static KeyItemCache KeyItem { get; } = new KeyItemCache();
        public static KeyItemCategoryCache KeyItemCategory { get; } = new KeyItemCategoryCache();
        public static LootTableCache LootTable { get; } = new LootTableCache();
        public static LootTableItemCache LootTableItem { get; } = new LootTableItemCache();
        public static MarketCategoryCache MarketCategory { get; } = new MarketCategoryCache();
        public static MessageCache Message { get; } = new MessageCache();
        public static NPCGroupCache NPCGroup { get; } = new NPCGroupCache();
        public static PCBaseCache PCBase { get; } = new PCBaseCache();
        public static PCBasePermissionCache PCBasePermission { get; } = new PCBasePermissionCache();
        public static PCBaseStructureCache PCBaseStructure { get; } = new PCBaseStructureCache();
        public static PCBaseStructureItemCache PCBaseStructureItem { get; } = new PCBaseStructureItemCache();
        public static PCBaseStructurePermissionCache PCBaseStructurePermission { get; } = new PCBaseStructurePermissionCache();
        public static PCBaseTypeCache PCBaseType { get; } = new PCBaseTypeCache();
        public static PCCooldownCache PCCooldown { get; } = new PCCooldownCache();
        public static PCCraftedBlueprintCache PCCraftedBlueprint { get; } = new PCCraftedBlueprintCache();
        public static PCCustomEffectCache PCCustomEffect { get; } = new PCCustomEffectCache();
        public static PCGuildPointCache PCGuildPoint { get; } = new PCGuildPointCache();
        public static PCImpoundedItemCache PCImpoundedItem { get; } = new PCImpoundedItemCache();
        public static PCKeyItemCache PCKeyItem { get; } = new PCKeyItemCache();
        public static PCMapPinCache PCMapPin { get; } = new PCMapPinCache();
        public static PCMapProgressionCache PCMapProgression { get; } = new PCMapProgressionCache();
        public static PCMarketListingCache PCMarketListing { get; } = new PCMarketListingCache();
        public static PCObjectVisibilityCache PCObjectVisibility { get; } = new PCObjectVisibilityCache();
        public static PCOutfitCache PCOutfit { get; } = new PCOutfitCache();
        public static PCOverflowItemCache PCOverflowItem { get; } = new PCOverflowItemCache();
        public static PCPerkCache PCPerk { get; } = new PCPerkCache();
        public static PCPerkRefundCache PCPerkRefund { get; } = new PCPerkRefundCache();
        public static PCQuestItemProgressCache PCQuestItemProgress { get; } = new PCQuestItemProgressCache();
        public static PCQuestKillTargetProgressCache PCQuestKillTargetProgress { get; } = new PCQuestKillTargetProgressCache();
        public static PCQuestStatusCache PCQuestStatus { get; } = new PCQuestStatusCache();
        public static PCRegionalFameCache PCRegionalFame { get; } = new PCRegionalFameCache();
        public static PCSkillCache PCSkill { get; } = new PCSkillCache();
        public static PCSkillPoolCache PCSkillPool { get; } = new PCSkillPoolCache();
        public static PerkCache Perk { get; } = new PerkCache();
        public static PerkCategoryCache PerkCategory { get; } = new PerkCategoryCache();
        public static PerkFeatCache PerkFeat { get; } = new PerkFeatCache();
        public static PerkLevelCache PerkLevel { get; } = new PerkLevelCache();
        public static PerkLevelQuestRequirementCache PerkLevelQuestRequirement { get; } = new PerkLevelQuestRequirementCache();
        public static PerkLevelSkillRequirementCache PerkLevelSkillRequirement { get; } = new PerkLevelSkillRequirementCache();
        public static PlayerCache Player { get; } = new PlayerCache();
        public static QuestCache Quest { get; } = new QuestCache();
        public static QuestKillTargetCache QuestKillTarget { get; } = new QuestKillTargetCache();
        public static QuestPrerequisiteCache QuestPrerequisite { get; } = new QuestPrerequisiteCache();
        public static QuestRequiredItemCache QuestRequiredItem { get; } = new QuestRequiredItemCache();
        public static QuestRequiredKeyItemCache QuestRequiredKeyItem { get; } = new QuestRequiredKeyItemCache();
        public static QuestRewardItemCache QuestRewardItem { get; } = new QuestRewardItemCache();
        public static QuestStateCache QuestState { get; } = new QuestStateCache();
        public static QuestTypeCache QuestType { get; } = new QuestTypeCache();
        public static ServerConfigurationCache ServerConfiguration { get; } = new ServerConfigurationCache();
        public static SkillCache Skill { get; } = new SkillCache();
        public static SkillCategoryCache SkillCategory { get; } = new SkillCategoryCache();
        public static SpaceEncounterCache SpaceEncounter { get; } = new SpaceEncounterCache();
        public static SpaceStarportCache SpaceStarport { get; } = new SpaceStarportCache();
        public static SpawnCache Spawn { get; } = new SpawnCache();
        public static SpawnObjectCache SpawnObject { get; } = new SpawnObjectCache();
        public static SpawnObjectTypeCache SpawnObjectType { get; } = new SpawnObjectTypeCache();
        public static StructureModeCache StructureMode { get; } = new StructureModeCache();


        static DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();

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

        private static void LoadCache<T>()
            where T: class, IEntity
        {
            var sw = new Stopwatch();
            sw.Start();

            var entities = Connection.GetAll<T>();
            foreach(var entity in entities)
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
        /// Retrieves all objects in frequently accessed data from the database and stores them into the cache.
        /// This should only be called one time at initial load.
        /// </summary>
        private static void InitializeCache()
        {
            Console.WriteLine("Initializing the cache...");
            LoadCache<Area>();
            LoadCache<AreaWalkmesh>();

            LoadCache<ApartmentBuilding>();
            LoadCache<Association>();
            LoadCache<Data.Entity.Attribute>();
            LoadCache<AuthorizedDM>();
            LoadCache<Bank>();
            LoadCache<BankItem>();
            LoadCache<BaseItemType>();
            LoadCache<BaseStructure>();
            LoadCache<Data.Entity.BaseStructureType>();
            LoadCache<BuildingStyle>();
            LoadCache<Data.Entity.BuildingType>();
            LoadCache<ChatChannel>();
            LoadCache<ModuleEventType>();
            LoadCache<Data.Entity.ComponentType>();
            LoadCache<CooldownCategory>();
            LoadCache<CraftBlueprint>();
            LoadCache<CraftBlueprintCategory>();
            LoadCache<CraftDevice>();
            LoadCache<Data.Entity.CustomEffect>();
            LoadCache<CustomEffectCategory>();
            LoadCache<DMRole>();
            LoadCache<EnmityAdjustmentRule>();
            LoadCache<FameRegion>();
            LoadCache<Guild>();
            LoadCache<GuildTask>();
            LoadCache<ItemType>();
            LoadCache<JukeboxSong>();
            LoadCache<KeyItem>();
            LoadCache<KeyItemCategory>();
            LoadCache<LootTable>();
            LoadCache<LootTableItem>();
            LoadCache<MarketCategory>();
            LoadCache<Message>();
            LoadCache<NPCGroup>();
            LoadCache<PCBase>();
            LoadCache<PCBasePermission>();
            LoadCache<PCBaseStructure>();
            LoadCache<PCBaseStructureItem>();
            LoadCache<PCBaseStructurePermission>();
            LoadCache<Data.Entity.PCBaseType>();
            LoadPCMarketListingCache();
            LoadCache<SpaceStarport>();
            LoadCache<SpaceEncounter>();

            
            LoadCache<PCCooldown>();
            LoadCache<PCCraftedBlueprint>();
            LoadCache<PCCustomEffect>();

            LoadPCImpoundedItemsCache();
            LoadCache<PCGuildPoint>();
            LoadCache<PCKeyItem>();
            LoadCache<PCMapPin>();
            LoadCache<PCMapProgression>();
            LoadCache<PCObjectVisibility>();
            LoadCache<PCOutfit>();
            LoadCache<PCOverflowItem>();
            LoadCache<PCPerk>();
            LoadCache<PCQuestItemProgress>();
            LoadCache<PCQuestKillTargetProgress>();
            LoadCache<PCQuestStatus>();
            LoadCache<PCRegionalFame>();
            LoadCache<PCSkill>();
            LoadCache<PCSkillPool>();
            LoadCache<PCPerkRefund>();

            LoadCache<Data.Entity.Perk>();
            LoadCache<PerkFeat>();
            LoadCache<PerkCategory>();
            LoadCache<PerkLevel>();
            LoadCache<PerkLevelQuestRequirement>();
            LoadCache<PerkLevelSkillRequirement>();
            LoadCache<Player>(); 
            LoadCache<Quest>();
            LoadCache<QuestKillTarget>();
            LoadCache<QuestPrerequisite>();
            LoadCache<QuestRequiredItem>();
            LoadCache<QuestRequiredKeyItem>();
            LoadCache<QuestRewardItem>();
            LoadCache<QuestState>();
            LoadCache<Data.Entity.QuestType>();
            LoadCache<ServerConfiguration>();
            LoadCache<Skill>();
            LoadCache<SkillCategory>();
            LoadCache<Spawn>();
            LoadCache<SpawnObject>();
            LoadCache<SpawnObjectType>();
            LoadCache<StructureMode>();
            Console.WriteLine("Cache initialized!");
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
            const string Sql = "SELECT * FROM dbo.PCMarketListing WHERE DateSold IS NULL AND DateRemoved IS NULL";

            var results = Connection.Query<PCMarketListing>(Sql);
            
            foreach (var result in results)
            {
                MessageHub.Instance.Publish(new OnCacheObjectSet<PCMarketListing>(result));
            }
        }

        /// <summary>
        /// PC Impounded Items should only be loaded into the cache if they:
        /// 1.) Haven't been retrieved.
        /// 2.) Haven't expired (30 days after the date they were impounded.)
        /// Should be called in the InitializeCache() method one time.
        /// </summary>
        private static void LoadPCImpoundedItemsCache()
        {
            const string Sql = "SELECT * FROM dbo.PCImpoundedItem WHERE DateRetrieved IS NULL AND GETUTCDATE() < DATEADD(DAY, 30, CAST(DateImpounded AS DATE))";

            var results = Connection.Query<PCImpoundedItem>(Sql);
            foreach (var result in results)
            {
                MessageHub.Instance.Publish(new OnCacheObjectSet<PCImpoundedItem>(result));
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
