using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AutoMapper;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Extension;
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
                new ResourceGroup("Apartment Buildings", ResourceType.ApartmentBuildings, nameof(ApartmentBuilding), typeof(ApartmentBuildingViewModel), new ObservableCollection<dynamic>(ActivePackage.ApartmentBuildings)),
                new ResourceGroup("Base Structures", ResourceType.BaseStructures, nameof(BaseStructure), typeof(BaseStructureViewModel), new ObservableCollection<dynamic>(ActivePackage.BaseStructures)),
                new ResourceGroup("Building Styles", ResourceType.BuildingStyles, nameof(BuildingStyle), typeof(BuildingStyleViewModel), new ObservableCollection<dynamic>(ActivePackage.BuildingStyles)),
                new ResourceGroup("Cooldown Categories", ResourceType.CooldownCategories, nameof(CooldownCategory), typeof(CooldownCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.CooldownCategories)),
                new ResourceGroup("Craft Blueprint Categories", ResourceType.CraftBlueprintCategories, nameof(CraftBlueprintCategory), typeof(CraftBlueprintCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.CraftBlueprintCategories)),
                new ResourceGroup("Craft Blueprints", ResourceType.CraftBlueprints, nameof(CraftBlueprint), typeof(CraftBlueprintViewModel), new ObservableCollection<dynamic>(ActivePackage.CraftBlueprints), "ItemName"),
                new ResourceGroup("Craft Devices", ResourceType.CraftDevices, nameof(CraftDevice), typeof(CraftDeviceViewModel), new ObservableCollection<dynamic>(ActivePackage.CraftDevices)),
                new ResourceGroup("Custom Effects", ResourceType.CustomEffects, nameof(CustomEffect), typeof(CustomEffectViewModel), new ObservableCollection<dynamic>(ActivePackage.CustomEffects)),
                new ResourceGroup("Downloads", ResourceType.Downloads, nameof(Download), typeof(DownloadViewModel), new ObservableCollection<dynamic>(ActivePackage.Downloads)),
                new ResourceGroup("Fame Regions", ResourceType.FameRegions, nameof(FameRegion), typeof(FameRegionViewModel), new ObservableCollection<dynamic>(ActivePackage.FameRegions)),
                new ResourceGroup("Game Topic Categories", ResourceType.GameTopicCategories, nameof(GameTopicCategory), typeof(GameTopicCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.GameTopicCategories)),
                new ResourceGroup("Game Topics", ResourceType.GameTopics, nameof(GameTopic), typeof(GameTopicViewModel), new ObservableCollection<dynamic>(ActivePackage.GameTopics)),
                new ResourceGroup("Key Item Categories", ResourceType.KeyItemCategories, nameof(KeyItemCategory), typeof(KeyItemCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.KeyItemCategories)),
                new ResourceGroup("Key Items", ResourceType.KeyItems, nameof(KeyItem), typeof(KeyItemViewModel), new ObservableCollection<dynamic>(ActivePackage.KeyItems)),
                new ResourceGroup("Loot Tables", ResourceType.LootTables, nameof(LootTable), typeof(LootTableViewModel), new ObservableCollection<dynamic>(ActivePackage.LootTables)),
                new ResourceGroup("Mods", ResourceType.Mods, nameof(Mod), typeof(ModViewModel), new ObservableCollection<dynamic>(ActivePackage.Mods)),
                new ResourceGroup("NPC Groups", ResourceType.NPCGroups, nameof(NPCGroup), typeof(NPCGroupViewModel), new ObservableCollection<dynamic>(ActivePackage.NPCGroups)),
                new ResourceGroup("Perk Categories", ResourceType.PerkCategories, nameof(PerkCategory), typeof(PerkCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.PerkCategories)),
                new ResourceGroup("Plants", ResourceType.Plants, nameof(Plant), typeof(PlantViewModel), new ObservableCollection<dynamic>(ActivePackage.Plants)),
                new ResourceGroup("Quests", ResourceType.Quests, nameof(Quest), typeof(QuestViewModel), new ObservableCollection<dynamic>(ActivePackage.Quests)),
                new ResourceGroup("Skill Categories", ResourceType.SkillCategories, nameof(SkillCategory), typeof(SkillCategoryViewModel), new ObservableCollection<dynamic>(ActivePackage.SkillCategories)),
                new ResourceGroup("Skills", ResourceType.Skills, nameof(Skill), typeof(SkillViewModel), new ObservableCollection<dynamic>(ActivePackage.Skills)),
                new ResourceGroup("Spawns", ResourceType.Spawns, nameof(Spawn), typeof(SpawnViewModel), new ObservableCollection<dynamic>(ActivePackage.Spawns)),
            };
            AvailableResources = new ObservableCollection<dynamic>();
            _addedResources = new ObservableCollection<dynamic>();

            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();
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



        private void LoadAvailableResources()
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

        }

        public void Cancel()
        {
            ActivePackage.Clear();
            AddedResources.Clear();
            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();
            TryClose();
        }
    }
}
