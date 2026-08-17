using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Archives
{
    public enum ErfArchiveMode
    {
        Import,
        Export
    }

    public sealed class ErfAssetRow : ObservableObject
    {
        private static readonly HashSet<string> AreaExtensions = new(
            new[] { "are", "git", "gic" },
            StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _fileNames = new();
        private readonly List<ErfArchiveAsset> _archiveAssets = new();
        private readonly List<ModuleArchiveAsset> _moduleAssets = new();
        private readonly List<ErfPreparedImport> _preparedImports = new();
        private readonly string _detail;
        private long _size;
        private bool _isSupported;
        private bool _isSelected;
        private bool _isRequired;
        private string _requiredReason = string.Empty;
        private string _conflictActionLabel = string.Empty;
        private string _renameResRef = string.Empty;
        private string _resourceName = string.Empty;

        public ErfAssetRow(ErfArchiveAsset asset)
        {
            ArgumentNullException.ThrowIfNull(asset);
            IsArea = AreaExtensions.Contains(asset.Extension);
            FileName = IsArea ? asset.ResRef : asset.FileName;
            ResRef = asset.ResRef;
            Extension = IsArea ? "area" : asset.Extension;
            TypeName = IsArea ? "Area" : asset.TypeName;
            _size = asset.Size;
            _isSupported = asset.IsSupported;
            _detail = IsArea
                ? "Module area"
                : asset.UnsupportedReason ??
                  $"Module/{asset.Extension}/{DestinationFileName(asset.Extension, asset.ResRef)}";
            _fileNames.Add(asset.FileName);
            _archiveAssets.Add(asset);
        }

        public ErfAssetRow(ModuleArchiveAsset asset)
        {
            ArgumentNullException.ThrowIfNull(asset);
            IsArea = AreaExtensions.Contains(asset.Extension);
            FileName = IsArea ? asset.ResRef : asset.FileName;
            ResRef = asset.ResRef;
            Extension = IsArea ? "area" : asset.Extension;
            TypeName = IsArea ? "Area" : asset.TypeName;
            _size = asset.Size;
            _isSupported = true;
            _detail = IsArea
                ? "Module area"
                : Path.GetRelativePath(
                    Directory.GetParent(Path.GetDirectoryName(asset.SourcePath)!)!.FullName,
                    asset.SourcePath);
            _resourceName = asset.ResourceName ?? string.Empty;
            _fileNames.Add(asset.FileName);
            _moduleAssets.Add(asset);
        }

        public IReadOnlyList<string> FileNames => _fileNames;
        public IReadOnlyList<ErfArchiveAsset> ArchiveAssets => _archiveAssets;
        public IReadOnlyList<ModuleArchiveAsset> ModuleAssets => _moduleAssets;
        public IReadOnlyList<ErfPreparedImport> PreparedImports => _preparedImports;
        public string FileName { get; }
        public string ResRef { get; }
        public string Extension { get; }
        public string TypeName { get; }
        public bool IsArea { get; }
        public long Size => _size;
        public bool IsSupported => _isSupported;
        public string Detail => _detail;
        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (SetProperty(ref _resourceName, value ?? string.Empty))
                    OnPropertyChanged(nameof(ResourceNameDisplay));
            }
        }
        public string ResourceNameDisplay =>
            string.IsNullOrWhiteSpace(ResourceName) ? "—" : ResourceName;
        public string SizeLabel => Size < 1024 ? $"{Size} B" : $"{Size / 1024d:N1} KB";
        public bool CanToggle => IsSupported && !IsRequired;
        public bool IsPrepared => _preparedImports.Count > 0;
        public bool HasConflict =>
            _preparedImports.Any(item => item.Conflict == ErfConflictKind.Different) ||
            (_preparedImports.Any(item => item.Conflict == ErfConflictKind.New) &&
             _preparedImports.Any(item => item.Conflict != ErfConflictKind.New));
        public bool WillWriteImport => ToImportChoices().Any(choice =>
            choice.Action is ErfConflictAction.Add
                or ErfConflictAction.Replace
                or ErfConflictAction.Rename);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!IsSupported || IsRequired)
                    return;
                if (SetProperty(ref _isSelected, value))
                    OnPropertyChanged(nameof(StatusLabel));
            }
        }

        public bool IsRequired
        {
            get => _isRequired;
            set
            {
                if (!SetProperty(ref _isRequired, value))
                    return;
                if (value)
                    SetProperty(ref _isSelected, true, nameof(IsSelected));
                OnPropertyChanged(nameof(CanToggle));
                OnPropertyChanged(nameof(StatusLabel));
            }
        }

        public string RequiredReason
        {
            get => _requiredReason;
            set
            {
                if (SetProperty(ref _requiredReason, value))
                    OnPropertyChanged(nameof(StatusLabel));
            }
        }

        public string ConflictLabel
        {
            get
            {
                if (!IsPrepared)
                    return IsSupported ? "Ready" : "Can't import";
                if (_preparedImports.All(item => item.Conflict == ErfConflictKind.New))
                    return "New";
                if (_preparedImports.All(item => item.Conflict == ErfConflictKind.Identical))
                    return "Identical";
                if (_preparedImports.Any(item => item.Conflict == ErfConflictKind.Different))
                    return "Different";
                return "Partially exists";
            }
        }

        public IReadOnlyList<string> AvailableActions
        {
            get
            {
                if (!IsPrepared)
                    return Array.Empty<string>();
                if (_preparedImports.All(item => item.Conflict == ErfConflictKind.New))
                    return SupportsRename
                        ? new[] { "Add", "Rename imported", "Skip" }
                        : new[] { "Add", "Skip" };
                if (_preparedImports.All(item => item.Conflict == ErfConflictKind.Identical))
                    return new[] { "Skip", "Replace" };
                return SupportsRename
                    ? new[] { "Keep existing", "Replace", "Rename imported" }
                    : new[] { "Keep existing", "Replace" };
            }
        }

        public string ConflictActionLabel
        {
            get => _conflictActionLabel;
            set
            {
                if (SetProperty(ref _conflictActionLabel, value))
                {
                    OnPropertyChanged(nameof(CanRename));
                    OnPropertyChanged(nameof(WillWriteImport));
                }
            }
        }

        public bool CanRename =>
            SupportsRename &&
            string.Equals(ConflictActionLabel, "Rename imported", StringComparison.Ordinal);

        private bool SupportsRename =>
            _archiveAssets.All(asset =>
                ErfArchiveService.CanRenameResource(asset.Extension, asset.ResRef));

        public string RenameResRef
        {
            get => _renameResRef;
            set => SetProperty(ref _renameResRef, value ?? string.Empty);
        }

        public string StatusLabel
        {
            get
            {
                if (!IsSupported)
                    return "Can't import";
                if (IsRequired)
                    return $"Added automatically · {RequiredReason}";
                return !IsPrepared ? (IsSelected ? "Selected" : "Available") : ConflictLabel;
            }
        }

        public bool MatchesSearch(string searchText) =>
            FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            ResourceName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            TypeName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            _fileNames.Any(fileName =>
                fileName.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        public void MergeArea(ErfAssetRow companion)
        {
            ArgumentNullException.ThrowIfNull(companion);
            if (!IsArea || !companion.IsArea ||
                !ResRef.Equals(companion.ResRef, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only companions from the same area can be grouped.");
            }

            foreach (var fileName in companion._fileNames)
            {
                if (!_fileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    _fileNames.Add(fileName);
            }
            _archiveAssets.AddRange(companion._archiveAssets);
            _moduleAssets.AddRange(companion._moduleAssets);
            _size += companion._size;
            _isSupported &= companion._isSupported;
            if (string.IsNullOrWhiteSpace(ResourceName) &&
                !string.IsNullOrWhiteSpace(companion.ResourceName))
            {
                ResourceName = companion.ResourceName;
            }

            OnPropertyChanged(nameof(FileNames));
            OnPropertyChanged(nameof(ArchiveAssets));
            OnPropertyChanged(nameof(ModuleAssets));
            OnPropertyChanged(nameof(Size));
            OnPropertyChanged(nameof(SizeLabel));
            OnPropertyChanged(nameof(IsSupported));
            OnPropertyChanged(nameof(CanToggle));
            OnPropertyChanged(nameof(StatusLabel));
        }

        public void ApplyPrepared(IEnumerable<ErfPreparedImport> prepared)
        {
            ArgumentNullException.ThrowIfNull(prepared);
            _preparedImports.Clear();
            _preparedImports.AddRange(prepared);
            if (_preparedImports.Count == 0)
                return;

            ConflictActionLabel = _preparedImports.All(
                item => item.Conflict == ErfConflictKind.New)
                ? "Add"
                : _preparedImports.All(item => item.Conflict == ErfConflictKind.Identical)
                    ? "Skip"
                    : "Keep existing";
            RenameResRef = SuggestedRename(ResRef);
            OnPropertyChanged(nameof(PreparedImports));
            OnPropertyChanged(nameof(IsPrepared));
            OnPropertyChanged(nameof(HasConflict));
            OnPropertyChanged(nameof(WillWriteImport));
            OnPropertyChanged(nameof(ConflictLabel));
            OnPropertyChanged(nameof(AvailableActions));
            OnPropertyChanged(nameof(StatusLabel));
        }

        public IReadOnlyList<ErfImportChoice> ToImportChoices()
        {
            return _preparedImports
                .Select(prepared =>
                {
                    var action = ConflictActionLabel switch
                    {
                        "Add" => ErfConflictAction.Add,
                        "Replace" => prepared.Conflict == ErfConflictKind.New
                            ? ErfConflictAction.Add
                            : ErfConflictAction.Replace,
                        "Rename imported" => ErfConflictAction.Rename,
                        "Keep existing" => IsArea
                            ? ErfConflictAction.Skip
                            : ErfConflictAction.KeepExisting,
                        _ => ErfConflictAction.Skip
                    };
                    return new ErfImportChoice(prepared, action, RenameResRef);
                })
                .ToList();
        }

        private static string SuggestedRename(string resRef)
        {
            const string suffix = "_imp";
            var prefixLength = Math.Max(1, 16 - suffix.Length);
            return resRef[..Math.Min(resRef.Length, prefixLength)] + suffix;
        }

        private static string DestinationFileName(string extension, string resRef) =>
            extension is "nss" or "ncs"
                ? $"{resRef}.{extension}"
                : $"{resRef}.{extension}.json";
    }

    public partial class ErfArchiveViewModel : ObservableObject, IDisposable
    {
        private static readonly IReadOnlyList<string> ImportStatusFilters =
            new[] { "All assets", "Selected", "Added automatically", "Can't import" };
        private static readonly IReadOnlyList<string> ExportStatusFilters =
            new[] { "All assets", "Selected" };

        private readonly ErfArchiveService _service;
        private readonly ToolsetSettings _settings;
        private readonly Dictionary<string, ErfAssetRow> _areaRows =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<ErfAssetRow> _explicitImportSelection = new();
        private ErfArchiveSession? _session;
        private CancellationTokenSource? _exportLoadCts;
        private CancellationTokenSource? _resourceNameLoadCts;
        private bool _isUpdatingVisibleSelection;
        private bool _disposed;

        public ObservableCollection<ErfAssetRow> Assets { get; } = new();
        public ObservableCollection<string> RecentArchives { get; } = new();
        public ObservableCollection<string> TypeFilters { get; } = new() { "All types" };
        public IReadOnlyList<string> StatusFilters => IsImport
            ? ImportStatusFilters
            : ExportStatusFilters;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsImport))]
        [NotifyPropertyChangedFor(nameof(IsExport))]
        [NotifyPropertyChangedFor(nameof(ShowImportFile))]
        [NotifyPropertyChangedFor(nameof(ShowExportSnapshot))]
        [NotifyPropertyChangedFor(nameof(ShowSelectionValidationProgress))]
        [NotifyPropertyChangedFor(nameof(ShowImportConflicts))]
        [NotifyPropertyChangedFor(nameof(ShowExportValidation))]
        [NotifyPropertyChangedFor(nameof(ModeTitle))]
        [NotifyPropertyChangedFor(nameof(StepOneLabel))]
        [NotifyPropertyChangedFor(nameof(StepTwoLabel))]
        [NotifyPropertyChangedFor(nameof(StepThreeLabel))]
        [NotifyPropertyChangedFor(nameof(StepFourLabel))]
        [NotifyPropertyChangedFor(nameof(StatusFilters))]
        [NotifyPropertyChangedFor(nameof(ShowImportAction))]
        [NotifyPropertyChangedFor(nameof(ShowRestartImportAction))]
        [NotifyPropertyChangedFor(nameof(CanRestartImport))]
        private ErfArchiveMode _mode = ErfArchiveMode.Import;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStepOne))]
        [NotifyPropertyChangedFor(nameof(IsStepTwo))]
        [NotifyPropertyChangedFor(nameof(IsStepThree))]
        [NotifyPropertyChangedFor(nameof(IsStepFour))]
        [NotifyPropertyChangedFor(nameof(ShowImportFile))]
        [NotifyPropertyChangedFor(nameof(ShowExportSnapshot))]
        [NotifyPropertyChangedFor(nameof(ShowSelectionValidationProgress))]
        [NotifyPropertyChangedFor(nameof(ShowImportConflicts))]
        [NotifyPropertyChangedFor(nameof(ShowExportValidation))]
        [NotifyPropertyChangedFor(nameof(CanGoBack))]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(ShowNext))]
        [NotifyPropertyChangedFor(nameof(ShowImportAction))]
        [NotifyPropertyChangedFor(nameof(ShowRestartImportAction))]
        [NotifyPropertyChangedFor(nameof(CanRestartImport))]
        [NotifyPropertyChangedFor(nameof(ShowExportAction))]
        [NotifyPropertyChangedFor(nameof(StepTitle))]
        [NotifyPropertyChangedFor(nameof(StepDescription))]
        private int _currentStep;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(CanCommit))]
        [NotifyPropertyChangedFor(nameof(CanClose))]
        [NotifyPropertyChangedFor(nameof(CanRestartImport))]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowSelectionValidationProgress))]
        [NotifyPropertyChangedFor(nameof(ShowImportConflicts))]
        [NotifyPropertyChangedFor(nameof(ShowExportValidation))]
        [NotifyPropertyChangedFor(nameof(ShowNext))]
        [NotifyPropertyChangedFor(nameof(StepTitle))]
        [NotifyPropertyChangedFor(nameof(StepDescription))]
        private bool _isValidatingSelection;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanCommit))]
        [NotifyPropertyChangedFor(nameof(CanGoBack))]
        [NotifyPropertyChangedFor(nameof(ShowImportAction))]
        [NotifyPropertyChangedFor(nameof(ShowRestartImportAction))]
        [NotifyPropertyChangedFor(nameof(CanRestartImport))]
        private bool _isComplete;

        [ObservableProperty]
        private string _statusText = "Choose an ERF file to begin.";

        [ObservableProperty]
        private string _importArchivePath = string.Empty;

        [ObservableProperty]
        private string? _selectedRecentArchive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        [NotifyPropertyChangedFor(nameof(VisibleSelectionState))]
        [NotifyPropertyChangedFor(nameof(CanToggleVisibleAssets))]
        private string _searchText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        [NotifyPropertyChangedFor(nameof(VisibleSelectionState))]
        [NotifyPropertyChangedFor(nameof(CanToggleVisibleAssets))]
        private string _selectedTypeFilter = "All types";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        [NotifyPropertyChangedFor(nameof(VisibleSelectionState))]
        [NotifyPropertyChangedFor(nameof(CanToggleVisibleAssets))]
        private string _selectedStatusFilter = "All assets";

        [ObservableProperty]
        private string _completionTitle = string.Empty;

        [ObservableProperty]
        private string _completionDetail = string.Empty;

        public ErfArchiveViewModel(ErfArchiveService service, ToolsetSettings settings)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            foreach (var path in settings.RecentErfArchives.Where(File.Exists))
                RecentArchives.Add(path);
        }

        public bool IsImport => Mode == ErfArchiveMode.Import;
        public bool IsExport => Mode == ErfArchiveMode.Export;
        public bool IsStepOne => CurrentStep == 0;
        public bool IsStepTwo => CurrentStep == 1;
        public bool IsStepThree => CurrentStep == 2;
        public bool IsStepFour => CurrentStep == 3;
        public bool CanGoBack =>
            !IsBusy &&
            !(IsImport && IsComplete) &&
            CurrentStep > 0 &&
            !(IsExport && CurrentStep == 1);
        public bool ShowNext => CurrentStep < 3 && !IsValidatingSelection;
        public bool ShowImportAction => IsImport && CurrentStep == 3 && !IsComplete;
        public bool ShowRestartImportAction => IsImport && CurrentStep == 3 && IsComplete;
        public bool ShowExportAction => IsExport && CurrentStep == 3;
        public bool ShowImportFile => IsImport && CurrentStep == 0;
        public bool ShowExportSnapshot => IsExport && CurrentStep == 0;
        public bool ShowSelectionValidationProgress =>
            CurrentStep == 2 && IsValidatingSelection;
        public bool ShowImportConflicts =>
            IsImport && CurrentStep == 2 && !IsValidatingSelection;
        public bool ShowExportValidation =>
            IsExport && CurrentStep == 2 && !IsValidatingSelection;
        public bool CanCommit => !IsBusy && !IsComplete;
        public bool CanRestartImport => !IsBusy && ShowRestartImportAction;
        public bool CanClose => !IsBusy;
        public bool CanGoNext => !IsBusy && CurrentStep < 3 && (CurrentStep != 0 || !IsImport || _session != null);
        public string ModeTitle => IsImport ? "Import ERF" : "Export ERF";
        public string StepOneLabel => IsImport ? "1  Select ERF file" : "1  Prepare export";
        public string StepTwoLabel => "2  Choose assets";
        public string StepThreeLabel => IsImport ? "3  Resolve conflicts" : "3  Validate";
        public string StepFourLabel => IsImport ? "4  Save to Module" : "4  Save ERF As";

        public string StepTitle => IsValidatingSelection && CurrentStep == 2
            ? IsImport
                ? "Preparing selected assets"
                : "Validating selected assets"
            : (Mode, CurrentStep) switch
        {
            (ErfArchiveMode.Import, 0) => "Select an ERF file",
            (ErfArchiveMode.Import, 1) => "Choose assets to import",
            (ErfArchiveMode.Import, 2) => "Resolve conflicts",
            (ErfArchiveMode.Import, 3) => "Save the import to Module",
            (ErfArchiveMode.Export, 0) => "Prepare your export",
            (ErfArchiveMode.Export, 1) => "Choose assets to export",
            (ErfArchiveMode.Export, 2) => "Validate the archive plan",
            _ => "Save ERF As"
        };

        public string StepDescription => IsValidatingSelection && CurrentStep == 2
            ? IsImport
                ? "Checking the selected assets and preparing any choices that need your attention."
                : "Checking only the assets you selected."
            : (Mode, CurrentStep) switch
        {
            (ErfArchiveMode.Import, 0) =>
                "Browse, drop, or reopen a recent .erf. The scan uses a private read-only snapshot.",
            (ErfArchiveMode.Import, 1) =>
                "Select assets to import. Anything else they need will be added automatically.",
            (ErfArchiveMode.Import, 2) =>
                "Identical resources are skipped. Choose whether different resources stay, are replaced, or are renamed with imported references updated.",
            (ErfArchiveMode.Import, 3) =>
                string.Empty,
            (ErfArchiveMode.Export, 0) =>
                "We're finding the module assets you can include in the ERF.",
            (ErfArchiveMode.Export, 1) =>
                "Select exactly what the ERF should contain. Area files stay grouped together.",
            (ErfArchiveMode.Export, 2) =>
                "The selected assets are checked before a destination can be chosen.",
            _ =>
                "Choose a destination in the native Save As dialog. The archive is written and validated beside it before an atomic replace."
        };

        public IEnumerable<ErfAssetRow> FilteredAssets
        {
            get
            {
                IEnumerable<ErfAssetRow> result = Assets;
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    result = result.Where(row => row.MatchesSearch(SearchText));
                }

                if (SelectedTypeFilter != "All types")
                    result = result.Where(row => row.TypeName == SelectedTypeFilter);

                result = SelectedStatusFilter switch
                {
                    "Selected" => result.Where(row => row.IsSelected),
                    "Added automatically" => result.Where(row => row.IsRequired),
                    "Can't import" => result.Where(row => !row.IsSupported),
                    _ => result
                };
                return result;
            }
        }

        public IEnumerable<ErfAssetRow> ConflictAssets =>
            Assets.Where(row => row.IsSelected && row.IsPrepared);

        public bool? VisibleSelectionState
        {
            get
            {
                var rows = FilteredAssets.Where(row => row.CanToggle).ToList();
                if (rows.Count == 0 || rows.All(row => !row.IsSelected))
                    return false;
                return rows.All(row => row.IsSelected) ? true : null;
            }
        }

        public bool CanToggleVisibleAssets =>
            FilteredAssets.Any(row => row.CanToggle);

        public async Task<bool> LoadArchiveAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (IsBusy)
                return false;

            CancelResourceNameLoading();
            IsBusy = true;
            StatusText = "Scanning ERF header and resource table...";
            try
            {
                var opened = await _service.OpenArchiveAsync(path, cancellationToken).ConfigureAwait(true);
                _session?.Dispose();
                _session = opened;
                IsComplete = false;
                ImportArchivePath = opened.SourcePath;
                _settings.AddRecentErfArchive(opened.SourcePath);
                RecentArchives.Remove(opened.SourcePath);
                RecentArchives.Insert(0, opened.SourcePath);

                SetRows(opened.Assets.Select(asset => new ErfAssetRow(asset)));
                BeginImportResourceNameLoading(opened);
                StatusText =
                    $"{Assets.Count} asset(s) found; {Assets.Count(row => row.IsSupported)} can be imported.";
                OnPropertyChanged(nameof(CanGoNext));
                NextCommand.NotifyCanExecuteChanged();
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Could not open ERF: {ex.GetBaseException().Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public Task<bool> LoadRecentArchiveAsync(CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(SelectedRecentArchive)
                ? Task.FromResult(false)
                : LoadArchiveAsync(SelectedRecentArchive, cancellationToken);

        [RelayCommand]
        private void StartImport()
        {
            if (IsBusy || IsImport)
                return;
            Mode = ErfArchiveMode.Import;
            CurrentStep = 0;
            IsComplete = false;
            SetRows(_session?.Assets.Select(asset => new ErfAssetRow(asset))
                ?? Enumerable.Empty<ErfAssetRow>());
            if (_session != null)
                BeginImportResourceNameLoading(_session);
            StatusText = _session == null
                ? "Choose an ERF file to begin."
                : $"{Assets.Count} asset(s) found.";
        }

        [RelayCommand]
        private void RestartImport()
        {
            if (!CanRestartImport)
                return;

            var session = _session;
            _session = null;
            _explicitImportSelection.Clear();
            ResetRows();
            session?.Dispose();
            ImportArchivePath = string.Empty;
            SelectedRecentArchive = null;
            CompletionTitle = string.Empty;
            CompletionDetail = string.Empty;
            CurrentStep = 0;
            IsComplete = false;
            StatusText = "Choose an ERF file to begin.";
        }

        [RelayCommand]
        private async Task StartExport()
        {
            if (IsBusy || IsExport)
                return;

            Mode = ErfArchiveMode.Export;
            CurrentStep = 0;
            IsComplete = false;
            ResetRows();

            var cancellation = new CancellationTokenSource();
            _exportLoadCts = cancellation;
            IsBusy = true;
            StatusText = "Finding module assets...";
            try
            {
                await foreach (var batch in _service.EnumerateModuleAssetBatchesAsync(
                                   cancellationToken: cancellation.Token))
                {
                    AppendRows(batch.Select(asset => new ErfAssetRow(asset)));
                    StatusText = $"Found {Assets.Count:N0} module asset(s)...";
                }

                StatusText =
                    $"{Assets.Count:N0} module asset(s) are ready to export.";
                BeginModuleResourceNameLoading();
                CurrentStep = 1;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (!_disposed)
                    StatusText = "Loading module assets was canceled.";
            }
            catch (Exception ex)
            {
                StatusText =
                    $"Could not load module assets: {ex.GetBaseException().Message}";
            }
            finally
            {
                if (ReferenceEquals(_exportLoadCts, cancellation))
                    _exportLoadCts = null;
                cancellation.Dispose();
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void Back()
        {
            if (!CanGoBack)
                return;

            if (IsImport && CurrentStep == 2)
                ClearPreparedImportSelection();

            CurrentStep--;
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task Next()
        {
            if (!CanGoNext)
                return;

            if (CurrentStep == 1)
            {
                if (!Assets.Any(row => row.IsSelected))
                {
                    StatusText = "Select at least one supported asset.";
                    return;
                }

                IsValidatingSelection = true;
                CurrentStep = 2;
                IsBusy = true;
                StatusText = IsImport
                    ? "Preparing selected assets..."
                    : "Validating selected assets...";
                await Task.Yield();
                try
                {
                    if (IsImport)
                        await PrepareImportSelectionAsync().ConfigureAwait(true);
                    else
                        await PrepareExportSelectionAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    StatusText = $"Could not prepare selection: {ex.GetBaseException().Message}";
                    CurrentStep = 1;
                    return;
                }
                finally
                {
                    IsValidatingSelection = false;
                    IsBusy = false;
                }

                UpdateReviewSummary();
                return;
            }
            else if (IsImport && CurrentStep == 2 && !ValidateConflictChoices())
            {
                return;
            }

            CurrentStep++;
            UpdateReviewSummary();
        }

        [RelayCommand]
        private void ToggleVisibleSelection()
        {
            var rows = FilteredAssets.Where(row => row.CanToggle).ToList();
            var shouldSelect = rows.Any(row => !row.IsSelected);
            _isUpdatingVisibleSelection = true;
            try
            {
                foreach (var row in rows)
                    row.IsSelected = shouldSelect;
            }
            finally
            {
                _isUpdatingVisibleSelection = false;
            }

            if (SelectedStatusFilter == "Selected")
                OnPropertyChanged(nameof(FilteredAssets));
            OnPropertyChanged(nameof(ConflictAssets));
            OnPropertyChanged(nameof(VisibleSelectionState));
        }

        public async Task<bool> ImportAsync(CancellationToken cancellationToken = default)
        {
            if (!ShowImportAction || !CanCommit || !ValidateConflictChoices())
                return false;

            IsBusy = true;
            StatusText = "Staging and validating import...";
            try
            {
                var selectedRows = Assets
                    .Where(row => row.IsSelected && row.IsPrepared)
                    .ToList();
                var choices = selectedRows
                    .SelectMany(row => row.ToImportChoices())
                    .ToList();
                var result = await _service.ImportAsync(choices, cancellationToken).ConfigureAwait(true);
                CompletionTitle = "Import complete";
                CompletionDetail = $"{selectedRows.Count(row => row.WillWriteImport)} asset(s) saved to Module." +
                    (result.BackupDirectory == null
                        ? string.Empty
                        : $" Backups: {result.BackupDirectory}");
                StatusText = CompletionDetail;
                IsComplete = true;
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Import failed; Module was rolled back: {ex.GetBaseException().Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<bool> ExportAsync(
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            if (!ShowExportAction || !CanCommit)
                return false;

            IsBusy = true;
            StatusText = "Converting assets and writing temporary ERF...";
            try
            {
                var selectedRows = Assets.Where(row => row.IsSelected).ToList();
                var selected = selectedRows.SelectMany(row => row.FileNames).ToList();
                var result = await _service.ExportAsync(selected, destinationPath, cancellationToken)
                    .ConfigureAwait(true);
                CompletionTitle = "Export complete";
                CompletionDetail =
                    $"{selectedRows.Count} asset(s) saved to {result.DestinationPath}.";
                StatusText = CompletionDetail;
                IsComplete = true;
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Export failed; the destination was not changed: {ex.GetBaseException().Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PrepareImportSelectionAsync()
        {
            var session = _session
                ?? throw new InvalidOperationException("Select an ERF file first.");
            var explicitSelection = SelectedFileNames();
            _explicitImportSelection.Clear();
            foreach (var row in Assets.Where(row => row.IsSelected))
                _explicitImportSelection.Add(row);
            StatusText = "Finding anything else the import needs...";
            var dependencies = await _service.FindImportDependenciesAsync(session, explicitSelection)
                .ConfigureAwait(true);
            ApplyDependencies(dependencies);

            StatusText = "Comparing the selected assets with the open module...";
            var selection = SelectedFileNames();
            var prepared = await _service.PrepareImportAsync(session, selection).ConfigureAwait(true);
            var byFile = RowsByPhysicalFileName();
            foreach (var group in prepared.GroupBy(item => byFile[item.Asset.FileName]))
                group.Key.ApplyPrepared(group);

            StatusText =
                $"{Assets.Count(row => row.IsSelected && row.IsPrepared)} asset(s) prepared; " +
                $"{Assets.Count(row => row.IsSelected && row.HasConflict)} conflict(s) need a choice.";
        }

        private void ClearPreparedImportSelection()
        {
            foreach (var row in Assets)
            {
                var wasAddedAutomatically = row.IsRequired && !_explicitImportSelection.Contains(row);
                row.IsRequired = false;
                row.RequiredReason = string.Empty;
                row.ApplyPrepared(Array.Empty<ErfPreparedImport>());
                if (wasAddedAutomatically)
                    row.IsSelected = false;
            }

            _explicitImportSelection.Clear();
            OnPropertyChanged(nameof(FilteredAssets));
            OnPropertyChanged(nameof(ConflictAssets));
            OnPropertyChanged(nameof(VisibleSelectionState));
        }

        private async Task PrepareExportSelectionAsync()
        {
            StatusText = "Checking the selected assets...";
            var selectedAssets = Assets
                .Where(row => row.IsSelected)
                .SelectMany(row => row.ModuleAssets)
                .ToList();
            await _service.ValidateExportSelectionAsync(selectedAssets)
                .ConfigureAwait(true);

            StatusText =
                $"Validation passed for {Assets.Count(row => row.IsSelected)} asset(s). ERF format: V1.0.";
        }

        private void ApplyDependencies(IEnumerable<ErfDependency> dependencies)
        {
            var byFile = RowsByPhysicalFileName();
            foreach (var dependency in dependencies)
            {
                if (!byFile.TryGetValue(dependency.FileName, out var row))
                    continue;
                row.RequiredReason = dependency.Reason;
                row.IsRequired = true;
            }
            OnPropertyChanged(nameof(FilteredAssets));
        }

        private bool ValidateConflictChoices()
        {
            foreach (var row in Assets.Where(row => row.IsSelected && row.IsPrepared))
            {
                if (row.ConflictActionLabel != "Rename imported")
                    continue;

                var value = row.RenameResRef;
                if (string.IsNullOrWhiteSpace(value) || value.Length > 16 ||
                    value.Any(character => character is not (>= 'a' and <= 'z'
                        or >= 'A' and <= 'Z'
                        or >= '0' and <= '9'
                        or '_')))
                {
                    StatusText =
                        $"'{row.FileName}' needs a new 1-16 character resref using letters, digits, or underscores.";
                    return false;
                }
            }

            return true;
        }

        private void UpdateReviewSummary()
        {
            var selected = Assets.Count(row => row.IsSelected);
            if (IsImport && CurrentStep == 3)
            {
                var writes = Assets.Count(row => row.IsSelected && row.WillWriteImport);
                var skipped = selected - writes;
                StatusText =
                    $"Ready to save {writes} asset(s) to Module; {skipped} will be skipped. " +
                    "No files have been changed yet.";
            }
            else if (IsExport && CurrentStep == 3)
            {
                StatusText =
                    $"Ready to save {selected} asset(s) to an ERF.";
            }
        }

        private void SetRows(IEnumerable<ErfAssetRow> rows)
        {
            ResetRows();
            AppendRows(rows);
        }

        private void ResetRows()
        {
            CancelResourceNameLoading();
            foreach (var row in Assets)
                row.PropertyChanged -= OnRowPropertyChanged;
            Assets.Clear();
            _areaRows.Clear();
            TypeFilters.Clear();
            TypeFilters.Add("All types");

            SelectedTypeFilter = "All types";
            SelectedStatusFilter = "All assets";
            SearchText = string.Empty;
            OnPropertyChanged(nameof(FilteredAssets));
            OnPropertyChanged(nameof(VisibleSelectionState));
            OnPropertyChanged(nameof(CanToggleVisibleAssets));
        }

        private void BeginModuleResourceNameLoading()
        {
            StartResourceNameLoading(async cancellationToken =>
            {
                var names = await _service.ReadModuleResourceNamesAsync(cancellationToken)
                    .ConfigureAwait(true);
                foreach (var batch in Assets.ToList().Chunk(128))
                {
                    foreach (var row in batch)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var name = row.ModuleAssets
                            .Select(asset => names.GetValueOrDefault(asset.FileName))
                            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                        if (!string.IsNullOrWhiteSpace(name))
                            row.ResourceName = name;
                    }
                    OnPropertyChanged(nameof(FilteredAssets));
                    await Task.Yield();
                }
            });
        }

        private void BeginImportResourceNameLoading(ErfArchiveSession session)
        {
            StartResourceNameLoading(async cancellationToken =>
            {
                var rows = Assets.ToList();
                foreach (var batch in rows.Chunk(12))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var physicalAssets = batch
                        .Select(row => row.IsArea
                            ? row.ArchiveAssets.FirstOrDefault(asset =>
                                asset.Extension.Equals("are", StringComparison.OrdinalIgnoreCase))
                            : row.ArchiveAssets.FirstOrDefault())
                        .Where(asset => asset != null)
                        .Cast<ErfArchiveAsset>()
                        .ToList();
                    var names = await _service.ReadImportResourceNamesAsync(
                            session,
                            physicalAssets,
                            cancellationToken)
                        .ConfigureAwait(true);
                    foreach (var row in batch)
                    {
                        var name = row.ArchiveAssets
                            .Select(asset => names.GetValueOrDefault(asset.FileName))
                            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                        if (!string.IsNullOrWhiteSpace(name) && Assets.Contains(row))
                            row.ResourceName = name;
                    }
                    OnPropertyChanged(nameof(FilteredAssets));
                }
            });
        }

        private void StartResourceNameLoading(Func<CancellationToken, Task> load)
        {
            CancelResourceNameLoading();
            var cancellation = new CancellationTokenSource();
            _resourceNameLoadCts = cancellation;
            _ = RunResourceNameLoadingAsync(load, cancellation);
        }

        private async Task RunResourceNameLoadingAsync(
            Func<CancellationToken, Task> load,
            CancellationTokenSource cancellation)
        {
            try
            {
                await load(cancellation.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Switching modes, choosing another archive, or closing the window stops optional
                // name discovery without changing the workflow status.
            }
            catch
            {
                // A resource name is optional. The asset remains usable by its file name.
            }
            finally
            {
                if (ReferenceEquals(_resourceNameLoadCts, cancellation))
                    _resourceNameLoadCts = null;
                cancellation.Dispose();
            }
        }

        private void CancelResourceNameLoading()
        {
            _resourceNameLoadCts?.Cancel();
            _resourceNameLoadCts = null;
        }

        private void AppendRows(IEnumerable<ErfAssetRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.IsArea &&
                    _areaRows.TryGetValue(row.ResRef, out var existingArea))
                {
                    existingArea.MergeArea(row);
                    continue;
                }

                row.PropertyChanged += OnRowPropertyChanged;
                Assets.Add(row);
                if (row.IsArea)
                    _areaRows.Add(row.ResRef, row);
                if (!TypeFilters.Contains(row.TypeName))
                    TypeFilters.Add(row.TypeName);
            }

            OnPropertyChanged(nameof(FilteredAssets));
            OnPropertyChanged(nameof(VisibleSelectionState));
            OnPropertyChanged(nameof(CanToggleVisibleAssets));
        }

        private List<string> SelectedFileNames() =>
            Assets
                .Where(row => row.IsSelected)
                .SelectMany(row => row.FileNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private Dictionary<string, ErfAssetRow> RowsByPhysicalFileName() =>
            Assets
                .SelectMany(row => row.FileNames.Select(fileName => (fileName, row)))
                .ToDictionary(pair => pair.fileName, pair => pair.row, StringComparer.OrdinalIgnoreCase);

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ErfAssetRow.IsSelected))
            {
                if (_isUpdatingVisibleSelection)
                    return;

                if (SelectedStatusFilter == "Selected")
                    OnPropertyChanged(nameof(FilteredAssets));
                OnPropertyChanged(nameof(ConflictAssets));
                OnPropertyChanged(nameof(VisibleSelectionState));
                return;
            }

            if (e.PropertyName == nameof(ErfAssetRow.IsRequired))
            {
                OnPropertyChanged(nameof(FilteredAssets));
                OnPropertyChanged(nameof(ConflictAssets));
                OnPropertyChanged(nameof(VisibleSelectionState));
                OnPropertyChanged(nameof(CanToggleVisibleAssets));
                return;
            }

            if (e.PropertyName == nameof(ErfAssetRow.ConflictActionLabel))
                OnPropertyChanged(nameof(ConflictAssets));
        }

        partial void OnCurrentStepChanged(int value)
        {
            BackCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        partial void OnModeChanged(ErfArchiveMode value)
        {
            NextCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsBusyChanged(bool value)
        {
            BackCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsCompleteChanged(bool value)
        {
            BackCommand.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _exportLoadCts?.Cancel();
            CancelResourceNameLoading();
            _session?.Dispose();
            _session = null;
        }
    }
}
