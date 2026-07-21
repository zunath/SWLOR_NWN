using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
// Both SWLOR.Game.Server.Entity.Mastery (the catalog entity) and
// SWLOR.Game.Server.Service.Mastery (the static orchestration service) are in scope in
// this file. This alias pins the bare "Mastery" identifier to the service so calls like
// Mastery.GetOrCreateProfile(...) resolve unambiguously - matching Service/Mastery.cs's
// and MasteriesViewModel.cs's own convention for the same collision.
using Mastery = SWLOR.Game.Server.Service.Mastery;

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
        private string _currentPartial;

        public const string PartialView = "PARTIAL";

        public const string DetailView = "DETAIL_VIEW";
        public const string SkillsView = "SKILLS_VIEW";
        public const string PerksView = "PERKS_VIEW";
        public const string NotesView = "NOTES_VIEW";
        public const string MasteriesView = "MASTERIES_VIEW";

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
        public bool IsMasteriesToggled
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

        // ---------------------------------------------------------------
        // Masteries tab
        // ---------------------------------------------------------------

        private class MasteryRowContext
        {
            public string MasteryId;
            public string Name;
            public int CurrentTier;

            /// <summary>Null = earned only. 0 = active training. &gt;0 = queued.</summary>
            public int? QueueIndex;
            public int TargetTier;
        }

        private readonly List<MasteryRowContext> _masteryRows = new();
        private int _masterySelectedIndex = -1;
        private List<Entity.Mastery> _grantCatalog = new();

        public string MasteryTotalsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> MasteryRowLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> MasteryRowToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsMasteryRowSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string MasterySelectedSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMasteryEarnedActionsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMasteryTrainingActionsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMasteryQueuedActionsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMasteryIncreaseEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMasteryMoveUpEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMasteryMoveDownEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string MasteryActionReason
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MasteryReduceDaysText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MasteryActionStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> GrantMasteryOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedGrantMasteryIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedGrantTier
        {
            get => Get<int>();
            set => Set(value);
        }

        public string GrantReason
        {
            get => Get<string>();
            set => Set(value);
        }

        public string QuickSlotReason
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> MasteryAuditLines
        {
            get => Get<GuiBindingList<string>>();
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

            SelectedGrantTier = 1;
            MasteryReduceDaysText = "3";
            MasteryActionReason = string.Empty;
            GrantReason = string.Empty;
            QuickSlotReason = string.Empty;
            MasteryActionStatusText = string.Empty;

            // "Open Full Profile" from the mastery review queue jumps straight to this tab
            // rather than defaulting to Details - see DMPlayerExaminePayload.InitialView.
            var openMasteries = initialPayload.InitialView == MasteriesView;

            IsDetailsToggled = !openMasteries;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;
            IsMasteriesToggled = openMasteries;

            if (openMasteries)
            {
                _currentPartial = MasteriesView;
                ChangePartialView(PartialView, _currentPartial);
                LoadTargetMasteries();
            }
            else
            {
                _currentPartial = DetailView;
                ChangePartialView(PartialView, _currentPartial);
                LoadTargetDetails();
            }

            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteDetail);
            WatchOnClient(model => model.MasteryActionReason);
            WatchOnClient(model => model.MasteryReduceDaysText);
            WatchOnClient(model => model.SelectedGrantMasteryIndex);
            WatchOnClient(model => model.SelectedGrantTier);
            WatchOnClient(model => model.GrantReason);
            WatchOnClient(model => model.QuickSlotReason);
        }

        /// <summary>
        /// A ShowModal confirmation (e.g. Abandon Training) restores the window's static
        /// main template, which resets the tab-content placeholder back to whatever it
        /// was at window-build time. Reapply whichever tab was actually active so the DM
        /// doesn't get bounced back to Details - see MasteriesViewModel/MasteryReviewViewModel's
        /// OnMainViewRestored for the same fix.
        /// </summary>
        protected override void OnMainViewRestored()
        {
            ChangePartialView(PartialView, _currentPartial);
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

        public Action OnClickDetails() => () =>
        {
            IsDetailsToggled = true;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;
            IsMasteriesToggled = false;

            _currentPartial = DetailView;
            ChangePartialView(PartialView, _currentPartial);
            LoadTargetDetails();
        };

        public Action OnClickSkills() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = true;
            IsPerksToggled = false;
            IsNotesToggled = false;
            IsMasteriesToggled = false;

            _currentPartial = SkillsView;
            ChangePartialView(PartialView, _currentPartial);
            LoadTargetSkills();
        };

        public Action OnClickPerks() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = true;
            IsNotesToggled = false;
            IsMasteriesToggled = false;

            _currentPartial = PerksView;
            ChangePartialView(PartialView, _currentPartial);
            LoadTargetPerks();
        };

        public Action OnClickNotes() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = true;
            IsMasteriesToggled = false;

            _currentPartial = NotesView;
            ChangePartialView(PartialView, _currentPartial);
            LoadTargetNotes();
        };

        public Action OnClickMasteries() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;
            IsMasteriesToggled = true;

            _currentPartial = MasteriesView;
            ChangePartialView(PartialView, _currentPartial);
            LoadTargetMasteries();
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

        // ---------------------------------------------------------------
        // Masteries tab
        // ---------------------------------------------------------------

        private static string ToRomanTier(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => tier.ToString()
        };

        private void LoadTargetMasteries()
        {
            // Lazily evaluate the queue so a DM examining a profile whose training
            // completed while the character was offline sees up-to-date tiers - see
            // MASTERY_SPEC.md's Processing section (login / window open / staff profile
            // open are the three lazy-evaluation points).
            Mastery.EvaluateTrainingQueue(_playerId, DateTime.UtcNow);
            var profile = Mastery.GetOrCreateProfile(_playerId);

            _masteryRows.Clear();

            var queueIndexByMasteryId = new Dictionary<string, int>();
            for (var i = 0; i < profile.TrainingQueue.Count; i++)
            {
                queueIndexByMasteryId[profile.TrainingQueue[i].MasteryId] = i;
            }

            var masteryIds = new HashSet<string>(profile.Masteries.Keys);
            foreach (var id in queueIndexByMasteryId.Keys) masteryIds.Add(id);

            foreach (var id in masteryIds)
            {
                var mastery = Mastery.GetMastery(id);
                var currentTier = profile.Masteries.TryGetValue(id, out var level) ? level.Tier : 0;
                int? queueIndex = queueIndexByMasteryId.TryGetValue(id, out var qi) ? qi : (int?)null;
                var targetTier = queueIndex.HasValue ? profile.TrainingQueue[queueIndex.Value].TargetTier : currentTier;

                _masteryRows.Add(new MasteryRowContext
                {
                    MasteryId = id,
                    Name = mastery?.Name ?? "Unknown Mastery",
                    CurrentTier = currentTier,
                    QueueIndex = queueIndex,
                    TargetTier = targetTier
                });
            }

            // Earned-only rows first (alphabetical), then the active training entry, then
            // queued entries - matching the mockup's row ordering.
            _masteryRows.Sort((a, b) =>
            {
                var groupA = !a.QueueIndex.HasValue ? 0 : a.QueueIndex.Value == 0 ? 1 : 2;
                var groupB = !b.QueueIndex.HasValue ? 0 : b.QueueIndex.Value == 0 ? 1 : 2;
                return groupA != groupB
                    ? groupA.CompareTo(groupB)
                    : groupA == 2
                        ? a.QueueIndex.Value.CompareTo(b.QueueIndex.Value)
                        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            foreach (var row in _masteryRows)
            {
                string status;

                if (!row.QueueIndex.HasValue)
                {
                    status = $"Tier {ToRomanTier(row.CurrentTier)} (Earned)";
                }
                else
                {
                    var entry = profile.TrainingQueue[row.QueueIndex.Value];
                    var totalDays = Math.Max(1, entry.DurationDays - entry.ReductionDays);

                    status = row.QueueIndex.Value == 0
                        ? $"Tier {ToRomanTier(row.CurrentTier)} -> {ToRomanTier(row.TargetTier)} (training, " +
                          $"{Math.Max(0, (int)Math.Min((DateTime.UtcNow - entry.StartDate).TotalDays, totalDays))}/{totalDays}d)"
                        : $"Tier {ToRomanTier(row.CurrentTier)} -> {ToRomanTier(row.TargetTier)} (queued)";
                }

                labels.Add($"{row.Name} - {status}");
                toggles.Add(false);
            }

            MasteryRowLabels = labels;
            MasteryRowToggles = toggles;
            _masterySelectedIndex = -1;

            var earnedLevels = MasteryRules.GetEarnedLevelTotal(profile);
            var rareCount = Mastery.GetOwnedMasteryCatalog(profile).Values.Count(m => m.Rarity == MasteryRarityType.Rare);

            MasteryTotalsText =
                $"Levels: {earnedLevels} / {MasteryRules.MaxTotalLevels}   " +
                $"Rare: {rareCount} / 1   " +
                $"Quick Slots: {profile.QuickSlotsAvailable}   " +
                $"Retrain Credits: {profile.RetrainCredits14}x14d, {profile.RetrainCredits7}x7d";

            LoadMasteryActionState(profile);
            LoadGrantMasteryOptions();
            LoadMasteryAuditLog(profile);

            MasteryActionReason = string.Empty;
            MasteryReduceDaysText = "3";
            GrantReason = string.Empty;
            QuickSlotReason = string.Empty;
        }

        private void LoadMasteryActionState(PlayerMasteryProfile profile)
        {
            if (_masterySelectedIndex < 0 || _masterySelectedIndex >= _masteryRows.Count)
            {
                IsMasteryRowSelected = false;
                MasterySelectedSummary = "Select a mastery to manage it.";
                IsMasteryEarnedActionsVisible = false;
                IsMasteryTrainingActionsVisible = false;
                IsMasteryQueuedActionsVisible = false;
                IsMasteryIncreaseEnabled = false;
                IsMasteryMoveUpEnabled = false;
                IsMasteryMoveDownEnabled = false;
                return;
            }

            var row = _masteryRows[_masterySelectedIndex];
            IsMasteryRowSelected = true;
            MasterySelectedSummary = row.Name;

            var isEarnedOnly = !row.QueueIndex.HasValue;
            var isActiveTraining = row.QueueIndex == 0;
            var isQueued = row.QueueIndex is > 0;

            IsMasteryEarnedActionsVisible = isEarnedOnly;
            IsMasteryTrainingActionsVisible = isActiveTraining;
            IsMasteryQueuedActionsVisible = isQueued;
            IsMasteryIncreaseEnabled = isEarnedOnly && row.CurrentTier < 5;

            if (isQueued)
            {
                var idx = row.QueueIndex.Value;
                IsMasteryMoveUpEnabled = idx > 1;
                IsMasteryMoveDownEnabled = idx < profile.TrainingQueue.Count - 1;
            }
            else
            {
                IsMasteryMoveUpEnabled = false;
                IsMasteryMoveDownEnabled = false;
            }
        }

        private bool TryGetSelectedMasteryRow(out MasteryRowContext row)
        {
            row = null;
            if (_masterySelectedIndex < 0 || _masterySelectedIndex >= _masteryRows.Count)
                return false;

            row = _masteryRows[_masterySelectedIndex];
            return true;
        }

        /// <summary>
        /// Re-selects a mastery row by Id after a reload (e.g. following a mutation), so
        /// the DM doesn't lose their place in the list. No-ops if the mastery no longer
        /// appears (e.g. it was fully revoked).
        /// </summary>
        private void ReselectMasteryRow(string masteryId)
        {
            var index = _masteryRows.FindIndex(r => r.MasteryId == masteryId);
            if (index < 0)
                return;

            _masterySelectedIndex = index;
            if (index < MasteryRowToggles.Count)
                MasteryRowToggles[index] = true;

            var profile = Mastery.GetOrCreateProfile(_playerId);
            LoadMasteryActionState(profile);
        }

        private void LoadGrantMasteryOptions()
        {
            _grantCatalog = Mastery.GetAllMasteries()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToList();

            var options = new GuiBindingList<GuiComboEntry>();
            for (var i = 0; i < _grantCatalog.Count; i++)
            {
                options.Add(new GuiComboEntry(_grantCatalog[i].Name, i));
            }

            GrantMasteryOptions = options;

            if (SelectedGrantMasteryIndex < 0 || SelectedGrantMasteryIndex >= _grantCatalog.Count)
                SelectedGrantMasteryIndex = 0;
        }

        private void LoadMasteryAuditLog(PlayerMasteryProfile profile)
        {
            var lines = new GuiBindingList<string>();

            foreach (var entry in profile.AuditLog.OrderByDescending(e => e.Date))
            {
                var reason = string.IsNullOrWhiteSpace(entry.Reason) ? string.Empty : $" - \"{entry.Reason}\"";
                lines.Add($"{entry.Date:yyyy-MM-dd HH:mm} - {entry.ActorName} [{entry.ActorCDKey}] {entry.Action}{reason}");
            }

            MasteryAuditLines = lines;
        }

        public Action OnClickSelectMasteryRow() => () =>
        {
            if (_masterySelectedIndex > -1 && _masterySelectedIndex < MasteryRowToggles.Count)
                MasteryRowToggles[_masterySelectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            _masterySelectedIndex = index;

            if (index >= 0 && index < MasteryRowToggles.Count)
                MasteryRowToggles[index] = true;

            MasteryActionReason = string.Empty;
            MasteryActionStatusText = string.Empty;
            MasteryReduceDaysText = "3";

            var profile = Mastery.GetOrCreateProfile(_playerId);
            LoadMasteryActionState(profile);
        };

        public Action OnClickIncreaseTier() => () =>
        {
            if (!TryGetSelectedMasteryRow(out var row) || row.QueueIndex.HasValue || row.CurrentTier >= 5)
                return;

            if (string.IsNullOrWhiteSpace(MasteryActionReason))
            {
                MasteryActionStatusText = "A reason is required.";
                return;
            }

            var masteryId = row.MasteryId;
            Mastery.GrantMastery(_playerId, masteryId, row.CurrentTier + 1, GetName(Player), GetPCPublicCDKey(Player), MasteryActionReason, DateTime.UtcNow);

            MasteryActionStatusText = "Tier increased.";
            LoadTargetMasteries();
            ReselectMasteryRow(masteryId);
        };

        public Action OnClickRevokeTier() => () =>
        {
            if (!TryGetSelectedMasteryRow(out var row) || row.QueueIndex.HasValue || row.CurrentTier <= 0)
                return;

            if (string.IsNullOrWhiteSpace(MasteryActionReason))
            {
                MasteryActionStatusText = "A reason is required.";
                return;
            }

            var masteryId = row.MasteryId;
            var ok = Mastery.RevokeMastery(_playerId, masteryId, row.CurrentTier, GetName(Player), GetPCPublicCDKey(Player), MasteryActionReason, DateTime.UtcNow);

            MasteryActionStatusText = ok ? "Tier revoked." : "Unable to revoke this tier.";
            LoadTargetMasteries();
            ReselectMasteryRow(masteryId);
        };

        public Action OnClickReduceTraining() => () =>
        {
            if (!TryGetSelectedMasteryRow(out var row) || row.QueueIndex != 0)
                return;

            if (string.IsNullOrWhiteSpace(MasteryActionReason))
            {
                MasteryActionStatusText = "A reason is required.";
                return;
            }

            if (!int.TryParse(MasteryReduceDaysText, out var days) || days <= 0)
            {
                MasteryActionStatusText = "Enter a whole number of days greater than zero.";
                return;
            }

            var masteryId = row.MasteryId;
            var targetTier = row.TargetTier;

            // Re-resolve the active entry immediately before mutation rather than trusting
            // the stale row snapshot - Mastery.ReduceTrainingTime always targets whichever
            // entry is currently at TrainingQueue[0], and the queue can change between
            // selection and click (e.g. completion advancing it), matching the
            // abandon/reorder handlers' re-resolution pattern above.
            var profile = Mastery.GetOrCreateProfile(_playerId);
            if (profile.TrainingQueue.Count == 0 ||
                profile.TrainingQueue[0].MasteryId != masteryId ||
                profile.TrainingQueue[0].TargetTier != targetTier)
            {
                MasteryActionStatusText = "This mastery is no longer the active training entry.";
                LoadTargetMasteries();
                return;
            }

            Mastery.ReduceTrainingTime(_playerId, days, GetName(Player), GetPCPublicCDKey(Player), MasteryActionReason, DateTime.UtcNow);

            MasteryActionStatusText = $"Reduced active training by {days} day(s).";
            LoadTargetMasteries();
            ReselectMasteryRow(masteryId);
        };

        public Action OnClickAbandonTraining() => () =>
        {
            if (!TryGetSelectedMasteryRow(out var row) || !row.QueueIndex.HasValue)
                return;

            if (string.IsNullOrWhiteSpace(MasteryActionReason))
            {
                MasteryActionStatusText = "A reason is required.";
                return;
            }

            // Capture a stable identifier (mastery id + target tier) rather than the
            // positional queue index - the queue can change between the click and the
            // modal confirm (e.g. an entry completing or another action reordering it),
            // which would make a captured index point at the wrong entry. Re-resolve the
            // current index from that identifier at confirm time instead - matching
            // MasteryReviewViewModel's approve/deny pattern of re-fetching by a stable id
            // rather than trusting anything captured at click time.
            var masteryId = row.MasteryId;
            var targetTier = row.TargetTier;
            var reason = MasteryActionReason;
            var name = row.Name;

            ShowModal($"Cancel training for {name}? Any Quick Slot spent on it will be refunded.", () =>
            {
                var profile = Mastery.GetOrCreateProfile(_playerId);
                var currentIndex = profile.TrainingQueue.FindIndex(e => e.MasteryId == masteryId && e.TargetTier == targetTier);

                if (currentIndex < 0)
                {
                    MasteryActionStatusText = "This training entry no longer exists.";
                    LoadTargetMasteries();
                    return;
                }

                Mastery.AbandonTrainingEntry(_playerId, currentIndex, GetName(Player), GetPCPublicCDKey(Player), reason, DateTime.UtcNow);

                MasteryActionStatusText = "Training entry cancelled.";
                LoadTargetMasteries();
            });
        };

        public Action OnClickMoveMasteryUp() => () => MoveQueueEntry(-1);

        public Action OnClickMoveMasteryDown() => () => MoveQueueEntry(1);

        private void MoveQueueEntry(int direction)
        {
            if (!TryGetSelectedMasteryRow(out var row) || row.QueueIndex is not > 0)
                return;

            // Re-resolve the current queue index by MasteryId+TargetTier rather than
            // trusting the snapshot QueueIndex from the last load - the queue can change
            // between selection and click (e.g. the active entry completing), matching
            // the abandon handler's pattern above.
            var masteryId = row.MasteryId;
            var targetTier = row.TargetTier;
            var profile = Mastery.GetOrCreateProfile(_playerId);
            var currentIndex = profile.TrainingQueue.FindIndex(
                e => e.MasteryId == masteryId && e.TargetTier == targetTier);

            if (currentIndex <= 0)
            {
                MasteryActionStatusText = "This queued training entry no longer exists.";
                LoadTargetMasteries();
                return;
            }

            var ok = Mastery.ReorderTrainingQueueEntry(_playerId, currentIndex, direction, GetName(Player), GetPCPublicCDKey(Player), DateTime.UtcNow);

            MasteryActionStatusText = ok ? "Queue reordered." : "Unable to reorder.";
            LoadTargetMasteries();
            ReselectMasteryRow(masteryId);
        }

        public Action OnClickGrantMastery() => () =>
        {
            if (_grantCatalog.Count == 0 || SelectedGrantMasteryIndex < 0 || SelectedGrantMasteryIndex >= _grantCatalog.Count)
                return;

            if (string.IsNullOrWhiteSpace(GrantReason))
            {
                MasteryActionStatusText = "A reason is required to grant a mastery.";
                return;
            }

            if (SelectedGrantTier < 1 || SelectedGrantTier > 5)
            {
                MasteryActionStatusText = "Tier must be between 1 and 5.";
                return;
            }

            var mastery = _grantCatalog[SelectedGrantMasteryIndex];
            Mastery.GrantMastery(_playerId, mastery.Id, SelectedGrantTier, GetName(Player), GetPCPublicCDKey(Player), GrantReason, DateTime.UtcNow);

            MasteryActionStatusText = $"Granted {mastery.Name} tier {SelectedGrantTier}.";
            LoadTargetMasteries();
        };

        public Action OnClickAwardQuickSlot() => () =>
        {
            if (string.IsNullOrWhiteSpace(QuickSlotReason))
            {
                MasteryActionStatusText = "A reason is required to award a Quick Slot.";
                return;
            }

            Mastery.AwardQuickSlot(_playerId, GetName(Player), GetPCPublicCDKey(Player), QuickSlotReason, DateTime.UtcNow);

            MasteryActionStatusText = "Quick Slot awarded.";
            LoadTargetMasteries();
        };
    }
}
