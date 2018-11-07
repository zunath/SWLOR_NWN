using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SWLOR.Game.Server.ValueObject
{
    public class DataPackageFile
    {
        public string PackageName { get; set; }
        public DateTime ExportDate { get; set; }

        public List<JObject> ApartmentBuildings { get; set; }
        public List<JObject> BaseStructures { get; set; }
        public List<JObject> BuildingStyles { get; set; }
        public List<JObject> CooldownCategories { get; set; }
        public List<JObject> CraftBlueprintCategories { get; set; }
        public List<JObject> CraftBlueprints { get; set; }
        public List<JObject> CraftDevices { get; set; }
        public List<JObject> CustomEffects { get; set; }
        public List<JObject> Downloads { get; set; }
        public List<JObject> FameRegions { get; set; }
        public List<JObject> GameTopicCategories { get; set; }
        public List<JObject> GameTopics { get; set; }
        public List<JObject> KeyItemCategories { get; set; }
        public List<JObject> KeyItems { get; set; }
        public List<JObject> LootTableItems { get; set; }
        public List<JObject> LootTables { get; set; }
        public List<JObject> Mods { get; set; }
        public List<JObject> NPCGroups { get; set; }
        public List<JObject> PerkCategories { get; set; }
        public List<JObject> Plants { get; set; }
        public List<JObject> Quests { get; set; }
        public List<JObject> SkillCategories { get; set; }
        public List<JObject> Skills { get; set; }
        public List<JObject> Spawns { get; set; }

        public DataPackageFile(string packageName)
        {
            PackageName = packageName;
            ExportDate = DateTime.UtcNow;

            ApartmentBuildings = new List<JObject>();
            BaseStructures = new List<JObject>();
            BuildingStyles = new List<JObject>();
            CooldownCategories = new List<JObject>();
            CraftBlueprintCategories = new List<JObject>();
            CraftBlueprints = new List<JObject>();
            CraftDevices = new List<JObject>();
            CustomEffects = new List<JObject>();
            Downloads = new List<JObject>();
            FameRegions = new List<JObject>();
            GameTopicCategories = new List<JObject>();
            GameTopics = new List<JObject>();
            KeyItemCategories = new List<JObject>();
            KeyItems = new List<JObject>();
            LootTableItems = new List<JObject>();
            LootTables = new List<JObject>();
            Mods = new List<JObject>();
            NPCGroups = new List<JObject>();
            PerkCategories = new List<JObject>();
            Plants = new List<JObject>();
            Quests = new List<JObject>();
            SkillCategories = new List<JObject>();
            Skills = new List<JObject>();
            Spawns = new List<JObject>();
        }
    }
}
