using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The behavior-shaped item editor. The base type chosen on Basic organizes everything after
    /// it: which family the item belongs to, which roles its Behavior rail offers, and which stat
    /// groups its Stats tab shows.
    /// </summary>
    public sealed partial class ItemEditorViewModel : ObservableObject, IDisposable
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<int, BaseItemRow?>? _baseItemRows;
        private readonly Func<JsonGffStruct, IconImage?>? _renderIcon;
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> BasicRows { get; } = new();

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

        /// <summary>Variables stays behind Custom, and equipment families have no roles at all.</summary>
        public bool ShowsVariablesTab =>
            Role.AllowsVariables && ItemRoleCatalog.RolesFor(Family).Count > 0;

        /// <summary>Equipment is what its base type says; only the carriable families choose a role.</summary>
        public bool ShowsBehaviorTab => ItemRoleCatalog.RolesFor(Family).Count > 0;

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
            bool isDirty = false)
        {
            ArgumentNullException.ThrowIfNull(item);

            _store = new ItemValueStore(item);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _baseItemRows = baseItemRows;
            _renderIcon = renderIcon;
            _choicePreviews = choicePreviews;
            HeaderOwner = headerOwner;
            IsDirty = isDirty;

            ReclassifyFamily();
            Role = ItemRoleCatalog.Classify(_store, Family);
            BuildBasicRows();
            Stats = new ItemStatsSectionViewModel(_store, RunEdit, null, resolveChoices);
            Stats.Rebuild(Family, Role.Id);
            Requirements = new ItemRequirementsSectionViewModel(_store, RunEdit, null, resolveChoices);
            Roles = new ItemRoleSectionViewModel(_store, RunEdit, resolveChoices, prompts, OnRoleChosen);
            Roles.Rebuild(Family, Role, FamilyDisplay);
            if (baseItemIcons != null && textureExists != null)
            {
                Appearance = new ItemAppearanceSectionViewModel(
                    _store, RunEdit, baseItemIcons, textureExists, choicePreviews, UpdatePreview);
            }
            Source = new ItemSourceSectionViewModel(headerOwner, sourceLookup);
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

            foreach (var row in BasicRows)
                row.Reload();
            if (previousFamily != Family || previousRole != Role.Id)
                Stats.Rebuild(Family, Role.Id);
            else
                Stats.ReloadFromDocument();
            Requirements.ReloadFromDocument();
            Roles.Rebuild(Family, Role, FamilyDisplay);
            RefreshAppearanceForBaseItem();
            Appearance?.ReloadFromDocument();
            Variables?.RefreshFromDocument();
            foreach (var row in BasicRows)
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
                BasicRows.Add(CreateRow(definition));
        }

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
                RefreshAppearanceForBaseItem();
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
                }
            }

            foreach (var row in BasicRows)
                row.RefreshStatus();

            OnHeaderFieldsChanged();
            RefreshCompleteness();
            UpdatePreview();
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

        private void RefreshAppearanceForBaseItem()
        {
            var baseItem = CurrentBaseItem();
            if (baseItem == _lastAppearanceBaseItem)
                return;

            _lastAppearanceBaseItem = baseItem;
            Appearance?.Rebuild();
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
            var missing = BasicRows
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
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var row in BasicRows)
                row.Dispose();
            PreviewImage = null;
        }
    }
}
