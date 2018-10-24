using System.Collections;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DataSyncViewModel: 
        Screen, 
        IDataSyncViewModel,
        IHandle<DatabaseConnectionSucceededMessage>,
        IHandle<DatabaseConnectionFailedMessage>,
        IHandle<DatabaseConnectingMessage>
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
                WriteDataFile<ApartmentBuilding>(db.ApartmentBuildings.ToList());
                WriteDataFile<BaseStructure>(db.BaseStructures.ToList());
                WriteDataFile<BaseStructureType>(db.BaseStructureTypes.ToList());
                WriteDataFile<BuildingStyle>(db.BuildingStyles.ToList());
                WriteDataFile<ComponentType>(db.ComponentTypes.ToList());
                WriteDataFile<CooldownCategory>(db.CooldownCategories.ToList());
                WriteDataFile<CraftBlueprint>(db.CraftBlueprints.ToList(), "ItemName");
                WriteDataFile<CraftBlueprintCategory>(db.CraftBlueprintCategories.ToList());
                WriteDataFile<CraftDevice>(db.CraftDevices.ToList());
                WriteDataFile<CustomEffect>(db.CustomEffects.ToList());
                WriteDataFile<Download>(db.Downloads.ToList());
                WriteDataFile<FameRegion>(db.FameRegions.ToList());
                WriteDataFile<GameTopic>(db.GameTopics.ToList());
                WriteDataFile<GameTopicCategory>(db.GameTopicCategories.ToList());
                WriteDataFile<GrowingPlant>(db.GrowingPlants.ToList());
                WriteDataFile<ItemType>(db.ItemTypes.ToList());
                WriteDataFile<KeyItem>(db.KeyItems.ToList());
                WriteDataFile<KeyItemCategory>(db.KeyItemCategories.ToList());
                WriteDataFile<LootTable>(db.LootTables.ToList());
                //WriteDataFile<LootTableItem>(db.LootTableItems.ToList(), "Resref");
                WriteDataFile<Mod>(db.Mods.ToList());
                WriteDataFile<NPCGroup>(db.NPCGroups.ToList());
                WriteDataFile<Perk>(db.Perks.ToList());
                WriteDataFile<PerkCategory>(db.PerkCategories.ToList());
                //WriteDataFile<PerkLevel>(db.PerkLevels.ToList(), "PerkLevelID");
                //WriteDataFile<PerkLevelQuestRequirement>(db.PerkLevelQuestRequirements.ToList(), "PerkLevelQuestRequirementID");
                //WriteDataFile<PerkLevelSkillRequirement>(db.PerkLevelSkillRequirements.ToList(), "PerkLevelSkillRequirementID");
                WriteDataFile<Plant>(db.Plants.ToList());
                WriteDataFile<Quest>(db.Quests.ToList());
                //WriteDataFile<QuestKillTargetList>(db.QuestKillTargetLists.ToList(), "QuestKillTargetListID");
                //WriteDataFile<QuestPrerequisite>(db.QuestPrerequisites.ToList(), "QuestPrerequisiteID");
                //WriteDataFile<QuestRequiredItemList>(db.QuestRequiredItemLists.ToList());
                //WriteDataFile<QuestRequiredKeyItemList>(db.QuestRequiredKeyItemLists.ToList());
                //WriteDataFile<QuestRewardItem>(db.QuestRewardItems.ToList());
                //WriteDataFile<QuestState>(db.QuestStates.ToList());
                WriteDataFile<Skill>(db.Skills.ToList());
                WriteDataFile<SkillCategory>(db.SkillCategories.ToList());
                //WriteDataFile<SkillXPRequirement>(db.SkillXPRequirements.ToList());
                WriteDataFile<Spawn>(db.Spawns.ToList());
                //WriteDataFile<SpawnObject>(db.SpawnObjects.ToList());

            }
        }

        private static void WriteDataFile<T>(IEnumerable set, string propertyName = "Name")
        {
            string Folder = typeof(T).Name;

            foreach (var item in set)
            {
                string fileName = (string)item.GetType().GetProperty(propertyName).GetValue(item, null);
                fileName = ReplaceSpecialCharacters(fileName);

                string json = JsonConvert.SerializeObject(item);
                File.WriteAllText("./Data/" + Folder + "/" + fileName + ".json", json);
            }

        }

        private static string ReplaceSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "_", RegexOptions.Compiled);
        }

        public void Handle(DatabaseConnectionSucceededMessage message)
        {
            DBConnectionViewVisibility = Visibility.Collapsed;
            SyncVisibility = Visibility.Visible;
            SyncEnabled = true;
        }

        public void Handle(DatabaseConnectionFailedMessage message)
        {
            _windowManager.ShowDialog(_errorVM);
            ControlsEnabled = true;
        }

        public void Handle(DatabaseConnectingMessage message)
        {
            ControlsEnabled = false;
        }
    }
}
