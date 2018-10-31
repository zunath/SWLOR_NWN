using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AutoMapper;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ImportViewModel:
        ImportExportViewModelBase,
        IImportViewModel
    {
        private readonly OpenFileDialog _openFile;
        private readonly IEventAggregator _eventAggregator;

        public ImportViewModel(IEventAggregator eventAggregator)
        {
            _openFile = new OpenFileDialog();
            _openFile.Filter = @"JSON File (*.json)|*.json";

            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        public bool IsImportEnabled
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

        private ImportExportPackageViewModel _activePackage;
        public ImportExportPackageViewModel ActivePackage
        {
            get => _activePackage;
            set
            {
                _activePackage = value;
                NotifyOfPropertyChange(() => ActivePackage);
                NotifyOfPropertyChange(() => IsPackageLoaded);
            }
        }

        public bool IsPackageLoaded => ActivePackage != null;

        public void SelectPackageToImport()
        {
            var result = _openFile.ShowDialog();
            if (result == DialogResult.Cancel) return;

            AvailableResources.Clear();
            AddedResources.Clear();

            string json = File.ReadAllText(_openFile.FileName);
            ActivePackage = JsonConvert.DeserializeObject<ImportExportPackageViewModel>(json);
            PackageName = ActivePackage.PackageName;
            PopulateDataSources();
            LoadAvailableResources();

            SelectedAvailableResourceGroup = ResourceGroups.First();
            SelectedAddedResourceGroup = ResourceGroups.First();
        }

        public void Import()
        {
            foreach (var group in ResourceGroups)
            {
                foreach (var item in group.TargetCollection)
                {
                    var message = new DataObjectImported(group.ResourceType, item);
                    _eventAggregator.PublishOnUIThread(message);
                }
            }

            ClearData();
            TryClose();
        }

        protected override void ClearData()
        {
            ActivePackage = null;
            base.ClearData();
        }

        protected override void Notify()
        {
            base.Notify();

            NotifyOfPropertyChange(() => IsImportEnabled);
        }

        public override void LoadAvailableResources()
        {
            AvailableResources.Clear();
            if (SelectedAvailableResourceGroup == null || ActivePackage == null) return;

            foreach (var item in SelectedAvailableResourceGroup.SourceCollection)
            {
                AvailableResources.Add(item);
            }

            Notify();
        }

        private void PopulateDataSources()
        {
            foreach (var group in ResourceGroups)
            {
                group.SourceCollection.Clear();
                group.TargetCollection.Clear();
                AddResources(group.ResourceType, group.SourceCollection, group.TargetCollection);
            }
        }
        
        private void AddMappedResource<T1, T2>(IEnumerable<T1> source, ObservableCollection<dynamic> sourceCollection, ObservableCollection<dynamic> targetCollection)
            where T2: IDBObjectViewModel
        {
            foreach (var item in source)
            {
                var vm = Mapper.Map<T1, T2>(item);
                vm.InternalEditorID = Guid.NewGuid().ToString();
                sourceCollection.Add(vm);
                targetCollection.Add(vm);
            }
        }

        private void AddResources(ResourceType resourceType, ObservableCollection<dynamic> sourceCollection, ObservableCollection<dynamic> targetCollection)
        {
            switch (resourceType)
            {
                case ResourceType.ApartmentBuildings:
                    AddMappedResource<ApartmentBuilding, ApartmentBuildingViewModel>(ActivePackage.ApartmentBuildings, sourceCollection, targetCollection);
                    break;
                case ResourceType.BaseStructures:
                    AddMappedResource<BaseStructure, BaseStructureViewModel>(ActivePackage.BaseStructures, sourceCollection, targetCollection);
                    break;
                case ResourceType.BuildingStyles:
                    AddMappedResource<BuildingStyle, BuildingStyleViewModel>(ActivePackage.BuildingStyles, sourceCollection, targetCollection);
                    break;
                case ResourceType.CooldownCategories:
                    AddMappedResource<CooldownCategory, CooldownCategoryViewModel>(ActivePackage.CooldownCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.CraftBlueprintCategories:
                    AddMappedResource<CraftBlueprintCategory, CraftBlueprintCategoryViewModel>(ActivePackage.CraftBlueprintCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.CraftBlueprints:
                    AddMappedResource<CraftBlueprint, CraftBlueprintViewModel>(ActivePackage.CraftBlueprints, sourceCollection, targetCollection);
                    break;
                case ResourceType.CraftDevices:
                    AddMappedResource<CraftDevice, CraftDeviceViewModel>(ActivePackage.CraftDevices, sourceCollection, targetCollection);
                    break;
                case ResourceType.CustomEffects:
                    AddMappedResource<CustomEffect, CustomEffectViewModel>(ActivePackage.CustomEffects, sourceCollection, targetCollection);
                    break;
                case ResourceType.Downloads:
                    AddMappedResource<Download, DownloadViewModel>(ActivePackage.Downloads, sourceCollection, targetCollection);
                    break;
                case ResourceType.FameRegions:
                    AddMappedResource<FameRegion, FameRegionViewModel>(ActivePackage.FameRegions, sourceCollection, targetCollection);
                    break;
                case ResourceType.GameTopicCategories:
                    AddMappedResource<GameTopicCategory, GameTopicCategoryViewModel>(ActivePackage.GameTopicCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.GameTopics:
                    AddMappedResource<GameTopic, GameTopicViewModel>(ActivePackage.GameTopics, sourceCollection, targetCollection);
                    break;
                case ResourceType.KeyItemCategories:
                    AddMappedResource<KeyItemCategory, KeyItemCategoryViewModel>(ActivePackage.KeyItemCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.KeyItems:
                    AddMappedResource<KeyItem, KeyItemViewModel>(ActivePackage.KeyItems, sourceCollection, targetCollection);
                    break;
                case ResourceType.LootTableItems:
                    AddMappedResource<LootTableItem, LootTableItemViewModel>(ActivePackage.LootTableItems, sourceCollection, targetCollection);
                    break;
                case ResourceType.LootTables:
                    AddMappedResource<LootTable, LootTableViewModel>(ActivePackage.LootTables, sourceCollection, targetCollection);
                    break;
                case ResourceType.Mods:
                    AddMappedResource<Mod, ModViewModel>(ActivePackage.Mods, sourceCollection, targetCollection);
                    break;
                case ResourceType.NPCGroups:
                    AddMappedResource<NPCGroup, NPCGroupViewModel>(ActivePackage.NPCGroups, sourceCollection, targetCollection);
                    break;
                case ResourceType.PerkCategories:
                    AddMappedResource<PerkCategory, PerkCategoryViewModel>(ActivePackage.PerkCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.Plants:
                    AddMappedResource<Plant, PlantViewModel>(ActivePackage.Plants, sourceCollection, targetCollection);
                    break;
                case ResourceType.Quests:
                    AddMappedResource<Quest, QuestViewModel>(ActivePackage.Quests, sourceCollection, targetCollection);
                    break;
                case ResourceType.SkillCategories:
                    AddMappedResource<SkillCategory, SkillCategoryViewModel>(ActivePackage.SkillCategories, sourceCollection, targetCollection);
                    break;
                case ResourceType.Skills:
                    AddMappedResource<Skill, SkillViewModel>(ActivePackage.Skills, sourceCollection, targetCollection);
                    break;
                case ResourceType.Spawns:
                    AddMappedResource<Spawn, SpawnViewModel>(ActivePackage.Spawns, sourceCollection, targetCollection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


    }
}
