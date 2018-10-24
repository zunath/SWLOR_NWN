using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DataSyncViewModel: 
        Screen, 
        IDataSyncViewModel,
        IHandle<DatabaseConnectionSucceeded>,
        IHandle<DatabaseConnectionFailed>,
        IHandle<DatabaseConnecting>
    {
        private IDatabaseConnectionViewModel _dbConnectionVm;
        private readonly IWindowManager _windowManager;
        private readonly IErrorViewModel _errorVM;
        private readonly AppSettings _appSettings;

        public DataSyncViewModel(
            IDatabaseConnectionViewModel dbConnectionVm,
            IWindowManager windowManager,
            IErrorViewModel errorVM,
            IEventAggregator eventAggregator,
            AppSettings appSettings)
        {
            _dbConnectionVm = dbConnectionVm;
            _windowManager = windowManager;
            _errorVM = errorVM;
            _appSettings = appSettings;

            ControlsEnabled = true;
            SyncVisibility = Visibility.Collapsed;
            eventAggregator.Subscribe(this);
        }

        public IDatabaseConnectionViewModel DatabaseConnectionVM
        {
            get => _dbConnectionVm;
            set
            {
                _dbConnectionVm = value;
                NotifyOfPropertyChange(() => DatabaseConnectionVM);
            }
        }

        private Visibility _dbConnectionViewVisibility;

        public Visibility DBConnectionViewVisibility
        {
            get => _dbConnectionViewVisibility;
            set
            {
                _dbConnectionViewVisibility = value;
                NotifyOfPropertyChange(() => DBConnectionViewVisibility);
            }
        }

        private bool _controlsEnabled;

        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set
            {
                _controlsEnabled = value;
                NotifyOfPropertyChange(() => ControlsEnabled);
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

        private Visibility _syncVisibility;

        public Visibility SyncVisibility
        {
            get => _syncVisibility;
            set
            {
                _syncVisibility = value;
                NotifyOfPropertyChange(() => SyncVisibility);
            }
        }

        public void Sync()
        {
            using (DataContext db = new DataContext(_appSettings.DatabaseIPAddress, _appSettings.DatabaseUsername, _appSettings.DatabasePassword, _appSettings.DatabaseName))
            {
                db.Configuration.LazyLoadingEnabled = false;

                WriteDataFileAsync(db.ApartmentBuildings.ToList());
                WriteDataFileAsync(db.BaseStructures.ToList());
                WriteDataFileAsync(db.BaseStructureTypes.ToList());
                WriteDataFileAsync(db.BuildingStyles.ToList());
                WriteDataFileAsync(db.ComponentTypes.ToList());
                WriteDataFileAsync(db.CooldownCategories.ToList());
                WriteDataFileAsync(db.CraftBlueprints.ToList(), "ItemName");
                WriteDataFileAsync(db.CraftBlueprintCategories.ToList());
                WriteDataFileAsync(db.CraftDevices.ToList());
                WriteDataFileAsync(db.CustomEffects.ToList());
                WriteDataFileAsync(db.Downloads.ToList());
                WriteDataFileAsync(db.FameRegions.ToList());
                WriteDataFileAsync(db.GameTopics.ToList());
                WriteDataFileAsync(db.GameTopicCategories.ToList());
                WriteDataFileAsync(db.GrowingPlants.ToList());
                WriteDataFileAsync(db.ItemTypes.ToList());
                WriteDataFileAsync(db.KeyItems.ToList());
                WriteDataFileAsync(db.KeyItemCategories.ToList());

                var lootTables = db.LootTables
                    .Include(i => i.LootTableItems)
                    .ToList();
                WriteDataFileAsync(lootTables);

                WriteDataFileAsync(db.Mods.ToList());
                WriteDataFileAsync(db.NPCGroups.ToList());

                var perks = db.Perks
                    .Include(i => i.PerkLevels.Select(x => x.PerkLevelQuestRequirements))
                    .Include(i => i.PerkLevels.Select(x => x.PerkLevelSkillRequirements))
                    .ToList();
                WriteDataFileAsync(perks);
                
                WriteDataFileAsync(db.PerkCategories.ToList());
                WriteDataFileAsync(db.Plants.ToList());

                var quests = db.Quests
                    .Include(i => i.QuestStates.Select(x => x.QuestKillTargetLists))
                    .Include(i => i.QuestStates.Select(x => x.QuestKillTargetLists))
                    .Include(i => i.QuestPrerequisites)
                    .Include(i => i.QuestStates.Select(x => x.QuestRequiredItemLists))
                    .Include(i => i.QuestStates.Select(x => x.QuestRequiredKeyItemLists))
                    .Include(i => i.QuestRewardItems)
                    .ToList();
                WriteDataFileAsync(quests);

                var skills = db.Skills
                    .Include(i => i.SkillXPRequirements)
                    .ToList();
                WriteDataFileAsync(skills);

                WriteDataFileAsync(db.SkillCategories.ToList());
                WriteDataFileAsync(db.Spawns.Include(i => i.SpawnObjects).ToList());

            }
        }

        private static void WriteDataFileAsync<T>(IEnumerable<T> set, string propertyName = "Name")
        {
            string Folder = typeof(T).Name;

            Parallel.ForEach(set, item =>
            {
                string fileName = (string) item.GetType().GetProperty(propertyName).GetValue(item, null);
                fileName = ReplaceSpecialCharacters(fileName);

                string json = JsonConvert.SerializeObject(item);
                File.WriteAllText("./Data/" + Folder + "/" + fileName + ".json", json);
            });

        }

        private static string ReplaceSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "_", RegexOptions.Compiled);
        }

        public void Handle(DatabaseConnectionSucceeded message)
        {
            DBConnectionViewVisibility = Visibility.Collapsed;
            SyncVisibility = Visibility.Visible;
            SyncEnabled = true;
        }

        public void Handle(DatabaseConnectionFailed message)
        {
            _windowManager.ShowDialog(_errorVM);
            ControlsEnabled = true;
        }

        public void Handle(DatabaseConnecting message)
        {
            ControlsEnabled = false;
        }
    }
}
