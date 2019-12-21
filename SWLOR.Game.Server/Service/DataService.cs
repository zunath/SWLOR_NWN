using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Diagnostics;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class DataService
    {
        public static ApartmentBuildingCache ApartmentBuilding { get; } = new ApartmentBuildingCache();
        public static AreaCache Area { get; } = new AreaCache();
        public static AssociationCache Association { get; } = new AssociationCache();
        public static AttributeCache Attribute { get; } = new AttributeCache();
        public static AuthorizedDMCache AuthorizedDM { get; } = new AuthorizedDMCache();
        public static BankCache Bank { get; } = new BankCache();
        public static BankItemCache BankItem { get; } = new BankItemCache();
        public static BaseItemTypeCache BaseItemType { get; } = new BaseItemTypeCache();
        public static BaseStructureCache BaseStructure { get; } = new BaseStructureCache();
        public static BaseStructureTypeCache BaseStructureType { get; } = new BaseStructureTypeCache();
        public static BuildingStyleCache BuildingStyle { get; } = new BuildingStyleCache();
        public static BuildingTypeCache BuildingType { get; } = new BuildingTypeCache();
        public static ChatChannelCache ChatChannel { get; } = new ChatChannelCache();
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
        public static PCHelmetCache PCHelmet { get; } = new PCHelmetCache();
        public static PCWeaponCache PCWeapon { get; } = new PCWeaponCache();
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
        public static ServerConfigurationCache ServerConfiguration { get; } = new ServerConfigurationCache();
        public static SkillCache Skill { get; } = new SkillCache();
        public static SkillCategoryCache SkillCategory { get; } = new SkillCategoryCache();
        public static SpaceEncounterCache SpaceEncounter { get; } = new SpaceEncounterCache();
        public static StarportCache Starport { get; } = new StarportCache();
        public static SpawnCache Spawn { get; } = new SpawnCache();
        public static SpawnObjectCache SpawnObject { get; } = new SpawnObjectCache();
        public static SpawnObjectTypeCache SpawnObjectType { get; } = new SpawnObjectTypeCache();
        public static StructureModeCache StructureMode { get; } = new StructureModeCache();

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
    }
}
