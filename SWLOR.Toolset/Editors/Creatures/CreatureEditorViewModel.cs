using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Editors.TintMaps;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>The creature editor shared by all creature blueprints.</summary>
    public sealed partial class CreatureEditorViewModel : ObservableObject, IModelPreviewSource, IDisposable
    {
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<JsonGffStruct, RenderModel?>? _resolveModel;
        private readonly ChoicePreviewService? _choicePreviews;
        private readonly Func<BehaviorChoice, string?>? _previewAudio;
        private readonly Func<IReadOnlyList<AppearanceOption>>? _appearanceOptionsLoader;
        private readonly OutputLogService? _log;
        private readonly Dictionary<string, IReadOnlyList<BehaviorRowViewModel>> _roleRowCache =
            new(StringComparer.Ordinal);
        private readonly object _choiceLoadSync = new();
        private readonly Dictionary<string, Task<IReadOnlyList<BehaviorChoice>>> _choiceLoads =
            new(StringComparer.Ordinal);
        private bool _previewSceneUpdateQueued;
        private int _previewModelGeneration;
        private readonly SemaphoreSlim _previewModelGate = new(1);
        private ModelPreviewControl? _previewView;
        private Task? _appearanceCatalogLoadTask;
        private Task? _appearanceDetailsLoadTask;
        private bool _appearanceCatalogLoaded;
        private int _referenceWarningGeneration;
        private bool _referenceWarningsRequested;
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> BasicRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> AppearanceRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> FlagRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> AiRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> RoleRows { get; } = new();
        public ObservableCollection<BehaviorListItemViewModel> RoleList { get; } = new();
        public ObservableCollection<CreatureAnimationOption> PreviewAnimations { get; } = new();
        public ObservableCollection<CreatureDeprecatedVariableViewModel> DeprecatedVariables { get; } = new();

        public CreatureEquipmentSet Equipment { get; }
        public CreatureStatsViewModel Stats { get; }
        public CreatureAbilitiesViewModel Abilities { get; }
        public CreatureAiViewModel Ai { get; }
        public CreatureLootViewModel Loot { get; }
        public CreatureBodyPartsViewModel BodyParts { get; }
        public CreatureEquipmentSlotsViewModel EquipmentSlots { get; }
        [ObservableProperty]
        private TintMapEditorViewModel? _tintMapEditor;

        public bool HasTintMapEditor => TintMapEditor != null;
        public VarTableSectionViewModel Variables { get; }
        public AppearanceGallerySectionViewModel? AppearanceGallery { get; }
        public bool HasAppearanceGallery => AppearanceGallery != null;
        public bool ShowsVariablesTab => SelectedRole.AllowsVariables;

        [ObservableProperty]
        private CreatureRole _selectedRole = CreatureRoleCatalog.Default;

        [ObservableProperty]
        private CreatureAnimationOption? _selectedPreviewAnimation;

        [ObservableProperty]
        private bool _isEquipmentTabSelected;

        [ObservableProperty]
        private bool _isBehaviorTabSelected;

        [ObservableProperty]
        private bool _isAppearanceTabSelected;

        [ObservableProperty]
        private bool _isAbilitiesTabSelected;

        [ObservableProperty]
        private bool _isLootTabSelected;

        [ObservableProperty]
        private bool _isAppearanceCatalogLoading;

        [ObservableProperty]
        private string _appearanceCatalogLoadError = string.Empty;

        public bool HasAppearanceCatalogLoadError => AppearanceCatalogLoadError.Length > 0;

        [ObservableProperty]
        private int _selectedAppearanceSectionIndex;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private bool _isModelPreviewLoading;

        public string HeaderName => "Creature";
        public string HeaderKind => "blueprint";
        public string HeaderOwner { get; }
        public string CreatureName
        {
            get
            {
                var first = _store.GetLocalizedText("FirstName");
                var last = _store.GetLocalizedText("LastName");
                return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrWhiteSpace(part)));
            }
        }
        public string TemplateResRef => _store.GetString(BehaviorFieldStorage.Field, "TemplateResRef");
        public string QuestUsage { get; private set; } = string.Empty;
        public bool HasQuestUsage => QuestUsage.Length > 0;
        public string DeprecatedWarning { get; private set; } = string.Empty;
        public bool HasDeprecatedWarning => DeprecatedWarning.Length > 0;
        public string ReferenceWarning { get; private set; } = string.Empty;
        public bool HasReferenceWarning => ReferenceWarning.Length > 0;
        public string StatWarning =>
            (_store.GetInteger(BehaviorFieldStorage.Field, "Plot") == 1 ||
             _store.GetInteger(BehaviorFieldStorage.Field, "IsImmortal") == 1) && Stats.HasStatSkin
                ? "Plot or immortal creatures ignore some authored combat outcomes. Check that these stats are intentional."
                : string.Empty;
        public bool HasStatWarning => StatWarning.Length > 0;

        public ResourceIndex? ResourceIndex { get; }
        public AreaScene? PreviewScene { get; private set; }
        public string? PreviewAnimationName => SelectedPreviewAnimation?.AnimationName;
        public bool IsAnimationPlaying => true;

        public Avalonia.Controls.Control PreviewView
        {
            get
            {
                if (_previewView != null)
                    return _previewView;
                _previewView = new ModelPreviewControl { DataContext = this };
                _previewView.SetHostVisible(true);
                return _previewView;
            }
        }

        public CreatureEditorViewModel(
            JsonGffStruct creature,
            string filePath,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            ResourceIndex? resourceIndex,
            Func<JsonGffStruct, RenderModel?>? resolveModel,
            Func<int, AppearanceRow?> appearance,
            ArmorPartCatalog? armorParts,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>>? equipmentChoices = null,
            Func<string, CreatureEquipmentChoice?>? equipmentDetails = null,
            ChoicePreviewService? choicePreviews = null,
            Func<BehaviorChoice, string?>? previewAudio = null,
            Action<string>? openLootDefinition = null,
            IReadOnlyList<AppearanceOption>? appearanceOptions = null,
            ThumbnailService? appearanceThumbnails = null,
            ArmorDyeSwatchService? colorPalettes = null,
            Func<string, string>? resolveItemName = null,
            Func<string, int, int, int,
                Task<IReadOnlyList<CreatureEquipmentChoice>>>? equipmentSearch = null,
            Func<IReadOnlyList<AppearanceOption>>? appearanceOptionsLoader = null,
            Func<int, string?>? abilityIcon = null,
            OutputLogService? log = null,
            TintMapCatalog? tintMapCatalog = null,
            Func<IDocumentEdit?>? captureCoalesceOrigin = null,
            Func<IDocumentEdit, string, Action, bool>? runCoalescedEdit = null)
        {
            _store = new CreatureValueStore(creature);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _resolveModel = resolveModel;
            _choicePreviews = choicePreviews;
            _previewAudio = previewAudio;
            _appearanceOptionsLoader = appearanceOptionsLoader;
            _log = log;
            HeaderOwner = headerOwner;
            ResourceIndex = resourceIndex;

            Equipment = new CreatureEquipmentSet(_store, filePath);
            Stats = new CreatureStatsViewModel(_store, Equipment, RunEdit);
            Abilities = new CreatureAbilitiesViewModel(
                _store,
                RunEdit,
                choicePreviews: _choicePreviews,
                iconResolver: abilityIcon);
            Ai = new CreatureAiViewModel(_store, RunEdit);
            Loot = new CreatureLootViewModel(
                _store, RunEdit, openDefinition: openLootDefinition, resolveItemName: resolveItemName);
            BodyParts = new CreatureBodyPartsViewModel(
                _store,
                RunEdit,
                appearance,
                armorParts,
                colorPalettes,
                OnBodyPartChanged,
                OnTintColorChanged,
                captureCoalesceOrigin,
                runCoalescedEdit);
            if (tintMapCatalog != null)
            {
                TintMapEditor = new TintMapEditorViewModel(
                    _store.Locals,
                    RunEdit,
                    tintMapCatalog,
                    colorChanged: OnTintColorChanged);
            }
            EquipmentSlots = new CreatureEquipmentSlotsViewModel(
                _store,
                Equipment,
                RunEdit,
                equipmentChoices ?? (() => Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(
                    Array.Empty<CreatureEquipmentChoice>())),
                equipmentDetails ?? (_ => null),
                OnEquipmentChanged,
                _choicePreviews,
                equipmentSearch);
            Variables = new VarTableSectionViewModel(RunEdit, _store.Locals, gameCodeIndex, IsCustomVariable);
            if (appearanceOptions != null || appearanceOptionsLoader != null)
            {
                AppearanceGallery = new AppearanceGallerySectionViewModel(
                    appearanceOptions ?? Array.Empty<AppearanceOption>(),
                    appearanceThumbnails,
                    CurrentAppearanceKey,
                    ApplyAppearance,
                    noun: "appearance");
                _appearanceCatalogLoaded = appearanceOptions != null;
            }

            BuildRows(CreatureEditorLayout.Basic, BasicRows);
            BuildRows(
                CreatureEditorLayout.Appearance,
                AppearanceRows,
                deferSearchableChoices: true,
                loadDeferredChoicesInBackground: true);
            BuildRows(CreatureEditorLayout.Flags, FlagRows);
            BuildRows(CreatureEditorLayout.Ai, AiRows);
            BehaviorListItemViewModel.Build(RoleList, CreatureRoleCatalog.All);
            SelectRole(CreatureRoleCatalog.Default);
            UpdateWarnings();
            UpdatePreviewScene();
        }

        [RelayCommand]
        private void ChooseRole(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is CreatureRole role)
                SelectRole(role);
        }

        public void ReloadFromDocument()
        {
            foreach (var row in AllRows())
                row.Reload();
            Stats.Reload();
            Abilities.Reload();
            Ai.Reload();
            Loot.Reload();
            BodyParts.Reload();
            EnsureSelectedAppearanceSectionLoaded();
            EquipmentSlots.Reload();
            AppearanceGallery?.ReloadFromDocument();
            Variables.RefreshFromDocument();
            UpdateWarnings();
            if (_referenceWarningsRequested)
                QueueReferenceWarningUpdate();
            UpdateQuestUsage();
            UpdatePreviewScene();
            NotifySummary();
        }

        public void RefreshPaletteChoices()
        {
            var index = -1;
            for (var current = 0; current < BasicRows.Count; current++)
            {
                if (BasicRows[current].Definition.Name != "PaletteID")
                    continue;
                index = current;
                break;
            }
            if (index < 0)
                return;
            var definition = BasicRows[index].Definition;
            BasicRows[index].Dispose();
            BasicRows[index] = CreateRow(definition);
        }

        public void NormalizeForSave() => Loot.Normalize();

        public void SetDirty(bool value) => IsDirty = value;

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                IsDirty = true;
            return applied;
        }

        private void BuildRows(
            IEnumerable<BehaviorFieldDefinition> definitions,
            ObservableCollection<BehaviorRowViewModel> target,
            bool deferSearchableChoices = false,
            bool loadDeferredChoicesInBackground = false)
        {
            foreach (var definition in definitions)
                target.Add(CreateRow(
                    definition,
                    deferSearchableChoices,
                    loadDeferredChoicesInBackground));
        }

        private IEnumerable<BehaviorRowViewModel> DirectRows() =>
            BasicRows.Concat(AppearanceRows).Concat(FlagRows).Concat(AiRows);

        private IEnumerable<BehaviorRowViewModel> AllRows() =>
            DirectRows().Concat(_roleRowCache.Values.SelectMany(rows => rows));

        private BehaviorRowViewModel CreateRow(
            BehaviorFieldDefinition definition,
            bool deferSearchableChoices = false,
            bool loadDeferredChoicesInBackground = false,
            bool forceInlineSearch = false)
        {
            var defersChoices = deferSearchableChoices &&
                                definition.IsSearchable &&
                                definition.ChoicesKey != null;
            var choices = defersChoices
                ? null
                : definition.ChoicesKey == null
                    ? definition.Choices
                    : _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
            Func<IReadOnlyList<BehaviorChoice>>? choiceLoader = defersChoices && !loadDeferredChoicesInBackground
                ? () => _resolveChoices?.Invoke(definition.ChoicesKey!) ?? Array.Empty<BehaviorChoice>()
                : null;
            Func<Task<IReadOnlyList<BehaviorChoice>>>? asyncChoiceLoader =
                defersChoices && loadDeferredChoicesInBackground
                    ? () => ResolveChoicesAsync(definition.ChoicesKey!)
                    : null;
            var row = new BehaviorRowViewModel(
                definition,
                _store,
                RunEdit,
                choices,
                OnDirectValueChanged,
                _choicePreviews,
                _previewAudio,
                choiceLoader,
                asyncChoiceLoader,
                forceInlineSearch: forceInlineSearch);
            row.Reload();
            return row;
        }

        /// <summary>
        /// Shares the raw catalog between visible rows that browse the same source. Each row still
        /// publishes its own bounded page, but five guild ranks no longer rescan module merchants
        /// five times when the Guild Master behavior opens.
        /// </summary>
        private Task<IReadOnlyList<BehaviorChoice>> ResolveChoicesAsync(string key)
        {
            lock (_choiceLoadSync)
            {
                if (_choiceLoads.TryGetValue(key, out var existing))
                    return existing;

                var load = Task.Run(() =>
                    _resolveChoices?.Invoke(key) ?? Array.Empty<BehaviorChoice>());
                _choiceLoads[key] = load;
                _ = load.ContinueWith(completed =>
                {
                    if (!completed.IsFaulted && !completed.IsCanceled)
                        return;

                    lock (_choiceLoadSync)
                    {
                        if (_choiceLoads.TryGetValue(key, out var current) &&
                            ReferenceEquals(current, load))
                        {
                            _choiceLoads.Remove(key);
                        }
                    }
                }, TaskScheduler.Default);
                return load;
            }
        }

        private void SelectRole(CreatureRole role)
        {
            RoleRows.Clear();
            SelectedRole = role;
            if (!_roleRowCache.TryGetValue(role.Id, out var rows))
            {
                rows = role.Fields.Select(definition => CreateRow(
                    definition,
                    deferSearchableChoices: true,
                    loadDeferredChoicesInBackground: true,
                    forceInlineSearch: true)).ToList();
                _roleRowCache[role.Id] = rows;
            }
            foreach (var row in rows)
                RoleRows.Add(row);
            BehaviorListItemViewModel.Select(RoleList, role.Id);
            OnPropertyChanged(nameof(ShowsVariablesTab));
            UpdateQuestUsage();
            EnsureSelectedBehaviorChoicesLoaded();
        }

        private void EnsureSelectedBehaviorChoicesLoaded()
        {
            if (!IsBehaviorTabSelected || _disposed)
                return;

            _ = LoadSelectedBehaviorChoicesAsync();
        }

        private async Task LoadSelectedBehaviorChoicesAsync()
        {
            try
            {
                foreach (var row in RoleRows.Where(row => row.IsInlineSearchChoice))
                {
                    await row.ActivateChoicesAsync().ConfigureAwait(true);
                    if (_disposed)
                        return;
                }
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    _log?.AppendLine($"Could not load behavior choices for '{SelectedRole.DisplayName}': {ex.Message}");
            }
        }

        private void OnDirectValueChanged()
        {
            BodyParts.Reload();
            ReconcileBodySectionAvailability();
            EnsureSelectedAppearanceSectionLoaded();
            UpdateWarnings();
            if (_referenceWarningsRequested)
                QueueReferenceWarningUpdate();
            UpdateQuestUsage();
            QueuePreviewSceneUpdate();
            NotifySummary();
        }

        private void OnEquipmentChanged()
        {
            QueuePreviewSceneUpdate();
        }

        private void OnBodyPartChanged()
        {
            QueuePreviewSceneUpdate();
        }

        private void OnTintColorChanged()
        {
            var model = PreviewScene?.Instances.FirstOrDefault()?.Model;
            if (model != null)
                ApplyPreviewScene(model);
            else
                QueuePreviewSceneUpdate();
        }

        private string CurrentAppearanceKey() =>
            (_store.GetInteger(BehaviorFieldStorage.Field, "Appearance_Type") ?? 0)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

        private bool ApplyAppearance(AppearanceOption option)
        {
            if (option.CreatureAppearanceId is not { } id)
                return false;

            if (!RunEdit(
                    $"Change appearance to {option.Caption}",
                    () => _store.SetInteger(
                        BehaviorFieldStorage.Field,
                        "Appearance_Type",
                        GffFieldType.Word,
                        id)))
            {
                return false;
            }

            OnDirectValueChanged();
            return true;
        }

        private void UpdateQuestUsage()
        {
            var group = _store.Locals.GetInt("QUEST_NPC_GROUP_ID");
            if (group.HasValue && _gameCodeIndex?.NpcGroupQuestIds.TryGetValue(group.Value, out var ids) == true)
            {
                QuestUsage = ids.Count == 0
                    ? string.Empty
                    : "Used by " + string.Join(", ", ids.Select(id =>
                        _gameCodeIndex.FindQuest(id) is { } quest ? quest.Name : id));
            }
            else
            {
                QuestUsage = string.Empty;
            }
            OnPropertyChanged(nameof(QuestUsage));
            OnPropertyChanged(nameof(HasQuestUsage));
        }

        private void UpdateWarnings()
        {
            var deprecated = _store.Locals.Select(entry => entry.Name)
                .Where(IsDeprecatedVariable)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
            DeprecatedVariables.Clear();
            foreach (var name in deprecated)
                DeprecatedVariables.Add(new CreatureDeprecatedVariableViewModel(name, DeprecatedDisplayName(name)));
            DeprecatedWarning = deprecated.Count == 0
                ? string.Empty
                : deprecated.Count == 1
                    ? "This creature has one obsolete behavior setting. Remove it if it is no longer needed."
                    : $"This creature has {deprecated.Count} obsolete behavior settings. Remove those no longer needed.";
            OnPropertyChanged(nameof(DeprecatedWarning));
            OnPropertyChanged(nameof(HasDeprecatedWarning));
            OnPropertyChanged(nameof(StatWarning));
            OnPropertyChanged(nameof(HasStatWarning));
        }

        /// <summary>
        /// Reference validation can scan every dialog and placed merchant in the module. It is
        /// useful while editing behavior, but it must not be part of drawing the Basic tab.
        /// Snapshot the few stored values on the UI thread and perform the catalog work in the
        /// background after Behavior is selected.
        /// </summary>
        private void QueueReferenceWarningUpdate()
        {
            if (_disposed || _resolveChoices == null)
                return;

            _referenceWarningsRequested = true;
            var generation = ++_referenceWarningGeneration;
            var snapshot = new ReferenceWarningSnapshot(
                _store.GetString(BehaviorFieldStorage.Field, "Conversation"),
                _store.Locals.GetString("CONVERSATION"),
                Enumerable.Range(1, 5)
                    .Select(rank => _store.Locals.GetString($"STORE_TAG_RANK_{rank}"))
                    .ToArray());
            _ = ResolveReferenceWarningsAsync(snapshot, generation);
        }

        private async Task ResolveReferenceWarningsAsync(
            ReferenceWarningSnapshot snapshot,
            int generation)
        {
            string warning;
            try
            {
                warning = await Task.Run(() => BuildReferenceWarning(snapshot)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A temporarily unavailable module index must not stop the editor from opening.
                warning = string.Empty;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_disposed || generation != _referenceWarningGeneration)
                    return;

                ReferenceWarning = warning;
                OnPropertyChanged(nameof(ReferenceWarning));
                OnPropertyChanged(nameof(HasReferenceWarning));
            });
        }

        private string BuildReferenceWarning(ReferenceWarningSnapshot snapshot)
        {
            var warnings = new List<string>();
            if (MissingChoice(snapshot.Conversation, CreatureChoiceKeys.Dialogs))
                warnings.Add("The selected conversation is not available.");
            if (MissingChoice(snapshot.ScriptedConversation, CreatureChoiceKeys.DialogDefinitions))
                warnings.Add("The selected scripted dialog is not registered.");
            var missingStores = snapshot.GuildStores.Count(store =>
                MissingChoice(store, CreatureChoiceKeys.GuildStores));
            if (missingStores > 0)
                warnings.Add(missingStores == 1
                    ? "One guild store does not resolve to a placed merchant."
                    : $"{missingStores} guild stores do not resolve to placed merchants.");
            return string.Join(" ", warnings);
        }

        private bool MissingChoice(string? stored, string choiceKey)
        {
            if (string.IsNullOrWhiteSpace(stored) || _resolveChoices == null)
                return false;
            var choices = _resolveChoices(choiceKey);
            return choices.Count > 0 && choices.All(choice =>
                !string.Equals(choice.StringValue, stored, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record ReferenceWarningSnapshot(
            string Conversation,
            string? ScriptedConversation,
            IReadOnlyList<string?> GuildStores);

        [RelayCommand]
        private void RemoveDeprecated(CreatureDeprecatedVariableViewModel? variable)
        {
            if (variable == null || !DeprecatedVariables.Contains(variable))
                return;
            if (!RunEdit($"Remove obsolete {variable.DisplayName}", () => _store.Locals.Remove(variable.Name)))
                return;
            Variables.RefreshFromDocument();
            UpdateWarnings();
        }

        private static string DeprecatedDisplayName(string name)
        {
            if (name.StartsWith("QUEST_ID_", StringComparison.Ordinal))
                return "legacy quest requirement";
            if (name.StartsWith("KEY_ITEM_", StringComparison.Ordinal))
                return "legacy key-item requirement";
            if (name.StartsWith("SKILL_", StringComparison.Ordinal))
                return "legacy skill requirement";
            if (name.StartsWith("STEAL_LOOT_TABLE_", StringComparison.Ordinal))
                return "legacy steal loot";
            if (name.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) ||
                name.StartsWith("LOOT_TABLE_ID", StringComparison.Ordinal))
                return "legacy loot setting";
            return name switch
            {
                "NPC_GROUP" => "legacy quest group",
                "BEHAVIOUR" or "BEHAVIOR" => "legacy behavior",
                "STORE_TAG" => "legacy merchant store",
                "barActivity" => "legacy ambient activity",
                "IS_SHIP" => "legacy ship marker",
                _ => "obsolete setting"
            };
        }

        private static bool IsDeprecatedVariable(string name)
        {
            var malformedLootRow = name.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) &&
                                   !System.Text.RegularExpressions.Regex.IsMatch(
                                       name,
                                       "^LOOT_TABLE_[0-9]+$");
            return malformedLootRow ||
                   name.StartsWith("QUEST_ID_", StringComparison.Ordinal) ||
                   name.StartsWith("KEY_ITEM_", StringComparison.Ordinal) ||
                   name.StartsWith("SKILL_", StringComparison.Ordinal) ||
                   name.StartsWith("STEAL_LOOT_TABLE_", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_ID", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_ATTEMPTS_", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_CHANCE_", StringComparison.Ordinal) ||
                   name is "NPC_GROUP" or "BEHAVIOUR" or "BEHAVIOR" or "STORE_TAG" or "barActivity" or "IS_SHIP";
        }

        private static bool IsCustomVariable(string name)
        {
            if (IsDeprecatedVariable(name))
                return true;
            if (name.StartsWith("PERK_LEVEL_", StringComparison.Ordinal))
                return false;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, "^LOOT_TABLE_[0-9]+$"))
                return false;
            return name is not (
                "QUEST_NPC_GROUP_ID" or "CONVERSATION" or "GUILD_ID" or
                "STORE_TAG_RANK_1" or "STORE_TAG_RANK_2" or "STORE_TAG_RANK_3" or
                "STORE_TAG_RANK_4" or "STORE_TAG_RANK_5" or "BEAST_TYPE" or
                "PERMANENT_VFX_ID" or "PARALYZE" or "DAZE" or "AI_PROFILE" or "AI_FLAGS");
        }

        /// <summary>
        /// Lets the picker close and publish its selected item before any preview work starts.
        /// Multiple equipment or appearance changes in one UI turn collapse into one model rebuild.
        /// </summary>
        private void QueuePreviewSceneUpdate()
        {
            if (_disposed || _previewSceneUpdateQueued)
                return;

            _previewSceneUpdateQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _previewSceneUpdateQueued = false;
                UpdatePreviewScene();
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Snapshots the UTC on the UI thread, then parses and composes its model on a worker.
        /// A serialized gate discards superseded queued requests before they start, matching the
        /// progressive preview strategy used by the item editor.
        /// </summary>
        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            if (_resolveModel == null)
            {
                _previewModelGeneration++;
                IsModelPreviewLoading = false;
                ApplyPreviewScene(null);
                return;
            }

            var generation = ++_previewModelGeneration;
            IsModelPreviewLoading = true;
            ApplyPreviewScene(null, preserveTintRowsWhenEmpty: true);

            // The resolver never observes a partially applied edit, and no worker touches the
            // live document while undo/redo or another field mutation is in progress.
            var snapshot = new JsonGffDocument("UTC ", _store.Creature).ToBytes();
            _ = ResolvePreviewModelAsync(snapshot, generation);
        }

        private async Task ResolvePreviewModelAsync(byte[] snapshot, int generation)
        {
            RenderModel? model;
            await _previewModelGate.WaitAsync().ConfigureAwait(false);
            try
            {
                // Rapid item picks cost at most the active composition plus the newest state.
                if (generation != Volatile.Read(ref _previewModelGeneration))
                    return;

                model = await Task.Run(() =>
                {
                    var creature = JsonGffDocument.Parse(snapshot).Root;
                    return _resolveModel!(creature);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                model = null;
            }
            finally
            {
                _previewModelGate.Release();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_disposed || generation != _previewModelGeneration)
                    return;

                IsModelPreviewLoading = false;
                ApplyPreviewScene(model);
            });
        }

        private void ApplyPreviewScene(RenderModel? model, bool preserveTintRowsWhenEmpty = false)
        {
            if (model != null || !preserveTintRowsWhenEmpty)
            {
                TintMapEditor?.Reload(
                    model,
                    includeItemOwnedMaterials: false,
                    includeCreatureLayersFromItemOwnedMaterials: true);
                BodyParts.SetTintMapRows(TintMapEditor?.Colors);
                ReconcileBodySectionAvailability();
            }
            PublishAnimations(model);
            PreviewScene = model == null
                ? null
                : new AreaScene
                {
                    Tileset = string.Empty,
                    Width = 1,
                    Height = 1,
                    Tiles = Array.Empty<TilePlacement>(),
                    Instances = new[]
                    {
                        new InstanceMarker
                        {
                            Kind = InstanceMarkerKind.Creature,
                            TemplateResRef = TemplateResRef,
                            Tag = _store.GetString(BehaviorFieldStorage.Field, "Tag"),
                            Position = new Vector3(AreaSceneBuilder.TileSize / 2f, AreaSceneBuilder.TileSize / 2f, 0f),
                            // The default model-preview camera sits on the -Y side while creature
                            // fronts are authored toward +Y, so turn the model around to greet the user.
                            Orientation = new Vector2(-1f, 0f),
                            LayerColorIndices = CurrentLayerColors(model),
                            Model = model,
                            TintMapOverrides = TintMapOverrides.Read(_store.Locals)
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };
            OnPropertyChanged(nameof(PreviewScene));
        }

        /// <summary>
        /// Publishes the creature's current semantic palette colors on the scene instance. The
        /// retained model is an immutable geometry snapshot, so its palette dictionary still
        /// contains the values from the last model build. Reusing that dictionary made a color
        /// selection appear only after an unrelated body-part edit forced the model to rebuild.
        /// </summary>
        private IReadOnlyDictionary<int, int> CurrentLayerColors(RenderModel model)
        {
            var colors = model.LayerColorIndices.ToDictionary(pair => pair.Key, pair => pair.Value);
            colors[SWLOR.NWN.Formats.Plt.PltLayers.Skin] =
                (int)(_store.GetInteger(BehaviorFieldStorage.Field, "Color_Skin") ?? 0);
            colors[SWLOR.NWN.Formats.Plt.PltLayers.Hair] =
                (int)(_store.GetInteger(BehaviorFieldStorage.Field, "Color_Hair") ?? 0);
            colors[SWLOR.NWN.Formats.Plt.PltLayers.Tattoo1] =
                (int)(_store.GetInteger(BehaviorFieldStorage.Field, "Color_Tattoo1") ?? 0);
            colors[SWLOR.NWN.Formats.Plt.PltLayers.Tattoo2] =
                (int)(_store.GetInteger(BehaviorFieldStorage.Field, "Color_Tattoo2") ?? 0);
            return colors;
        }

        private void PublishAnimations(RenderModel? model)
        {
            var previous = SelectedPreviewAnimation?.Display;
            PreviewAnimations.Clear();
            if (model != null)
            {
                var names = model.Animations.Select(animation => animation.Name).ToList();
                var idle = !string.IsNullOrWhiteSpace(model.DefaultAnimationName)
                    ? model.DefaultAnimationName
                    : names.FirstOrDefault(name =>
                        name.Contains("pause", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("idle", StringComparison.OrdinalIgnoreCase));
                var walk = names.FirstOrDefault(name =>
                    name.Contains("walk", StringComparison.OrdinalIgnoreCase));
                var attack = names.FirstOrDefault(name =>
                    name.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("slash", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("stab", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("kick", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("shot", StringComparison.OrdinalIgnoreCase));

                // The three fixed segments are part of the editor contract. A model without one of
                // the optional clips keeps that segment visible and simply falls back to its rest pose.
                PreviewAnimations.Add(new CreatureAnimationOption("Idle", idle));
                PreviewAnimations.Add(new CreatureAnimationOption("Walk", walk));
                PreviewAnimations.Add(new CreatureAnimationOption("Attack", attack));
            }
            SelectedPreviewAnimation = PreviewAnimations.FirstOrDefault(option => option.Display == previous)
                                       ?? PreviewAnimations.FirstOrDefault();
        }

        partial void OnSelectedPreviewAnimationChanged(CreatureAnimationOption? value) =>
            OnPropertyChanged(nameof(PreviewAnimationName));

        partial void OnSelectedAppearanceSectionIndexChanged(int value) =>
            EnsureSelectedAppearanceSectionLoaded();

        partial void OnIsBehaviorTabSelectedChanged(bool value)
        {
            if (value)
            {
                QueueReferenceWarningUpdate();
                EnsureSelectedBehaviorChoicesLoaded();
            }
        }

        partial void OnIsAppearanceTabSelectedChanged(bool value)
        {
            if (!value)
                return;

            _ = EnsureAppearanceCatalogLoadedAsync();
            EnsureSelectedAppearanceSectionLoaded();
        }

        partial void OnIsEquipmentTabSelectedChanged(bool value)
        {
            if (value)
                EquipmentSlots.ActivateSelected();
        }

        partial void OnIsAbilitiesTabSelectedChanged(bool value)
        {
            if (value)
                _ = Abilities.EnsureLoadedAsync();
        }

        partial void OnIsLootTabSelectedChanged(bool value)
        {
            if (value)
                _ = Loot.EnsureLoadedAsync();
        }

        /// <summary>
        /// appearance.2da is thousands of rows. Load and project it only when Appearance becomes
        /// visible, then let the shared gallery keep its existing progressive tile behavior.
        /// </summary>
        public Task EnsureAppearanceCatalogLoadedAsync()
        {
            if (_appearanceCatalogLoaded || _disposed || _appearanceOptionsLoader == null)
                return Task.CompletedTask;
            if (_appearanceCatalogLoadTask != null)
                return _appearanceCatalogLoadTask;

            IsAppearanceCatalogLoading = true;
            _appearanceCatalogLoadTask = LoadAppearanceCatalogAsync();
            return _appearanceCatalogLoadTask;
        }

        private async Task LoadAppearanceCatalogAsync()
        {
            try
            {
                var options = await Task.Run(_appearanceOptionsLoader!).ConfigureAwait(true);
                if (_disposed)
                    return;

                AppearanceGallery?.SetOptions(options);
                _appearanceCatalogLoaded = true;
                AppearanceCatalogLoadError = string.Empty;
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    AppearanceCatalogLoadError = $"Appearances could not be loaded: {ex.Message}";
            }
            finally
            {
                IsAppearanceCatalogLoading = false;
                _appearanceCatalogLoadTask = null;
            }
        }

        private void EnsureSelectedAppearanceSectionLoaded()
        {
            if (SelectedAppearanceSectionIndex == 1)
            {
                _ = EnsureAppearanceDetailsLoadedAsync();
                return;
            }

            if (SelectedAppearanceSectionIndex != 2)
                return;

            if (BodyParts.HasEditableContent && BodyParts.IsDynamic)
                _ = BodyParts.EnsureLoadedAsync();
        }

        private void ReconcileBodySectionAvailability()
        {
            if (!BodyParts.HasEditableContent && SelectedAppearanceSectionIndex == 2)
                SelectedAppearanceSectionIndex = 0;
        }

        /// <summary>
        /// Details owns three catalogs that builders browse directly. Resolve them off the UI
        /// thread only when that section becomes visible, then let the shared controls publish a
        /// bounded search page or gallery page and request only visible previews.
        /// </summary>
        public Task EnsureAppearanceDetailsLoadedAsync()
        {
            if (_disposed)
                return Task.CompletedTask;

            return _appearanceDetailsLoadTask ??= LoadAppearanceDetailsAsync();
        }

        private async Task LoadAppearanceDetailsAsync()
        {
            try
            {
                // Paint the Details view before resolving its deferred catalogs. Race, portrait,
                // and sound-set data are independent, so they should not block one another.
                await Task.Yield();
                var activationTasks = AppearanceRows
                    .Where(row => row.Definition.IsInlineSearch || row.Definition.IsInlineGallery)
                    .Select(row => row.ActivateChoicesAsync())
                    .ToArray();
                await Task.WhenAll(activationTasks).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    _log?.AppendLine($"Could not load appearance details: {ex.Message}");
            }
            finally
            {
                _appearanceDetailsLoadTask = null;
            }
        }

        public void ReloadGameResources()
        {
            if (_disposed)
                return;

            _previewModelGeneration++;
            AppearanceGallery?.ReloadPreviews();
            UpdatePreviewScene();
        }

        public void ReloadTintMapCatalog(TintMapCatalog? catalog)
        {
            if (catalog == null)
            {
                TintMapEditor = null;
                BodyParts.SetTintMapRows(null);
                ReconcileBodySectionAvailability();
                return;
            }

            if (TintMapEditor == null)
            {
                TintMapEditor = new TintMapEditorViewModel(
                    _store.Locals,
                    RunEdit,
                    catalog,
                    colorChanged: OnTintColorChanged);
                UpdatePreviewScene();
                return;
            }

            TintMapEditor.ReloadCatalog(catalog);
            UpdatePreviewScene();
        }

        partial void OnTintMapEditorChanged(TintMapEditorViewModel? value) =>
            OnPropertyChanged(nameof(HasTintMapEditor));

        private void NotifySummary()
        {
            OnPropertyChanged(nameof(CreatureName));
            OnPropertyChanged(nameof(TemplateResRef));
        }

        partial void OnAppearanceCatalogLoadErrorChanged(string value) =>
            OnPropertyChanged(nameof(HasAppearanceCatalogLoadError));

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _previewModelGeneration++;
            _referenceWarningGeneration++;
            _previewSceneUpdateQueued = false;
            IsModelPreviewLoading = false;
            foreach (var row in AllRows())
                row.Dispose();
            Abilities.Dispose();
            Loot.Dispose();
            AppearanceGallery?.Dispose();
            Equipment.Dispose();
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
