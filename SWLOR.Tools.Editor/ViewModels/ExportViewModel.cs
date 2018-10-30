using System;
using System.Collections.ObjectModel;
using System.IO;
using AutoMapper;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;
using Action = System.Action;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ExportViewModel :
        Screen,
        IExportViewModel
    {
        public ExportViewModel()
        {
            ActivePackage = new ImportExportPackageViewModel(string.Empty);
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
                new ResourceGroup("Key Item Categories", ResourceType.KeyItemCategories, nameof(KeyItemCategory), typeof(KeyItemViewModel)),
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

        private readonly ObservableCollection<dynamic> _addedResources;
        public ObservableCollection<dynamic> AddedResources
        {
            get
            {
                _addedResources.Clear();
                if (SelectedAddedResourceGroup == null) return _addedResources;

                switch (SelectedAddedResourceGroup.ResourceType)
                {
                    case ResourceType.ApartmentBuildings:
                        foreach(var x in ActivePackage.ApartmentBuildings)
                            _addedResources.Add(x);
                        break;
                    case ResourceType.BaseStructures:
                        foreach (var x in ActivePackage.BaseStructures)
                            _addedResources.Add(x);
                        break;
                    case ResourceType.BuildingStyles:
                        foreach (var x in ActivePackage.BuildingStyles)
                            _addedResources.Add(x);
                        break;
                    case ResourceType.CooldownCategories:
                        foreach (var x in ActivePackage.CooldownCategories)
                            _addedResources.Add(x);
                        break;
                    case ResourceType.CraftBlueprintCategories:
                        break;
                    case ResourceType.CraftBlueprints:
                        break;
                    case ResourceType.CraftDevices:
                        break;
                    case ResourceType.CustomEffects:
                        break;
                    case ResourceType.Downloads:
                        break;
                    case ResourceType.FameRegions:
                        break;
                    case ResourceType.GameTopicCategories:
                        break;
                    case ResourceType.GameTopics:
                        break;
                    case ResourceType.KeyItemCategories:
                        break;
                    case ResourceType.KeyItems:
                        break;
                    case ResourceType.LootTableItems:
                        break;
                    case ResourceType.LootTables:
                        break;
                    case ResourceType.Mods:
                        break;
                    case ResourceType.NPCGroups:
                        break;
                    case ResourceType.PerkCategories:
                        break;
                    case ResourceType.Plants:
                        break;
                    case ResourceType.Quests:
                        break;
                    case ResourceType.SkillCategories:
                        break;
                    case ResourceType.Skills:
                        break;
                    case ResourceType.Spawns:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return _addedResources;
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

        private ImportExportPackageViewModel _activePackage;
        public ImportExportPackageViewModel ActivePackage
        {
            get => _activePackage;
            set
            {
                _activePackage = value;
                NotifyOfPropertyChange(() => ActivePackage);
            }
        }

        public bool IsExportEnabled => ActivePackage.HasData;
        public bool IsRemoveEnabled => SelectedAddedResource != null;
        public bool IsAddEnabled => SelectedAvailableResource != null && !AddedResources.Contains(SelectedAvailableResource);

        private void LoadAvailableResources()
        {
            AvailableResources.Clear();
            if (SelectedAvailableResourceGroup == null) return;

            string path = "./Data/" + SelectedAvailableResourceGroup.FolderName + "/";
            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);

                switch (SelectedAvailableResourceGroup.ResourceType)
                {
                    case ResourceType.ApartmentBuildings:
                        var dbObject = JsonConvert.DeserializeObject<ApartmentBuildingViewModel>(json);
                        AvailableResources.Add(dbObject);
                        break;
                    case ResourceType.BaseStructures:
                        break;
                    case ResourceType.BuildingStyles:
                        break;
                    case ResourceType.CooldownCategories:
                        break;
                    case ResourceType.CraftBlueprintCategories:
                        break;
                    case ResourceType.CraftBlueprints:
                        break;
                    case ResourceType.CraftDevices:
                        break;
                    case ResourceType.CustomEffects:
                        break;
                    case ResourceType.Downloads:
                        break;
                    case ResourceType.FameRegions:
                        break;
                    case ResourceType.GameTopicCategories:
                        break;
                    case ResourceType.GameTopics:
                        break;
                    case ResourceType.KeyItemCategories:
                        break;
                    case ResourceType.KeyItems:
                        break;
                    case ResourceType.LootTableItems:
                        break;
                    case ResourceType.LootTables:
                        break;
                    case ResourceType.Mods:
                        break;
                    case ResourceType.NPCGroups:
                        break;
                    case ResourceType.PerkCategories:
                        break;
                    case ResourceType.Plants:
                        break;
                    case ResourceType.Quests:
                        break;
                    case ResourceType.SkillCategories:
                        break;
                    case ResourceType.Skills:
                        break;
                    case ResourceType.Spawns:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void LoadAddedResources()
        {
            
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
            switch (SelectedAvailableResourceGroup.ResourceType)
            {
                case ResourceType.ApartmentBuildings:
                    var obj = Mapper.Map<ApartmentBuildingViewModel, ApartmentBuilding>((ApartmentBuildingViewModel) SelectedAvailableResource);
                    ActivePackage.ApartmentBuildings.Add(obj);
                    break;
                case ResourceType.BaseStructures:
                    break;
                case ResourceType.BuildingStyles:
                    break;
                case ResourceType.CooldownCategories:
                    break;
                case ResourceType.CraftBlueprintCategories:
                    break;
                case ResourceType.CraftBlueprints:
                    break;
                case ResourceType.CraftDevices:
                    break;
                case ResourceType.CustomEffects:
                    break;
                case ResourceType.Downloads:
                    break;
                case ResourceType.FameRegions:
                    break;
                case ResourceType.GameTopicCategories:
                    break;
                case ResourceType.GameTopics:
                    break;
                case ResourceType.KeyItemCategories:
                    break;
                case ResourceType.KeyItems:
                    break;
                case ResourceType.LootTableItems:
                    break;
                case ResourceType.LootTables:
                    break;
                case ResourceType.Mods:
                    break;
                case ResourceType.NPCGroups:
                    break;
                case ResourceType.PerkCategories:
                    break;
                case ResourceType.Plants:
                    break;
                case ResourceType.Quests:
                    break;
                case ResourceType.SkillCategories:
                    break;
                case ResourceType.Skills:
                    break;
                case ResourceType.Spawns:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Notify();
        }

        public void RemoveResource()
        {
            AddedResources.Remove(SelectedAddedResource);
            Notify();
        }

        public void Export()
        {

        }

        public void Cancel()
        {
            ActivePackage.Clear();
            TryClose();
        }
    }
}
