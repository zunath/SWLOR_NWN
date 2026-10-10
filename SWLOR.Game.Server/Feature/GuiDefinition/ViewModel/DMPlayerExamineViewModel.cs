using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
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

            EventsPlugin.SkipEvent();
            Gui.ClosePlayerWindow(dm, GuiWindowType.DMPlayerExamine);
            Gui.TogglePlayerWindow(dm, GuiWindowType.DMPlayerExamine, payload);
        }

        private string _playerId;
        private uint _target;
        private bool _initialized;

        public const string PartialView = "PARTIAL";
        public const string DetailView = "DETAIL_VIEW";
        public const string SkillsView = "SKILLS_VIEW";
        public const string PerksView = "PERKS_VIEW";
        public const string EffectsView = "EFFECTS_VIEW";
        public const string NotesView = "NOTES_VIEW";

        private static readonly GuiTabGroup<DMPlayerExamineViewModel, DMPlayerExaminePayload> Tabs =
            new GuiTabGroup<DMPlayerExamineViewModel, DMPlayerExaminePayload>()
                .AddTab(0, DetailView, model => model.LoadTargetDetails())
                .AddTab(1, SkillsView, model => model.LoadTargetSkills())
                .AddTab(2, PerksView, model => model.LoadTargetPerks())
                .AddTab(3, EffectsView, model => model.LoadTargetEffects())
                .AddTab(4, NotesView, model => model.LoadTargetNotes());
        private readonly GuiToggleGroupSync _topToggles = new(0, 1, 2);
        private readonly GuiToggleGroupSync _bottomToggles = new(3, 4);
        private int _selectedTabId;

        public int TopTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_initialized)
                    _topToggles.HandleClientChange(value, SelectTab);
            }
        }

        public int BottomTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_initialized)
                    _bottomToggles.HandleClientChange(value, SelectTab);
            }
        }

        public GuiBindingList<GuiComboEntry> SkillCategories { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public GuiBindingList<GuiComboEntry> PerkCategories { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public string SkillSummary { get => Get<string>(); set => Set(value); }
        public string PerkSummary { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> SkillDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> PerkDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public string PortraitResref { get => Get<string>(); set => Set(value); }
        public string Health { get => Get<string>(); set => Set(value); }
        public string FP { get => Get<string>(); set => Set(value); }
        public string STM { get => Get<string>(); set => Set(value); }
        public string AlignmentText { get => Get<string>(); set => Set(value); }
        public string RPXP { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> EffectNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EffectDurations { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EffectSources { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EffectDetails { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public string EffectSummary { get => Get<string>(); set => Set(value); }

        public int SelectedSkillCategoryId
        {
            get => Get<int>();
            set { Set(value); if (_initialized) LoadTargetSkills(); }
        }
        public string SkillSearchText
        {
            get => Get<string>();
            set { Set(value); if (_initialized) LoadTargetSkills(); }
        }
        public int SelectedPerkCategoryId
        {
            get => Get<int>();
            set { Set(value); if (_initialized) LoadTargetPerks(); }
        }
        public int SelectedPerkSortOrderId
        {
            get => Get<int>();
            set { Set(value); if (_initialized) LoadTargetPerks(); }
        }
        public int SelectedPerkStatusId
        {
            get => Get<int>();
            set { Set(value); if (_initialized) LoadTargetPerks(); }
        }
        public string PerkSearchText
        {
            get => Get<string>();
            set { Set(value); if (_initialized) LoadTargetPerks(); }
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

        public string Descriptor
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TrueName
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

        public GuiBindingList<string> SkillLevels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkLevels
        {
            get => Get<GuiBindingList<string>>();
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
            _initialized = false;
            _selectedIndex = -1;
            _target = initialPayload.Target;
            _playerId = GetObjectUUID(_target);
            SkillSearchText = string.Empty;
            PerkSearchText = string.Empty;
            SelectedSkillCategoryId = 0;
            SelectedPerkCategoryId = 0;
            SelectedPerkSortOrderId = 2;
            SelectedPerkStatusId = 1;
            TopTabId = 0;
            BottomTabId = -1;

            var skillCategories = new GuiBindingList<GuiComboEntry> { new("All Skills", 0) };
            foreach (var (type, detail) in Skill.GetAllActiveSkillCategories().OrderBy(x => x.Value.Sequence))
                skillCategories.Add(new GuiComboEntry(detail.Name, (int)type));
            SkillCategories = skillCategories;
            var perkCategories = new GuiBindingList<GuiComboEntry> { new("All Categories", 0) };
            var categories = Perk.GetAllActivePerkCategories(PerkGroupType.Player);
            var dbPlayer = DB.Get<Player>(_playerId);
            if (dbPlayer != null)
            {
                foreach (var type in dbPlayer.Perks.Keys)
                {
                    if (Perk.TryGetPerkDetails(type, out var detail))
                        categories[detail.Category] = Perk.GetPerkCategoryDetails(detail.Category);
                }
            }
            foreach (var (type, detail) in categories.OrderBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase))
                perkCategories.Add(new GuiComboEntry(detail.Name, (int)type));
            PerkCategories = perkCategories;

            ActiveNoteName = string.Empty;
            ActiveNoteCreator = string.Empty;
            ActiveNoteDetail = string.Empty;
            IsNoteSelected = false;
            _noteIds.Clear();
            NoteNames = new GuiBindingList<string>();
            NoteToggles = new GuiBindingList<bool>();
            LoadTargetDetails();
            LoadTargetSkills();
            LoadTargetPerks();
            LoadTargetEffects();
            _initialized = true;

            WatchOnClient(model => model.TopTabId);
            WatchOnClient(model => model.BottomTabId);
            WatchOnClient(model => model.SelectedSkillCategoryId);
            WatchOnClient(model => model.SkillSearchText);
            WatchOnClient(model => model.SelectedPerkCategoryId);
            WatchOnClient(model => model.SelectedPerkSortOrderId);
            WatchOnClient(model => model.SelectedPerkStatusId);
            WatchOnClient(model => model.PerkSearchText);
            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteDetail);
            SelectTab(0);
        }

        private void SelectTab(int tabId)
        {
            _selectedTabId = tabId;
            _topToggles.SyncTo(tabId, value => TopTabId = value);
            _bottomToggles.SyncTo(tabId, value => BottomTabId = value);
            Tabs.Select(this, PartialView, tabId, onAfterApply: RefreshTabInputs);
        }

        protected override void OnModalClosedRestore() =>
            Tabs.Select(this, PartialView, _selectedTabId, onAfterApply: RefreshTabInputs);

        private void RefreshTabInputs()
        {
            // Watched inputs are not replayed by the root-layout swap. Republish without
            // invoking their setters, which would reload the filtered data.
            OnPropertyChanged(nameof(TopTabId));
            OnPropertyChanged(nameof(BottomTabId));
            switch (_selectedTabId)
            {
                case 1:
                    OnPropertyChanged(nameof(SelectedSkillCategoryId));
                    OnPropertyChanged(nameof(SkillSearchText));
                    break;
                case 2:
                    OnPropertyChanged(nameof(SelectedPerkCategoryId));
                    OnPropertyChanged(nameof(SelectedPerkSortOrderId));
                    OnPropertyChanged(nameof(SelectedPerkStatusId));
                    OnPropertyChanged(nameof(PerkSearchText));
                    break;
                case 4:
                    OnPropertyChanged(nameof(ActiveNoteName));
                    OnPropertyChanged(nameof(ActiveNoteDetail));
                    break;
            }
        }

        private void LoadTargetDetails()
        {
            Name = UtilPlugin.StripColors(PlayerName.GetDisplayName(Player, _target));
            Description = GetDescription(_target);
            PortraitResref = GetPortraitResRef(_target) + "l";
            CharacterType = GetClassByPosition(1, _target) == ClassType.ForceSensitive
                ? "Force Sensitive" : "Standard";
            Credits = GetGold(_target).ToString("N0");
            Descriptor = GetIsPC(_target) && !GetIsDM(_target) ? Disguise.GetDisplayDescriptor(_target) : "N/A";
            TrueName = GetName(_target);
            AccountName = GetPCPlayerName(_target);
            PublicCDKey = GetPCPublicCDKey(_target);
            var currentHP = ObjectPlugin.GetCurrentHitPoints(_target) + TemporaryHitPointEffects.GetRemaining(_target);
            Health = $"{currentHP:N0} / {GetMaxHitPoints(_target):N0}";
            FP = GetClassByPosition(1, _target) == ClassType.Standard
                ? "0 / 0"
                : $"{Math.Max(0, Stat.GetCurrentFP(_target)):N0} / {Math.Max(0, Stat.GetMaxFP(_target)):N0}";
            STM = $"{Math.Max(0, Stat.GetCurrentStamina(_target)):N0} / {Math.Max(0, Stat.GetMaxStamina(_target)):N0}";
            var lawChaos = GetAlignmentLawChaos(_target);
            var goodEvil = GetAlignmentGoodEvil(_target);
            AlignmentText = lawChaos == Alignment.Neutral && goodEvil == Alignment.Neutral
                ? "Neutral" : $"{lawChaos} {goodEvil}";
            var dbPlayer = DB.Get<Player>(_playerId);
            RPXP = dbPlayer?.UnallocatedXP.ToString("N0") ?? "N/A";
        }

        private void LoadTargetSkills()
        {
            var dbPlayer = DB.Get<Player>(_playerId);
            var names = new GuiBindingList<string>();
            var levels = new GuiBindingList<string>();
            var descriptions = new GuiBindingList<string>();
            if (dbPlayer != null)
            {
                var skills = DMPlayerExamineListFilter.Skills(Skill.GetAllActiveSkillsForDisplay(),
                    dbPlayer, SelectedSkillCategoryId, SkillSearchText);
                foreach (var (type, detail) in skills)
                {
                    names.Add(detail.Name);
                    levels.Add(dbPlayer.Skills.TryGetValue(type, out var skill) ? skill.Rank.ToString() : "0");
                    descriptions.Add(detail.Description);
                }
            }
            SkillNames = names;
            SkillLevels = levels;
            SkillDescriptions = descriptions;
            SkillSummary = dbPlayer == null ? "No saved skill data." : $"{names.Count} skills | Available XP: {dbPlayer.UnallocatedXP} | XP Debt: {dbPlayer.XPDebt}";
        }

        private void LoadTargetPerks()
        {
            var dbPlayer = DB.Get<Player>(_playerId);
            var names = new GuiBindingList<string>();
            var levels = new GuiBindingList<string>();
            var descriptions = new GuiBindingList<string>();
            if (dbPlayer != null)
            {
                bool CanBuy(PerkDetail detail, int rank) => GetIsObjectValid(_target) &&
                    detail.PerkLevels.TryGetValue(rank + 1, out var next) && dbPlayer.UnallocatedSP >= next.Price &&
                    next.Requirements.All(requirement => string.IsNullOrWhiteSpace(requirement.CheckRequirements(_target)));

                var perks = DMPlayerExamineListFilter.Perks(Perk.GetAllPerks(),
                    dbPlayer.Perks, SelectedPerkCategoryId, PerkSearchText, SelectedPerkStatusId,
                    SelectedPerkSortOrderId, CanBuy);
                foreach (var (type, detail) in perks)
                {
                    dbPlayer.Perks.TryGetValue(type, out var rank);
                    names.Add(detail.Name);
                    levels.Add($"{rank}/{detail.PerkLevels.Count}");
                    descriptions.Add(detail.Description);
                }
            }
            PerkNames = names;
            PerkLevels = levels;
            PerkDescriptions = descriptions;
            PerkSummary = dbPlayer == null ? "No saved perk data." : $"{names.Count} perks | Available SP: {dbPlayer.UnallocatedSP}";
        }

        private void LoadTargetEffects()
        {
            var rows = new List<(string Name, string Duration, string Source, string Detail)>();
            if (GetIsObjectValid(_target))
            {
                var statuses = StatusEffect.GetCreatureStatusEffects(_target).GetAllEffects();
                var statusIds = statuses.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
                var now = DateTime.UtcNow;
                foreach (var effect in statuses.Where(x => !x.IsFlaggedForRemoval))
                {
                    var duration = FormatDuration(effect.GetRemainingDurationSeconds(now));
                    var source = GetEffectSourceName(effect.Source);
                    var detail = $"{effect.Name}\nRemaining: {duration}\nSource: {source}\nCategories: {effect.Categories}";
                    if (effect.OriginatingAbility != null)
                        detail += $"\nAbility: {effect.OriginatingAbility.Name}";
                    rows.Add((effect.Name, duration, source, detail));
                }

                for (var effect = GetFirstEffect(_target); GetIsEffectValid(effect); effect = GetNextEffect(_target))
                {
                    var tag = GetEffectTag(effect);
                    // Managed effects have a logical row above, including their linked native payloads.
                    if (IsManagedEffectTag(tag, statusIds))
                        continue;
                    var type = GetEffectType(effect);
                    if (type == EffectTypeScript.RunScript || type == EffectTypeScript.Visualeffect ||
                        type == EffectTypeScript.Invalideffect)
                        continue;
                    var name = Regex.Replace(type.ToString(), "([a-z])([A-Z])", "$1 $2");
                    if (type == EffectTypeScript.Icon &&
                        int.TryParse(Get2DAString("effecticons", "StrRef", GetEffectInteger(effect, 0)), out var strRef))
                    {
                        var iconName = GetStringByStrRef(strRef);
                        if (!string.IsNullOrWhiteSpace(iconName)) name = iconName;
                    }
                    var duration = GetEffectDurationType(effect) == (int)DurationType.Permanent
                        ? "Permanent" : FormatDuration(GetEffectDurationRemaining(effect));
                    var source = GetEffectSourceName(GetEffectCreator(effect));
                    var detail = $"{name}\nRemaining: {duration}\nSource: {source}\nNative type: {type}";
                    if (!string.IsNullOrEmpty(tag)) detail += $"\nTag: {tag}";
                    var spellId = GetEffectSpellId(effect);
                    if (spellId >= 0 && int.TryParse(Get2DAString("spells", "Name", spellId), out var spellStrRef))
                        detail += $"\nSpell: {GetStringByStrRef(spellStrRef)}";
                    rows.Add((name, duration, source, detail));
                }
            }
            var ordered = rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var names = new GuiBindingList<string>();
            var durations = new GuiBindingList<string>();
            var sources = new GuiBindingList<string>();
            var details = new GuiBindingList<string>();
            foreach (var row in ordered)
            {
                names.Add(row.Name);
                durations.Add(row.Duration);
                sources.Add(row.Source);
                details.Add(row.Detail);
            }
            EffectNames = names;
            EffectDurations = durations;
            EffectSources = sources;
            EffectDetails = details;
            EffectSummary = rows.Count == 0 ? "No active effects." : $"{rows.Count} active effects (hover for details)";
        }

        private string GetEffectSourceName(uint source) => GetIsObjectValid(source)
            ? UtilPlugin.StripColors(PlayerName.GetDisplayName(Player, source)) : "Unknown";

        private static bool IsManagedEffectTag(string tag, HashSet<string> statusIds) =>
            statusIds.Contains(tag) || statusIds.Any(id => tag.StartsWith(id + ":Native:", StringComparison.Ordinal));

        private static string FormatDuration(float seconds)
        {
            if (seconds < 0f) return "Permanent";
            var duration = TimeSpan.FromSeconds(Math.Ceiling(seconds));
            return duration.TotalDays >= 1 ? duration.ToString(@"d\.hh\:mm\:ss") : duration.ToString(@"hh\:mm\:ss");
        }

        public Action OnClickRefreshEffects() => LoadTargetEffects;

        private void LoadTargetNotes()
        {
            var dbPlayer = DB.Get<Player>(_playerId);

            if (dbPlayer == null)
                return;

            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), _playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true);
            var dbNotes = DB.Search(query);

            var selectedNoteId = _selectedIndex >= 0 && _selectedIndex < _noteIds.Count
                ? _noteIds[_selectedIndex] : null;
            _noteIds.Clear();
            var noteNames = new GuiBindingList<string>();
            var noteToggles = new GuiBindingList<bool>();

            foreach (var note in dbNotes)
            {
                _noteIds.Add(note.Id);
                noteNames.Add(note.Name);
                noteToggles.Add(note.Id == selectedNoteId);
            }

            NoteNames = noteNames;
            NoteToggles = noteToggles;
            _selectedIndex = selectedNoteId == null ? -1 : _noteIds.IndexOf(selectedNoteId);
            IsNoteSelected = _selectedIndex >= 0;
        }

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
