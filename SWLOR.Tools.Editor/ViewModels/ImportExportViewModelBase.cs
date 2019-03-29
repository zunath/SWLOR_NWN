using System.Collections.ObjectModel;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.ViewModels.Data;
using Screen = Caliburn.Micro.Screen;

namespace SWLOR.Tools.Editor.ViewModels
{
    public abstract class ImportExportViewModelBase :
        Screen
    {
        protected ImportExportViewModelBase()
        {
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



        public abstract void LoadAvailableResources();

        private void LoadAddedResources()
        {
            AddedResources.Clear();
            var set = SelectedAddedResourceGroup.TargetCollection;
            foreach(var item in set)
            {
                AddedResources.Add(item);
            }
        }
        
        protected virtual void Notify()
        {
            NotifyOfPropertyChange(() => AddedResources);
            NotifyOfPropertyChange(() => IsAddEnabled);
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

        protected virtual void ClearData()
        {
            PackageName = string.Empty;
            AddedResources.Clear();
            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();

            foreach (var group in ResourceGroups)
            {
                group.SourceCollection.Clear();
                group.TargetCollection.Clear();
            }

            Notify();
        }
        
        public void Cancel()
        {
            ClearData();
            TryClose();
        }
    }
}
