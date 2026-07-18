using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
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
        private string _targetDescription;
        private string _characterType;
        private string _credits;

        // Row DTOs replacing the hand-synced parallel GuiBindingList instances that
        // LoadTargetSkills/LoadTargetPerks/LoadTargetNotes used to build in lockstep.
        private sealed class SkillEntry
        {
            public string Name { get; }
            public int Level { get; }

            public SkillEntry(string name, int level)
            {
                Name = name;
                Level = level;
            }
        }

        private sealed class PerkEntry
        {
            public string Name { get; }
            public int Level { get; }

            public PerkEntry(string name, int level)
            {
                Name = name;
                Level = level;
            }
        }

        private sealed class NoteEntry
        {
            public string Id { get; }
            public string Name { get; }

            public NoteEntry(string id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        private static readonly GuiTableSource<DMPlayerExamineViewModel, SkillEntry> SkillsTable =
            new GuiTableSource<DMPlayerExamineViewModel, SkillEntry>()
                .Column((m, v) => m.SkillNames = v, r => r.Name)
                .Column((m, v) => m.SkillLevels = v, r => r.Level);

        private static readonly GuiTableSource<DMPlayerExamineViewModel, PerkEntry> PerksTable =
            new GuiTableSource<DMPlayerExamineViewModel, PerkEntry>()
                .Column((m, v) => m.PerkNames = v, r => r.Name)
                .Column((m, v) => m.PerkLevels = v, r => r.Level);

        private static readonly GuiTableSource<DMPlayerExamineViewModel, NoteEntry> NotesTable =
            new GuiTableSource<DMPlayerExamineViewModel, NoteEntry>()
                .Column((m, v) => m.NoteNames = v, r => r.Name)
                .Column((m, v) => m.NoteToggles = v, r => false);

        public const string PartialView = "PARTIAL";

        public const string DetailView = "DETAIL_VIEW";
        public const string SkillsView = "SKILLS_VIEW";
        public const string PerksView = "PERKS_VIEW";
        public const string NotesView = "NOTES_VIEW";

        public bool IsDetailsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSkillsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsPerksToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsNotesToggled
        {
            get => Get<bool>();
            set => Set(value);
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
            _targetDescription = GetDescription(initialPayload.Target);
            _characterType = GetClassByPosition(1, initialPayload.Target) == ClassType.ForceSensitive
                ? "Force Sensitive"
                : "Standard";
            _credits = $"{GetGold(initialPayload.Target)}cr";

            ActiveNoteName = string.Empty;
            ActiveNoteCreator = string.Empty;
            ActiveNoteDetail = string.Empty;

            IsDetailsToggled = true;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, DetailView);
            LoadTargetDetails();

            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteDetail);
        }

        private void LoadTargetDetails()
        {
            Name = _targetName;
            Description = _targetDescription;
            CharacterType = _characterType;
            Credits = _credits;
        }

        private void LoadTargetSkills()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var rows = new List<SkillEntry>();
            foreach (var (type, detail) in Skill.GetAllActiveSkills())
            {
                rows.Add(new SkillEntry(detail.Name, dbPlayer.Skills[type].Rank));
            }

            SkillsTable.Refresh(this, rows);
        }

        private void LoadTargetPerks()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var rows = new List<PerkEntry>();
            foreach (var (type, level) in dbPlayer.Perks)
            {
                var detail = Perk.GetPerkDetails(type);
                rows.Add(new PerkEntry(detail.Name, level));
            }

            PerksTable.Refresh(this, rows);
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

            var rows = new List<NoteEntry>();
            foreach (var note in dbNotes)
            {
                rows.Add(new NoteEntry(note.Id, note.Name));
            }

            // Row-index lookups (OnClickNote, OnClickDeleteNote, OnClickSaveChanges)
            // index into this in lockstep with the bound lists.
            _noteIds.Clear();
            foreach (var row in rows)
                _noteIds.Add(row.Id);

            NotesTable.Refresh(this, rows);
        }

        public Action OnClickDetails() => () =>
        {
            IsDetailsToggled = true;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, DetailView);
            LoadTargetDetails();
        };

        public Action OnClickSkills() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = true;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, SkillsView);
            LoadTargetSkills();
        };

        public Action OnClickPerks() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = true;
            IsNotesToggled = false;

            ChangePartialView(PartialView, PerksView);
            LoadTargetPerks();
        };

        public Action OnClickNotes() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = true;

            ChangePartialView(PartialView, NotesView);
            LoadTargetNotes();
        };

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
