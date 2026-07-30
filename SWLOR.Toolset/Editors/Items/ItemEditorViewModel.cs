using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Editors.Behaviors;
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
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<int, BaseItemRow?>? _baseItemRows;
        private readonly Func<JsonGffStruct, IconImage?>? _renderIcon;
        private readonly Func<JsonGffStruct, bool, RenderModel?>? _resolveModel;
        private ModelPreviewControl? _previewView;
        private RenderModel? _cachedModel;
        private string? _cachedModelSignature;
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

        public ItemSourceSectionViewModel Source { get; }

        public bool ShowsSourceTab => Source.IsLoaded;

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private ItemRole _role = ItemRoleCatalog.Custom;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private Avalonia.Media.Imaging.Bitmap? _previewImage;

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
            UpdatePreview();
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
            ArmorPartCatalog? armorPartModels = null)
        {
            ArgumentNullException.ThrowIfNull(item);

            _store = new ItemValueStore(item);
            _runEdit = runEdit;
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
            Stats = new ItemStatsSectionViewModel(_store, RunEdit, null, resolveChoices, costTables);
            Stats.Rebuild(Family, Role.Id);
            Requirements = new ItemRequirementsSectionViewModel(_store, RunEdit, null, resolveChoices, costTables);
            Roles = new ItemRoleSectionViewModel(_store, RunEdit, resolveChoices, prompts, OnRoleChosen);
            Roles.Rebuild(Family, Role, FamilyDisplay);
            if (baseItemIcons != null && textureExists != null)
            {
                Appearance = new ItemAppearanceSectionViewModel(
                    _store, RunEdit, baseItemIcons, textureExists, choicePreviews, UpdatePreview,
                    armorDyeSwatches, armorPartModels);
            }
            Source = new ItemSourceSectionViewModel(headerOwner, sourceLookup, itemSourcesReady);
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

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                IsDirty = true;
            return applied;
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
            PreviewImage = icon == null ? null : Workspace.ThumbnailService.ToBitmap(icon);
            UpdatePreviewScene();
        }

        /// <summary>
        /// Rebuilds the orbitable 3D scene alongside the 2D icon, so both refresh together on every
        /// appearance/base-type edit. Null when no resolver was supplied (construction-only callers,
        /// which must never resolve or render anything) or the base type has no world model worth
        /// showing (<see cref="Domain.Render.BlueprintModelResolver"/> decides that).
        /// </summary>
        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            var model = ResolveCachedModel();
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
                            Model = model
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
            OnPropertyChanged(nameof(HasModelPreview));
        }

        /// <summary>
        /// The composed preview model, reused while nothing about the item's GEOMETRY has changed.
        /// </summary>
        /// <remarks>
        /// Resolving it means reading every body-part model, composing them onto a skeleton and
        /// rebuilding the viewport's vertex buffers. Dye edits do none of that - they only recolour
        /// textures - so doing the whole job on each swatch click is what made picking a colour lag.
        /// </remarks>
        private RenderModel? ResolveCachedModel()
        {
            if (_resolveModel == null)
                return null;

            var signature = GeometrySignature();
            if (_cachedModelSignature == signature)
                return _cachedModel;

            _cachedModel = _resolveModel(_store.Item, PreviewFemale);
            _cachedModelSignature = signature;
            return _cachedModel;
        }

        /// <summary>Everything that changes the shape of the preview, and nothing that only recolours it.</summary>
        private string GeometrySignature()
        {
            var parts = new System.Text.StringBuilder();
            parts.Append(PreviewFemale ? 'f' : 'm');
            parts.Append(':').Append(CurrentBaseItem());

            foreach (var field in GeometryFields)
                parts.Append(':').Append(_store.GetInteger(BehaviorFieldStorage.Field, field) ?? -1);

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
            foreach (var row in AllBasicRows())
                row.Dispose();
            PreviewImage = null;
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
