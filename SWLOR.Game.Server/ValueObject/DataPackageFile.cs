using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.ValueObject
{
    public class DataPackageFile
    {
        public string PackageName { get; set; }
        public DateTime ExportDate { get; set; }

        public List<ApartmentBuilding> ApartmentBuildings { get; set; }
        public List<BaseStructure> BaseStructures { get; set; }
        public List<BuildingStyle> BuildingStyles { get; set; }
        public List<CooldownCategory> CooldownCategories { get; set; }
        public List<CraftBlueprintCategory> CraftBlueprintCategories { get; set; }
        public List<CraftBlueprint> CraftBlueprints { get; set; }
        public List<CraftDevice> CraftDevices { get; set; }
        public List<Data.Entity.CustomEffect> CustomEffects { get; set; }
        public List<Download> Downloads { get; set; }
        public List<FameRegion> FameRegions { get; set; }
        public List<GameTopicCategory> GameTopicCategories { get; set; }
        public List<GameTopic> GameTopics { get; set; }
        public List<KeyItemCategory> KeyItemCategories { get; set; }
        public List<KeyItem> KeyItems { get; set; }
        public List<LootTableItem> LootTableItems { get; set; }
        public List<LootTable> LootTables { get; set; }
        public List<Data.Entity.Mod> Mods { get; set; }
        public List<NPCGroup> NPCGroups { get; set; }
        public List<PerkCategory> PerkCategories { get; set; }
        public List<Plant> Plants { get; set; }
        public List<Quest> Quests { get; set; }
        public List<SkillCategory> SkillCategories { get; set; }
        public List<Data.Entity.Skill> Skills { get; set; }
        public List<Spawn> Spawns { get; set; }

        public DataPackageFile(string packageName)
        {
            PackageName = packageName;
            ExportDate = DateTime.UtcNow;

            ApartmentBuildings = new List<ApartmentBuilding>();
            BaseStructures = new List<BaseStructure>();
            BuildingStyles = new List<BuildingStyle>();
            CooldownCategories = new List<CooldownCategory>();
            CraftBlueprintCategories = new List<CraftBlueprintCategory>();
            CraftBlueprints = new List<CraftBlueprint>();
            CraftDevices = new List<CraftDevice>();
            CustomEffects = new List<Data.Entity.CustomEffect>();
            Downloads = new List<Download>();
            FameRegions = new List<FameRegion>();
            GameTopicCategories = new List<GameTopicCategory>();
            GameTopics = new List<GameTopic>();
            KeyItemCategories = new List<KeyItemCategory>();
            KeyItems = new List<KeyItem>();
            LootTableItems = new List<LootTableItem>();
            LootTables = new List<LootTable>();
            Mods = new List<Data.Entity.Mod>();
            NPCGroups = new List<NPCGroup>();
            PerkCategories = new List<PerkCategory>();
            Plants = new List<Plant>();
            Quests = new List<Quest>();
            SkillCategories = new List<SkillCategory>();
            Skills = new List<Data.Entity.Skill>();
            Spawns = new List<Spawn>();
        }
    }
}
