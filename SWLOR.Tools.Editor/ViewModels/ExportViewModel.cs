using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AutoMapper;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;
using Action = System.Action;
using Screen = Caliburn.Micro.Screen;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ExportViewModel :
        Screen, IExportViewModel
    {
        private SaveFileDialog _saveFile;

        public ExportViewModel()
        {
            _saveFile = new SaveFileDialog();
            _saveFile.Filter = @"JSON File (*.json)|*.json";

            PackageName = string.Empty;
            ResourceGroups = new ObservableCollection<ResourceGroup>
            {
                new ResourceGroup("Apartment Buildings", ResourceType.ApartmentBuildings, nameof(ApartmentBuilding), typeof(ApartmentBuildingViewModel)),
                new ResourceGroup("Base Structures", ResourceType.BaseStructures, nameof(BaseStructure), typeof(BaseStructureViewModel)),
                new ResourceGroup("Building Styles", ResourceType.BuildingStyles, nameof(BuildingStyle), typeof(BuildingStyleViewModel)),
                new ResourceGroup("Cooldown Categories", ResourceType.CooldownCategories, nameof(CooldownCategory), typeof(CooldownCategoryViewModel)),
                new ResourceGroup("Craft Blueprint Categories", ResourceType.CraftBlueprintCategories, nameof(CraftBlueprintCategory), typeof(CraftBlueprintCategoryViewModel)),
                new ResourceGroup("Craft Blueprints", ResourceType.CraftBlueprints, nameof(CraftBlueprint), typeof(CraftBlueprintViewModel)),
                new ResourceGroup("Craft Devices", ResourceType.CraftDevices, nameof(CraftDevice), typeof(CraftDeviceViewModel)),
                new ResourceGroup("Custom Effects", ResourceType.CustomEffects, nameof(CustomEffect), typeof(CustomEffectViewModel)),
                new ResourceGroup("Downloads", ResourceType.Downloads, nameof(Download), typeof(DownloadViewModel)),
                new ResourceGroup("Fame Regions", ResourceType.FameRegions, nameof(FameRegion), typeof(FameRegionViewModel)),
                new ResourceGroup("Game Topic Categories", ResourceType.GameTopicCategories, nameof(GameTopicCategory), typeof(GameTopicCategoryViewModel)),
                new ResourceGroup("Game Topics", ResourceType.GameTopics, nameof(GameTopic), typeof(GameTopicViewModel)),
                new ResourceGroup("Key Item Categories", ResourceType.KeyItemCategories, nameof(KeyItemCategory), typeof(KeyItemCategoryViewModel)),
                new ResourceGroup("Key Items", ResourceType.KeyItems, nameof(KeyItem), typeof(KeyItemViewModel)),
                new ResourceGroup("Loot Tables", ResourceType.LootTables, nameof(LootTable), typeof(LootTableViewModel)),
                new ResourceGroup("Mods", ResourceType.Mods, nameof(Mod), typeof(ModViewModel)),
                new ResourceGroup("NPC Groups", ResourceType.NPCGroups, nameof(NPCGroup), typeof(NPCGroupViewModel)),
                new ResourceGroup("Perk Categories", ResourceType.PerkCategories, nameof(PerkCategory), typeof(PerkCategoryViewModel)),
                new ResourceGroup("Plants", ResourceType.Plants, nameof(Plant), typeof(PlantViewModel)),
                new ResourceGroup("Quests", ResourceType.Quests, nameof(Quest), typeof(QuestViewModel)),
                new ResourceGroup("Skill Categories", ResourceType.SkillCategories, nameof(SkillCategory), typeof(SkillCategoryViewModel)),
                new ResourceGroup("Skills", ResourceType.Skills, nameof(Skill), typeof(SkillViewModel)),
                new ResourceGroup("Spawns", ResourceType.Spawns, nameof(Spawn), typeof(SpawnViewModel)),
            };
            AvailableResources = new ObservableCollection<dynamic>();
            _addedResources = new ObservableCollection<dynamic>();

            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();
        }

        private string _packageName;
        public string PackageName
        {
            get => _packageName;
            set
            {
                _packageName = value;
                NotifyOfPropertyChange(() => PackageName);
                Notify();
            }

        }

        private ObservableCollection<ResourceGroup> _resourceGroups;
        public ObservableCollection<ResourceGroup> ResourceGroups
        {
            get => _resourceGroups;
            set
            {
                _resourceGroups = value;
                NotifyOfPropertyChange(() => ResourceGroups);
            }
        }

        private ResourceGroup _selectedAvailableResourceGroup;
        public ResourceGroup SelectedAvailableResourceGroup
        {
            get => _selectedAvailableResourceGroup;
            set
            {
                _selectedAvailableResourceGroup = value;
                NotifyOfPropertyChange(() => SelectedAvailableResourceGroup);
                LoadAvailableResources();
            }
        }

        private ObservableCollection<dynamic> _availableResources;
        public ObservableCollection<dynamic> AvailableResources
        {
            get => _availableResources;
            set
            {
                _availableResources = value;
                NotifyOfPropertyChange(() => AvailableResources);
            }
        }

        private object _selectedAvailableResource;
        public object SelectedAvailableResource
        {
            get => _selectedAvailableResource;
            set
            {
                _selectedAvailableResource = value;
                NotifyOfPropertyChange(() => SelectedAvailableResource);
                NotifyOfPropertyChange(() => AddedResources);
                Notify();
            }
        }

        private ResourceGroup _selectedAddedResourceGroup;
        public ResourceGroup SelectedAddedResourceGroup
        {
            get => _selectedAddedResourceGroup;
            set
            {
                _selectedAddedResourceGroup = value;
                NotifyOfPropertyChange(() => SelectedAddedResourceGroup);
                LoadAddedResources();
                NotifyOfPropertyChange(() => AddedResources);
            }
        }

        private ObservableCollection<dynamic> _addedResources;
        public ObservableCollection<dynamic> AddedResources
        {
            get => _addedResources;
            set
            {
                _addedResources = value;
                NotifyOfPropertyChange(() => AddedResources);
            }
        }

        private object _selectedAddedResource;
        public object SelectedAddedResource
        {
            get => _selectedAddedResource;
            set
            {
                _selectedAddedResource = value;
                NotifyOfPropertyChange(() => SelectedAddedResource);
                Notify();
            }
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
        public bool IsRemoveEnabled => SelectedAddedResource != null;

        public bool IsAddEnabled
        {
            get
            {
                bool isEnabled = SelectedAvailableResource != null;

                if (SelectedAvailableResource != null)
                {

                    foreach (var group in ResourceGroups)
                    {
                        var existing = group.TargetCollection.FirstOrDefault(x => x.InternalEditorID == ((DBObjectViewModelBase) SelectedAvailableResource).InternalEditorID);
                        if (existing != null)
                        {
                            isEnabled = false;
                            break;
                        }
                    }
                }

                return isEnabled;
            }
        }



        public void LoadAvailableResources()
        {
            AvailableResources.Clear();
            if (SelectedAvailableResourceGroup == null) return;

            string path = "./Data/" + SelectedAvailableResourceGroup.FolderName + "/";
            string[] files = Directory.GetFiles(path);

            foreach(var file in files)
            {
                string json = File.ReadAllText(file);
                var dbObject = JsonConvert.DeserializeObject(json, SelectedAvailableResourceGroup.Type);
                AvailableResources.Add(dbObject);
            }
        }

        private void LoadAddedResources()
        {
            AddedResources.Clear();
            var set = SelectedAddedResourceGroup.TargetCollection;
            foreach(var item in set)
            {
                AddedResources.Add(item);
            }
        }
        
        private void Notify()
        {
            NotifyOfPropertyChange(() => AddedResources);
            NotifyOfPropertyChange(() => IsAddEnabled);
            NotifyOfPropertyChange(() => IsExportEnabled);
            NotifyOfPropertyChange(() => IsRemoveEnabled);
        }

        public void AddResource()
        {
            SelectedAvailableResourceGroup.TargetCollection.Add(SelectedAvailableResource);
            SelectedAddedResourceGroup = SelectedAvailableResourceGroup;
            Notify();
        }

        public void RemoveResource()
        {
            SelectedAddedResourceGroup.TargetCollection.Remove(SelectedAddedResource);
            LoadAddedResources();
            Notify();
        }

        public void Export()
        {
            var result = _saveFile.ShowDialog();

            if (result == DialogResult.Cancel) return;

            string fileName = _saveFile.FileName;

            var package = new ImportExportPackageViewModel(PackageName);
            foreach (var group in ResourceGroups)
            {
                foreach(var item in group.TargetCollection)
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

            string json = JsonConvert.SerializeObject(package);
            File.WriteAllText(fileName, json);

            TryClose();
        }

        public void Cancel()
        {
            AddedResources.Clear();
            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();
            TryClose();
        }
    }
}
