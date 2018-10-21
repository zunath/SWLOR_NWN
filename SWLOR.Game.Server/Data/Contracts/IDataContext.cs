using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataContext
    {
        DbSet<ApartmentBuilding> ApartmentBuildings { get; set; }
        DbSet<Area> Areas { get; set; }
        DbSet<AreaWalkmesh> AreaWalkmeshes { get; set; }
        DbSet<Association> Associations { get; set; }
        DbSet<BaseStructure> BaseStructures { get; set; }
        DbSet<BaseStructureType> BaseStructureTypes { get; set; }
        DbSet<BugReport> BugReports { get; set; }
        DbSet<PCBasePermission> PCBasePermissions { get; set; }
        DbSet<PCBaseStructurePermission> PCBaseStructurePermissions { get; set; }
        DbSet<BuildingStyle> BuildingStyles { get; set; }
        DbSet<BuildingType> BuildingTypes { get; set; }
        DbSet<Attribute> Attributes { get; set; }
        DbSet<AuthorizedDM> AuthorizedDMs { get; set; }
        DbSet<BaseItemType> BaseItemTypes { get; set; }
        DbSet<ChatChannelsDomain> ChatChannelsDomains { get; set; }
        DbSet<ChatLog> ChatLogs { get; set; }
        DbSet<ClientLogEvent> ClientLogEvents { get; set; }
        DbSet<ClientLogEventTypesDomain> ClientLogEventTypesDomains { get; set; }
        DbSet<ComponentType> ComponentTypes { get; set; }
        DbSet<CooldownCategory> CooldownCategories { get; set; }
        DbSet<CraftBlueprintCategory> CraftBlueprintCategories { get; set; }
        DbSet<CraftBlueprint> CraftBlueprints { get; set; }
        DbSet<CraftDevice> CraftDevices { get; set; }
        DbSet<CustomEffect> CustomEffects { get; set; }
        DbSet<DiscordChatQueue> DiscordChatQueues { get; set; }
        DbSet<DMRoleDomain> DMRoleDomains { get; set; }
        DbSet<Download> Downloads { get; set; }
        DbSet<EnmityAdjustmentRule> EnmityAdjustmentRules { get; set; }
        DbSet<FameRegion> FameRegions { get; set; }
        DbSet<GameTopicCategory> GameTopicCategories { get; set; }
        DbSet<GameTopic> GameTopics { get; set; }
        DbSet<GrowingPlant> GrowingPlants { get; set; }
        DbSet<ItemType> ItemTypes { get; set; }
        DbSet<KeyItemCategory> KeyItemCategories { get; set; }
        DbSet<KeyItem> KeyItems { get; set; }
        DbSet<LootTableItem> LootTableItems { get; set; }
        DbSet<LootTable> LootTables { get; set; }
        DbSet<NPCGroup> NPCGroups { get; set; }
        DbSet<PCBase> PCBases { get; set; }
        DbSet<PCBaseStructure> PCBaseStructures { get; set; }
        DbSet<PCBaseStructureItem> PCBaseStructureItems { get; set; }
        DbSet<PCCooldown> PCCooldowns { get; set; }
        DbSet<PCCraftedBlueprint> PCCraftedBlueprints { get; set; }
        DbSet<PCCustomEffect> PCCustomEffects { get; set; }
        DbSet<PCImpoundedItem> PCImpoundedItems { get; set; }
        DbSet<PCKeyItem> PCKeyItems { get; set; }
        DbSet<PCMapPin> PCMapPins { get; set; }
        DbSet<PCMapProgression> PCMapProgressions { get; set; }
        DbSet<PCMigrationItem> PCMigrationItems { get; set; }
        DbSet<PCMigration> PCMigrations { get; set; }
        DbSet<PCOutfit> PCOutfits { get; set; }
        DbSet<PCObjectVisibility> PCObjectVisibilities { get; set; }
        DbSet<PCOverflowItem> PCOverflowItems { get; set; }
        DbSet<PCPerk> PCPerks { get; set; }
        DbSet<PCPerkRefund> PCPerkRefunds { get; set; }
        DbSet<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }
        DbSet<PCQuestItemProgress> PCQuestItemProgresses { get; set; }
        DbSet<PCQuestStatus> PCQuestStatus { get; set; }
        DbSet<PCRegionalFame> PCRegionalFames { get; set; }
        DbSet<PCSearchSiteItem> PCSearchSiteItems { get; set; }
        DbSet<PCSearchSite> PCSearchSites { get; set; }
        DbSet<PCSkill> PCSkills { get; set; }
        DbSet<PerkCategory> PerkCategories { get; set; }
        DbSet<PerkExecutionType> PerkExecutionTypes { get; set; }
        DbSet<PerkLevel> PerkLevels { get; set; }
        DbSet<PerkLevelQuestRequirement> PerkLevelQuestRequirements { get; set; }
        DbSet<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }
        DbSet<Perk> Perks { get; set; }
        DbSet<Plant> Plants { get; set; }
        DbSet<PlayerCharacter> PlayerCharacters { get; set; }
        DbSet<QuestKillTargetList> QuestKillTargetLists { get; set; }
        DbSet<QuestPrerequisite> QuestPrerequisites { get; set; }
        DbSet<QuestRequiredItemList> QuestRequiredItemLists { get; set; }
        DbSet<QuestRequiredKeyItemList> QuestRequiredKeyItemLists { get; set; }
        DbSet<QuestRewardItem> QuestRewardItems { get; set; }
        DbSet<Quest> Quests { get; set; }
        DbSet<QuestState> QuestStates { get; set; }
        DbSet<QuestTypeDomain> QuestTypeDomains { get; set; }
        DbSet<Mod> Mods { get; set; }
        DbSet<ServerConfiguration> ServerConfigurations { get; set; }
        DbSet<SkillCategory> SkillCategories { get; set; }
        DbSet<Skill> Skills { get; set; }
        DbSet<SkillXPRequirement> SkillXPRequirements { get; set; }
        DbSet<Bank> Banks { get; set; }
        DbSet<BankItem> BankItems { get; set; }
        DbSet<SpawnObject> SpawnObjects { get; set; }
        DbSet<SpawnObjectType> SpawnObjectTypes { get; set; }
        DbSet<Spawn> Spawns { get; set; }
        DbSet<User> Users { get; set; }

        DbContextConfiguration Configuration { get; }
        Database Database { get; }
        void Dispose();
        int SaveChanges();
        void StoredProcedure(string procedureName, params SqlParameter[] args);
        List<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args);
        T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args);
    }
}