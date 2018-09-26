using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using Attribute = SWLOR.Game.Server.Data.Entities.Attribute;

namespace SWLOR.Game.Server.Data
{
    public partial class DataContext : DbContext, IDataContext
    {
        public DataContext()
            : base(BuildConnectionString())
        {
        }

        private static string BuildConnectionString()
        {
            var ipAddress = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS");
            var username = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME");
            var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
            var database = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE");

            return $"server={ipAddress};database={database};user id={username};password={password};Integrated Security=False;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False";
        }

        public virtual IDbSet<Area> Areas { get; set; }
        public virtual IDbSet<AreaWalkmesh> AreaWalkmeshes { get; set; }
        public virtual IDbSet<BaseStructure> BaseStructures { get; set; }
        public virtual IDbSet<BaseStructureType> BaseStructureTypes { get; set; }
        public virtual IDbSet<Attribute> Attributes { get; set; }
        public virtual IDbSet<AuthorizedDM> AuthorizedDMs { get; set; }
        public virtual IDbSet<BaseItemType> BaseItemTypes { get; set; }
        public virtual IDbSet<Bank> Banks { get; set; }
        public virtual IDbSet<BankItem> BankItems { get; set; }
        public virtual IDbSet<BuildingStyle> BuildingStyles { get; set; }
        public virtual IDbSet<ChatChannelsDomain> ChatChannelsDomains { get; set; }
        public virtual IDbSet<ChatLog> ChatLogs { get; set; }
        public virtual IDbSet<ClientLogEvent> ClientLogEvents { get; set; }
        public virtual IDbSet<ClientLogEventTypesDomain> ClientLogEventTypesDomains { get; set; }
        public virtual IDbSet<ComponentType> ComponentTypes { get; set; }
        public virtual IDbSet<CooldownCategory> CooldownCategories { get; set; }
        public virtual IDbSet<CraftBlueprintCategory> CraftBlueprintCategories { get; set; }
        public virtual IDbSet<CraftBlueprint> CraftBlueprints { get; set; }
        public virtual IDbSet<CraftDevice> CraftDevices { get; set; }
        public virtual IDbSet<Entities.CustomEffect> CustomEffects { get; set; }
        public virtual IDbSet<DMRoleDomain> DMRoleDomains { get; set; }
        public virtual IDbSet<Download> Downloads { get; set; }
        public virtual IDbSet<EnmityAdjustmentRule> EnmityAdjustmentRules { get; set; }
        public virtual IDbSet<FameRegion> FameRegions { get; set; }
        public virtual IDbSet<GameTopicCategory> GameTopicCategories { get; set; }
        public virtual IDbSet<GameTopic> GameTopics { get; set; }
        public virtual IDbSet<GrowingPlant> GrowingPlants { get; set; }
        public virtual IDbSet<ItemType> ItemTypes { get; set; }
        public virtual IDbSet<KeyItemCategory> KeyItemCategories { get; set; }
        public virtual IDbSet<KeyItem> KeyItems { get; set; }
        public virtual IDbSet<LootTableItem> LootTableItems { get; set; }
        public virtual IDbSet<LootTable> LootTables { get; set; }
        public virtual IDbSet<NPCGroup> NPCGroups { get; set; }
        public virtual IDbSet<PCBase> PCBases { get; set; }
        public virtual IDbSet<PCBaseStructure> PCBaseStructures { get; set; }
        public virtual IDbSet<PCBaseStructureItem> PCBaseStructureItems { get; set; }
        public virtual IDbSet<PCBasePermission> PCBasePermissions { get; set; }
        public virtual IDbSet<PCBaseStructurePermission> PCBaseStructurePermissions { get; set; }
        public virtual IDbSet<PCCooldown> PCCooldowns { get; set; }
        public virtual IDbSet<PCCustomEffect> PCCustomEffects { get; set; }
        public virtual IDbSet<PCImpoundedItem> PCImpoundedItems { get; set; }
        public virtual IDbSet<PCKeyItem> PCKeyItems { get; set; }
        public virtual IDbSet<PCMapPin> PCMapPins { get; set; }
        public virtual IDbSet<PCMigrationItem> PCMigrationItems { get; set; }
        public virtual IDbSet<PCMigration> PCMigrations { get; set; }
        public virtual IDbSet<PCObjectVisibility> PCObjectVisibilities { get; set; }
        public virtual IDbSet<PCOutfit> PCOutfits { get; set; }
        public virtual IDbSet<PCOverflowItem> PCOverflowItems { get; set; }
        public virtual IDbSet<PCPerk> PCPerks { get; set; }
        public virtual IDbSet<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }
        public virtual IDbSet<PCQuestItemProgress> PCQuestItemProgresses { get; set; }
        public virtual IDbSet<PCQuestStatus> PCQuestStatus { get; set; }
        public virtual IDbSet<PCRegionalFame> PCRegionalFames { get; set; }
        public virtual IDbSet<PCSearchSiteItem> PCSearchSiteItems { get; set; }
        public virtual IDbSet<PCSearchSite> PCSearchSites { get; set; }
        public virtual IDbSet<PCSkill> PCSkills { get; set; }
        public virtual IDbSet<PerkCategory> PerkCategories { get; set; }
        public virtual IDbSet<PerkExecutionType> PerkExecutionTypes { get; set; }
        public virtual IDbSet<PerkLevel> PerkLevels { get; set; }
        public virtual IDbSet<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }
        public virtual IDbSet<Entities.Perk> Perks { get; set; }
        public virtual IDbSet<PCPerkRefund> PCPerkRefunds { get; set; }
        public virtual IDbSet<Plant> Plants { get; set; }
        public virtual IDbSet<PlayerCharacter> PlayerCharacters { get; set; }
        public virtual IDbSet<QuestKillTargetList> QuestKillTargetLists { get; set; }
        public virtual IDbSet<QuestPrerequisite> QuestPrerequisites { get; set; }
        public virtual IDbSet<QuestRequiredItemList> QuestRequiredItemLists { get; set; }
        public virtual IDbSet<QuestRequiredKeyItemList> QuestRequiredKeyItemLists { get; set; }
        public virtual IDbSet<QuestRewardItem> QuestRewardItems { get; set; }
        public virtual IDbSet<Quest> Quests { get; set; }
        public virtual IDbSet<QuestState> QuestStates { get; set; }
        public virtual IDbSet<QuestTypeDomain> QuestTypeDomains { get; set; }
        public virtual IDbSet<Entities.Mod> Mods { get; set; }
        public virtual IDbSet<ServerConfiguration> ServerConfigurations { get; set; }
        public virtual IDbSet<SkillCategory> SkillCategories { get; set; }
        public virtual IDbSet<Skill> Skills { get; set; }
        public virtual IDbSet<SkillXPRequirement> SkillXPRequirements { get; set; }
        public virtual IDbSet<SpawnObject> SpawnObjects { get; set; }
        public virtual IDbSet<SpawnObjectType> SpawnObjectTypes { get; set; }
        public virtual IDbSet<Spawn> Spawns { get; set; }
        public virtual IDbSet<User> Users { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Area>()
                .HasMany(e => e.AreaWalkmeshes)
                .WithRequired(e => e.Area)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Attribute>()
                .HasMany(e => e.Skills)
                .WithRequired(e => e.Attribute)
                .HasForeignKey(e => e.Primary)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Attribute>()
                .HasMany(e => e.Skills1)
                .WithRequired(e => e.Attribute1)
                .HasForeignKey(e => e.Secondary)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Attribute>()
                .HasMany(e => e.Skills2)
                .WithRequired(e => e.Attribute2)
                .HasForeignKey(e => e.Tertiary)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<BaseItemType>()
                .HasMany(e => e.PCMigrationItems)
                .WithRequired(e => e.BaseItemType)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaseStructure>()
                .HasMany(e => e.BuildingStyles)
                .WithRequired(e => e.BaseStructure)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaseStructure>()
                .HasMany(e => e.PCBaseStructures)
                .WithRequired(e => e.BaseStructure)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaseStructureType>()
                .HasMany(e => e.BaseStructures)
                .WithRequired(e => e.BaseStructureType)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BuildingStyle>()
                .HasMany(e => e.PCBaseExteriorStructures)
                .WithOptional(e => e.ExteriorStyle)
                .HasForeignKey(e => e.ExteriorStyleID);

            modelBuilder.Entity<BuildingStyle>()
                .HasMany(e => e.PCBaseInteriorStructures)
                .WithOptional(e => e.InteriorStyle)
                .HasForeignKey(e => e.InteriorStyleID);

            modelBuilder.Entity<ChatChannelsDomain>()
                .HasMany(e => e.ChatLogs)
                .WithRequired(e => e.ChatChannelsDomain)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ClientLogEventTypesDomain>()
                .HasMany(e => e.ClientLogEvents)
                .WithRequired(e => e.ClientLogEventTypesDomain)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<CooldownCategory>()
                .HasMany(e => e.PCCooldowns)
                .WithRequired(e => e.CooldownCategory)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CraftBlueprintCategory>()
                .HasMany(e => e.CraftBlueprints)
                .WithRequired(e => e.CraftBlueprintCategory)
                .HasForeignKey(e => e.CraftCategoryID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ComponentType>()
                .HasMany(e => e.MainCraftBlueprints)
                .WithRequired(e => e.MainComponentType)
                .HasForeignKey(e => e.MainComponentTypeID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ComponentType>()
                .HasMany(e => e.SecondaryCraftBlueprints)
                .WithRequired(e => e.SecondaryComponentType)
                .HasForeignKey(e => e.SecondaryComponentTypeID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ComponentType>()
                .HasMany(e => e.TertiaryCraftBlueprints)
                .WithRequired(e => e.TertiaryComponentType)
                .HasForeignKey(e => e.TertiaryComponentTypeID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CraftDevice>()
                .HasMany(e => e.CraftBlueprints)
                .WithRequired(e => e.CraftDevice)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Entities.CustomEffect>()
                .HasMany(e => e.PCCustomEffects)
                .WithRequired(e => e.CustomEffect)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DMRoleDomain>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.DMRoleDomain)
                .HasForeignKey(e => e.RoleID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<EnmityAdjustmentRule>()
                .HasMany(e => e.Perks)
                .WithRequired(e => e.EnmityAdjustmentRule)
                .HasForeignKey(e => e.EnmityAdjustmentRuleID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FameRegion>()
                .HasMany(e => e.PCRegionalFames)
                .WithRequired(e => e.FameRegion)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FameRegion>()
                .HasMany(e => e.Quests)
                .WithRequired(e => e.FameRegion)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<GameTopicCategory>()
                .HasMany(e => e.GameTopics)
                .WithRequired(e => e.GameTopicCategory)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<KeyItemCategory>()
                .HasMany(e => e.KeyItems)
                .WithRequired(e => e.KeyItemCategory)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<KeyItem>()
                .HasMany(e => e.PCKeyItems)
                .WithRequired(e => e.KeyItem)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<KeyItem>()
                .HasMany(e => e.QuestRequiredKeyItemLists)
                .WithRequired(e => e.KeyItem)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<KeyItem>()
                .HasMany(e => e.Quests)
                .WithOptional(e => e.RewardKeyItem)
                .HasForeignKey(e => e.RewardKeyItemID);

            modelBuilder.Entity<KeyItem>()
                .HasMany(e => e.Quests1)
                .WithOptional(e => e.StartKeyItem)
                .HasForeignKey(e => e.StartKeyItemID);

            modelBuilder.Entity<LootTableItem>()
                .Property(e => e.Resref)
                .IsUnicode(false);

            modelBuilder.Entity<LootTable>()
                .HasMany(e => e.LootTableItems)
                .WithRequired(e => e.LootTable)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<NPCGroup>()
                .HasMany(e => e.PCQuestKillTargetProgresses)
                .WithRequired(e => e.NPCGroup)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<NPCGroup>()
                .HasMany(e => e.QuestKillTargetLists)
                .WithRequired(e => e.NPCGroup)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PCBase>()
                .HasMany(e => e.PCBaseStructures)
                .WithRequired(e => e.PCBase)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PCBase>()
                .HasMany(e => e.PCBasePermissions)
                .WithRequired(e => e.PCBase)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PCBaseStructureItem>()
                .Property(e => e.ItemObject)
                .IsUnicode(false);

            modelBuilder.Entity<PCBaseStructure>()
                .HasMany(e => e.PCBaseStructureItems)
                .WithRequired(e => e.PCBaseStructure)
                .WillCascadeOnDelete(false);

            
            modelBuilder.Entity<PCBaseStructure>()
                .HasMany(e => e.ChildStructures)
                .WithOptional(e => e.ParentPCBaseStructure)
                .HasForeignKey(e => e.ParentPCBaseStructureID);
            
            modelBuilder.Entity<PCKeyItem>()
                .Property(e => e.AcquiredDate)
                .HasPrecision(0);

            modelBuilder.Entity<PCMigration>()
                .HasMany(e => e.PCMigrationItems)
                .WithRequired(e => e.PCMigration)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PCQuestStatus>()
                .HasMany(e => e.PCQuestKillTargetProgresses)
                .WithRequired(e => e.PcQuestStatus)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PCQuestStatus>()
                .HasMany(e => e.PCQuestItemProgresses)
                .WithRequired(e => e.PCQuestStatus)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PerkCategory>()
                .HasMany(e => e.Perks)
                .WithRequired(e => e.PerkCategory)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PerkExecutionType>()
                .HasMany(e => e.Perks)
                .WithRequired(e => e.PerkExecutionType)
                .HasForeignKey(e => e.ExecutionTypeID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PerkLevel>()
                .HasMany(e => e.PerkLevelSkillRequirements)
                .WithRequired(e => e.PerkLevel)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Entities.Perk>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Entities.Perk>()
                .Property(e => e.ScriptName)
                .IsUnicode(false);

            modelBuilder.Entity<Entities.Perk>()
                .HasMany(e => e.PCPerks)
                .WithRequired(e => e.Perk)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Entities.Perk>()
                .HasMany(e => e.PerkLevels)
                .WithRequired(e => e.Perk)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<Entities.Perk>()
                .HasMany(e => e.PCPerkRefunds)
                .WithRequired(e => e.Perk)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Plant>()
                .HasMany(e => e.GrowingPlants)
                .WithRequired(e => e.Plant)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCPerkRefunds)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCBasePermissions)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCBaseStructurePermissions)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .Property(e => e.CreateTimestamp)
                .HasPrecision(0);

            modelBuilder.Entity<PCBaseStructure>()
                .HasMany(e => e.PrimaryResidencePlayerCharacters)
                .WithOptional(e => e.PrimaryResidencePCBaseStructure)
                .HasForeignKey(e => e.PrimaryResidencePCBaseStructureID);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCBases)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.NortheastAreas)
                .WithOptional(e => e.NortheastOwnerPlayer)
                .HasForeignKey(e => e.NortheastOwner);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.NorthwestAreas)
                .WithOptional(e => e.NorthwestOwnerPlayer)
                .HasForeignKey(e => e.NorthwestOwner);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.SoutheastAreas)
                .WithOptional(e => e.SoutheastOwnerPlayer)
                .HasForeignKey(e => e.SoutheastOwner);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.SouthwestAreas)
                .WithOptional(e => e.SouthwestOwnerPlayer)
                .HasForeignKey(e => e.SouthwestOwner);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.ChatLogs)
                .WithOptional(e => e.PlayerCharacter)
                .HasForeignKey(e => e.ReceiverPlayerID);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.ChatLogs1)
                .WithOptional(e => e.PlayerCharacter1)
                .HasForeignKey(e => e.SenderPlayerID);
            
            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCCooldowns)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCCustomEffects)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCKeyItems)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCMapPins)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCObjectVisibilities)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasOptional(e => e.PCOutfit)
                .WithRequired(e => e.PlayerCharacter);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCOverflowItems)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCPerks)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCQuestKillTargetProgresses)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCQuestItemProgresses)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCQuestStatus)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCRegionalFames)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCSearchSiteItems)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCSearchSites)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.PCSkills)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<QuestRewardItem>()
                .HasMany(e => e.PCQuestStatus)
                .WithOptional(e => e.QuestRewardItem)
                .HasForeignKey(e => e.SelectedItemRewardID);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.PCQuestStatus)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestKillTargetLists)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestPrerequisites)
                .WithRequired(e => e.Quest)
                .HasForeignKey(e => e.QuestID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.RequiredQuestPrerequisites)
                .WithRequired(e => e.RequiredQuest)
                .HasForeignKey(e => e.RequiredQuestID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestRequiredItemLists)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestRequiredKeyItemLists)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestRewardItems)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quest>()
                .HasMany(e => e.QuestStates)
                .WithRequired(e => e.Quest)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuestState>()
                .HasMany(e => e.PCQuestStatus)
                .WithRequired(e => e.CurrentQuestState)
                .HasForeignKey(e => e.CurrentQuestStateID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuestState>()
                .HasMany(e => e.QuestKillTargetLists)
                .WithRequired(e => e.QuestState)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuestState>()
                .HasMany(e => e.QuestRequiredItemLists)
                .WithRequired(e => e.QuestState)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuestState>()
                .HasMany(e => e.QuestRequiredKeyItemLists)
                .WithRequired(e => e.QuestState)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuestTypeDomain>()
                .HasMany(e => e.QuestStates)
                .WithRequired(e => e.QuestTypeDomain)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<ServerConfiguration>()
                .Property(e => e.ServerName)
                .IsUnicode(false);

            modelBuilder.Entity<ServerConfiguration>()
                .Property(e => e.MessageOfTheDay)
                .IsUnicode(false);

            modelBuilder.Entity<SkillCategory>()
                .HasMany(e => e.Skills)
                .WithRequired(e => e.SkillCategory)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Skill>()
                .HasMany(e => e.CraftBlueprints)
                .WithRequired(e => e.Skill)
                .WillCascadeOnDelete(false);
            
            modelBuilder.Entity<Skill>()
                .HasMany(e => e.PCSkills)
                .WithRequired(e => e.Skill)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Skill>()
                .HasMany(e => e.PerkLevelSkillRequirements)
                .WithRequired(e => e.Skill)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Skill>()
                .HasMany(e => e.SkillXPRequirements)
                .WithRequired(e => e.Skill)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Bank>()
                .HasMany(e => e.BankItems)
                .WithRequired(e => e.Bank)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlayerCharacter>()
                .HasMany(e => e.BankItems)
                .WithRequired(e => e.PlayerCharacter)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SpawnObjectType>()
                .HasMany(e => e.Spawns)
                .WithRequired(e => e.SpawnObjectType)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Spawn>()
                .HasMany(e => e.SpawnObjects)
                .WithRequired(e => e.Spawn)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaseStructureType>()
                .HasMany(e => e.BaseStructures)
                .WithRequired(e => e.BaseStructureType)
                .WillCascadeOnDelete(false);
        }

        private string BuildSQLQuery(string procedureName, params SqlParameter[] args)
        {
            string sql = procedureName;

            for (int x = 0; x < args.Length; x++)
            {
                sql += " @" + args[x].ParameterName;

                if (x + 1 < args.Length) sql += ",";
            }

            return sql;
        }

        public void StoredProcedure(string procedureName, params SqlParameter[] args)
        {
            Database.ExecuteSqlCommand(BuildSQLQuery(procedureName, args), args);
        }

        public List<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args)
        {
            return Database.SqlQuery<T>(BuildSQLQuery(procedureName, args), args).ToList();
        }

        public T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args)
        {
            return Database.SqlQuery<T>(BuildSQLQuery(procedureName, args), args).SingleOrDefault();
        }
    }
}
