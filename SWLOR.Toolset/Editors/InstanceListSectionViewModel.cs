using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
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

        public int Index { get; }
        public string TemplateResRef { get; }

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

        public InstanceRow(int index, string tag, string templateResRef, float x, float y, float z)
        {
            Index = index;
            _tag = tag;
            TemplateResRef = templateResRef;
            _x = x;
            _y = y;
            _z = z;
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
    public partial class InstanceListSectionViewModel : ObservableObject
    {
        private readonly DocumentSession _gitSession;
        private readonly ModuleWorkspace _workspace;
        private readonly ResourceType _blueprintType;
        private readonly string _listFieldName;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;

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

        [ObservableProperty]
        private VarTableSectionViewModel? _varTableSection;

        [ObservableProperty]
        private PaletteBrowserViewModel? _activePaletteBrowser;

        public InstanceListSectionViewModel(
            string title,
            string listFieldName,
            ResourceType blueprintType,
            DocumentSession gitSession,
            ModuleWorkspace workspace,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<uint, string?>? resolveStrRef = null)
        {
            Title = title;
            _listFieldName = listFieldName;
            _blueprintType = blueprintType;
            _gitSession = gitSession;
            _workspace = workspace;
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _log = log;
            _prompts = prompts;
            _resolveStrRef = resolveStrRef;

            RefreshFromDocument();
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
                        x, y, z));
                }
            }

            SelectedRow = selectedIndex.HasValue && selectedIndex.Value < Rows.Count
                ? Rows[selectedIndex.Value]
                : null;
        }

        partial void OnSelectedRowChanged(InstanceRow? value)
        {
            HasSelection = value != null;
            var element = value != null ? GetElement(value.Index) : null;
            if (element == null)
            {
                VarTableSection = null;
                return;
            }

            LoadDetailFromElement(element);

            VarTableSection = new VarTableSectionViewModel(
                new EditorFieldContext(_gitSession.Document, (description, mutation) => _runEdit(description, mutation)),
                new VarTable(element),
                _gameCodeIndex);
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

        private void AddFromPalette(string resRef) => AddInstanceAt(resRef, 0f, 0f, 0f);

        /// <summary>
        /// Creates a new instance from <paramref name="resRef"/>'s blueprint at the given
        /// placement (via <see cref="InstanceFieldMap.CreateInstance"/>) and inserts it as one
        /// RunGitEdit transaction - the exact path this section's own "Add..." (at the origin) and
        /// the 3D-view "Place..." flow (at the clicked ground position) both use.
        /// </summary>
        public bool AddInstanceAt(
            string resRef, float x, float y, float z, float xOrientation = 1f, float yOrientation = 0f)
        {
            try
            {
                var blueprint = _workspace.LoadBlueprint(_blueprintType, resRef);
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
                    listField.InsertElement(listField.Elements!.Count, instance);
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
            });

            RefreshFromDocument();
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedRow is not { } row)
                return;

            _runEdit($"Delete {Title} instance", () =>
            {
                var listField = _gitSession.Document.Root.Get(_listFieldName);
                listField.RemoveElementAt(row.Index);
            });

            SelectedRow = null;
            RefreshFromDocument();
        }

        private JsonGffStruct? GetElement(int index)
        {
            var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
            if (listField?.Elements == null || index < 0 || index >= listField.Elements.Count)
                return null;

            return listField.Elements[index];
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
