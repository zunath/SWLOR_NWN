using System;
using System.IO;
using System.Windows.Forms;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
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

        private delegate T MappingDelegate<T>(T source);

        private JObject ProcessViewModel<T1, T2>(T1 input, JObject parent = null, MappingDelegate<T2> mapping = null)
            where T1: DBObjectViewModelBase
            where T2: IEntity
        {
            var mapped = Mapper.Map<T1, T2>(input);
            if (mapping != null)
            {
                mapped = mapping.Invoke(mapped);
            }

            var result = JObject.FromObject(mapped);
            var exportID = Guid.NewGuid().ToString();
            result.Add("ExportID", exportID);

            if (parent != null)
            {
                result.Add("ParentExportID", parent["ExportID"]);
            }

            return result;
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
                            var apartmentBuilding = ProcessViewModel<ApartmentBuildingViewModel, ApartmentBuilding>(item);
                            package.ApartmentBuildings.Add(apartmentBuilding);
                            break;
                        case ResourceType.BaseStructures:
                            var baseStructure = ProcessViewModel<BaseStructureViewModel, BaseStructure>(item);
                            package.BaseStructures.Add(baseStructure);
                            break;
                        case ResourceType.BuildingStyles:
                            var buildingStyle = ProcessViewModel<BuildingStyleViewModel, BuildingStyle>(item);
                            package.BuildingStyles.Add(buildingStyle);
                            break;
                        case ResourceType.CooldownCategories:
                            var cooldownCategory = ProcessViewModel<CooldownCategoryViewModel, CooldownCategory>(item);
                            package.CooldownCategories.Add(cooldownCategory);
                            break;
                        case ResourceType.CraftBlueprintCategories:
                            var craftBlueprintCategory = ProcessViewModel<CraftBlueprintCategoryViewModel, CraftBlueprintCategory>(item);
                            package.CraftBlueprintCategories.Add(craftBlueprintCategory);
                            break;
                        case ResourceType.CraftBlueprints:
                            var craftBlueprint = ProcessViewModel<CraftBlueprintViewModel, CraftBlueprint>(item);
                            package.CraftBlueprints.Add(craftBlueprint);
                            break;
                        case ResourceType.CraftDevices:
                            var craftDevice = ProcessViewModel<CraftDeviceViewModel, CraftDevice>(item);
                            package.CraftDevices.Add(craftDevice);
                            break;
                        case ResourceType.CustomEffects:
                            var customEffect = ProcessViewModel<CustomEffectViewModel, CustomEffect>(item);
                            package.CustomEffects.Add(customEffect);
                            break;
                        case ResourceType.Downloads:
                            var download = ProcessViewModel<DownloadViewModel, Download>(item);
                            package.Downloads.Add(download);
                            break;
                        case ResourceType.FameRegions:
                            var fameRegion = ProcessViewModel<FameRegionViewModel, FameRegion>(item);
                            package.FameRegions.Add(fameRegion);
                            break;
                        case ResourceType.GameTopicCategories:
                            var gameTopicCategories = ProcessViewModel<GameTopicCategoryViewModel, GameTopicCategory>(item);
                            package.GameTopicCategories.Add(gameTopicCategories);
                            break;
                        case ResourceType.GameTopics:
                            var gameTopic = ProcessViewModel<GameTopicViewModel, GameTopic>(item);
                            package.GameTopics.Add(gameTopic);
                            break;
                        case ResourceType.KeyItemCategories:
                            var keyItemCategory = ProcessViewModel<KeyItemCategoryViewModel, KeyItemCategory>(item);
                            package.KeyItemCategories.Add(keyItemCategory);
                            break;
                        case ResourceType.KeyItems:
                            var keyItem = ProcessViewModel<KeyItemViewModel, KeyItem>(item);
                            package.KeyItems.Add(keyItem);
                            break;
                        case ResourceType.LootTables:
                            JObject lootTable = ProcessViewModel<LootTableViewModel, LootTable>(item);
                            var vm = (LootTableViewModel) item;
                            
                            foreach (var lti in vm.LootTableItems)
                            {
                                JObject ltiVM = JObject.FromObject(ProcessViewModel<LootTableItemViewModel, LootTableItem>(lti, lootTable));
                                package.LootTableItems.Add(ltiVM);
                            }
                            
                            package.LootTables.Add(lootTable);
                            break;
                        case ResourceType.Mods:
                            var mod = ProcessViewModel<ModViewModel, Mod>(item);
                            package.Mods.Add(mod);
                            break;
                        case ResourceType.NPCGroups:
                            var npcGroup = ProcessViewModel<NPCGroupViewModel, NPCGroup>(item);
                            package.NPCGroups.Add(npcGroup);
                            break;
                        case ResourceType.PerkCategories:
                            var perkCategory = ProcessViewModel<PerkCategoryViewModel, PerkCategory>(item);
                            package.PerkCategories.Add(perkCategory);
                            break;
                        case ResourceType.Plants:
                            var plant = ProcessViewModel<PlantViewModel, Plant>(item);
                            package.Plants.Add(plant);
                            break;
                        case ResourceType.Quests:
                            var quest = ProcessViewModel<QuestViewModel, Quest>(item);
                            package.Quests.Add(quest);
                            break;
                        case ResourceType.SkillCategories:
                            var skillCategory = ProcessViewModel<SkillCategoryViewModel, SkillCategory>(item);
                            package.SkillCategories.Add(skillCategory);
                            break;
                        case ResourceType.Skills:
                            var skill = ProcessViewModel<SkillViewModel, Skill>(item);
                            package.Skills.Add(skill);
                            break;
                        case ResourceType.Spawns:
                            var spawn = ProcessViewModel<SpawnViewModel, Spawn>(item);
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
