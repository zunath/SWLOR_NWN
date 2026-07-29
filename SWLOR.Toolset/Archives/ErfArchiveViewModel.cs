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
        private bool _isSelected;
        private bool _isRequired;
        private string _requiredReason = string.Empty;
        private string _conflictActionLabel = string.Empty;
        private string _renameResRef = string.Empty;

        public ErfAssetRow(ErfArchiveAsset asset)
        {
            ArchiveAsset = asset;
            FileName = asset.FileName;
            ResRef = asset.ResRef;
            Extension = asset.Extension;
            TypeName = asset.TypeName;
            Size = asset.Size;
            IsSupported = asset.IsSupported;
            Detail = asset.UnsupportedReason ?? $"Module/{asset.Extension}/{DestinationFileName(asset.Extension, asset.ResRef)}";
        }

        public ErfAssetRow(ModuleArchiveAsset asset)
        {
            ModuleAsset = asset;
            FileName = asset.FileName;
            ResRef = asset.ResRef;
            Extension = asset.Extension;
            TypeName = asset.TypeName;
            Size = asset.Size;
            IsSupported = true;
            Detail = Path.GetRelativePath(
                Directory.GetParent(Path.GetDirectoryName(asset.SourcePath)!)!.FullName,
                asset.SourcePath);
        }

        public ErfArchiveAsset? ArchiveAsset { get; }
        public ModuleArchiveAsset? ModuleAsset { get; }
        public ErfPreparedImport? Prepared { get; set; }
        public string FileName { get; }
        public string ResRef { get; }
        public string Extension { get; }
        public string TypeName { get; }
        public long Size { get; }
        public bool IsSupported { get; }
        public string Detail { get; }
        public string SizeLabel => Size < 1024 ? $"{Size} B" : $"{Size / 1024d:N1} KB";
        public bool CanToggle => IsSupported && !IsRequired;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!IsSupported || IsRequired)
                    return;
                SetProperty(ref _isSelected, value);
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

        public string ConflictLabel => Prepared?.Conflict switch
        {
            ErfConflictKind.New => "New",
            ErfConflictKind.Identical => "Identical",
            ErfConflictKind.Different => "Different",
            _ => IsSupported ? "Ready" : "Unsupported"
        };

        public IReadOnlyList<string> AvailableActions => Prepared?.Conflict switch
        {
            ErfConflictKind.New => new[] { "Add", "Rename imported", "Skip" },
            ErfConflictKind.Identical => new[] { "Skip", "Replace" },
            ErfConflictKind.Different => new[] { "Keep existing", "Replace", "Rename imported" },
            _ => Array.Empty<string>()
        };

        public string ConflictActionLabel
        {
            get => _conflictActionLabel;
            set
            {
                if (SetProperty(ref _conflictActionLabel, value))
                    OnPropertyChanged(nameof(CanRename));
            }
        }

        public bool CanRename =>
            string.Equals(ConflictActionLabel, "Rename imported", StringComparison.Ordinal);

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
                    return "Unsupported";
                if (IsRequired)
                    return $"Required · {RequiredReason}";
                return Prepared == null ? (IsSelected ? "Selected" : "Available") : ConflictLabel;
            }
        }

        public void ApplyPrepared(ErfPreparedImport prepared)
        {
            Prepared = prepared;
            ConflictActionLabel = prepared.DefaultAction switch
            {
                ErfConflictAction.Add => "Add",
                ErfConflictAction.Skip => "Skip",
                ErfConflictAction.KeepExisting => "Keep existing",
                ErfConflictAction.Replace => "Replace",
                ErfConflictAction.Rename => "Rename imported",
                _ => "Skip"
            };
            RenameResRef = SuggestedRename(prepared.Asset.ResRef);
            OnPropertyChanged(nameof(ConflictLabel));
            OnPropertyChanged(nameof(AvailableActions));
            OnPropertyChanged(nameof(StatusLabel));
        }

        public ErfImportChoice ToImportChoice()
        {
            var prepared = Prepared
                ?? throw new InvalidOperationException($"'{FileName}' has not been prepared for import.");
            var action = ConflictActionLabel switch
            {
                "Add" => ErfConflictAction.Add,
                "Replace" => ErfConflictAction.Replace,
                "Rename imported" => ErfConflictAction.Rename,
                "Keep existing" => ErfConflictAction.KeepExisting,
                _ => ErfConflictAction.Skip
            };
            return new ErfImportChoice(prepared, action, RenameResRef);
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
        private readonly ErfArchiveService _service;
        private readonly ToolsetSettings _settings;
        private ErfArchiveSession? _session;
        private CancellationTokenSource? _exportLoadCts;
        private bool _disposed;
        private bool _synchronizingAreaRename;

        public ObservableCollection<ErfAssetRow> Assets { get; } = new();
        public ObservableCollection<string> RecentArchives { get; } = new();
        public ObservableCollection<string> TypeFilters { get; } = new() { "All types" };
        public IReadOnlyList<string> StatusFilters { get; } =
            new[] { "All statuses", "Selected", "Required", "Conflicts", "Unsupported" };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsImport))]
        [NotifyPropertyChangedFor(nameof(IsExport))]
        [NotifyPropertyChangedFor(nameof(ShowImportFile))]
        [NotifyPropertyChangedFor(nameof(ShowExportSnapshot))]
        [NotifyPropertyChangedFor(nameof(ShowImportConflicts))]
        [NotifyPropertyChangedFor(nameof(ShowExportValidation))]
        [NotifyPropertyChangedFor(nameof(ModeTitle))]
        [NotifyPropertyChangedFor(nameof(StepOneLabel))]
        [NotifyPropertyChangedFor(nameof(StepTwoLabel))]
        [NotifyPropertyChangedFor(nameof(StepThreeLabel))]
        [NotifyPropertyChangedFor(nameof(StepFourLabel))]
        private ErfArchiveMode _mode = ErfArchiveMode.Import;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStepOne))]
        [NotifyPropertyChangedFor(nameof(IsStepTwo))]
        [NotifyPropertyChangedFor(nameof(IsStepThree))]
        [NotifyPropertyChangedFor(nameof(IsStepFour))]
        [NotifyPropertyChangedFor(nameof(ShowImportFile))]
        [NotifyPropertyChangedFor(nameof(ShowExportSnapshot))]
        [NotifyPropertyChangedFor(nameof(ShowImportConflicts))]
        [NotifyPropertyChangedFor(nameof(ShowExportValidation))]
        [NotifyPropertyChangedFor(nameof(CanGoBack))]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(ShowNext))]
        [NotifyPropertyChangedFor(nameof(ShowImportAction))]
        [NotifyPropertyChangedFor(nameof(ShowExportAction))]
        [NotifyPropertyChangedFor(nameof(StepTitle))]
        [NotifyPropertyChangedFor(nameof(StepDescription))]
        private int _currentStep;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(CanCommit))]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanCommit))]
        private bool _isComplete;

        [ObservableProperty]
        private string _statusText = "Choose an ERF file to begin.";

        [ObservableProperty]
        private string _importArchivePath = string.Empty;

        [ObservableProperty]
        private string? _selectedRecentArchive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        private string _searchText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        private string _selectedTypeFilter = "All types";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredAssets))]
        private string _selectedStatusFilter = "All statuses";

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
        public bool CanGoBack => !IsBusy && CurrentStep > 0;
        public bool ShowNext => CurrentStep < 3;
        public bool ShowImportAction => IsImport && CurrentStep == 3;
        public bool ShowExportAction => IsExport && CurrentStep == 3;
        public bool ShowImportFile => IsImport && CurrentStep == 0;
        public bool ShowExportSnapshot => IsExport && CurrentStep == 0;
        public bool ShowImportConflicts => IsImport && CurrentStep == 2;
        public bool ShowExportValidation => IsExport && CurrentStep == 2;
        public bool CanCommit => !IsBusy && !IsComplete;
        public bool CanGoNext => !IsBusy && CurrentStep < 3 && (CurrentStep != 0 || !IsImport || _session != null);
        public string ModeTitle => IsImport ? "Import ERF" : "Export ERF";
        public string StepOneLabel => IsImport ? "1  Select ERF file" : "1  Saved workspace";
        public string StepTwoLabel => "2  Choose assets";
        public string StepThreeLabel => IsImport ? "3  Resolve conflicts" : "3  Validate";
        public string StepFourLabel => IsImport ? "4  Save to Module" : "4  Save ERF As";

        public string StepTitle => (Mode, CurrentStep) switch
        {
            (ErfArchiveMode.Import, 0) => "Select an ERF file",
            (ErfArchiveMode.Import, 1) => "Choose assets to import",
            (ErfArchiveMode.Import, 2) => "Resolve conflicts",
            (ErfArchiveMode.Import, 3) => "Save the import to Module",
            (ErfArchiveMode.Export, 0) => "Saved module snapshot",
            (ErfArchiveMode.Export, 1) => "Choose assets to export",
            (ErfArchiveMode.Export, 2) => "Validate the archive plan",
            _ => "Save ERF As"
        };

        public string StepDescription => (Mode, CurrentStep) switch
        {
            (ErfArchiveMode.Import, 0) =>
                "Browse, drop, or reopen a recent .erf. The scan uses a private read-only snapshot.",
            (ErfArchiveMode.Import, 1) =>
                "Select explicit assets. Required area companions, referenced resources, and script includes are added automatically.",
            (ErfArchiveMode.Import, 2) =>
                "Identical resources are skipped. Choose whether different resources stay, are replaced, or are renamed with imported references updated.",
            (ErfArchiveMode.Import, 3) =>
                string.Empty,
            (ErfArchiveMode.Export, 0) =>
                "All open editors were saved before this modal opened. Export reads only that stable on-disk snapshot.",
            (ErfArchiveMode.Export, 1) =>
                "Select module assets. Area companions, matching resource references, and script includes are added before validation.",
            (ErfArchiveMode.Export, 2) =>
                "The selected JSON, resource names, and dependency closure are checked before a destination can be chosen.",
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
                    result = result.Where(row =>
                        row.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        row.TypeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                if (SelectedTypeFilter != "All types")
                    result = result.Where(row => row.TypeName == SelectedTypeFilter);

                result = SelectedStatusFilter switch
                {
                    "Selected" => result.Where(row => row.IsSelected),
                    "Required" => result.Where(row => row.IsRequired),
                    "Conflicts" => result.Where(row =>
                        row.Prepared?.Conflict == ErfConflictKind.Different),
                    "Unsupported" => result.Where(row => !row.IsSupported),
                    _ => result
                };
                return result;
            }
        }

        public IEnumerable<ErfAssetRow> ConflictAssets =>
            Assets.Where(row => row.IsSelected && row.Prepared != null);

        public async Task<bool> LoadArchiveAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (IsBusy)
                return false;

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
                StatusText =
                    $"{Assets.Count} resource(s) scanned; {Assets.Count(row => row.IsSupported)} can be imported.";
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
            StatusText = _session == null
                ? "Choose an ERF file to begin."
                : $"{Assets.Count} resource(s) scanned.";
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
            StatusText = "Finding module resources...";
            try
            {
                await foreach (var batch in _service.EnumerateModuleAssetBatchesAsync(
                                   cancellationToken: cancellation.Token))
                {
                    AppendRows(batch.Select(asset => new ErfAssetRow(asset)));
                    StatusText = $"Loading module resources... {Assets.Count:N0} found.";
                }

                StatusText =
                    $"Saved snapshot ready: {Assets.Count:N0} module resource(s) available.";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (!_disposed)
                    StatusText = "Module resource loading was canceled.";
            }
            catch (Exception ex)
            {
                StatusText =
                    $"Could not load module resources: {ex.GetBaseException().Message}";
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
            if (CanGoBack)
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

                IsBusy = true;
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
                    return;
                }
                finally
                {
                    IsBusy = false;
                }
            }
            else if (IsImport && CurrentStep == 2 && !ValidateConflictChoices())
            {
                return;
            }

            CurrentStep++;
            UpdateReviewSummary();
        }

        [RelayCommand]
        private void SelectVisible()
        {
            foreach (var row in FilteredAssets.Where(row => row.CanToggle))
                row.IsSelected = true;
        }

        [RelayCommand]
        private void ClearVisible()
        {
            foreach (var row in FilteredAssets.Where(row => row.CanToggle))
                row.IsSelected = false;
        }

        public async Task<bool> ImportAsync(CancellationToken cancellationToken = default)
        {
            if (!ShowImportAction || !CanCommit || !ValidateConflictChoices())
                return false;

            IsBusy = true;
            StatusText = "Staging and validating import...";
            try
            {
                var choices = Assets
                    .Where(row => row.IsSelected && row.Prepared != null)
                    .Select(row => row.ToImportChoice())
                    .ToList();
                var result = await _service.ImportAsync(choices, cancellationToken).ConfigureAwait(true);
                CompletionTitle = "Import complete";
                CompletionDetail =
                    $"{result.Imported} resource(s) saved to Module; {result.Replaced} replaced, " +
                    $"{result.Renamed} renamed, {result.Skipped} skipped." +
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
                var selected = Assets.Where(row => row.IsSelected).Select(row => row.FileName).ToList();
                var result = await _service.ExportAsync(selected, destinationPath, cancellationToken)
                    .ConfigureAwait(true);
                CompletionTitle = "Export complete";
                CompletionDetail =
                    $"{result.Exported} resource(s) saved to {result.DestinationPath}.";
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
            var explicitSelection = Assets.Where(row => row.IsSelected).Select(row => row.FileName).ToList();
            StatusText = "Finding required dependencies...";
            var dependencies = await _service.FindImportDependenciesAsync(session, explicitSelection)
                .ConfigureAwait(true);
            ApplyDependencies(dependencies);

            StatusText = "Converting selected GFF resources and comparing Module destinations...";
            var selection = Assets.Where(row => row.IsSelected).Select(row => row.FileName).ToList();
            var prepared = await _service.PrepareImportAsync(session, selection).ConfigureAwait(true);
            var byFile = Assets.ToDictionary(row => row.FileName, StringComparer.OrdinalIgnoreCase);
            foreach (var item in prepared)
                byFile[item.Asset.FileName].ApplyPrepared(item);

            StatusText =
                $"{prepared.Count} resource(s) prepared; " +
                $"{prepared.Count(item => item.Conflict == ErfConflictKind.Different)} conflict(s) need a choice.";
        }

        private async Task PrepareExportSelectionAsync()
        {
            StatusText = "Finding required dependencies...";
            var explicitSelection = Assets.Where(row => row.IsSelected).Select(row => row.FileName).ToList();
            var dependencies = await _service.FindExportDependenciesAsync(explicitSelection)
                .ConfigureAwait(true);
            ApplyDependencies(dependencies);

            // Dependency traversal parses every selected GFF JSON and reads every selected script.
            // Reaching here is therefore the validation pass, not a decorative review page.
            StatusText =
                $"Validation passed for {Assets.Count(row => row.IsSelected)} resource(s). ERF format: V1.0.";
        }

        private void ApplyDependencies(IEnumerable<ErfDependency> dependencies)
        {
            var byFile = Assets.ToDictionary(row => row.FileName, StringComparer.OrdinalIgnoreCase);
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
            foreach (var row in Assets.Where(row => row.IsSelected && row.Prepared != null))
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
                var writes = Assets.Count(row => row.IsSelected &&
                    row.ConflictActionLabel is "Add" or "Replace" or "Rename imported");
                var skipped = selected - writes;
                StatusText =
                    $"Ready to save {writes} resource(s) to Module; {skipped} will be skipped. " +
                    "No files have been changed yet.";
            }
            else if (IsExport && CurrentStep == 3)
            {
                StatusText =
                    $"Ready to write and validate {selected} resource(s) as an ERF V1.0 archive.";
            }
        }

        private void SetRows(IEnumerable<ErfAssetRow> rows)
        {
            ResetRows();
            AppendRows(rows);
        }

        private void ResetRows()
        {
            foreach (var row in Assets)
                row.PropertyChanged -= OnRowPropertyChanged;
            Assets.Clear();
            TypeFilters.Clear();
            TypeFilters.Add("All types");

            SelectedTypeFilter = "All types";
            SelectedStatusFilter = "All statuses";
            SearchText = string.Empty;
            OnPropertyChanged(nameof(FilteredAssets));
        }

        private void AppendRows(IEnumerable<ErfAssetRow> rows)
        {
            foreach (var row in rows)
            {
                row.PropertyChanged += OnRowPropertyChanged;
                Assets.Add(row);
                if (!TypeFilters.Contains(row.TypeName))
                    TypeFilters.Add(row.TypeName);
            }

            OnPropertyChanged(nameof(FilteredAssets));
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_synchronizingAreaRename &&
                sender is ErfAssetRow changed &&
                changed.Extension is "are" or "git" or "gic" &&
                e.PropertyName is nameof(ErfAssetRow.ConflictActionLabel)
                    or nameof(ErfAssetRow.RenameResRef))
            {
                SynchronizeAreaRename(changed, e.PropertyName);
            }

            if (e.PropertyName is nameof(ErfAssetRow.IsSelected)
                or nameof(ErfAssetRow.IsRequired)
                or nameof(ErfAssetRow.ConflictActionLabel))
            {
                OnPropertyChanged(nameof(FilteredAssets));
                OnPropertyChanged(nameof(ConflictAssets));
            }
        }

        private void SynchronizeAreaRename(ErfAssetRow changed, string? propertyName)
        {
            var companions = Assets.Where(row =>
                    row.IsSelected &&
                    row.Prepared != null &&
                    row.ResRef.Equals(changed.ResRef, StringComparison.OrdinalIgnoreCase) &&
                    row.Extension is "are" or "git" or "gic")
                .ToList();
            if (companions.Count <= 1)
                return;

            _synchronizingAreaRename = true;
            try
            {
                if (changed.ConflictActionLabel == "Rename imported")
                {
                    foreach (var companion in companions)
                    {
                        companion.ConflictActionLabel = "Rename imported";
                        companion.RenameResRef = changed.RenameResRef;
                    }
                }
                else if (propertyName == nameof(ErfAssetRow.ConflictActionLabel) &&
                         companions.Any(row => row.ConflictActionLabel == "Rename imported"))
                {
                    // Cancelling the grouped rename returns each companion to the safe default for
                    // its own comparison state; one area can never be split across two resrefs.
                    foreach (var companion in companions)
                    {
                        companion.ConflictActionLabel = companion.Prepared!.DefaultAction switch
                        {
                            ErfConflictAction.Add => "Add",
                            ErfConflictAction.Replace => "Replace",
                            ErfConflictAction.KeepExisting => "Keep existing",
                            _ => "Skip"
                        };
                    }
                }
            }
            finally
            {
                _synchronizingAreaRename = false;
            }
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

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _exportLoadCts?.Cancel();
            _session?.Dispose();
            _session = null;
        }
    }
}
