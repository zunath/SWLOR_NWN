using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.TintMaps;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The behavior-shaped item editor. The base type chosen on Basic organizes everything after
    /// it: which family the item belongs to, which roles its Behavior rail offers, and which stat
    /// groups its Stats tab shows.
    /// </summary>
    public sealed partial class ItemEditorViewModel : ObservableObject, IModelPreviewSource, IDisposable
    {
        private readonly ItemValueStore _store;
        private bool _previewUpdateQueued;
        private bool _previewSceneUpdateQueued;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<IDocumentEdit?>? _captureCoalesceOrigin;
        private readonly Func<IDocumentEdit, string, Action, bool>? _runCoalescedEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<int, BaseItemRow?>? _baseItemRows;
        private readonly Func<JsonGffStruct, IconImage?>? _renderIcon;
        private readonly Func<JsonGffStruct, bool, RenderModel?>? _resolveModel;
        private ModelPreviewControl? _previewView;
        private RenderModel? _cachedModel;
        private string? _cachedModelSignature;
        private string? _pendingModelSignature;
        private IDocumentEdit? _pendingModelEditOrigin;
        private string? _pendingModelEditOriginSignature;
        private int _previewModelGeneration;
        private readonly SemaphoreSlim _previewModelGate = new(1);
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> BasicRows { get; } = new();

        /// <summary>The Check-kind Basic rows (Plot, Stolen, Cursed, Identified, No Economy), shown in their own Flags card.</summary>
        public ObservableCollection<BehaviorRowViewModel> FlagRows { get; } = new();

        public ItemStatsSectionViewModel Stats { get; }

        public ItemRequirementsSectionViewModel Requirements { get; }

        public ItemRoleSectionViewModel Roles { get; }

        /// <summary>Null when the session has no 2DA/resource layer to probe artwork with.</summary>
        public ItemAppearanceSectionViewModel? Appearance { get; }

        public bool ShowsAppearanceTab => Appearance != null;

        [ObservableProperty]
        private TintMapEditorViewModel? _tintMapEditor;

        public ItemSourceSectionViewModel Source { get; }

        public Sources.ObjectSourceSectionViewModel? PlacementSource { get; }

        public bool ShowsSourceTab => Source.IsLoaded || PlacementSource != null;

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private ItemRole _role = ItemRoleCatalog.Custom;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private Avalonia.Media.Imaging.Bitmap? _previewImage;

        [ObservableProperty]
        private bool _isModelPreviewLoading;

        [ObservableProperty]
        private int _selectedTabIndex;

        /// <summary>Null unless a resolver was supplied and the item's base type has a world model to preview.</summary>
        public AreaScene? PreviewScene { get; private set; }

        /// <summary>Whether the orbitable 3D viewport has anything to show alongside the 2D icon.</summary>
        public bool HasModelPreview => PreviewScene != null;

        /// <summary>
        /// Which mannequin wears an armor blueprint in the 3D preview. Only armor is previewed on
        /// a body, so only armor shows the toggle.
        /// </summary>
        [ObservableProperty]
        private bool _previewFemale;

        /// <summary>
        /// The other half of the body picker. The two halves are one choice, so this is simply the
        /// inverse - it exists because a segmented control binds each half to its own IsChecked,
        /// and a one-way negation would leave the male half unable to select itself.
        /// </summary>
        public bool PreviewMale
        {
            get => !PreviewFemale;
            set
            {
                if (value == !PreviewFemale)
                    return;
                PreviewFemale = !value;
            }
        }

        public bool ShowsMannequinToggle => Family == ItemFamily.Armor && _resolveModel != null;

        partial void OnPreviewFemaleChanged(bool value)
        {
            OnPropertyChanged(nameof(PreviewMale));
            // The inventory icon is gender-independent; only the mannequin geometry changes.
            QueuePreviewSceneUpdate();
        }

        public ResourceIndex? ResourceIndex { get; }

        public string? PreviewAnimationName => null;

        public bool IsAnimationPlaying => false;

        /// <summary>The reusable one-model viewport, lazily created the first time it is bound.</summary>
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

        public ItemFamily Family { get; private set; }

        /// <summary>The item's display name; the header shows it because equipment has no role to name it by.</summary>
        public string HeaderName
        {
            get
            {
                var name = _store.GetLocalizedText("LocalizedName");
                return string.IsNullOrWhiteSpace(name) ? HeaderOwner : name;
            }
        }

        public string HeaderKind => "blueprint";

        public string HeaderOwner { get; private set; }

        public string ItemTag => _store.GetString(BehaviorFieldStorage.Field, "Tag");

        public string TemplateResRef => _store.GetString(BehaviorFieldStorage.Field, "TemplateResRef");

        /// <summary>What the preview caption calls the item: its family, in words.</summary>
        public string FamilyDisplay => Family switch
        {
            ItemFamily.MeleeWeapon => "Melee Weapon",
            ItemFamily.RangedWeapon => "Ranged Weapon",
            ItemFamily.CreatureItem => "Creature Item",
            _ => Family.ToString()
        };

        /// <summary>
        /// Variables stays behind Custom where roles exist. Equipment families have no roles and
        /// therefore no Behavior tab to reach Custom through, so they always expose it - without
        /// this, a weapon's locals (the new-item template's NO_ECONOMY opt-out among them) would
        /// be uneditable anywhere in the toolset.
        /// </summary>
        public bool ShowsVariablesTab =>
            ItemRoleCatalog.RolesFor(Family).Count == 0 || Role.AllowsVariables;

        /// <summary>Equipment is what its base type says; only the carriable families choose a role.</summary>
        public bool ShowsBehaviorTab => ItemRoleCatalog.RolesFor(Family).Count > 0;

        /// <summary>
        /// A family/role combination with no stat groups hides the tab outright; the engine card is
        /// the one reason a group-less item still warrants it.
        /// </summary>
        public bool ShowsStatsTab =>
            Stats.Groups.Count > 0 || (Stats.Engine?.HasEntries ?? false);

        public string? Incomplete { get; private set; }

        public bool IsIncomplete => Incomplete != null;

        public ItemEditorViewModel(
            JsonGffStruct item,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            Func<int, BaseItemRow?>? baseItemRows = null,
            Func<JsonGffStruct, IconImage?>? renderIcon = null,
            ChoicePreviewService? choicePreviews = null,
            Services.IEditorPromptService? prompts = null,
            Func<int, BaseItemIconRow?>? baseItemIcons = null,
            Func<string, bool>? textureExists = null,
            Func<string, IReadOnlyList<Domain.Workspace.ItemSourceEntry>>? sourceLookup = null,
            Func<bool>? itemSourcesReady = null,
            bool isDirty = false,
            ItemCostTableRanges? costTables = null,
            Func<JsonGffStruct, bool, RenderModel?>? resolveModel = null,
            ResourceIndex? resourceIndex = null,
            ArmorDyeSwatchService? armorDyeSwatches = null,
            ArmorPartCatalog? armorPartModels = null,
            Sources.ObjectSourceSectionViewModel? placementSource = null,
            Workspace.OutputLogService? log = null,
            TintMapCatalog? tintMapCatalog = null,
            Func<IDocumentEdit?>? captureCoalesceOrigin = null,
            Func<IDocumentEdit, string, Action, bool>? runCoalescedEdit = null)
        {
            ArgumentNullException.ThrowIfNull(item);

            _store = new ItemValueStore(item);
            _runEdit = runEdit;
            _captureCoalesceOrigin = captureCoalesceOrigin;
            _runCoalescedEdit = runCoalescedEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _baseItemRows = baseItemRows;
            _renderIcon = renderIcon;
            _resolveModel = resolveModel;
            ResourceIndex = resourceIndex;
            _choicePreviews = choicePreviews;
            HeaderOwner = headerOwner;
            IsDirty = isDirty;

            ReclassifyFamily();
            Role = ItemRoleCatalog.Classify(_store, Family);
            BuildBasicRows();
            Stats = new ItemStatsSectionViewModel(
                _store,
                RunEdit,
                () => OnPropertyChanged(nameof(ShowsStatsTab)),
                resolveChoices,
                costTables);
            Stats.Rebuild(Family, Role.Id);
            Requirements = new ItemRequirementsSectionViewModel(_store, RunEdit, null, resolveChoices, costTables);
            Roles = new ItemRoleSectionViewModel(_store, RunEdit, resolveChoices, prompts, OnRoleChosen, log);
            Roles.Rebuild(Family, Role, FamilyDisplay);
            if (baseItemIcons != null && textureExists != null)
            {
                Appearance = new ItemAppearanceSectionViewModel(
                    _store, RunEdit, baseItemIcons, textureExists, choicePreviews, OnAppearanceChanged,
                    armorDyeSwatches, armorPartModels);
            }
            if (tintMapCatalog != null)
            {
                TintMapEditor = new TintMapEditorViewModel(
                    _store.Locals,
                    RunEdit,
                    tintMapCatalog,
                    colorChanged: UpdatePreview,
                    runCoalescedEdit: _runCoalescedEdit);
                Appearance?.SetTintMapEditor(TintMapEditor);
            }
            Source = new ItemSourceSectionViewModel(headerOwner, sourceLookup, itemSourcesReady);
            PlacementSource = placementSource;
            _lastAppearanceBaseItem = CurrentBaseItem();
            RebuildVariablesSection();
            RefreshCompleteness();
            UpdatePreview();
        }

        /// <summary>The rail chose a role; the classification-owning state follows it.</summary>
        private void OnRoleChosen(ItemRole role)
        {
            Role = role;
            RebuildVariablesSection();
            Stats.Rebuild(Family, role.Id);
            OnPropertyChanged(nameof(ShowsVariablesTab));
            OnPropertyChanged(nameof(ShowsStatsTab));
        }

        private readonly ChoicePreviewService? _choicePreviews;

        /// <summary>Called after a rename saves, so the header and Source lookup follow the file.</summary>
        public void SetHeaderOwner(string resRef)
        {
            HeaderOwner = resRef;
            Source.Refresh(resRef);
            PlacementSource?.SetResRef(resRef);
            OnPropertyChanged(nameof(HeaderOwner));
            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(Source));
        }

        /// <summary>
        /// Writes the save-time canonical form of the resref (trimmed, lowercased) back into the
        /// field and its row, as one undoable step.
        /// </summary>
        public bool NormalizeResRef(string value)
        {
            if (!RunEdit("Normalize ResRef", () =>
                    _store.SetString(BehaviorFieldStorage.Field, "TemplateResRef", GffFieldType.ResRef, value)))
            {
                return false;
            }

            foreach (var row in BasicRows.Where(row => row.Definition.Name == "TemplateResRef"))
                row.Reload();
            OnPropertyChanged(nameof(TemplateResRef));
            return true;
        }

        public void SetDirty(bool value) => IsDirty = value;

        /// <summary>Re-queries obtainability after a save may have changed what grants the item.</summary>
        public void RefreshSource()
        {
            Source.Refresh(HeaderOwner);
            OnPropertyChanged(nameof(Source));
            OnPropertyChanged(nameof(ShowsSourceTab));
        }

        /// <summary>
        /// Enforces item invariants that depend on workspace-wide source data immediately before
        /// serialization. Returns a user-facing problem when the source index cannot answer yet.
        /// </summary>
        public string? EnforceSaveInvariants()
        {
            if (!Source.IsLoaded)
                return null;
            if (!Source.IsReady)
                return "Cannot save while the item obtainability index is still being built.";
            var targetResRef = TemplateResRef.Trim().ToLowerInvariant();
            if (Source.HasPlayerSource(targetResRef) ||
                _store.GetInteger(
                    BehaviorFieldStorage.Local,
                    ItemEditorLayout.NoEconomyLocal) == 1)
            {
                return null;
            }

            if (!RunEdit(
                    "Keep unobtainable item out of the economy",
                    () => _store.SetInteger(
                        BehaviorFieldStorage.Local,
                        ItemEditorLayout.NoEconomyLocal,
                        GffFieldType.Int,
                        1)))
            {
                return "Could not restore NO_ECONOMY for an item with no player source.";
            }

            ReloadNoEconomyRow();
            return null;
        }

        public void ReloadFromDocument()
        {
            var previousFamily = Family;
            var previousRole = Role.Id;
            ReclassifyFamily();

            var classified = ItemRoleCatalog.Classify(_store, Family);
            if (classified.Id != Role.Id || previousFamily != Family)
            {
                Role = classified;
                RebuildVariablesSection();
            }

            foreach (var row in AllBasicRows())
                row.Reload();
            if (previousFamily != Family || previousRole != Role.Id)
                Stats.Rebuild(Family, Role.Id);
            else
                Stats.ReloadFromDocument();
            OnPropertyChanged(nameof(ShowsStatsTab));
            Requirements.ReloadFromDocument();
            Roles.Rebuild(Family, Role, FamilyDisplay);
            RefreshAppearanceForBaseItem();
            Appearance?.ReloadFromDocument();
            Variables?.RefreshFromDocument();
            foreach (var row in AllBasicRows())
                row.RefreshStatus();

            OnHeaderFieldsChanged();
            RefreshCompleteness();
            UpdatePreview();
        }

        /// <summary>Rebuilds the category row after its module ITP changes.</summary>
        public void RefreshPaletteChoices()
        {
            var index = BasicRows
                .Select((row, rowIndex) => (row, rowIndex))
                .Where(entry => entry.row.Definition.Name == "PaletteID")
                .Select(entry => entry.rowIndex)
                .DefaultIfEmpty(-1)
                .Single();
            if (index < 0)
                return;

            var definition = BasicRows[index].Definition;
            BasicRows[index].Dispose();
            BasicRows[index] = CreateRow(definition);
            RefreshCompleteness();
        }

        /// <summary>Rebuilds every materialized choice row after TLK-backed labels change.</summary>
        public void RefreshTlkLabels()
        {
            for (var index = 0; index < BasicRows.Count; index++)
            {
                if (BasicRows[index].Definition.ChoicesKey == null)
                    continue;

                var definition = BasicRows[index].Definition;
                BasicRows[index].Dispose();
                BasicRows[index] = CreateRow(definition);
            }

            Roles.Rebuild(Family, Role, FamilyDisplay);
            RefreshCompleteness();
        }

        private bool RunEdit(string description, Action mutation)
        {
            var identifiedBefore = _store.GetLocalizedText("DescIdentified");
            var noEconomyBefore = _store.GetInteger(
                BehaviorFieldStorage.Local,
                ItemEditorLayout.NoEconomyLocal);

            var applied = _runEdit(description, () =>
            {
                mutation();

                // SWLOR exposes one description even though Aurora stores identified and
                // unidentified slots. The visible DescIdentified field is authoritative.
                var identifiedAfter = _store.GetLocalizedText("DescIdentified");
                if (!string.Equals(identifiedAfter, identifiedBefore, StringComparison.Ordinal))
                {
                    _store.CopyLocalizedValue("DescIdentified", "Description");
                }

                // A source-less item must remain excluded from player-facing economy searches.
                // Restore an attempted clear inside the same undo transaction as the checkbox edit.
                var noEconomyAfter = _store.GetInteger(
                    BehaviorFieldStorage.Local,
                    ItemEditorLayout.NoEconomyLocal);
                if (noEconomyBefore == 1 &&
                    noEconomyAfter != 1 &&
                    Source.IsLoaded &&
                    Source.IsReady &&
                    !Source.HasPlayerSource(TemplateResRef.Trim().ToLowerInvariant()))
                {
                    _store.SetInteger(
                        BehaviorFieldStorage.Local,
                        ItemEditorLayout.NoEconomyLocal,
                        GffFieldType.Int,
                        1);
                }
            });
            if (applied)
                IsDirty = true;
            return applied;
        }

        private void ReloadNoEconomyRow()
        {
            foreach (var row in FlagRows.Where(row =>
                         row.Definition.Name == ItemEditorLayout.NoEconomyLocal))
            {
                row.Reload();
            }
        }

        private void BuildBasicRows()
        {
            foreach (var definition in ItemEditorLayout.Basic)
            {
                var row = CreateRow(definition);
                if (definition.Kind == BehaviorFieldKind.Check)
                    FlagRows.Add(row);
                else
                    BasicRows.Add(row);
            }
        }

        /// <summary>Every Basic-tab row, split or not - Reload/RefreshStatus/Dispose sweep both collections.</summary>
        private IEnumerable<BehaviorRowViewModel> AllBasicRows() => BasicRows.Concat(FlagRows);

        private BehaviorRowViewModel CreateRow(BehaviorFieldDefinition definition)
        {
            var row = new BehaviorRowViewModel(
                definition,
                _store,
                RunEdit,
                ResolveChoices(definition),
                () => OnRowChanged(definition),
                _choicePreviews);

            // The base row leaves the initial read to the concrete row's constructor; these rows
            // ARE the base class, so the read happens here.
            row.Reload();
            return row;
        }

        private IReadOnlyList<BehaviorChoice> ResolveChoices(BehaviorFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
        }

        private void OnRowChanged(BehaviorFieldDefinition definition)
        {
            if (definition.Name == ItemEditorLayout.NoEconomyLocal)
                ReloadNoEconomyRow();

            if (definition.Name == "BaseItem")
            {
                // Only a real user-driven base-type change should pick a default appearance for
                // the builder - never the initial construction/reload paths, which must leave
                // whatever the document already stores untouched (the byte-stability audit sweep
                // constructs the editor over every corpus blueprint and asserts nothing is written).
                RefreshAppearanceForBaseItem(ensureSelection: true);
                var previous = Family;
                ReclassifyFamily();
                if (previous != Family)
                {
                    var classified = ItemRoleCatalog.Classify(_store, Family);
                    if (classified.Id != Role.Id)
                    {
                        Role = classified;
                        RebuildVariablesSection();
                    }
                    Stats.Rebuild(Family, Role.Id);
                    Roles.Rebuild(Family, Role, FamilyDisplay);
                    OnPropertyChanged(nameof(ShowsVariablesTab));

                    // Raised again AFTER the rebuild: ReclassifyFamily above already announced it,
                    // but at that moment Stats still held the outgoing family's groups, so the tab
                    // read as populated and then quietly emptied - a Stats tab with nothing in it.
                    OnPropertyChanged(nameof(ShowsStatsTab));
                }

                // The base type is the only Basic row that changes the artwork; re-rendering
                // the icon and 3D scene per keystroke of Name/Tag/ResRef would decode every
                // texture layer on the UI thread for fields that never touch them.
                UpdatePreview();
            }

            foreach (var row in AllBasicRows())
                row.RefreshStatus();

            OnHeaderFieldsChanged();
            RefreshCompleteness();
        }

        private void OnHeaderFieldsChanged()
        {
            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(ItemTag));
            OnPropertyChanged(nameof(TemplateResRef));
            OnPropertyChanged(nameof(FamilyDisplay));
        }

        /// <summary>The last BaseItem the Appearance tab was built for; a change re-probes artwork.</summary>
        private int _lastAppearanceBaseItem = -1;

        private int CurrentBaseItem() =>
            (int)(_store.GetInteger(BehaviorFieldStorage.Field, "BaseItem") ?? -1);

        private void RefreshAppearanceForBaseItem(bool ensureSelection = false)
        {
            var baseItem = CurrentBaseItem();
            if (baseItem == _lastAppearanceBaseItem)
                return;

            _lastAppearanceBaseItem = baseItem;
            Appearance?.Rebuild();
            if (ensureSelection)
                Appearance?.EnsureSelection();
        }

        private void ReclassifyFamily()
        {
            var baseItem = (int)(_store.GetInteger(BehaviorFieldStorage.Field, "BaseItem") ?? -1);
            var row = baseItem < 0 ? null : _baseItemRows?.Invoke(baseItem);
            Family = row == null
                ? ItemFamily.Miscellaneous
                : ItemFamilyClassifier.Classify(row);
            OnPropertyChanged(nameof(Family));
            OnPropertyChanged(nameof(FamilyDisplay));
            OnPropertyChanged(nameof(ShowsVariablesTab));
            OnPropertyChanged(nameof(ShowsBehaviorTab));
            OnPropertyChanged(nameof(ShowsStatsTab));
            OnPropertyChanged(nameof(ShowsMannequinToggle));
        }

        private void RebuildVariablesSection()
        {
            Variables = ShowsVariablesTab
                ? new VarTableSectionViewModel(RunEdit, _store.Locals, _gameCodeIndex)
                : null;
            OnPropertyChanged(nameof(ShowsVariablesTab));
        }

        private void RefreshCompleteness()
        {
            var missing = AllBasicRows()
                .Where(row => row.IsRequired && !row.HasValue)
                .Select(row => row.Label)
                .ToList();

            Incomplete = missing.Count == 0
                ? null
                : $"The item still needs {string.Join(", ", missing)}.";
            OnPropertyChanged(nameof(Incomplete));
            OnPropertyChanged(nameof(IsIncomplete));
        }

        private void UpdatePreview()
        {
            if (_disposed)
                return;

            var icon = _renderIcon?.Invoke(_store.Item);
            ReplacePreviewImage(icon == null ? null : Workspace.ThumbnailService.ToBitmap(icon));
            UpdatePreviewScene();
        }

        private void ReplacePreviewImage(Avalonia.Media.Imaging.Bitmap? image)
        {
            var previous = PreviewImage;
            PreviewImage = image;
            if (!ReferenceEquals(previous, image))
                previous?.Dispose();
        }

        /// <summary>
        /// Lets an appearance control paint its new selection before decoding the icon and composing
        /// the 3D model. Multiple field changes in one UI turn collapse into one preview rebuild.
        /// </summary>
        private void QueuePreviewUpdate()
        {
            if (_disposed || _previewUpdateQueued)
                return;

            _previewUpdateQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _previewUpdateQueued = false;
                UpdatePreview();
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Appearance controls invoke this synchronously after their document edit commits. Capture
        /// that transaction before the background-priority preview callback can observe a later,
        /// unrelated edit as the current undo entry.
        /// </summary>
        private void OnAppearanceChanged()
        {
            var signature = GeometrySignature();
            if (_cachedModelSignature != null &&
                !string.Equals(_cachedModelSignature, signature, StringComparison.Ordinal) &&
                !string.Equals(
                    _pendingModelEditOriginSignature,
                    signature,
                    StringComparison.Ordinal))
            {
                _pendingModelEditOrigin = _captureCoalesceOrigin?.Invoke();
                _pendingModelEditOriginSignature = signature;
            }

            QueuePreviewUpdate();
        }

        private void ClearPendingModelEditOrigin(string? expectedSignature = null)
        {
            if (expectedSignature != null &&
                !string.Equals(
                    _pendingModelEditOriginSignature,
                    expectedSignature,
                    StringComparison.Ordinal))
            {
                return;
            }

            _pendingModelEditOrigin = null;
            _pendingModelEditOriginSignature = null;
        }

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
        /// Starts rebuilding the orbitable 3D scene alongside the 2D icon. Model parsing and
        /// composition run on a worker so opening the editor and changing a part never block the UI.
        /// </summary>
        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            if (_resolveModel == null)
            {
                _previewModelGeneration++;
                _pendingModelSignature = null;
                ClearPendingModelEditOrigin();
                IsModelPreviewLoading = false;
                ApplyPreviewScene(null, carryItemCustomColorsAcrossMaterials: false);
                return;
            }

            var signature = GeometrySignature();
            var carryItemCustomColors =
                _cachedModelSignature != null &&
                !string.Equals(
                    _cachedModelSignature,
                    signature,
                    StringComparison.Ordinal);
            var carryOrigin = carryItemCustomColors ? _pendingModelEditOrigin : null;
            if (_cachedModelSignature == signature)
            {
                _previewModelGeneration++;
                _pendingModelSignature = null;
                ClearPendingModelEditOrigin();
                IsModelPreviewLoading = false;
                ApplyPreviewScene(
                    _cachedModel,
                    carryItemCustomColorsAcrossMaterials: carryItemCustomColors,
                    coalesceOrigin: carryOrigin);
                return;
            }

            if (_pendingModelSignature == signature)
                return;

            var generation = ++_previewModelGeneration;
            _pendingModelSignature = signature;
            IsModelPreviewLoading = true;
            ApplyPreviewScene(
                null,
                carryItemCustomColorsAcrossMaterials: carryItemCustomColors,
                coalesceOrigin: carryOrigin);

            // Snapshot on the UI thread. The background resolver never observes a field halfway
            // through another edit, and an older completion is discarded by its generation.
            var snapshot = new JsonGffDocument("UTI ", _store.Item).ToBytes();
            var female = PreviewFemale;
            _ = ResolvePreviewModelAsync(
                snapshot,
                female,
                signature,
                carryItemCustomColors,
                generation);
        }

        public void ReloadGameResources()
        {
            if (_disposed)
                return;

            _previewModelGeneration++;
            _pendingModelSignature = null;
            _cachedModel = null;
            _cachedModelSignature = null;
            ClearPendingModelEditOrigin();
            UpdatePreview();
        }

        public void ReloadTintMapCatalog(TintMapCatalog? catalog)
        {
            if (catalog == null)
            {
                TintMapEditor = null;
                Appearance?.SetTintMapEditor(null);
                return;
            }

            if (TintMapEditor == null)
            {
                TintMapEditor = new TintMapEditorViewModel(
                    _store.Locals,
                    RunEdit,
                    catalog,
                    colorChanged: UpdatePreview,
                    runCoalescedEdit: _runCoalescedEdit);
                Appearance?.SetTintMapEditor(TintMapEditor);
                return;
            }

            TintMapEditor.ReloadCatalog(catalog);
        }

        /// <summary>
        /// Refreshes immutable picker and cost-table models after the module's custom content changes.
        /// </summary>
        public void ReloadGameResources(ItemCostTableRanges? costTables)
        {
            if (_disposed)
                return;

            Stats.ReloadGameResources(costTables);
            Requirements.ReloadGameResources(costTables);
            OnPropertyChanged(nameof(ShowsStatsTab));
            ReloadGameResources();
        }

        private async Task ResolvePreviewModelAsync(
            byte[] snapshot,
            bool female,
            string signature,
            bool carryItemCustomColors,
            int generation)
        {
            RenderModel? model;
            await _previewModelGate.WaitAsync().ConfigureAwait(false);
            try
            {
                // An older request waiting behind an expensive composition is discarded before it
                // starts. Rapid part clicks therefore cost at most the active render plus the
                // newest one, rather than rendering every intermediate selection.
                if (generation != Volatile.Read(ref _previewModelGeneration))
                    return;

                model = await Task.Run(() =>
                {
                    var item = JsonGffDocument.Parse(snapshot).Root;
                    return _resolveModel!(item, female);
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

                _pendingModelSignature = null;
                _cachedModel = model;
                _cachedModelSignature = signature;
                ClearPendingModelEditOrigin(signature);
                IsModelPreviewLoading = false;
                ApplyPreviewScene(
                    model,
                    carryItemCustomColorsAcrossMaterials: carryItemCustomColors);
            });
        }

        private void ApplyPreviewScene(
            RenderModel? model,
            bool carryItemCustomColorsAcrossMaterials,
            IDocumentEdit? coalesceOrigin = null)
        {
            var hasItemOwnedMeshes = model?.Meshes.Any(mesh => mesh.UsesItemTintOverrides) == true;
            TintMapEditor?.Reload(
                model,
                includeNonItemOwnedMaterials: !hasItemOwnedMeshes,
                carryItemCustomColorsAcrossMaterials: carryItemCustomColorsAcrossMaterials,
                coalesceOrigin: coalesceOrigin);
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
                            Kind = InstanceMarkerKind.Item,
                            TemplateResRef = TemplateResRef,
                            Tag = ItemTag,
                            Position = new Vector3(
                                AreaSceneBuilder.TileSize / 2f,
                                AreaSceneBuilder.TileSize / 2f,
                                0f),
                            // Turned to face the camera. The default view sits at azimuth 225 - on
                            // the -Y side - and a body's front is +Y (its cloak hangs at negative Y),
                            // so an unrotated mannequin presented its back on every load.
                            Orientation = new Vector2(-1f, 0f),
                            LayerColorIndices = CurrentLayerColors(),
                            Model = model,
                            TintMapOverrides = TintMapOverrides.Read(_store.Locals)
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
            OnPropertyChanged(nameof(HasModelPreview));
        }

        /// <summary>Everything that changes the shape of the preview, and nothing that only recolours it.</summary>
        private string GeometrySignature()
        {
            var parts = new System.Text.StringBuilder();
            parts.Append(PreviewFemale ? 'f' : 'm');
            parts.Append(':').Append(ItemGeometrySignature());

            return parts.ToString();
        }

        private string ItemGeometrySignature()
        {
            var parts = new System.Text.StringBuilder();
            parts.Append(CurrentBaseItem());

            foreach (var field in GeometryFields)
                parts.Append(':').Append(ItemAppearanceValues.Read(_store.Item, field) ?? -1);

            return parts.ToString();
        }

        private static readonly string[] GeometryFields =
        {
            "ModelPart1", "ModelPart2", "ModelPart3",
            "ArmorPart_Neck", "ArmorPart_Torso", "ArmorPart_Belt", "ArmorPart_Pelvis", "ArmorPart_Robe",
            "ArmorPart_LShoul", "ArmorPart_RShoul", "ArmorPart_LBicep", "ArmorPart_RBicep",
            "ArmorPart_LFArm", "ArmorPart_RFArm", "ArmorPart_LHand", "ArmorPart_RHand",
            "ArmorPart_LThigh", "ArmorPart_RThigh", "ArmorPart_LShin", "ArmorPart_RShin",
            "ArmorPart_LFoot", "ArmorPart_RFoot"
        };

        /// <summary>The item's dye choices, which travel on the scene instance rather than the model.</summary>
        private IReadOnlyDictionary<int, int> CurrentLayerColors()
        {
            // Every layer is named, including the ones an item has no field for: leaving a layer out
            // is not the same as setting it to 0, because the viewport's override replaces the
            // model's whole set. Row 0 is what Aurora shows for an unspecified layer.
            var colors = new Dictionary<int, int>
            {
                [SWLOR.NWN.Formats.Plt.PltLayers.Skin] = 0,
                [SWLOR.NWN.Formats.Plt.PltLayers.Hair] = 0,
                [SWLOR.NWN.Formats.Plt.PltLayers.Tattoo1] = 0,
                [SWLOR.NWN.Formats.Plt.PltLayers.Tattoo2] = 0,
            };

            foreach (var (layer, field) in DyeFields)
                colors[layer] = (int)(_store.GetInteger(BehaviorFieldStorage.Field, field) ?? 0);

            return colors;
        }

        private static readonly (int Layer, string Field)[] DyeFields =
        {
            (SWLOR.NWN.Formats.Plt.PltLayers.Cloth1, "Cloth1Color"),
            (SWLOR.NWN.Formats.Plt.PltLayers.Cloth2, "Cloth2Color"),
            (SWLOR.NWN.Formats.Plt.PltLayers.Leather1, "Leather1Color"),
            (SWLOR.NWN.Formats.Plt.PltLayers.Leather2, "Leather2Color"),
            (SWLOR.NWN.Formats.Plt.PltLayers.Metal1, "Metal1Color"),
            (SWLOR.NWN.Formats.Plt.PltLayers.Metal2, "Metal2Color"),
        };

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _previewModelGeneration++;
            _pendingModelSignature = null;
            ClearPendingModelEditOrigin();
            IsModelPreviewLoading = false;
            foreach (var row in AllBasicRows())
                row.Dispose();
            ReplacePreviewImage(null);
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
