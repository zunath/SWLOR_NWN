using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>One row of an instance-list grid: enough to display and re-locate the backing
    /// struct (by list index) for the detail form below the grid.</summary>
    public sealed class InstanceRow : ObservableObject
    {
        private string _tag;
        private float _x;
        private float _y;
        private float _z;
        private string _templateResRef;
        private string _displayName;

        public int Index { get; }

        /// <summary>
        /// The name this placement carries itself, or empty when it inherits its blueprint's.
        /// </summary>
        /// <remarks>
        /// Read here rather than resolved from the blueprint, because the two disagree constantly:
        /// see <see cref="InstanceFieldMap.GetDisplayName"/>. Callers that want a name that is never
        /// blank should go through <c>AreaEditorViewModel.ResolveInstanceName</c>, which falls back
        /// through the blueprint, the tag, and the resref.
        /// </remarks>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string TemplateResRef
        {
            get => _templateResRef;
            set => SetProperty(ref _templateResRef, value);
        }

        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        public float X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public float Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public float Z
        {
            get => _z;
            set => SetProperty(ref _z, value);
        }

        public InstanceRow(
            int index, string tag, string templateResRef, float x, float y, float z,
            string displayName = "")
        {
            Index = index;
            _tag = tag;
            _templateResRef = templateResRef;
            _x = x;
            _y = y;
            _z = z;
            _displayName = displayName;
        }
    }

    /// <summary>
    /// One expandable section of the composite area editor: the placed-instance list for a
    /// single blueprint type (e.g. "Creature List"). Lists the placed instances in a grid,
    /// edits the selected instance's Tag/position/heading and local-variable table through a
    /// small detail form, and supports Add (via a palette browser + InstanceFieldMap),
    /// Duplicate, and Delete - all through DocumentTransactions on the shared .git
    /// DocumentSession supplied by the owning AreaEditorViewModel.
    /// </summary>
    public partial class InstanceListSectionViewModel : ObservableObject, IDisposable
    {
        private readonly DocumentSession _gitSession;
        private readonly DocumentSession _gicSession;
        private readonly ModuleWorkspace _workspace;
        private readonly ResourceType _blueprintType;
        private readonly string _listFieldName;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly Doors.DoorEditorServices? _doorEditorServices;
        private WaypointEditorServices? _waypointEditorServices;
        private readonly string _soundHeaderOwner;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveSoundChoices;
        private readonly IReadOnlyList<string> _audioResources;
        private readonly Services.SoundPreviewService? _soundPreview;

        /// <summary>Resolves the STRREF labels the module's palettes use instead of inline names.</summary>
        private readonly Func<uint, string?>? _resolveStrRef;
        private bool _isLoadingDetail;

        public string Title { get; }

        /// <summary>The blueprint type this section's list holds (e.g. Utc for Creatures) - used by AreaEditorViewModel to map a 3D-view <see cref="Domain.Render.InstanceMarkerKind"/> to the matching section for selection sync.</summary>
        public ResourceType BlueprintType => _blueprintType;

        public ObservableCollection<InstanceRow> Rows { get; } = new();

        [ObservableProperty]
        private InstanceRow? _selectedRow;

        [ObservableProperty]
        private bool _hasSelection;

        /// <summary>
        /// Whether this kind's placed-instance details are open on the area's Properties tab.
        /// Kept on the view model so switching document tabs does not collapse the builder's work.
        /// </summary>
        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private string _detailTag = string.Empty;

        [ObservableProperty]
        private double _detailX;

        [ObservableProperty]
        private double _detailY;

        [ObservableProperty]
        private double _detailZ;

        [ObservableProperty]
        private double _detailXOrientation;

        [ObservableProperty]
        private double _detailYOrientation;

        [ObservableProperty]
        private double _detailTriggerWidth;

        [ObservableProperty]
        private double _detailTriggerHeight;

        public bool HasTriggerGeometry => _blueprintType == ResourceType.Utt;

        public bool UsesDoorEditor => _blueprintType == ResourceType.Utd;

        public bool UsesGenericDetailEditor =>
            !UsesDoorEditor && !HasWaypointBehaviorEditor && !HasSoundBehaviorEditor;

        [ObservableProperty]
        private Doors.DoorEditorViewModel? _doorEditor;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasWaypointBehaviorEditor))]
        [NotifyPropertyChangedFor(nameof(UsesGenericDetailEditor))]
        private WaypointEditorViewModel? _waypointEditor;

        public bool HasWaypointBehaviorEditor => WaypointEditor != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSoundBehaviorEditor))]
        [NotifyPropertyChangedFor(nameof(UsesGenericDetailEditor))]
        private SoundEditorViewModel? _soundEditor;

        public bool HasSoundBehaviorEditor => SoundEditor != null;

        [ObservableProperty]
        private VarTableSectionViewModel? _varTableSection;

        [ObservableProperty]
        private PaletteBrowserViewModel? _activePaletteBrowser;

        public InstanceListSectionViewModel(
            string title,
            string listFieldName,
            ResourceType blueprintType,
            DocumentSession gitSession,
            DocumentSession gicSession,
            ModuleWorkspace workspace,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<uint, string?>? resolveStrRef = null,
            Doors.DoorEditorServices? doorEditorServices = null,
            WaypointEditorServices? waypointEditorServices = null,
            string? soundHeaderOwner = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveSoundChoices = null,
            IReadOnlyList<string>? audioResources = null,
            Services.SoundPreviewService? soundPreview = null)
        {
            Title = title;
            _listFieldName = listFieldName;
            _blueprintType = blueprintType;
            _gitSession = gitSession;
            _gicSession = gicSession;
            _workspace = workspace;
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _log = log;
            _prompts = prompts;
            _resolveStrRef = resolveStrRef;
            _doorEditorServices = doorEditorServices;
            _waypointEditorServices = waypointEditorServices;
            _soundHeaderOwner = soundHeaderOwner ?? string.Empty;
            _resolveSoundChoices = resolveSoundChoices;
            _audioResources = audioResources ?? Array.Empty<string>();
            _soundPreview = soundPreview;

            RefreshFromDocument();
        }

        /// <summary>
        /// Raised once at the end of every <see cref="RefreshFromDocument"/>.
        /// </summary>
        /// <remarks>
        /// One signal for "this list changed", whatever moved it - an add, a delete, an undo, a redo,
        /// or a reload after an external edit. The Area Contents panel rebuilds its tree from this
        /// rather than watching <see cref="Rows"/>, which reports a clear plus one add per row and so
        /// would rebuild 1,599 times for one refresh of a busy area.
        /// </remarks>
        public event Action? RowsRefreshed;

        /// <summary>Refreshes a selected specialized instance editor after its ITP changes.</summary>
        public void RefreshPaletteChoices()
        {
            DoorEditor?.RefreshPaletteChoices();
            WaypointEditor?.RefreshPaletteChoices();
            SoundEditor?.RefreshPaletteChoices();
        }

        /// <summary>
        /// Rebinds both the currently selected waypoint and future selections to the latest
        /// module transition-destination catalog.
        /// </summary>
        public void RefreshWaypointCatalog(WaypointBehaviorCatalog catalog)
        {
            if (_waypointEditorServices == null)
                return;

            _waypointEditorServices = _waypointEditorServices with { Catalog = catalog };
            WaypointEditor?.RefreshCatalog(catalog);
        }

        /// <summary>Applies save-time normalization required by the selected specialized editor.</summary>
        public bool PrepareForSave()
        {
            if (WaypointEditor?.PrepareForSave() == false)
                return false;

            return !HasSingletonWaypointTagConflicts();
        }

        private bool HasSingletonWaypointTagConflicts()
        {
            if (_blueprintType != ResourceType.Utw || _waypointEditorServices == null)
                return false;

            var list = _gitSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
            if (list == null)
                return false;

            var singletonTags = list
                .Select(_workspace.TagIndex.ResolveWaypointTag)
                .OfType<string>()
                .Where(tag => _waypointEditorServices.Catalog.IsSingletonDestinationTag(tag))
                .ToList();
            if (singletonTags
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            {
                return true;
            }

            return singletonTags.Any(tag =>
                _workspace.TagIndex.CountWaypointPlacementsOutsideArea(
                    tag,
                    _waypointEditorServices.HeaderOwner) > 0);
        }

        /// <summary>Rebuilds the grid rows from the current document state for initial load,
        /// structural edits, and undo/redo. Detail-field edits update the selected row in place
        /// so typing does not recreate every row in a large area.</summary>
        public void RefreshFromDocument()
        {
            var selectedIndex = SelectedRow?.Index;
            Rows.Clear();

            var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
            if (listField?.Elements != null)
            {
                for (var i = 0; i < listField.Elements.Count; i++)
                {
                    var element = listField.Elements[i];
                    var (x, y, z) = InstanceFieldMap.GetPosition(_blueprintType, element);
                    Rows.Add(new InstanceRow(
                        i,
                        InstanceFieldMap.GetTag(element) ?? string.Empty,
                        InstanceFieldMap.GetTemplateResRef(_blueprintType, element) ?? string.Empty,
                        x, y, z,
                        InstanceFieldMap.GetDisplayName(_blueprintType, element) ?? string.Empty));
                }
            }

            SelectedRow = selectedIndex.HasValue && selectedIndex.Value < Rows.Count
                ? Rows[selectedIndex.Value]
                : null;

            RowsRefreshed?.Invoke();
        }

        partial void OnSelectedRowChanged(InstanceRow? value)
        {
            HasSelection = value != null;
            var element = value != null ? GetElement(value.Index) : null;
            if (element == null)
            {
                DoorEditor?.Dispose();
                DoorEditor = null;
                WaypointEditor?.Dispose();
                WaypointEditor = null;
                VarTableSection = null;
                SoundEditor?.Dispose();
                SoundEditor = null;
                return;
            }

            LoadDetailFromElement(element);

            DoorEditor?.Dispose();
            DoorEditor = null;
            WaypointEditor?.Dispose();
            WaypointEditor = null;
            SoundEditor?.Dispose();
            SoundEditor = null;

            if (UsesDoorEditor)
            {
                DoorEditor = new Doors.DoorEditorViewModel(
                    element,
                    _doorEditorServices?.HeaderOwner ?? "area",
                    isInstance: true,
                    RunDoorEdit,
                    _gameCodeIndex,
                    _doorEditorServices?.ResolveTag,
                    _doorEditorServices?.ResolveChoices,
                    _doorEditorServices?.Appearances,
                    _doorEditorServices?.ResourceIndex,
                    _doorEditorServices?.ResolveModel,
                    _gitSession.UndoStack.IsDirty,
                    _doorEditorServices?.Thumbnails,
                    _doorEditorServices?.ChoicePreviews,
                    _prompts,
                    log: _log);
                VarTableSection = null;
            }
            else if (_blueprintType == ResourceType.Utw && _waypointEditorServices != null)
            {
                VarTableSection = null;
                WaypointEditor = new WaypointEditorViewModel(
                    element,
                    _waypointEditorServices.HeaderOwner,
                    isInstance: true,
                    RunWaypointEdit,
                    _waypointEditorServices.Catalog,
                    _gameCodeIndex,
                    _waypointEditorServices.ResolveChoices,
                    _waypointEditorServices.ChoicePreviews,
                    _prompts,
                    tag => IsSingletonWaypointTagInUse(value!.Index, tag),
                    log: _log);
            }
            else if (_blueprintType == ResourceType.Uts)
            {
                VarTableSection = null;
                SoundEditor = new SoundEditorViewModel(
                    element,
                    _soundHeaderOwner,
                    isInstance: true,
                    _runEdit,
                    _gameCodeIndex,
                    _resolveSoundChoices,
                    _audioResources,
                    _soundPreview,
                    _prompts,
                    _log);
                SoundEditor.ValueChanged += () =>
                {
                    if (SelectedRow is { } row)
                        row.Tag = InstanceFieldMap.GetTag(element) ?? string.Empty;
                };
            }
            else
            {
                VarTableSection = new VarTableSectionViewModel(
                    (description, mutation) => _runEdit(description, mutation),
                    new VarTable(element),
                    _gameCodeIndex);
            }
        }

        private bool RunDoorEdit(string description, Action mutation)
        {
            return RunSpecializedEdit(description, mutation);
        }

        private bool RunWaypointEdit(string description, Action mutation)
        {
            return RunSpecializedEdit(description, mutation);
        }

        private bool IsSingletonWaypointTagInUse(int currentIndex, string tag)
        {
            if (_waypointEditorServices == null ||
                !_waypointEditorServices.Catalog.IsSingletonDestinationTag(tag))
            {
                return false;
            }

            if (_workspace.TagIndex.CountWaypointPlacementsOutsideArea(
                    tag,
                    _waypointEditorServices.HeaderOwner) > 0)
            {
                return true;
            }

            var list = _gitSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
            if (list == null)
                return false;

            for (var index = 0; index < list.Count; index++)
            {
                if (index == currentIndex)
                    continue;

                var otherTag = _workspace.TagIndex.ResolveWaypointTag(list[index]);
                if (string.Equals(otherTag, tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool RunSpecializedEdit(string description, Action mutation)
        {
            if (!_runEdit(description, mutation))
                return false;

            if (SelectedRow is { } row && GetElement(row.Index) is { } element)
            {
                row.Tag = InstanceFieldMap.GetTag(element) ?? string.Empty;
                row.TemplateResRef =
                    InstanceFieldMap.GetTemplateResRef(_blueprintType, element) ?? string.Empty;
                LoadDetailFromElement(element);
            }

            return true;
        }

        partial void OnDetailTagChanged(string value)
        {
            if (_isLoadingDetail || SelectedRow is not { } row)
                return;

            var element = GetElement(row.Index);
            if (element == null)
                return;

            if (_runEdit($"Change {Title} tag", () => InstanceFieldMap.SetTag(element, value)))
                row.Tag = value;
            else
                LoadDetailFromElement(element);
        }

        partial void OnDetailXChanged(double value) => ApplyPositionEdit();
        partial void OnDetailYChanged(double value) => ApplyPositionEdit();
        partial void OnDetailZChanged(double value) => ApplyPositionEdit();
        partial void OnDetailXOrientationChanged(double value) => ApplyOrientationEdit();
        partial void OnDetailYOrientationChanged(double value) => ApplyOrientationEdit();
        partial void OnDetailTriggerWidthChanged(double value) => ApplyTriggerGeometryEdit();
        partial void OnDetailTriggerHeightChanged(double value) => ApplyTriggerGeometryEdit();

        private void ApplyPositionEdit()
        {
            if (_isLoadingDetail || SelectedRow is not { } row)
                return;

            var element = GetElement(row.Index);
            if (element == null)
                return;

            var x = (float)DetailX;
            var y = (float)DetailY;
            var z = (float)DetailZ;
            if (_runEdit(
                    $"Move {Title} instance",
                    () => InstanceFieldMap.SetPosition(_blueprintType, element, x, y, z)))
            {
                row.X = x;
                row.Y = y;
                row.Z = z;
            }
            else
            {
                LoadDetailFromElement(element);
            }
        }

        private void ApplyOrientationEdit()
        {
            if (_isLoadingDetail || SelectedRow is not { } row)
                return;

            var element = GetElement(row.Index);
            if (element == null)
                return;

            var xOrientation = (float)DetailXOrientation;
            var yOrientation = (float)DetailYOrientation;
            if (!_runEdit(
                    $"Rotate {Title} instance",
                    () => InstanceFieldMap.SetOrientation(_blueprintType, element, xOrientation, yOrientation)))
            {
                LoadDetailFromElement(element);
            }
        }

        private void ApplyTriggerGeometryEdit()
        {
            if (_isLoadingDetail || !HasTriggerGeometry || SelectedRow is not { } row ||
                DetailTriggerWidth <= 0 || DetailTriggerHeight <= 0)
                return;

            var element = GetElement(row.Index);
            if (element == null)
                return;

            if (!_runEdit(
                    $"Resize {Title} geometry",
                    () => InstanceFieldMap.SetTriggerGeometrySize(
                        element, (float)DetailTriggerWidth, (float)DetailTriggerHeight)))
            {
                LoadDetailFromElement(element);
            }
        }

        private void LoadDetailFromElement(JsonGffStruct element)
        {
            _isLoadingDetail = true;
            try
            {
                DetailTag = InstanceFieldMap.GetTag(element) ?? string.Empty;
                var (x, y, z) = InstanceFieldMap.GetPosition(_blueprintType, element);
                DetailX = x;
                DetailY = y;
                DetailZ = z;
                var (xOrientation, yOrientation) = InstanceFieldMap.GetOrientation(_blueprintType, element);
                DetailXOrientation = xOrientation;
                DetailYOrientation = yOrientation;
                if (HasTriggerGeometry)
                {
                    var (width, height) = InstanceFieldMap.GetTriggerGeometrySize(element);
                    DetailTriggerWidth = width;
                    DetailTriggerHeight = height;
                }
            }
            finally
            {
                _isLoadingDetail = false;
            }
        }

        [RelayCommand]
        private void Add() => OpenPaletteBrowser(AddFromPalette, () => { });

        /// <summary>
        /// Opens this section's palette browser - the same flow this section's own "Add..." uses
        /// below - and invokes <paramref name="onResRefChosen"/> once a blueprint is picked, or
        /// <paramref name="onCancelled"/> if the browser is dismissed instead. Also used by the
        /// 3D-view "Place..." flow (AreaEditorViewModel.BeginPlace), which reuses this exact
        /// path rather than a parallel one, so both entry points browse identically.
        /// </summary>
        public void OpenPaletteBrowser(Action<string> onResRefChosen, Action onCancelled)
        {
            Action<string> complete = resRef =>
            {
                ActivePaletteBrowser = null;
                onResRefChosen(resRef);
            };
            Action cancel = () =>
            {
                ActivePaletteBrowser = null;
                onCancelled();
            };

            if (ActivePaletteBrowser is { } activeBrowser)
            {
                activeBrowser.RebindCompletionActions(complete, cancel);
                return;
            }

            var itpPath = Path.Combine(_workspace.ModuleRoot, "itp", PaletteFileName(_blueprintType));
            if (!File.Exists(itpPath))
            {
                _log.AppendLine($"No palette file found for {Title} ('{itpPath}').");
                return;
            }

            ActivePaletteBrowser = new PaletteBrowserViewModel(
                Title,
                itpPath,
                complete,
                cancel,
                _log,
                _resolveStrRef);
        }

        /// <summary>Dismisses the palette picker, if one is open.</summary>
        internal void ClosePalette() => ActivePaletteBrowser = null;

        public void Dispose()
        {
            DoorEditor?.Dispose();
            DoorEditor = null;
            WaypointEditor?.Dispose();
            WaypointEditor = null;
            SoundEditor?.Dispose();
            SoundEditor = null;
            ActivePaletteBrowser = null;
        }

        private void AddFromPalette(string resRef) => AddInstanceAt(resRef, 0f, 0f, 0f);

        /// <summary>
        /// Creates a new instance from <paramref name="resRef"/>'s blueprint at the given
        /// placement (via <see cref="InstanceFieldMap.CreateInstance"/>) and inserts it as one
        /// RunGitEdit transaction - the exact path this section's own "Add..." (at the origin) and
        /// the 3D-view "Place..." flow (at the clicked ground position) both use.
        /// </summary>
        public bool AddInstanceAt(
            string resRef,
            float x,
            float y,
            float z,
            float xOrientation = 1f,
            float yOrientation = 0f,
            bool useIndexedBlueprint = false)
        {
            try
            {
                var blueprint = useIndexedBlueprint
                    ? _workspace.LoadIndexedBlueprint(_blueprintType, resRef)
                    : _workspace.LoadBlueprint(_blueprintType, resRef);
                var ok = _runEdit($"Add {Title} instance", () =>
                {
                    var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
                    if (listField == null)
                    {
                        listField = JsonGffField.CreateList();
                        _gitSession.Document.Root.Add(_listFieldName, listField);
                    }

                    var instance = InstanceFieldMap.CreateInstance(
                        _blueprintType, blueprint.Document, resRef,
                        x, y, z, xOrientation, yOrientation);
                    var insertAt = listField.Elements!.Count;
                    listField.InsertElement(insertAt, instance);
                    new GicDocument(_gicSession.Document)
                        .InsertBlankComment(
                            _listFieldName,
                            _blueprintType,
                            insertAt,
                            listField.Elements.Count);
                });

                if (ok)
                    RefreshFromDocument();

                return ok;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to add {Title} instance '{resRef}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inserts a copied instance at a new map position while preserving every other authored GIT
        /// field and its paired GIC comment.
        /// </summary>
        internal bool AddCopiedInstanceAt(
            AreaInstanceClipboardEntry copy,
            float x,
            float y,
            float z,
            float xOrientation,
            float yOrientation)
        {
            if (copy.Type != _blueprintType ||
                !string.Equals(copy.ModuleRoot, _workspace.ModuleRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var ok = _runEdit($"Paste {Title} instance", () =>
                {
                    var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
                    if (listField == null)
                    {
                        listField = JsonGffField.CreateList();
                        _gitSession.Document.Root.Add(_listFieldName, listField);
                    }

                    var instance = InstanceFieldMap.Duplicate(copy.Instance);
                    InstanceFieldMap.SetPosition(_blueprintType, instance, x, y, z);
                    InstanceFieldMap.SetOrientation(
                        _blueprintType, instance, xOrientation, yOrientation);

                    var insertAt = listField.Elements!.Count;
                    listField.InsertElement(insertAt, instance);
                    new GicDocument(_gicSession.Document).InsertCopiedComment(
                        _listFieldName,
                        _blueprintType,
                        insertAt,
                        listField.Elements.Count,
                        copy.Comment);
                });

                if (ok)
                    RefreshFromDocument();

                return ok;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to paste {Title} instance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the position of the instance at <paramref name="index"/> through
        /// <see cref="InstanceFieldMap.SetPosition"/> - the exact setter the detail form's X/Y/Z
        /// editors use - as one RunGitEdit transaction. Used by the 3D-view move gizmo
        /// (AreaEditorViewModel.MoveSelectedInstance) so a 3D-view drag produces the identical diff
        /// shape a detail-form edit would.
        /// </summary>
        public bool SetInstancePosition(int index, float x, float y, float z, string? description = null)
        {
            var element = GetElement(index);
            if (element == null)
                return false;

            var ok = _runEdit(description ?? $"Move {Title} instance",
                () => InstanceFieldMap.SetPosition(_blueprintType, element, x, y, z));

            if (ok)
                RefreshFromDocument();

            return ok;
        }

        /// <summary>Mirrors <see cref="SetInstancePosition"/> for heading, via <see cref="InstanceFieldMap.SetOrientation"/> - used by the 3D-view rotate gizmo.</summary>
        public bool SetInstanceOrientation(int index, float xOrientation, float yOrientation, string? description = null)
        {
            var element = GetElement(index);
            if (element == null)
                return false;

            var ok = _runEdit(description ?? $"Rotate {Title} instance",
                () => InstanceFieldMap.SetOrientation(_blueprintType, element, xOrientation, yOrientation));

            if (ok)
                RefreshFromDocument();

            return ok;
        }

        /// <summary>
        /// Sets position and heading together as one document transaction. Door snapping uses this
        /// because the doorway position and orientation are one invariant: undoing only one of them
        /// leaves the door stranded sideways in its previous frame.
        /// </summary>
        public bool SetInstanceTransform(
            int index,
            float x,
            float y,
            float z,
            float xOrientation,
            float yOrientation,
            string? description = null)
        {
            var element = GetElement(index);
            if (element == null)
                return false;

            var ok = _runEdit(description ?? $"Move {Title} instance", () =>
            {
                InstanceFieldMap.SetPosition(_blueprintType, element, x, y, z);
                InstanceFieldMap.SetOrientation(_blueprintType, element, xOrientation, yOrientation);
            });

            if (ok)
                RefreshFromDocument();

            return ok;
        }

        [RelayCommand]
        private void Duplicate()
        {
            if (SelectedRow is not { } row)
                return;

            var element = GetElement(row.Index);
            if (element == null)
                return;

            _runEdit($"Duplicate {Title} instance", () =>
            {
                var listField = _gitSession.Document.Root.Get(_listFieldName);
                var clone = InstanceFieldMap.Duplicate(element);
                listField.InsertElement(row.Index + 1, clone);
                new GicDocument(_gicSession.Document).DuplicateComment(
                    _listFieldName,
                    _blueprintType,
                    row.Index,
                    listField.Elements!.Count);
            });

            RefreshFromDocument();
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedRow is { } row)
                DeleteInstances(new[] { row.Index });
        }

        /// <summary>
        /// Removes the placements at <paramref name="indices"/> as one transaction, so a whole group
        /// deleted from the Area Contents tree is one undo rather than one per object.
        /// </summary>
        /// <remarks>
        /// Removal runs highest index first. Ascending order is wrong and wrong quietly: every index
        /// after the first has shifted down by one, so the second removal takes its neighbour and the
        /// last one runs off the end of the list.
        /// </remarks>
        public bool DeleteInstances(IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0)
                return false;

            var ordered = indices.Distinct().OrderByDescending(index => index).ToList();
            var description = ordered.Count == 1
                ? $"Delete {Title} instance"
                : $"Delete {ordered.Count} {Title} instances";

            var ok = _runEdit(description, () =>
            {
                var listField = _gitSession.Document.Root.Get(_listFieldName);
                var comments = new GicDocument(_gicSession.Document);

                foreach (var index in ordered)
                {
                    if (listField.Elements == null || index < 0 || index >= listField.Elements.Count)
                        continue;

                    listField.RemoveElementAt(index);
                    comments.RemoveComment(
                        _listFieldName, _blueprintType, index, listField.Elements.Count);
                }
            });

            if (!ok)
                return false;

            SelectedRow = null;
            RefreshFromDocument();
            return true;
        }

        private JsonGffStruct? GetElement(int index)
        {
            var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
            if (listField?.Elements == null || index < 0 || index >= listField.Elements.Count)
                return null;

            return listField.Elements[index];
        }

        /// <summary>
        /// The authored instance at a row index, for the area editor's single-marker scene update.
        /// Kept internal so mutable document structures do not escape the editor assembly.
        /// </summary>
        internal JsonGffStruct? GetInstanceForScene(int index) => GetElement(index);

        /// <summary>
        /// Takes an independent clipboard snapshot of one GIT instance and its aligned GIC comment.
        /// </summary>
        internal AreaInstanceClipboardEntry? CopyInstanceForPlacement(
            int index,
            InstanceMarker preview)
        {
            var instance = GetElement(index);
            if (instance == null)
                return null;

            var comments = _gicSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
            var comment = comments != null && index >= 0 && index < comments.Count
                ? InstanceFieldMap.Duplicate(comments[index])
                : null;

            return new AreaInstanceClipboardEntry(
                _workspace.ModuleRoot,
                _blueprintType,
                InstanceFieldMap.Duplicate(instance),
                comment,
                preview);
        }

        private static string PaletteFileName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => "creaturepalcus.itp.json",
                ResourceType.Utp => "placeablepalcus.itp.json",
                ResourceType.Utd => "doorpalcus.itp.json",
                ResourceType.Utw => "waypointpalcus.itp.json",
                ResourceType.Utm => "storepalcus.itp.json",
                ResourceType.Uts => "soundpalcus.itp.json",
                ResourceType.Utt => "triggerpalcus.itp.json",
                ResourceType.Uti => "itempalcus.itp.json",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No palette file mapping for this type.")
            };
        }
    }
}
