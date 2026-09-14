using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DisguiseViewModel: GuiViewModelBase<DisguiseViewModel, GuiPayloadBase>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
    {
        public const string ContentPartialElement = "DISGUISE_CONTENT_PARTIAL";
        public const string ContentAvailablePartial = "DISGUISE_CONTENT_AVAILABLE";
        public const string ContentRetiredPartial = "DISGUISE_CONTENT_RETIRED";
        public const string ContentEditPartial = "DISGUISE_CONTENT_EDIT";
        public const string ContentEmptyPartial = "DISGUISE_CONTENT_EMPTY";

        private const int SoundSetPageSize = 25;

        private static readonly GuiColor _rowActiveColor = new(120, 210, 140);
        private static readonly GuiColor _rowNormalColor = new(224, 220, 192);
        private static readonly GuiColor _statusActiveColor = new(120, 210, 140);
        private static readonly GuiColor _statusInactiveColor = new(170, 162, 138);
        private static readonly GuiColor _statusEditColor = new(120, 185, 225);
        private static readonly GuiColor _slotFreeColor = new(90, 150, 95);
        private static readonly GuiColor _slotFullColor = new(200, 100, 70);

        private readonly List<string> _disguiseIds = new();
        private readonly List<int> _soundSetIds = new();
        private readonly Dictionary<int, int> _soundSetIndexesById = new();
        private readonly List<GuiBindingList<GuiComboEntry>> _soundSetOptionPages = new();
        private bool _suppressSoundSetPageChange;
        private int _selectedDisguiseIndex = -1;
        private string _selectedDisguiseId = string.Empty;
        private int _activePortraitInternalId = 1;
        private int _soundSetPageIndex;
        private int _selectedSoundSetId = -1;

        public GuiBindingList<string> DisguiseNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> DisguiseToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> DisguiseColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public string SlotBarLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActivationDelayNote
        {
            get => Get<string>();
            set => Set(value);
        }

        public float SlotUsageProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public GuiColor SlotUsageColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> SoundSetOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedSoundSetIndex
        {
            get => Get<int>();
            set
            {
                var sanitizedIndex = SanitizeSoundSetIndex(value);
                Set(sanitizedIndex);
                SetSelectedSoundSetFromPageIndex(sanitizedIndex);

                if (sanitizedIndex != value)
                    DelayCommand(0.0f, () => SelectedSoundSetIndex = sanitizedIndex);
            }
        }

        public GuiBindingList<GuiComboEntry> SoundSetPageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedSoundSetPageIndex
        {
            get => Get<int>();
            set
            {
                var sanitizedIndex = SanitizeSoundSetPageIndex(value);
                Set(sanitizedIndex);

                if (_suppressSoundSetPageChange)
                    return;

                if (sanitizedIndex != value)
                    DelayCommand(0.0f, () => SelectedSoundSetPageIndex = sanitizedIndex);

                _soundSetPageIndex = sanitizedIndex;
                LoadSoundSetPageOptions(GetSelectedSoundSetIndexOnCurrentPage(), true);
            }
        }

        public bool IsAvailableSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRetiredSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HasSelection
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowEmptyState
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsEditMode
        {
            get => Get<bool>();
            set
            {
                Set(value);
                IsViewMode = HasSelection && !value;
                RefreshActionVisibility();
            }
        }

        public bool IsViewMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowActivateButton
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowEditButton
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowRetireButton
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowUnretireButton
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ActivateButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SlotCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EmptyStateTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EmptyStateText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PrivateName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Descriptor
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Biography
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PortraitInternalId
        {
            get => Get<string>();
            set
            {
                var maxPortraits = GetMaxPortraitCount();
                if (!int.TryParse(value, out var parsed))
                    parsed = _activePortraitInternalId;

                parsed = Math.Clamp(parsed, 1, maxPortraits);
                _activePortraitInternalId = parsed;
                var sanitizedValue = parsed.ToString();
                Set(sanitizedValue);
                PortraitResref = ResolvePortraitResref(_activePortraitInternalId);

                if (value != sanitizedValue)
                    DelayCommand(0.0f, () => PortraitInternalId = sanitizedValue);
            }
        }

        public string PortraitResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SoundSetName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool ScrambleAccountId
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            IsAvailableSelected = true;
            IsRetiredSelected = false;
            HasSelection = false;
            ShowEmptyState = true;
            IsEditMode = false;
            IsViewMode = false;
            PrivateName = string.Empty;
            Descriptor = string.Empty;
            Biography = string.Empty;
            PortraitInternalId = "1";
            SoundSetName = string.Empty;
            PortraitResref = string.Empty;
            ScrambleAccountId = true;
            EmptyStateTitle = string.Empty;
            EmptyStateText = string.Empty;
            SlotBarLabel = string.Empty;
            ActivationDelayNote = string.Empty;
            SlotUsageProgress = 0f;
            SlotUsageColor = _slotFreeColor;
            StatusText = string.Empty;
            StatusColor = _statusInactiveColor;
            DisguiseColors = new GuiBindingList<GuiColor>();
            SoundSetPageNumbers = new GuiBindingList<GuiComboEntry>();

            LoadSoundSetOptions();
            SelectSoundSet(GetDefaultSoundSetId());
            LoadList();
            RefreshLayoutPartials();

            WatchOnClient(model => model.PrivateName);
            WatchOnClient(model => model.Descriptor);
            WatchOnClient(model => model.Biography);
            WatchOnClient(model => model.PortraitInternalId);
            WatchOnClient(model => model.SelectedSoundSetIndex);
            WatchOnClient(model => model.SelectedSoundSetPageIndex);
            WatchOnClient(model => model.ScrambleAccountId);
        }

        public Action OnClickAvailable() => () =>
        {
            IsAvailableSelected = true;
            IsRetiredSelected = false;
            IsEditMode = false;
            LoadList();
        };

        public Action OnClickRetired() => () =>
        {
            IsAvailableSelected = false;
            IsRetiredSelected = true;
            IsEditMode = false;
            LoadList();
        };

        public Action OnClickDisguise() => () =>
        {
            if (_selectedDisguiseIndex > -1 && _selectedDisguiseIndex < DisguiseToggles.Count)
                DisguiseToggles[_selectedDisguiseIndex] = false;

            _selectedDisguiseIndex = NuiGetEventArrayIndex();
            if (_selectedDisguiseIndex < 0 || _selectedDisguiseIndex >= _disguiseIds.Count)
            {
                ClearSelection();
                ConfigureEmptyState(false);
                RefreshLayoutPartials();
                return;
            }

            SelectDisguiseAtIndex(_selectedDisguiseIndex);
        };

        public Action OnClickNew() => () =>
        {
            ShowModal("Creating a new disguise will consume one of your disguise slots. Retired disguises also occupy disguise slots until they are wiped. Are you sure?",
                WithLayoutRestore(() =>
                {
                    var newDisguise = Disguise.CreateDisguise(Player);
                    if (newDisguise == null)
                    {
                        FloatingTextStringOnCreature("You do not have any available disguise slots.", Player, false);
                        return;
                    }

                    IsAvailableSelected = true;
                    IsRetiredSelected = false;
                    LoadList(newDisguise.Id);
                    IsEditMode = true;
                    RefreshLayoutPartials();
                }),
                RestoreLayoutPartials);
        };

        public Action OnClickEdit() => () =>
        {
            if (!HasSelection || IsRetiredSelected)
                return;

            IsEditMode = true;
            RefreshLayoutPartials();
        };

        public Action OnClickCancelEdit() => () =>
        {
            IsEditMode = false;
            LoadSelectedDisguise();
        };

        public Action OnClickSave() => () =>
        {
            if (!HasSelection)
                return;

            PortraitInternalId = _activePortraitInternalId.ToString();

            var result = Disguise.SaveDisguise(
                Player,
                _selectedDisguiseId,
                PrivateName,
                Descriptor,
                Biography,
                _activePortraitInternalId,
                _selectedSoundSetId,
                ScrambleAccountId);

            if (!result.IsSuccessful)
            {
                FloatingTextStringOnCreature(result.ErrorMessage, Player, false);
                return;
            }

            IsEditMode = false;
            LoadList(_selectedDisguiseId);
            SendMessageToPC(Player, ColorToken.Green("Disguise saved."));
        };

        private int GetActivationDelayMinutes()
        {
            return (int)Math.Round(Disguise.GetActivationDelay(Player).TotalMinutes);
        }

        public Action OnClickActivateOrDeactivate() => () =>
        {
            if (!HasSelection || IsRetiredSelected)
                return;

            var selectedDisguiseId = _selectedDisguiseId;
            var delayMinutes = GetActivationDelayMinutes();

            if (IsSelectedDisguiseActive())
            {
                ShowModal($"Deactivating this disguise immediately restores your normal identity. Deactivation does not trigger the {delayMinutes}-minute delay between disguise activations. Are you sure?",
                    WithLayoutRestore(() =>
                    {
                        if (Disguise.Deactivate(Player))
                            SendMessageToPC(Player, ColorToken.Green("Disguise deactivated."));

                        ReloadAvailableDisguise(selectedDisguiseId);
                    }),
                    RestoreLayoutPartials);
            }
            else
            {
                ShowModal($"Activating this disguise starts a {delayMinutes}-minute delay before you can activate another disguise. Deactivation has no delay. Are you sure?",
                    WithLayoutRestore(() =>
                    {
                        var result = Disguise.Activate(Player, selectedDisguiseId);
                        if (!result.IsSuccessful)
                        {
                            FloatingTextStringOnCreature(result.ErrorMessage, Player, false);
                            return;
                        }

                        SendMessageToPC(Player, ColorToken.Green("Disguise activated."));
                        ReloadAvailableDisguise(selectedDisguiseId);
                    }),
                    RestoreLayoutPartials);
            }
        };

        public Action OnClickRetire() => () =>
        {
            if (!HasSelection || IsRetiredSelected)
                return;

            ShowModal("Retiring this disguise will make it unavailable until an Identity Broker wipes it. Retired disguises still occupy disguise slots. Are you sure?",
                WithLayoutRestore(() =>
                {
                    if (Disguise.Retire(Player, _selectedDisguiseId))
                    {
                        SendMessageToPC(Player, ColorToken.Green("Disguise retired."));
                        LoadList();
                    }
                }),
                RestoreLayoutPartials);
        };

        public Action OnClickUnretire() => () =>
        {
            if (!HasSelection || !IsRetiredSelected)
                return;

            ShowModal("Restoring this disguise will move it back to your available disguises. Are you sure?",
                WithLayoutRestore(() =>
                {
                    if (!Disguise.Unretire(Player, _selectedDisguiseId))
                    {
                        FloatingTextStringOnCreature("Unable to restore that disguise.", Player, false);
                        return;
                    }

                    SendMessageToPC(Player, ColorToken.Green("Disguise restored."));
                    IsAvailableSelected = true;
                    IsRetiredSelected = false;
                    IsEditMode = false;
                    LoadList(_selectedDisguiseId);
                }),
                RestoreLayoutPartials);
        };

        public Action OnClickPreviousPortrait() => () =>
        {
            var next = _activePortraitInternalId - 1;
            if (next < 1)
                next = GetMaxPortraitCount();

            PortraitInternalId = next.ToString();
        };

        public Action OnClickNextPortrait() => () =>
        {
            var next = _activePortraitInternalId + 1;
            if (next > GetMaxPortraitCount())
                next = 1;

            PortraitInternalId = next.ToString();
        };

        public Action OnClickPreviousSoundSetPage() => () =>
        {
            if (!IsEditMode)
                return;

            var newPage = SelectedSoundSetPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedSoundSetPageIndex = newPage;
        };

        public Action OnClickNextSoundSetPage() => () =>
        {
            if (!IsEditMode)
                return;

            var newPage = SelectedSoundSetPageIndex + 1;
            if (newPage > SoundSetPageNumbers.Count - 1)
                newPage = SoundSetPageNumbers.Count - 1;

            SelectedSoundSetPageIndex = newPage;
        };

        public Action OnClickPreviewSoundSet() => () =>
        {
            if (_selectedSoundSetId < 0)
                return;

            var previewSoundResref = Cache.GetSoundSetPreviewSoundResref(_selectedSoundSetId);
            if (string.IsNullOrWhiteSpace(previewSoundResref))
            {
                SendMessageToPC(Player, ColorToken.Red("No preview sound is available for this sound set."));
                return;
            }

            PlayerPlugin.PlaySound(Player, previewSoundResref, OBJECT_INVALID);
        };

        private void LoadList(string selectedDisguiseId = "")
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            RefreshSlotCapacity(playerId, dbPlayer);
            RefreshActivationDelayNote();

            var disguises = Disguise.GetDisguises(playerId, IsRetiredSelected);
            var disguiseNames = new GuiBindingList<string>();
            var disguiseToggles = new GuiBindingList<bool>();
            var disguiseColors = new GuiBindingList<GuiColor>();
            _disguiseIds.Clear();

            foreach (var disguise in disguises)
            {
                var isActive = !disguise.IsRetired && dbPlayer?.ActiveDisguiseId == disguise.Id;
                var name = disguise.PrivateName;
                if (isActive)
                    name += " (Active)";

                _disguiseIds.Add(disguise.Id);
                disguiseNames.Add(name);
                disguiseToggles.Add(disguise.Id == selectedDisguiseId);
                disguiseColors.Add(isActive ? _rowActiveColor : _rowNormalColor);
            }

            DisguiseNames = disguiseNames;
            DisguiseToggles = disguiseToggles;
            DisguiseColors = disguiseColors;

            if (!string.IsNullOrWhiteSpace(selectedDisguiseId) && _disguiseIds.Contains(selectedDisguiseId))
            {
                SelectDisguiseAtIndex(_disguiseIds.IndexOf(selectedDisguiseId));
                return;
            }

            if (_disguiseIds.Count > 0)
            {
                SelectDisguiseAtIndex(0);
                return;
            }

            ClearSelection();
            ConfigureEmptyState(_disguiseIds.Count > 0);
            RefreshLayoutPartials();
        }

        private void RefreshSlotCapacity()
        {
            var playerId = GetObjectUUID(Player);
            RefreshSlotCapacity(playerId, DB.Get<Player>(playerId));
        }

        private void RefreshSlotCapacity(string playerId, Player dbPlayer)
        {
            var usedSlots = Disguise.CountUsedSlots(playerId);
            var slotLimit = Disguise.GetDisguiseSlotLimit(Player, dbPlayer);
            SlotCountText = $"Slots Used: {usedSlots} / {slotLimit}";
            SlotBarLabel = $"Disguise Slots   {usedSlots} / {slotLimit}";
            SlotUsageProgress = slotLimit <= 0
                ? 0f
                : Math.Clamp((float)usedSlots / slotLimit, 0f, 1f);
            SlotUsageColor = usedSlots >= slotLimit ? _slotFullColor : _slotFreeColor;
        }

        private void RefreshActivationDelayNote()
        {
            ActivationDelayNote = $"Activating starts a {GetActivationDelayMinutes()}-minute cooldown before you can activate another disguise.";
        }

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            RefreshPerkDependentBindings(payload.Type);
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            RefreshPerkDependentBindings(payload.Type);
        }

        private void RefreshPerkDependentBindings(PerkType perkType)
        {
            switch (perkType)
            {
                case PerkType.FalseIdentities:
                    RefreshSlotCapacity();
                    break;
                case PerkType.CoverStory:
                    RefreshActivationDelayNote();
                    break;
            }
        }

        private void LoadSelectedDisguise()
        {
            var disguise = DB.Get<PlayerDisguise>(_selectedDisguiseId);
            if (disguise == null)
            {
                ClearSelection();
                return;
            }

            HasSelection = true;
            ShowEmptyState = false;
            IsViewMode = !IsEditMode;
            PrivateName = disguise.PrivateName;
            Descriptor = disguise.Descriptor;
            Biography = disguise.Biography ?? string.Empty;
            PortraitInternalId = disguise.PortraitInternalId.ToString();
            ScrambleAccountId = disguise.ScrambleAccountId;
            SelectSoundSet(disguise.SoundSetId);
            Instructions = string.Empty;
            EmptyStateTitle = string.Empty;
            EmptyStateText = string.Empty;
            RefreshActionVisibility();
            RefreshLayoutPartials();
        }

        private void ClearSelection()
        {
            _selectedDisguiseIndex = -1;
            _selectedDisguiseId = string.Empty;
            HasSelection = false;
            ShowEmptyState = true;
            IsEditMode = false;
            IsViewMode = false;
            PrivateName = string.Empty;
            Descriptor = string.Empty;
            Biography = string.Empty;
            PortraitInternalId = "1";
            SoundSetName = string.Empty;
            PortraitResref = string.Empty;
            SelectSoundSet(GetDefaultSoundSetId());
            ScrambleAccountId = true;
            RefreshActionVisibility();
            RefreshLayoutPartials();
        }

        private void ConfigureEmptyState(bool hasDisguises)
        {
            Instructions = string.Empty;
            ShowEmptyState = true;

            if (hasDisguises)
            {
                EmptyStateTitle = "No Disguise Selected";
                EmptyStateText = "Disguise details will appear here.";
            }
            else if (IsRetiredSelected)
            {
                EmptyStateTitle = "No Retired Disguises";
                EmptyStateText = "Retired disguises will appear here.";
            }
            else
            {
                EmptyStateTitle = "No Available Disguises";
                EmptyStateText = "Create a disguise to begin.";
            }
        }

        private void ReloadAvailableDisguise(string selectedDisguiseId)
        {
            IsAvailableSelected = true;
            IsRetiredSelected = false;
            IsEditMode = false;
            LoadList(selectedDisguiseId);
        }

        private void RefreshActionVisibility()
        {
            ShowActivateButton = HasSelection && IsAvailableSelected && !IsEditMode;
            ShowEditButton = HasSelection && IsAvailableSelected && !IsEditMode;
            ShowRetireButton = HasSelection && IsAvailableSelected && !IsEditMode;
            ShowUnretireButton = HasSelection && IsRetiredSelected && !IsEditMode;
            ActivateButtonText = IsSelectedDisguiseActive() ? "Deactivate" : "Activate";
            UpdateStatusTag();
        }

        private void UpdateStatusTag()
        {
            if (!HasSelection)
            {
                StatusText = string.Empty;
                StatusColor = _statusInactiveColor;
            }
            else if (IsEditMode)
            {
                StatusText = "Editing";
                StatusColor = _statusEditColor;
            }
            else if (IsRetiredSelected)
            {
                StatusText = "Retired";
                StatusColor = _statusInactiveColor;
            }
            else if (IsSelectedDisguiseActive())
            {
                StatusText = "● Active";
                StatusColor = _statusActiveColor;
            }
            else
            {
                StatusText = "Inactive";
                StatusColor = _statusInactiveColor;
            }
        }

        private void SelectDisguiseAtIndex(int index)
        {
            _selectedDisguiseIndex = index;
            _selectedDisguiseId = _disguiseIds[index];
            DisguiseToggles[index] = true;
            IsEditMode = false;
            LoadSelectedDisguise();
        }

        private void RefreshLayoutPartials()
        {
            ChangePartialView(ContentPartialElement, GetContentPartialName());

            if (HasSelection)
                RefreshSoundSetBindings();
        }

        private Action WithLayoutRestore(Action action)
        {
            return () =>
            {
                try
                {
                    action();
                }
                finally
                {
                    RestoreLayoutPartials();
                }
            };
        }

        private void RestoreLayoutPartials()
        {
            void ApplyLayoutPartials()
            {
                RefreshLayoutPartials();
            }

            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
            ApplyLayoutPartials();
            DelayCommand(0.0f, ApplyLayoutPartials);
        }

        private string GetContentPartialName()
        {
            if (!HasSelection)
                return ContentEmptyPartial;

            if (IsEditMode)
                return ContentEditPartial;

            if (IsRetiredSelected)
                return ContentRetiredPartial;

            return IsAvailableSelected ? ContentAvailablePartial : ContentEmptyPartial;
        }

        private bool IsSelectedDisguiseActive()
        {
            if (string.IsNullOrWhiteSpace(_selectedDisguiseId))
                return false;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            return dbPlayer?.ActiveDisguiseId == _selectedDisguiseId;
        }

        private void LoadSoundSetOptions()
        {
            _soundSetIds.Clear();
            _soundSetIndexesById.Clear();
            _soundSetOptionPages.Clear();

            var currentPage = new GuiBindingList<GuiComboEntry>();
            var optionIndex = 0;

            foreach (var (soundSetId, label) in Cache.GetSoundSets())
            {
                var absoluteIndex = _soundSetIds.Count;
                _soundSetIds.Add(soundSetId);
                _soundSetIndexesById[soundSetId] = absoluteIndex;

                if (currentPage.Count >= SoundSetPageSize)
                {
                    _soundSetOptionPages.Add(currentPage);
                    currentPage = new GuiBindingList<GuiComboEntry>();
                    optionIndex = 0;
                }

                currentPage.Add(new GuiComboEntry(label, optionIndex));
                optionIndex++;
            }

            _soundSetOptionPages.Add(currentPage);
            _soundSetPageIndex = 0;
            LoadSoundSetPageOptions(0);
        }

        private void SelectSoundSet(int soundSetId)
        {
            if (!_soundSetIndexesById.TryGetValue(soundSetId, out var absoluteIndex))
                absoluteIndex = _soundSetIds.Count > 0 ? 0 : -1;

            if (absoluteIndex < 0)
            {
                _selectedSoundSetId = -1;
                _soundSetPageIndex = 0;
                LoadSoundSetPageOptions(-1);
                return;
            }

            _selectedSoundSetId = _soundSetIds[absoluteIndex];
            _soundSetPageIndex = absoluteIndex / SoundSetPageSize;
            LoadSoundSetPageOptions(absoluteIndex % SoundSetPageSize);
        }

        private int GetDefaultSoundSetId()
        {
            return _soundSetIds.Count > 0
                ? _soundSetIds[0]
                : -1;
        }

        private int SanitizeSoundSetIndex(int index)
        {
            if (index < 0)
                return -1;

            var pageCount = GetCurrentSoundSetPageSize();
            if (pageCount == 0)
                return -1;

            return Math.Clamp(index, 0, pageCount - 1);
        }

        private int SanitizeSoundSetPageIndex(int index)
        {
            return Math.Clamp(index, 0, GetSoundSetPageCount() - 1);
        }

        private int GetCurrentSoundSetPageSize()
        {
            if (_soundSetPageIndex < 0 || _soundSetPageIndex >= _soundSetOptionPages.Count)
                return 0;

            return _soundSetOptionPages[_soundSetPageIndex].Count;
        }

        private int GetSoundSetPageCount()
        {
            return Math.Max(1, _soundSetOptionPages.Count);
        }

        private void LoadSoundSetPageNumbers()
        {
            var pageNumbers = new GuiBindingList<GuiComboEntry>();

            for (var pageIndex = 0; pageIndex < GetSoundSetPageCount(); pageIndex++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {pageIndex + 1}", pageIndex));
            }

            SoundSetPageNumbers = pageNumbers;
        }

        private void LoadSoundSetPageOptions(int selectedPageIndex)
        {
            LoadSoundSetPageOptions(selectedPageIndex, false);
        }

        private void LoadSoundSetPageOptions(int selectedPageIndex, bool selectFirstWhenInvalid)
        {
            _soundSetPageIndex = Math.Clamp(_soundSetPageIndex, 0, GetSoundSetPageCount() - 1);
            LoadSoundSetPageNumbers();

            SoundSetOptions = _soundSetOptionPages[_soundSetPageIndex];
            _suppressSoundSetPageChange = true;
            SelectedSoundSetPageIndex = _soundSetPageIndex;
            _suppressSoundSetPageChange = false;

            if (selectFirstWhenInvalid &&
                (selectedPageIndex < 0 || selectedPageIndex >= GetCurrentSoundSetPageSize()))
            {
                selectedPageIndex = GetCurrentSoundSetPageSize() > 0 ? 0 : -1;
            }

            SelectedSoundSetIndex = selectedPageIndex;
        }

        private int GetSelectedSoundSetIndexOnCurrentPage()
        {
            if (!_soundSetIndexesById.TryGetValue(_selectedSoundSetId, out var absoluteIndex))
                return -1;

            if (absoluteIndex / SoundSetPageSize != _soundSetPageIndex)
                return -1;

            return absoluteIndex % SoundSetPageSize;
        }

        private void SetSelectedSoundSetFromPageIndex(int pageIndex)
        {
            var absoluteIndex = _soundSetPageIndex * SoundSetPageSize + pageIndex;
            if (pageIndex < 0 || absoluteIndex < 0 || absoluteIndex >= _soundSetIds.Count)
            {
                _selectedSoundSetId = -1;
                SoundSetName = string.Empty;
                return;
            }

            _selectedSoundSetId = _soundSetIds[absoluteIndex];
            SoundSetName = _soundSetOptionPages[_soundSetPageIndex][pageIndex].Label;
        }

        private void RefreshSoundSetBindings()
        {
            if (_selectedSoundSetId < 0)
            {
                LoadSoundSetPageOptions(-1);
                return;
            }

            if (!_soundSetIndexesById.TryGetValue(_selectedSoundSetId, out var absoluteIndex))
            {
                SelectSoundSet(GetDefaultSoundSetId());
                return;
            }

            _soundSetPageIndex = absoluteIndex / SoundSetPageSize;
            LoadSoundSetPageOptions(absoluteIndex % SoundSetPageSize);
        }

        private static int GetMaxPortraitCount()
        {
            return Math.Max(1, Cache.PortraitCount);
        }

        private static string ResolvePortraitResref(int portraitInternalId)
        {
            try
            {
                return Cache.GetPortraitResrefByInternalId(portraitInternalId) + "l";
            }
            catch (KeyNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}
