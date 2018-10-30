using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    public class ImportExportPackageViewModel
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
        public List<CustomEffect> CustomEffects { get; set; }
        public List<Download> Downloads { get; set; }
        public List<FameRegion> FameRegions { get; set; }
        public List<GameTopicCategory> GameTopicCategories { get; set; }
        public List<GameTopic> GameTopics { get; set; }
        public List<KeyItemCategory> KeyItemCategories { get; set; }
        public List<KeyItem> KeyItems { get; set; }
        public List<LootTableItem> LootTableItems { get; set; }
        public List<LootTable> LootTables { get; set; }
        public List<Mod> Mods { get; set; }
        public List<NPCGroup> NPCGroups { get; set; }
        public List<PerkCategory> PerkCategories { get; set; }
        public List<Plant> Plants { get; set; }
        public List<Quest> Quests { get; set; }
        public List<SkillCategory> SkillCategories { get; set; }
        public List<Skill> Skills { get; set; }
        public List<Spawn> Spawns { get; set; }

        public ImportExportPackageViewModel(string packageName)
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
            CustomEffects = new List<CustomEffect>();
            Downloads = new List<Download>();
            FameRegions = new List<FameRegion>();
            GameTopicCategories = new List<GameTopicCategory>();
            GameTopics = new List<GameTopic>();
            KeyItemCategories = new List<KeyItemCategory>();
            KeyItems = new List<KeyItem>();
            LootTableItems = new List<LootTableItem>();
            LootTables = new List<LootTable>();
            Mods = new List<Mod>();
            NPCGroups = new List<NPCGroup>();
            PerkCategories = new List<PerkCategory>();
            Plants = new List<Plant>();
            Quests = new List<Quest>();
            SkillCategories = new List<SkillCategory>();
            Skills = new List<Skill>();
            Spawns = new List<Spawn>();
        }

        public bool HasData => ApartmentBuildings.Count > 0 ||
                               BaseStructures.Count > 0 ||
                               BuildingStyles.Count > 0 ||
                               CooldownCategories.Count > 0 ||
                               CraftBlueprintCategories.Count > 0 ||
                               CraftBlueprints.Count > 0 ||
                               CraftDevices.Count > 0 ||
                               CustomEffects.Count > 0 ||
                               Downloads.Count > 0 ||
                               FameRegions.Count > 0 ||
                               GameTopicCategories.Count > 0 ||
                               GameTopics.Count > 0 ||
                               KeyItemCategories.Count > 0 ||
                               KeyItems.Count > 0 ||
                               LootTableItems.Count > 0 ||
                               LootTables.Count > 0 ||
                               Mods.Count > 0 ||
                               NPCGroups.Count > 0 ||
                               PerkCategories.Count > 0 ||
                               Plants.Count > 0 ||
                               Quests.Count > 0 ||
                               SkillCategories.Count > 0 ||
                               Skills.Count > 0 ||
                               Spawns.Count > 0;

        public void Clear()
        {
            PackageName = string.Empty;
            ExportDate = DateTime.UtcNow;

            ApartmentBuildings.Clear();
            BaseStructures.Clear();
            BuildingStyles.Clear();
            CooldownCategories.Clear();
            CraftBlueprintCategories.Clear();
            CraftBlueprints.Clear();
            CraftDevices.Clear();
            CustomEffects.Clear();
            Downloads.Clear();
            FameRegions.Clear();
            GameTopicCategories.Clear();
            GameTopics.Clear();
            KeyItemCategories.Clear();
            KeyItems.Clear();
            LootTableItems.Clear();
            LootTables.Clear();
            Mods.Clear();
            NPCGroups.Clear();
            PerkCategories.Clear();
            Plants.Clear();
            Quests.Clear();
            SkillCategories.Clear();
            Skills.Clear();
            Spawns.Clear();
        }
    }
}
