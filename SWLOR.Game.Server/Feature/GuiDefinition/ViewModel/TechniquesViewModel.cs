using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using PlayerEntity = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TechniquesViewModel : GuiViewModelBase<TechniquesViewModel, TechniquesPayload>,
        IGuiRefreshable<TechniqueChangedRefreshEvent>
    {
        public const string BodyPartial = "TechniquesBody";
        public const string BodyElement = "techniques_body";
        private const string NoSelectionText = "Select a technique to view its details.";
        private List<FeatType> _draft;
        private List<FeatType> _savedSnapshot;
        private List<string> _loadoutIds = new();
        private string _selectedLoadoutId;
        private bool _settingSelection;

        public GuiBindingList<GuiComboEntry> LoadoutOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SelectedLoadoutIndex { get => Get<int>(); set => Set(value); }
        public string ActiveLoadoutText { get => Get<string>(); set => Set(value); }
        public string PreviewTitle { get => Get<string>(); set => Set(value); }
        public string BuildsText { get => Get<string>(); set => Set(value); }
        public float BuildsProgress { get => Get<float>(); set => Set(value); }
        public bool CanSaveAs { get => Get<bool>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public string ChangesText { get => Get<string>(); set => Set(value); }
        private bool IsDirty => !MimicryLoadouts.Matches(_draft, _savedSnapshot);
        private PlayerEntity Record => DB.Get<PlayerEntity>(GetObjectUUID(Player));

        private List<FeatType> _unequippedFeats;
        private List<FeatType> _equippedFeats;
        private int _selectedUnequippedIndex = -1;
        private int _selectedEquippedIndex = -1;

        public GuiBindingList<string> UnequippedNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> UnequippedSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> UnequippedColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> UnequippedIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EquippedNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> EquippedSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> EquippedColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> EquippedIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string SlotsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor SlotsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public float SlotsProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string SelectedDetails
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsEquipEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUnequipEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Search box; category filter (0 = all, else a TechniqueCategory value); sort order.
        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedSortOrderId
        {
            get => Get<int>();
            set => Set(value);
        }

        public TechniquesViewModel()
        {
            _unequippedFeats = new List<FeatType>();
            _equippedFeats = new List<FeatType>();
        }

        protected override void Initialize(TechniquesPayload initialPayload)
        {
            SelectedDetails = NoSelectionText;
            IsEquipEnabled = false;
            IsUnequipEnabled = false;

            SearchText = string.Empty;
            SelectedCategoryId = 0;
            SelectedSortOrderId = 0;

            var record = Record;
            if (MimicryLoadouts.EnsureDefault(record))
                DB.Set(record);
            if (_draft == null || !record.MimicryLoadouts.Any(x => x.Id == _selectedLoadoutId))
            {
                LoadDraft(record.ActiveMimicryLoadoutId.Length > 0 ? record.ActiveMimicryLoadoutId : record.MimicryLoadouts[0].Id);
                // Apply can persist an equipped set without overwriting its saved preset.
                // A new window must show that set while retaining the preset for Save/Revert.
                _draft = record.EquippedTechniques.ToList();
            }
            RefreshLoadouts();
            StatusText = "Preview changes do not equip techniques. Save builds before logging out.";
            ChangePartialView(BodyElement, BodyPartial);
            LoadLists();

            // Re-filter/sort the Learned list whenever the player changes the search box,
            // category dropdown, or sort dropdown (the same reactive pattern the Perks window uses).
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedCategoryId);
            WatchOnClient(model => model.SelectedSortOrderId);
            WatchOnClient(model => model.SelectedLoadoutIndex);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName == nameof(SelectedLoadoutIndex) && !_settingSelection)
            {
                var index = SelectedLoadoutIndex;
                if (index < 0 || index >= _loadoutIds.Count || _loadoutIds[index] == _selectedLoadoutId)
                    return;
                var next = _loadoutIds[index];
                SetSelection(_loadoutIds.IndexOf(_selectedLoadoutId));
                ConfirmDraftChange(() => { LoadDraft(next); RefreshLoadouts(); LoadLists(); });
                return;
            }
            if (propertyName == nameof(SearchText) ||
                propertyName == nameof(SelectedCategoryId) ||
                propertyName == nameof(SelectedSortOrderId))
            {
                LoadLists();
            }
        }

        private void LoadLists()
        {
            ClearSelections();

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<PlayerEntity>(playerId);
            var learnedFeats = Mimicry.GetLearnedTechniques(dbPlayer);
            var equippedFeats = _draft.Select(feat => (Feat: feat, Detail: Mimicry.GetTechniqueDetail(feat))).ToList();

            var unequippedNames = new GuiBindingList<string>();
            var unequippedSelections = new GuiBindingList<bool>();
            var unequippedColors = new GuiBindingList<GuiColor>();
            var unequippedIcons = new GuiBindingList<string>();
            var equippedNames = new GuiBindingList<string>();
            var equippedSelections = new GuiBindingList<bool>();
            var equippedColors = new GuiBindingList<GuiColor>();
            var equippedIcons = new GuiBindingList<string>();

            _unequippedFeats = new List<FeatType>();
            _equippedFeats = new List<FeatType>();

            var equippedFeatSet = equippedFeats.Select(x => x.Feat).ToHashSet();

            // Requirement state, computed once, drives the per-row color (mirrors the Perk window's
            // grey = locked / amber = met-but-can't-afford / green = actionable convention).
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var usedSlots = _draft.Sum(feat => Mimicry.GetTechniqueDetail(feat)?.MimicrySlotCost ?? 0);
            var maxSlots = Mimicry.GetMaxSlots(dbPlayer);

            // The Learned list honors the search box, category dropdown, and sort dropdown.
            var search = (SearchText ?? string.Empty).Trim().ToLower();
            var learnedView = learnedFeats
                .Where(x => !equippedFeatSet.Contains(x.Feat))
                .Where(x => SelectedCategoryId == 0 || GetTechniqueCategoryId(x.Detail) == SelectedCategoryId)
                .Where(x => search.Length == 0 || x.Detail.Name.ToLower().Contains(search));

            learnedView = SelectedSortOrderId switch
            {
                1 => learnedView.OrderByDescending(x => x.Detail.Name),
                2 => learnedView.OrderBy(x => x.Detail.MimicrySkillRequirement).ThenBy(x => x.Detail.Name),
                3 => learnedView.OrderByDescending(x => x.Detail.MimicrySkillRequirement).ThenBy(x => x.Detail.Name),
                _ => learnedView.OrderBy(x => x.Detail.Name),
            };

            foreach (var (feat, detail) in learnedView)
            {
                _unequippedFeats.Add(feat);
                unequippedNames.Add(BuildRowText(detail));
                unequippedSelections.Add(false);
                unequippedColors.Add(GetUnequippedRowColor(detail, skillRank, usedSlots, maxSlots));
                unequippedIcons.Add(Mimicry.GetTechniqueIcon(feat));
            }

            foreach (var (feat, detail) in equippedFeats)
            {
                _equippedFeats.Add(feat);
                equippedNames.Add(detail == null ? "Unavailable technique (remove to apply)" : BuildRowText(detail));
                equippedSelections.Add(false);
                equippedColors.Add(detail == null || skillRank < detail.MimicrySkillRequirement ? LockedColor : EquippedColor);
                equippedIcons.Add(Mimicry.GetTechniqueIcon(feat));
            }

            UnequippedNames = unequippedNames;
            UnequippedSelections = unequippedSelections;
            UnequippedColors = unequippedColors;
            UnequippedIcons = unequippedIcons;
            EquippedNames = equippedNames;
            EquippedSelections = equippedSelections;
            EquippedColors = equippedColors;
            EquippedIcons = equippedIcons;

            RefreshSlots(dbPlayer);
            RefreshStatus(dbPlayer);
        }

        // Perk-window color convention, reused for consistency.
        private static readonly GuiColor LockedColor = GuiColor.Grey;         // requirement not met (skill rank)
        private static readonly GuiColor NoRoomColor = new(230, 180, 70);     // met, but no free slots
        private static readonly GuiColor EquippableColor = new(60, 200, 90);  // ready to equip
        private static readonly GuiColor EquippedColor = new(90, 170, 230);   // currently equipped / active

        // Category ids match the dropdown option values in TechniquesDefinition:
        // 1 Single-Target, 2 Area, 3 Stance, 4 Support, 5 Passive Trait. Trait/stance/support are
        // checked first because they take precedence over the ability's targeting shape.
        private static int GetTechniqueCategoryId(AbilityDetail detail)
        {
            if (detail.IsMimicryTrait) return 5;
            if (detail.IsMimicryStance) return 3;
            if (detail.IsMimicryUtility) return 4;
            if (detail.IsAreaAbility) return 2;
            return 1;
        }

        private static GuiColor GetUnequippedRowColor(AbilityDetail detail, int skillRank, int usedSlots, int maxSlots)
        {
            if (skillRank < detail.MimicrySkillRequirement)
                return LockedColor;

            if (usedSlots + detail.MimicrySlotCost > maxSlots)
                return NoRoomColor;

            return EquippableColor;
        }

        private static string BuildRowText(AbilityDetail detail)
        {
            var slotLabel = detail.MimicrySlotCost == 1 ? "slot" : "slots";
            return $"{detail.Name} (Rank {detail.MimicrySkillRequirement} / {detail.MimicrySlotCost} {slotLabel})";
        }

        private void RefreshSlots(PlayerEntity dbPlayer)
        {
            var used = _draft.Sum(feat => Mimicry.GetTechniqueDetail(feat)?.MimicrySlotCost ?? 0);
            var max = Mimicry.GetMaxSlots(dbPlayer);

            SlotsText = $"Technique slots: {used} / {max}";
            SlotsColor = used > max ? GuiColor.Red : EquippedColor;
            SlotsProgress = max > 0 ? Math.Clamp((float)used / max, 0f, 1f) : 0f;
        }

        private void ClearSelections()
        {
            // The bound selection lists are replaced wholesale by LoadLists (the only caller),
            // so only the index and UI state resets matter here.
            _selectedUnequippedIndex = -1;
            _selectedEquippedIndex = -1;
            IsEquipEnabled = false;
            IsUnequipEnabled = false;
            SelectedDetails = NoSelectionText;
        }

        private void ShowDetails(FeatType feat)
        {
            var detail = Mimicry.GetTechniqueDetail(feat);
            if (detail == null)
            {
                SelectedDetails = "This saved technique is unavailable. Remove it from the preview before applying.";
                return;
            }

            var text = $"{detail.Name}\n\n";

            var description = GetTechniqueDescription(feat);
            if (!string.IsNullOrWhiteSpace(description))
                text += $"{description}\n\n";

            text += $"Slot Cost: {detail.MimicrySlotCost}\n" +
                    $"Requires: Mimicry Rank {detail.MimicrySkillRequirement}\n";

            if (detail.IsMimicryTrait)
            {
                // Traits are passive: they are never cast, so stamina/recast do not apply.
                text += "Type: Passive Trait";
            }
            else
            {
                var staminaCost = detail.Requirements
                    .OfType<AbilityRequirementStamina>()
                    .Select(x => x.RequiredSTM)
                    .DefaultIfEmpty(0)
                    .First();

                var recastSeconds = detail.RecastDelay != null
                    ? detail.RecastDelay(Player)
                    : 0f;

                text += $"Stamina Cost: {staminaCost}\n" +
                        $"Recast: {recastSeconds:0.#}s";
            }

            SelectedDetails = text;
        }

        // The menu adds its own costs, so use authored text without the hotbar header.
        private static string GetTechniqueDescription(FeatType feat) => Mimicry.GetTechniqueDescription(feat);

        public Action OnSelectUnequipped() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _unequippedFeats.Count)
                return;

            if (_selectedEquippedIndex > -1 && _selectedEquippedIndex < EquippedSelections.Count)
            {
                EquippedSelections[_selectedEquippedIndex] = false;
                _selectedEquippedIndex = -1;
            }

            if (_selectedUnequippedIndex > -1 && _selectedUnequippedIndex < UnequippedSelections.Count)
            {
                UnequippedSelections[_selectedUnequippedIndex] = false;
            }

            _selectedUnequippedIndex = index;
            UnequippedSelections[index] = true;

            ShowDetails(_unequippedFeats[index]);

            IsEquipEnabled = true;
            IsUnequipEnabled = false;
        };

        public Action OnSelectEquipped() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _equippedFeats.Count)
                return;

            if (_selectedUnequippedIndex > -1 && _selectedUnequippedIndex < UnequippedSelections.Count)
            {
                UnequippedSelections[_selectedUnequippedIndex] = false;
                _selectedUnequippedIndex = -1;
            }

            if (_selectedEquippedIndex > -1 && _selectedEquippedIndex < EquippedSelections.Count)
            {
                EquippedSelections[_selectedEquippedIndex] = false;
            }

            _selectedEquippedIndex = index;
            EquippedSelections[index] = true;

            ShowDetails(_equippedFeats[index]);

            IsEquipEnabled = false;
            IsUnequipEnabled = true;
        };

        public Action OnClickEquip() => () =>
        {
            if (_selectedUnequippedIndex < 0 || _selectedUnequippedIndex >= _unequippedFeats.Count)
                return;

            var feat = _unequippedFeats[_selectedUnequippedIndex];

            _draft.Add(feat);
            LoadLists();
        };

        public Action OnClickUnequip() => () =>
        {
            if (_selectedEquippedIndex < 0 || _selectedEquippedIndex >= _equippedFeats.Count)
                return;

            var feat = _equippedFeats[_selectedEquippedIndex];

            _draft.Remove(feat);
            LoadLists();
        };

        private void LoadDraft(string id)
        {
            var loadout = Record.MimicryLoadouts.FirstOrDefault(x => x.Id == id) ?? Record.MimicryLoadouts[0];
            _selectedLoadoutId = loadout.Id;
            _draft = loadout.Techniques.ToList();
            _savedSnapshot = loadout.Techniques.ToList();
        }

        private void SetSelection(int index)
        {
            _settingSelection = true;
            SelectedLoadoutIndex = index;
            _settingSelection = false;
        }

        private void RefreshLoadouts()
        {
            var record = Record;
            var options = new GuiBindingList<GuiComboEntry>();
            _loadoutIds = record.MimicryLoadouts.Select(x => x.Id).ToList();
            for (var index = 0; index < record.MimicryLoadouts.Count; index++)
                options.Add(new GuiComboEntry(record.MimicryLoadouts[index].Name, index));
            LoadoutOptions = options;
            SetSelection(_loadoutIds.IndexOf(_selectedLoadoutId));
            BuildsText = $"Saved builds: {record.MimicryLoadouts.Count} / {MimicryLoadouts.MaxLoadouts}";
            BuildsProgress = (float)record.MimicryLoadouts.Count / MimicryLoadouts.MaxLoadouts;
            CanSaveAs = record.MimicryLoadouts.Count < MimicryLoadouts.MaxLoadouts;
        }

        private void RefreshStatus(PlayerEntity record)
        {
            var selected = record.MimicryLoadouts.First(x => x.Id == _selectedLoadoutId);
            var active = record.MimicryLoadouts.FirstOrDefault(x => x.Id == record.ActiveMimicryLoadoutId);
            ActiveLoadoutText = active == null ? "Equipped: Custom build" :
                $"Equipped: {active.Name}" + (MimicryLoadouts.Matches(active.Techniques, record.EquippedTechniques) ? "" : " (Modified)");
            PreviewTitle = $"Build preview: {selected.Name}" + (IsDirty ? " *" : "");
            var added = _draft.Except(record.EquippedTechniques).Count();
            var removed = record.EquippedTechniques.Except(_draft).Count();
            ChangesText = $"Compared with equipped: +{added} / -{removed}";
            if (!Mimicry.ValidateLoadout(record, _draft, out var error))
                StatusText = error;
            else
                StatusText = IsDirty ? "Unsaved changes. Apply equips the preview; Save stores it." :
                    "Ready to apply. You can edit and save builds during combat; applying requires leaving combat.";
        }

        private bool SaveDraft(string id, string name)
        {
            var record = Record;
            if (!MimicryLoadouts.Save(record, id, name, _draft, out var saved, out var error))
            {
                StatusText = error;
                return false;
            }
            if (MimicryLoadouts.Matches(_draft, record.EquippedTechniques))
                Mimicry.CaptureLoadoutHotbar(Player, saved);
            DB.Set(record);
            _selectedLoadoutId = saved.Id;
            _savedSnapshot = _draft.ToList();
            RefreshLoadouts();
            LoadLists();
            return true;
        }

        public Action OnClickSave() => () => SaveDraft(_selectedLoadoutId,
            Record.MimicryLoadouts.First(x => x.Id == _selectedLoadoutId).Name);

        public Action OnClickSaveAs() => () => ShowInputModal(
            $"Save build as (up to {MimicryLoadouts.MaxNameLength} characters):", string.Empty,
            () => SaveDraft(null, ModalInputText), confirmText: "Save");

        public Action OnClickManage() => () => ShowModal("Manage the selected saved build:",
            () => ShowInputModal("Rename build:", Record.MimicryLoadouts.First(x => x.Id == _selectedLoadoutId).Name,
                () =>
                {
                    var record = Record;
                    if (!MimicryLoadouts.Rename(record, _selectedLoadoutId, ModalInputText, out var error))
                    {
                        StatusText = error;
                        return;
                    }
                    DB.Set(record);
                    RefreshLoadouts();
                    LoadLists();
                }, confirmText: "Rename"),
            () => ShowModal("Delete this saved build? Equipped techniques stay equipped.", () =>
            {
                var record = Record;
                if (!MimicryLoadouts.Delete(record, _selectedLoadoutId, out var error))
                {
                    StatusText = error;
                    return;
                }
                DB.Set(record);
                LoadDraft(record.MimicryLoadouts[0].Id);
                RefreshLoadouts();
                LoadLists();
            }, confirmText: "Delete", cancelText: "Cancel"), "Rename", "Delete...");

        private void ConfirmDraftChange(Action proceed)
        {
            if (!IsDirty)
            {
                proceed();
                return;
            }
            ShowModal("Save your changes before switching builds?", () =>
            {
                if (SaveDraft(_selectedLoadoutId, Record.MimicryLoadouts.First(x => x.Id == _selectedLoadoutId).Name))
                    proceed();
            }, () => ShowModal("Discard your unsaved changes?", proceed,
                confirmText: "Discard", cancelText: "Keep editing"), "Save", "More...");
        }

        public Action OnClickRevert() => () => ShowModal("Revert the preview to the saved build?", () =>
        {
            LoadDraft(_selectedLoadoutId);
            LoadLists();
        }, confirmText: "Revert", cancelText: "Cancel");

        public Action OnClickApply() => () =>
        {
            if (!Mimicry.ApplyLoadout(Player, _selectedLoadoutId, _draft, out var error))
                StatusText = error;
            else
                LoadLists();
        };

        public void Refresh(TechniqueChangedRefreshEvent payload)
        {
            if (_draft != null)
                LoadLists();
        }

        protected override void OnMainViewRestored()
        {
            // The client can drop input binds while the modal replaces their widgets.
            SearchText = SearchText;
            SelectedCategoryId = SelectedCategoryId;
            SelectedSortOrderId = SelectedSortOrderId;
            SetSelection(_loadoutIds.IndexOf(_selectedLoadoutId));
        }
    }
}
