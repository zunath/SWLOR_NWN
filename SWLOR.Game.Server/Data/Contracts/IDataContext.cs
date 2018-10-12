using SWLOR.Game.Server.Data.Entities;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Attribute = SWLOR.Game.Server.Data.Entities.Attribute;

namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataContext
    {
        IDbSet<ApartmentBuilding> ApartmentBuildings { get; set; }
        IDbSet<Area> Areas { get; set; }
        IDbSet<AreaWalkmesh> AreaWalkmeshes { get; set; }
        IDbSet<Association> Associations { get; set; }
        IDbSet<BaseStructure> BaseStructures { get; set; }
        IDbSet<BaseStructureType> BaseStructureTypes { get; set; }
        IDbSet<PCBasePermission> PCBasePermissions { get; set; }
        IDbSet<PCBaseStructurePermission> PCBaseStructurePermissions { get; set; }
        IDbSet<BuildingStyle> BuildingStyles { get; set; }
        IDbSet<BuildingType> BuildingTypes { get; set; }
        IDbSet<Attribute> Attributes { get; set; }
        IDbSet<AuthorizedDM> AuthorizedDMs { get; set; }
        IDbSet<BaseItemType> BaseItemTypes { get; set; }
        IDbSet<ChatChannelsDomain> ChatChannelsDomains { get; set; }
        IDbSet<ChatLog> ChatLogs { get; set; }
        IDbSet<ClientLogEvent> ClientLogEvents { get; set; }
        IDbSet<ClientLogEventTypesDomain> ClientLogEventTypesDomains { get; set; }
        IDbSet<ComponentType> ComponentTypes { get; set; }
        IDbSet<CooldownCategory> CooldownCategories { get; set; }
        IDbSet<CraftBlueprintCategory> CraftBlueprintCategories { get; set; }
        IDbSet<CraftBlueprint> CraftBlueprints { get; set; }
        IDbSet<CraftDevice> CraftDevices { get; set; }
        IDbSet<Entities.CustomEffect> CustomEffects { get; set; }
        IDbSet<DiscordChatQueue> DiscordChatQueues { get; set; }
        IDbSet<DMRoleDomain> DMRoleDomains { get; set; }
        IDbSet<Download> Downloads { get; set; }
        IDbSet<EnmityAdjustmentRule> EnmityAdjustmentRules { get; set; }
        IDbSet<FameRegion> FameRegions { get; set; }
        IDbSet<GameTopicCategory> GameTopicCategories { get; set; }
        IDbSet<GameTopic> GameTopics { get; set; }
        IDbSet<GrowingPlant> GrowingPlants { get; set; }
        IDbSet<ItemType> ItemTypes { get; set; }
        IDbSet<KeyItemCategory> KeyItemCategories { get; set; }
        IDbSet<KeyItem> KeyItems { get; set; }
        IDbSet<LootTableItem> LootTableItems { get; set; }
        IDbSet<LootTable> LootTables { get; set; }
        IDbSet<NPCGroup> NPCGroups { get; set; }
        IDbSet<PCBase> PCBases { get; set; }
        IDbSet<PCBaseStructure> PCBaseStructures { get; set; }
        IDbSet<PCBaseStructureItem> PCBaseStructureItems { get; set; }
        IDbSet<PCCooldown> PCCooldowns { get; set; }
        IDbSet<PCCustomEffect> PCCustomEffects { get; set; }
        IDbSet<PCImpoundedItem> PCImpoundedItems { get; set; }
        IDbSet<PCKeyItem> PCKeyItems { get; set; }
        IDbSet<PCMapPin> PCMapPins { get; set; }
        IDbSet<PCMapProgression> PCMapProgressions { get; set; }
        IDbSet<PCMigrationItem> PCMigrationItems { get; set; }
        IDbSet<PCMigration> PCMigrations { get; set; }
        IDbSet<PCOutfit> PCOutfits { get; set; }
        IDbSet<PCObjectVisibility> PCObjectVisibilities { get; set; }
        IDbSet<PCOverflowItem> PCOverflowItems { get; set; }
        IDbSet<PCPerk> PCPerks { get; set; }
        IDbSet<PCPerkRefund> PCPerkRefunds { get; set; }
        IDbSet<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }
        IDbSet<PCQuestItemProgress> PCQuestItemProgresses { get; set; }
        IDbSet<PCQuestStatus> PCQuestStatus { get; set; }
        IDbSet<PCRegionalFame> PCRegionalFames { get; set; }
        IDbSet<PCSearchSiteItem> PCSearchSiteItems { get; set; }
        IDbSet<PCSearchSite> PCSearchSites { get; set; }
        IDbSet<PCSkill> PCSkills { get; set; }
        IDbSet<PerkCategory> PerkCategories { get; set; }
        IDbSet<PerkExecutionType> PerkExecutionTypes { get; set; }
        IDbSet<PerkLevel> PerkLevels { get; set; }
        IDbSet<PerkLevelQuestRequirement> PerkLevelQuestRequirements { get; set; }
        IDbSet<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }
        IDbSet<Entities.Perk> Perks { get; set; }
        IDbSet<Plant> Plants { get; set; }
        IDbSet<PlayerCharacter> PlayerCharacters { get; set; }
        IDbSet<QuestKillTargetList> QuestKillTargetLists { get; set; }
        IDbSet<QuestPrerequisite> QuestPrerequisites { get; set; }
        IDbSet<QuestRequiredItemList> QuestRequiredItemLists { get; set; }
        IDbSet<QuestRequiredKeyItemList> QuestRequiredKeyItemLists { get; set; }
        IDbSet<QuestRewardItem> QuestRewardItems { get; set; }
        IDbSet<Quest> Quests { get; set; }
        IDbSet<QuestState> QuestStates { get; set; }
        IDbSet<QuestTypeDomain> QuestTypeDomains { get; set; }
        IDbSet<Entities.Mod> Mods { get; set; }
        IDbSet<ServerConfiguration> ServerConfigurations { get; set; }
        IDbSet<SkillCategory> SkillCategories { get; set; }
        IDbSet<Skill> Skills { get; set; }
        IDbSet<SkillXPRequirement> SkillXPRequirements { get; set; }
        IDbSet<Bank> Banks { get; set; }
        IDbSet<BankItem> BankItems { get; set; }
        IDbSet<SpawnObject> SpawnObjects { get; set; }
        IDbSet<SpawnObjectType> SpawnObjectTypes { get; set; }
        IDbSet<Spawn> Spawns { get; set; }
        IDbSet<User> Users { get; set; }

        DbContextConfiguration Configuration { get; }
        Database Database { get; }
        void Dispose();
        int SaveChanges();
        void StoredProcedure(string procedureName, params SqlParameter[] args);
        List<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args);
        T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args);
    }
}