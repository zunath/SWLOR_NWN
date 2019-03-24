using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Extension;

using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AutoMapper;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Tools.Editor.Attributes;
using ApartmentBuildingViewModel = SWLOR.Tools.Editor.ViewModels.Data.ApartmentBuildingViewModel;
using LootTableItemViewModel = SWLOR.Tools.Editor.ViewModels.Data.LootTableItemViewModel;
using Screen = Caliburn.Micro.Screen;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DataSyncViewModel :
        Screen,
        IDataSyncViewModel,
        IHandle<DatabaseConnectionSucceeded>,
        IHandle<DatabaseConnectionFailed>,
        IHandle<DatabaseConnecting>
    {
        private IDatabaseConnectionViewModel _dbConnectionVm;
        private readonly IWindowManager _windowManager;
        private readonly IErrorViewModel _errorVM;
        private readonly IYesNoViewModel _yesNo;
        private readonly IEventAggregator _eventAggregator;
        
        private readonly BackgroundWorker _worker;

        public DataSyncViewModel(
            IDatabaseConnectionViewModel dbConnectionVm,
            IWindowManager windowManager,
            IErrorViewModel errorVM,
            IYesNoViewModel yesNo,
            IEventAggregator eventAggregator)
        {
            _dbConnectionVm = dbConnectionVm;
            _windowManager = windowManager;
            _errorVM = errorVM;
            _yesNo = yesNo;
            _eventAggregator = eventAggregator;

            _worker = new BackgroundWorker();
            _worker.DoWork += PerformDataSyncAsync;
            _worker.RunWorkerCompleted += DataSyncCompleted;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += SyncProgressChanged;

            ProgressMax = 100;

            IsCancelEnabled = false;
            DatabaseControlsEnabled = true;
            _eventAggregator.Subscribe(this);
        }


        // WARNING: This is on the worker thread, not the UI thread.
        private int _progress = 0;
        // END WARNING

        public IDatabaseConnectionViewModel DatabaseConnectionVM
        {
            get => _dbConnectionVm;
            set
            {
                _dbConnectionVm = value;
                NotifyOfPropertyChange(() => DatabaseConnectionVM);
            }
        }

        private int _progressMax;

        public int ProgressMax
        {
            get => _progressMax;
            set
            {
                _progressMax = value;
                NotifyOfPropertyChange(() => ProgressMax);
            }
        }

        private int _currentProgress;

        public int CurrentProgress
        {
            get => _currentProgress;
            set
            {
                _currentProgress = value;
                NotifyOfPropertyChange(() => CurrentProgress);
            }
        }

        private bool _isCancelEnabled;

        public bool IsCancelEnabled
        {
            get => _isCancelEnabled;
            set
            {
                _isCancelEnabled = value;
                NotifyOfPropertyChange(() => IsCancelEnabled);
            }
        }

        private bool _databaseControlsEnabled;

        public bool DatabaseControlsEnabled
        {
            get => _databaseControlsEnabled;
            set
            {
                _databaseControlsEnabled = value;
                NotifyOfPropertyChange(() => DatabaseControlsEnabled);
            }
        }

        private bool _syncEnabled;

        public bool SyncEnabled
        {
            get => _syncEnabled;
            set
            {
                _syncEnabled = value;
                NotifyOfPropertyChange(() => SyncEnabled);
            }
        }

        public void Sync()
        {
            _yesNo.Prompt = "WARNING: This will overwrite any local changes you have made. It's highly suggested you back up your data files or push them to the server before syncing. Are you sure you want to continue?";
            _windowManager.ShowDialog(_yesNo);

            if (_yesNo.Result == DialogResult.Yes)
            {
                IsCancelEnabled = false;
                SyncEnabled = false;
                _worker.RunWorkerAsync();
            }
        }

        private delegate T MapObjectDelegate<T>(T source);

        private List<T2> BuildViewModel<T1, T2>(IEnumerable<T1> source, MapObjectDelegate<T2> mappingRule = null)
            where T1: IEntity
            where T2: DBObjectViewModelBase
        {
            var mapped = new List<T2>();
            foreach (var record in source)
            {
                var mappedRecord = Mapper.Map<T1, T2>(record);
                var finalRecord = mappedRecord;
                if (mappingRule != null)
                {
                    finalRecord = mappingRule.Invoke(mappedRecord);
                }

                mapped.Add(finalRecord);
            }

            return mapped;
        }

        private List<LootTableItem> _lootTableItems;
        private List<KeyItem> _keyItems;
        private void PerformDataSyncAsync(object sender, DoWorkEventArgs e)
        {
            _progress = 0;
            _lootTableItems = DataService.GetAll<LootTableItem>().ToList();
            _keyItems = DataService.GetAll<KeyItem>().ToList();

            WriteDataFileAsync(BuildViewModel<ApartmentBuilding, ApartmentBuildingViewModel>(DataService.GetAll<ApartmentBuilding>()));
            WriteDataFileAsync(BuildViewModel<BaseStructure, BaseStructureViewModel>(DataService.GetAll<BaseStructure>()));
            WriteDataFileAsync(BuildViewModel<BuildingStyle, BuildingStyleViewModel>(DataService.GetAll<BuildingStyle>()));
            WriteDataFileAsync(BuildViewModel<CooldownCategory, CooldownCategoryViewModel>(DataService.GetAll<CooldownCategory>()));
            WriteDataFileAsync(BuildViewModel<CraftBlueprint, CraftBlueprintViewModel>(DataService.GetAll<CraftBlueprint>()));
            WriteDataFileAsync(BuildViewModel<CraftBlueprintCategory, CraftBlueprintCategoryViewModel>(DataService.GetAll<CraftBlueprintCategory>()));
            WriteDataFileAsync(BuildViewModel<CraftDevice, CraftDeviceViewModel>(DataService.GetAll<CraftDevice>()));
            WriteDataFileAsync(BuildViewModel<CustomEffect, CustomEffectViewModel>(DataService.GetAll<CustomEffect>()));
            WriteDataFileAsync(BuildViewModel<Download, DownloadViewModel>(DataService.GetAll<Download>()));
            WriteDataFileAsync(BuildViewModel<FameRegion, FameRegionViewModel>(DataService.GetAll<FameRegion>()));
            WriteDataFileAsync(BuildViewModel<GameTopic, GameTopicViewModel>(DataService.GetAll<GameTopic>()));
            WriteDataFileAsync(BuildViewModel<GameTopicCategory, GameTopicCategoryViewModel>(DataService.GetAll<GameTopicCategory>()));
            WriteDataFileAsync(BuildViewModel<KeyItemCategory, KeyItemCategoryViewModel>(DataService.GetAll<KeyItemCategory>(), KeyItemCategoryMapping));
            WriteDataFileAsync(BuildViewModel<LootTable, LootTableViewModel>(DataService.GetAll<LootTable>(), LootTableMapping));
            WriteDataFileAsync(BuildViewModel<NPCGroup, NPCGroupViewModel>(DataService.GetAll<NPCGroup>()));
            WriteDataFileAsync(BuildViewModel<Perk, PerkViewModel>(DataService.GetAll<Perk>()));
            WriteDataFileAsync(BuildViewModel<PerkCategory, PerkCategoryViewModel>(DataService.GetAll<PerkCategory>()));
            WriteDataFileAsync(BuildViewModel<Plant, PlantViewModel>(DataService.GetAll<Plant>()));
            WriteDataFileAsync(BuildViewModel<Quest, QuestViewModel>(DataService.GetAll<Quest>()));
            WriteDataFileAsync(BuildViewModel<Skill, SkillViewModel>(DataService.GetAll<Skill>()));
            WriteDataFileAsync(BuildViewModel<SkillCategory, SkillCategoryViewModel>(DataService.GetAll<SkillCategory>()));
            WriteDataFileAsync(BuildViewModel<Spawn, SpawnViewModel>(DataService.GetAll<Spawn>()));
        }

        private LootTableViewModel LootTableMapping(LootTableViewModel source)
        {
            var items = _lootTableItems.Where(x => x.LootTableID == source.ID);

            foreach (var item in items)
            {
                var itemVM = Mapper.Map<LootTableItem, LootTableItemViewModel>(item);
                source.LootTableItems.Add(itemVM);
            }

            return source;
        }

        private KeyItemCategoryViewModel KeyItemCategoryMapping(KeyItemCategoryViewModel source)
        {
            var items = _keyItems.Where(x => x.KeyItemCategoryID == source.ID);

            foreach (var item in items)
            {
                var itemVM = Mapper.Map<KeyItem, KeyItemViewModel>(item);
                source.KeyItems.Add(itemVM);
            }

            return source;
        }

        private void SyncProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CurrentProgress = e.ProgressPercentage;
        }

        private void DataSyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _errorVM.ErrorDetails = e.Error.ToMessageAndCompleteStacktrace();
                _windowManager.ShowDialog(_errorVM);
            }
            else
            {
                TryClose();
            }

            CurrentProgress = 0;
            SyncEnabled = true;
            IsCancelEnabled = true;
        }

        private void WriteDataFileAsync<T>(IEnumerable<T> set)
        {
            string Folder = ((FolderAttribute)typeof(T).GetCustomAttributes(typeof(FolderAttribute), false).First()).Folder;
            string path = "./Data/" + Folder + "/";
            string[] files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                File.Delete(file);
            }

            foreach (var record in set)
            {
                JObject jObj = JObject.FromObject(record);
                jObj[nameof(DBObjectViewModelBase.InternalEditorID)] = Guid.NewGuid().ToString();
                string fileName = Guid.NewGuid().ToString();
                string json = JsonConvert.SerializeObject(jObj);
                File.WriteAllText("./Data/" + Folder + "/" + fileName + ".json", json);
            }

            _progress++;
            int percentDone = Convert.ToInt32(_progress / 25.0f * 100);
            _worker.ReportProgress(percentDone);
            _eventAggregator.PublishOnBackgroundThread(new DataObjectsLoadedFromDisk(Folder));
        }

        public void Cancel()
        {
            TryClose();
        }

        public void Handle(DatabaseConnectionSucceeded message)
        {
            SyncEnabled = true;
        }

        public void Handle(DatabaseConnectionFailed message)
        {
            _errorVM.ErrorDetails = message.Exception.ToMessageAndCompleteStacktrace();
            _windowManager.ShowDialog(_errorVM);
            DatabaseControlsEnabled = true;
        }

        public void Handle(DatabaseConnecting message)
        {
            DatabaseControlsEnabled = false;
        }
    }
}
