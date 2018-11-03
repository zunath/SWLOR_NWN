using System;
using System.IO;
using System.Windows.Forms;
using AutoMapper;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ExportViewModel: 
        ImportExportViewModelBase,
        IExportViewModel
    {
        private readonly SaveFileDialog _saveFile;

        public ExportViewModel()
        {
            _saveFile = new SaveFileDialog();
            _saveFile.Filter = @"JSON File (*.json)|*.json";

        }


        public bool IsExportEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PackageName))
                    return false;

                foreach (var group in ResourceGroups)
                {
                    if (group.TargetCollection.Count > 0)
                        return true;
                }

                return false;
            }
        }

        public override void LoadAvailableResources()
        {
            AvailableResources.Clear();
            if (SelectedAvailableResourceGroup == null) return;

            string path = "./Data/" + SelectedAvailableResourceGroup.FolderName + "/";
            string[] files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                var dbObject = JsonConvert.DeserializeObject(json, SelectedAvailableResourceGroup.Type);
                AvailableResources.Add(dbObject);
            }
        }

        protected override void Notify()
        {
            base.Notify();
            NotifyOfPropertyChange(() => IsExportEnabled);
        }

        public void Export()
        {
            var result = _saveFile.ShowDialog();
            if (result == DialogResult.Cancel) return;
            string fileName = _saveFile.FileName;

            var package = BuildPackage();
            string json = JsonConvert.SerializeObject(package);
            File.WriteAllText(fileName, json);

            ClearData();
            TryClose();
        }

        private DataPackageFile BuildPackage()
        {
            var package = new DataPackageFile(PackageName);
            foreach (var group in ResourceGroups)
            {
                foreach (var item in group.TargetCollection)
                {
                    switch (group.ResourceType)
                    {
                        case ResourceType.ApartmentBuildings:
                            var apartmentBuilding = Mapper.Map<ApartmentBuildingViewModel, ApartmentBuilding>(item);
                            package.ApartmentBuildings.Add(apartmentBuilding);
                            break;
                        case ResourceType.BaseStructures:
                            var baseStructure = Mapper.Map<BaseStructureViewModel, BaseStructure>(item);
                            package.BaseStructures.Add(baseStructure);
                            break;
                        case ResourceType.BuildingStyles:
                            var buildingStyle = Mapper.Map<BuildingStyleViewModel, BuildingStyle>(item);
                            package.BuildingStyles.Add(buildingStyle);
                            break;
                        case ResourceType.CooldownCategories:
                            var cooldownCategory = Mapper.Map<CooldownCategoryViewModel, CooldownCategory>(item);
                            package.CooldownCategories.Add(cooldownCategory);
                            break;
                        case ResourceType.CraftBlueprintCategories:
                            var craftBlueprintCategory = Mapper.Map<CraftBlueprintCategoryViewModel, CraftBlueprintCategory>(item);
                            package.CraftBlueprintCategories.Add(craftBlueprintCategory);
                            break;
                        case ResourceType.CraftBlueprints:
                            var craftBlueprint = Mapper.Map<CraftBlueprintViewModel, CraftBlueprint>(item);
                            package.CraftBlueprints.Add(craftBlueprint);
                            break;
                        case ResourceType.CraftDevices:
                            var craftDevice = Mapper.Map<CraftDeviceViewModel, CraftDevice>(item);
                            package.CraftDevices.Add(craftDevice);
                            break;
                        case ResourceType.CustomEffects:
                            var customEffect = Mapper.Map<CustomEffectViewModel, CustomEffect>(item);
                            package.CustomEffects.Add(customEffect);
                            break;
                        case ResourceType.Downloads:
                            var download = Mapper.Map<DownloadViewModel, Download>(item);
                            package.Downloads.Add(download);
                            break;
                        case ResourceType.FameRegions:
                            var fameRegion = Mapper.Map<FameRegionViewModel, FameRegion>(item);
                            package.FameRegions.Add(fameRegion);
                            break;
                        case ResourceType.GameTopicCategories:
                            var gameTopicCategories = Mapper.Map<GameTopicCategoryViewModel, GameTopicCategory>(item);
                            package.GameTopicCategories.Add(gameTopicCategories);
                            break;
                        case ResourceType.GameTopics:
                            var gameTopic = Mapper.Map<GameTopicViewModel, GameTopic>(item);
                            package.GameTopics.Add(gameTopic);
                            break;
                        case ResourceType.KeyItemCategories:
                            var keyItemCategory = Mapper.Map<KeyItemCategoryViewModel, KeyItemCategory>(item);
                            package.KeyItemCategories.Add(keyItemCategory);
                            break;
                        case ResourceType.KeyItems:
                            var keyItem = Mapper.Map<KeyItemViewModel, KeyItem>(item);
                            package.KeyItems.Add(keyItem);
                            break;
                        case ResourceType.LootTableItems:
                            var lootTableItem = Mapper.Map<LootTableItemViewModel, LootTableItem>(item);
                            package.LootTableItems.Add(lootTableItem);
                            break;
                        case ResourceType.LootTables:
                            var lootTable = Mapper.Map<LootTableViewModel, LootTable>(item);
                            package.LootTables.Add(lootTable);
                            break;
                        case ResourceType.Mods:
                            var mod = Mapper.Map<ModViewModel, Mod>(item);
                            package.Mods.Add(mod);
                            break;
                        case ResourceType.NPCGroups:
                            var npcGroup = Mapper.Map<NPCGroupViewModel, NPCGroup>(item);
                            package.NPCGroups.Add(npcGroup);
                            break;
                        case ResourceType.PerkCategories:
                            var perkCategory = Mapper.Map<PerkCategoryViewModel, PerkCategory>(item);
                            package.PerkCategories.Add(perkCategory);
                            break;
                        case ResourceType.Plants:
                            var plant = Mapper.Map<PlantViewModel, Plant>(item);
                            package.Plants.Add(plant);
                            break;
                        case ResourceType.Quests:
                            var quest = Mapper.Map<QuestViewModel, Quest>(item);
                            package.Quests.Add(quest);
                            break;
                        case ResourceType.SkillCategories:
                            var skillCategory = Mapper.Map<SkillCategoryViewModel, SkillCategory>(item);
                            package.SkillCategories.Add(skillCategory);
                            break;
                        case ResourceType.Skills:
                            var skill = Mapper.Map<SkillViewModel, Skill>(item);
                            package.Skills.Add(skill);
                            break;
                        case ResourceType.Spawns:
                            var spawn = Mapper.Map<SpawnViewModel, Spawn>(item);
                            package.Spawns.Add(spawn);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return package;
        }
    }
}
