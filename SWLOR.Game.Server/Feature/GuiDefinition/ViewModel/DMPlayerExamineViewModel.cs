using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMPlayerExamineViewModel: GuiViewModelBase<DMPlayerExamineViewModel, DMPlayerExaminePayload>
    {
        private const int MaxNotes = 50;

        [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
        public static void ExaminePlayer()
        {
            var dm = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));

            if (!GetIsDM(dm) && !GetIsDMPossessed(dm))
                return;

            if (!GetIsPC(target) && !GetIsDM(target) && !GetIsDMPossessed(target))
                return;

            var payload = new DMPlayerExaminePayload(target);

            SetGuiPanelDisabled(dm, GuiPanel.ExamineCreature, true);
            Gui.TogglePlayerWindow(dm, GuiWindowType.DMPlayerExamine, payload);
            DelayCommand(1f, () => SetGuiPanelDisabled(dm, GuiPanel.ExamineCreature, false));
        }

        private string _playerId;
        private string _targetName;
        private string _targetDescriptor;
        private string _targetAccountName;
        private string _targetPublicCDKey;
        private string _targetDescription;
        private string _characterType;
        private string _credits;

        public const string PartialView = "PARTIAL";

        public const string DetailView = "DETAIL_VIEW";
        public const string SkillsView = "SKILLS_VIEW";
        public const string PerksView = "PERKS_VIEW";
        public const string NotesView = "NOTES_VIEW";

        private const int DetailsTab = 0;
        private const int SkillsTab = 1;
        private const int PerksTab = 2;
        private const int NotesTab = 3;

        private static readonly GuiTabGroup<DMPlayerExamineViewModel, DMPlayerExaminePayload> Tabs =
            new GuiTabGroup<DMPlayerExamineViewModel, DMPlayerExaminePayload>()
                .AddTab(DetailsTab, DetailView)
                .AddTab(SkillsTab, SkillsView)
                .AddTab(PerksTab, PerksView)
                .AddTab(NotesTab, NotesView);

        private readonly GuiToggleGroupSync _tabToggles = new(DetailsTab, SkillsTab, PerksTab, NotesTab);
        private int _selectedTabId = -1;

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                _tabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public bool IsNoteSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Descriptor
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AccountName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PublicCDKey
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CharacterType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Credits
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> SkillNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> SkillLevels
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> PerkLevels
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        private readonly List<string> _noteIds = new();
        private int _selectedIndex;

        public GuiBindingList<string> NoteNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> NoteToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string ActiveNoteName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveNoteCreator
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveNoteDetail
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(DMPlayerExaminePayload initialPayload)
        {
            _selectedIndex = -1;
            _playerId = GetObjectUUID(initialPayload.Target);
            _targetName = GetName(initialPayload.Target);
            _targetDescriptor = Disguise.GetDisplayDescriptor(initialPayload.Target);
            _targetAccountName = GetPCPlayerName(initialPayload.Target);
            _targetPublicCDKey = GetPCPublicCDKey(initialPayload.Target);
            _targetDescription = GetDescription(initialPayload.Target);
            _characterType = GetClassByPosition(1, initialPayload.Target) == ClassType.ForceSensitive
                ? "Force Sensitive"
                : "Standard";
            _credits = $"{GetGold(initialPayload.Target)}cr";

            ActiveNoteName = string.Empty;
            ActiveNoteCreator = string.Empty;
            ActiveNoteDetail = string.Empty;
            IsNoteSelected = false;
            SkillNames = new GuiBindingList<string>();
            SkillLevels = new GuiBindingList<int>();
            PerkNames = new GuiBindingList<string>();
            PerkLevels = new GuiBindingList<int>();
            NoteNames = new GuiBindingList<string>();
            NoteToggles = new GuiBindingList<bool>();

            LoadTargetDetails();
            _tabToggles.SyncTo(DetailsTab, value => TabToggleValue = value);

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteDetail);

            SelectTab(DetailsTab);
        }

        private void LoadTargetDetails()
        {
            Name = _targetName;
            Descriptor = _targetDescriptor;
            AccountName = _targetAccountName;
            PublicCDKey = _targetPublicCDKey;
            Description = _targetDescription;
            CharacterType = _characterType;
            Credits = _credits;
        }

        private void LoadTargetSkills()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var skillNames = new GuiBindingList<string>();
            var skillLevels = new GuiBindingList<int>();
            foreach (var (type, detail) in Skill.GetAllActiveSkills())
            {
                skillNames.Add(detail.Name);
                skillLevels.Add(dbPlayer.Skills[type].Rank);
            }

            SkillNames = skillNames;
            SkillLevels = skillLevels;
        }

        private void LoadTargetPerks()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var perkNames = new GuiBindingList<string>();
            var perkLevels = new GuiBindingList<int>();
            foreach (var (type, level) in dbPlayer.Perks)
            {
                var detail = Perk.GetPerkDetails(type);
                perkNames.Add(detail.Name);
                perkLevels.Add(level);
            }

            PerkNames = perkNames;
            PerkLevels = perkLevels;
        }

        private void LoadTargetNotes()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), _playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true);
            var dbNotes = DB.Search(query);

            _noteIds.Clear();
            var noteNames = new GuiBindingList<string>();
            var noteToggles = new GuiBindingList<bool>();

            foreach (var note in dbNotes)
            {
                _noteIds.Add(note.Id);
                noteNames.Add(note.Name);
                noteToggles.Add(false);
            }

            NoteNames = noteNames;
            NoteToggles = noteToggles;
        }

        private void SelectTab(int tabId)
        {
            if (_selectedTabId == tabId)
                return;

            _selectedTabId = tabId;
            _tabToggles.SyncTo(tabId, value => TabToggleValue = value);
            switch (tabId)
            {
                case DetailsTab:
                    LoadTargetDetails();
                    break;
                case SkillsTab:
                    LoadTargetSkills();
                    break;
                case PerksTab:
                    LoadTargetPerks();
                    break;
                case NotesTab:
                    LoadTargetNotes();
                    break;
            }

            Tabs.Select(this, PartialView, tabId);
        }

        protected override void OnModalClosedRestore() => Tabs.Select(this, PartialView, _selectedTabId);

        public Action OnClickNote() => () =>
        {
            if(_selectedIndex > -1)
                NoteToggles[_selectedIndex] = false;
            _selectedIndex = NuiGetEventArrayIndex();

            var index = NuiGetEventArrayIndex();
            var noteId = _noteIds[index];
            var dbNote = DB.Get<PlayerNote>(noteId);

            ActiveNoteName = dbNote.Name;
            ActiveNoteCreator = $"{dbNote.DMCreatorName} [{dbNote.DMCreatorCDKey}]";
            ActiveNoteDetail = dbNote.Text;

            NoteToggles[_selectedIndex] = true;
            IsNoteSelected = true;
        };

        public Action OnClickNewNote() => () =>
        {
            if (_noteIds.Count > MaxNotes)
                return;

            var dbNote = new PlayerNote
            {
                PlayerId = _playerId,
                Name = "New Note",
                Text = string.Empty,
                IsDMNote = true,
                DMCreatorCDKey = GetPCPublicCDKey(Player),
                DMCreatorName = GetName(Player)
            };

            DB.Set(dbNote);

            _noteIds.Add(dbNote.Id);
            NoteNames.Add(dbNote.Name);
            NoteToggles.Add(false);
        };

        public Action OnClickDeleteNote() => () =>
        {
            if (_selectedIndex <= -1)
                return;

            ShowModal("Are you sure you want to delete this note?", () =>
            {
                var noteId = _noteIds[_selectedIndex];
                DB.Delete<PlayerNote>(noteId);

                NoteToggles[_selectedIndex] = false;

                NoteNames.RemoveAt(_selectedIndex);
                NoteToggles.RemoveAt(_selectedIndex);
                _noteIds.RemoveAt(_selectedIndex);

                _selectedIndex = -1;

                IsNoteSelected = false;
            });
        };

        public Action OnClickSaveChanges() => () =>
        {
            if (_selectedIndex <= -1)
                return;

            var noteId = _noteIds[_selectedIndex];
            var dbNote = DB.Get<PlayerNote>(noteId);

            dbNote.Name = ActiveNoteName;
            dbNote.Text = ActiveNoteDetail;

            DB.Set(dbNote);

            NoteNames[_selectedIndex] = ActiveNoteName;

        };
    }
}
