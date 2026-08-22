using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Module
{
    public sealed record ModuleHakRow(string Name, bool IsMissing)
    {
        public string Status => IsMissing ? "Missing" : "Available";
    }

    public sealed class ModuleEventRowViewModel : ObservableObject
    {
        private readonly ModulePropertiesDocumentViewModel _owner;

        internal ModuleEventRowViewModel(
            ModulePropertiesDocumentViewModel owner,
            string label,
            string fieldName)
        {
            _owner = owner;
            Label = label;
            FieldName = fieldName;
            EditCommand = new RelayCommand(
                () => _owner.OpenScript(Script),
                () => !string.IsNullOrWhiteSpace(Script));
        }

        public string Label { get; }
        public string FieldName { get; }
        public IReadOnlyList<string> ScriptChoices => _owner.ScriptChoices;

        public string Script
        {
            get => _owner.Document.GetScript(FieldName);
            set
            {
                var normalized = value?.Trim() ?? string.Empty;
                if (Script == normalized)
                    return;

                if (_owner.SetEventScript(this, normalized))
                    NotifyValueChanged();
            }
        }

        public IRelayCommand EditCommand { get; }

        internal void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(Script));
            EditCommand.NotifyCanExecuteChanged();
        }
    }

    public sealed record ModulePropertiesActions(
        Func<Task>? RunValidation = null,
        Func<Task>? BuildAllScripts = null,
        Func<Task>? PackModule = null,
        Action<string>? OpenFile = null);

    /// <summary>
    /// Undoable document editor for module.ifo. It is deliberately a normal dock document: File
    /// Save, Edit Undo/Redo, unsaved-close prompts, and activation all follow the same path as the
    /// existing blueprint and area editors.
    /// </summary>
    public partial class ModulePropertiesDocumentViewModel : Document, IEditorDocument
    {
        private static readonly (string Label, string Field)[] EventDefinitions =
        {
            ("On Acquire Item", "Mod_OnAcquirItem"),
            ("On Activate Item", "Mod_OnActvtItem"),
            ("On Client Enter", "Mod_OnClientEntr"),
            ("On Client Leave", "Mod_OnClientLeav"),
            ("On Cutscene Abort", "Mod_OnCutsnAbort"),
            ("On Heartbeat", "Mod_OnHeartbeat"),
            ("On Module Load", "Mod_OnModLoad"),
            ("On Module Start", "Mod_OnModStart"),
            ("On NUI Event", "Mod_OnNuiEvent"),
            ("On Player Chat", "Mod_OnPlrChat"),
            ("On Player Death", "Mod_OnPlrDeath"),
            ("On Player Dying", "Mod_OnPlrDying"),
            ("On Player Equip Item", "Mod_OnPlrEqItm"),
            ("On Player GUI Event", "Mod_OnPlrGuiEvt"),
            ("On Player Level Up", "Mod_OnPlrLvlUp"),
            ("On Player Respawn", "Mod_OnSpawnBtnDn"),
            ("On Player Rest", "Mod_OnPlrRest"),
            ("On Player Target", "Mod_OnPlrTarget"),
            ("On Player Tile Action", "Mod_OnPlrTileAct"),
            ("On Player Unequip Item", "Mod_OnPlrUnEqItm"),
            ("On Unacquire Item", "Mod_OnUnAqreItem"),
            ("On User Defined", "Mod_OnUsrDefined")
        };

        private readonly DocumentSession _session;
        private readonly string _moduleRoot;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly ModuleCustomContentService? _customContent;
        private readonly Action<string>? _openScript;
        private readonly ModulePropertiesActions _actions;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;
        private bool _refreshing;
        private CancellationTokenSource? _customReloadCts;
        private IReadOnlyList<string> _allAvailableHaks = Array.Empty<string>();

        internal IfoDocument Document { get; }

        public ObservableCollection<ModuleEventRowViewModel> Events { get; } = new();
        public ObservableCollection<ModuleHakRow> Haks { get; } = new();
        public ObservableCollection<string> AvailableHaks { get; } = new();
        public ObservableCollection<string> CustomTlkChoices { get; } = new();
        public ObservableCollection<string> StartingMovieChoices { get; } = new();
        public VarTableSectionViewModel Variables { get; private set; }
        public IReadOnlyList<string> ScriptChoices { get; }

        [ObservableProperty] private ModuleHakRow? _selectedHak;
        [ObservableProperty] private string? _selectedAvailableHak;
        [ObservableProperty] private string _customContentStatus = "Ready";
        [ObservableProperty] private bool _isReloadingCustomContent;

        public bool IsDirty => _session.UndoStack.IsDirty;
        public bool CanUndo => _session.UndoStack.CanUndo;
        public bool CanRedo => _session.UndoStack.CanRedo;
        public string FilePath => _session.FilePath;

        public string Name
        {
            get => Document.Name.Text ?? string.Empty;
            set => SetText("Change module name", Name, value, text => Document.Name.Text = text, nameof(Name));
        }

        public string Tag
        {
            get => Document.Tag ?? string.Empty;
            set => SetText("Change module tag", Tag, value, text => Document.Tag = text, nameof(Tag));
        }

        public string EntryArea => Document.EntryArea ?? string.Empty;
        public string EntryX => Document.EntryX.ToString("0.###", CultureInfo.InvariantCulture);
        public string EntryY => Document.EntryY.ToString("0.###", CultureInfo.InvariantCulture);
        public string EntryZ => Document.EntryZ.ToString("0.###", CultureInfo.InvariantCulture);

        public decimal MinutesPerHour
        {
            get => Document.MinutesPerHour;
            set => SetNumber("Change minutes per hour", Document.MinutesPerHour, value, 1, 255,
                number => Document.MinutesPerHour = number, nameof(MinutesPerHour));
        }

        public decimal DawnHour
        {
            get => Document.DawnHour;
            set => SetNumber("Change dawn hour", Document.DawnHour, value, 0, 23,
                number => Document.DawnHour = number, nameof(DawnHour));
        }

        public decimal DuskHour
        {
            get => Document.DuskHour;
            set => SetNumber("Change dusk hour", Document.DuskHour, value, 0, 23,
                number => Document.DuskHour = number, nameof(DuskHour));
        }

        public decimal StartingMonth
        {
            get => Document.StartingMonth;
            set => SetNumber("Change starting month", Document.StartingMonth, value, 1, 12,
                number => Document.StartingMonth = number, nameof(StartingMonth));
        }

        public decimal StartingDay
        {
            get => Document.StartingDay;
            set => SetNumber("Change starting day", Document.StartingDay, value, 1, 28,
                number => Document.StartingDay = number, nameof(StartingDay));
        }

        public decimal StartingHour
        {
            get => Document.StartingHour;
            set => SetNumber("Change starting hour", Document.StartingHour, value, 0, 23,
                number => Document.StartingHour = number, nameof(StartingHour));
        }

        public decimal StartingYear
        {
            get => Document.StartingYear;
            set => SetNumber("Change starting year", checked((int)Document.StartingYear), value, 0, int.MaxValue,
                number => Document.StartingYear = (uint)number, nameof(StartingYear));
        }

        public decimal XpScale
        {
            get => Document.XpScale;
            set => SetNumber("Change XP scale", Document.XpScale, value, 0, 200,
                number => Document.XpScale = number, nameof(XpScale));
        }

        public string StartingMovie
        {
            get => Document.StartingMovie ?? string.Empty;
            set => SetText(
                "Change starting movie",
                StartingMovie,
                value?.Trim() ?? string.Empty,
                text => Document.StartingMovie = text,
                nameof(StartingMovie));
        }

        public string Description
        {
            get => Document.Description.Text ?? string.Empty;
            set => SetText(
                "Change module description",
                Description,
                value,
                text => Document.Description.Text = text,
                nameof(Description));
        }

        public string? SelectedCustomTlk
        {
            get => string.IsNullOrWhiteSpace(Document.CustomTlk) ? null : Document.CustomTlk;
            set
            {
                if (_refreshing)
                    return;
                var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                if (string.Equals(SelectedCustomTlk, normalized, StringComparison.OrdinalIgnoreCase))
                    return;

                if (RunEdit("Change custom TLK", () => Document.CustomTlk = normalized))
                {
                    OnPropertyChanged(nameof(SelectedCustomTlk));
                    QueueCustomContentReload();
                }
            }
        }

        public string NwnIniPath { get; private set; } = string.Empty;
        public string HakDirectory { get; private set; } = "Not configured";
        public string TlkDirectory { get; private set; } = "Not configured";
        public string ModuleRootDisplay => _moduleRoot;
        public string HakConfigurationPath { get; }
        public string PackOutput { get; private set; } = "Not configured";
        public bool ChecksumChecking { get; private set; }
        public string MinimumGameVersion => Document.Fields.GetStringOrNull("Mod_MinGameVer") ?? string.Empty;
        public string ModuleVersion => (Document.Fields.GetUIntOrNull("Mod_Version") ?? 0).ToString();
        public int AreaCount => Document.AreaList.Count;
        public int AssignedHakCount => Haks.Count;
        public int MissingHakCount => Haks.Count(row => row.IsMissing);

        public IAsyncRelayCommand RunValidationCommand { get; }
        public IAsyncRelayCommand BuildAllScriptsCommand { get; }
        public IAsyncRelayCommand PackModuleCommand { get; }
        public IAsyncRelayCommand CheckHakConflictsCommand { get; }
        public IRelayCommand OpenBuildConfigurationCommand { get; }

        public event Action<ModulePropertiesDocumentViewModel>? Closed;
        public event Action<ModulePropertiesDocumentViewModel>? CloseRequested;
        public event Action? Saved;

        public ModulePropertiesDocumentViewModel(
            string filePath,
            string moduleRoot,
            Domain.Workspace.ModuleWorkspace workspace,
            OutputLogService log,
            IEditorPromptService prompts,
            IGameCodeIndex? gameCodeIndex = null,
            ModuleCustomContentService? customContent = null,
            Action<string>? openScript = null,
            ModulePropertiesActions? actions = null)
        {
            _moduleRoot = moduleRoot;
            _log = log;
            _prompts = prompts;
            _customContent = customContent;
            _openScript = openScript;
            _actions = actions ?? new ModulePropertiesActions();
            Id = $"module-properties:{filePath}";
            _session = DocumentSession.Open(filePath);
            Document = new IfoDocument(_session.Document);
            Variables = new VarTableSectionViewModel(RunEdit, Document.VarTable, gameCodeIndex);
            ScriptChoices = BuildScriptChoices(workspace, Document);
            foreach (var (label, field) in EventDefinitions)
                Events.Add(new ModuleEventRowViewModel(this, label, field));

            HakConfigurationPath = Path.Combine(Directory.GetParent(moduleRoot)?.FullName ?? moduleRoot,
                "Build", "hakbuilder.json");
            ReadBuildConfiguration();
            RefreshCustomContentDiscovery();

            RunValidationCommand = new AsyncRelayCommand(() => _actions.RunValidation?.Invoke() ?? Task.CompletedTask);
            BuildAllScriptsCommand = new AsyncRelayCommand(() => _actions.BuildAllScripts?.Invoke() ?? Task.CompletedTask);
            PackModuleCommand = new AsyncRelayCommand(() => _actions.PackModule?.Invoke() ?? Task.CompletedTask);
            CheckHakConflictsCommand = new AsyncRelayCommand(CheckHakConflictsAsync);
            OpenBuildConfigurationCommand = new RelayCommand(
                OpenBuildConfiguration,
                () => File.Exists(HakConfigurationPath));
            UpdateTitle();
        }

        private static IReadOnlyList<string> BuildScriptChoices(
            Domain.Workspace.ModuleWorkspace workspace,
            IfoDocument document)
        {
            var scripts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var script in workspace.EnumerateResRefs(ResourceType.Nss))
                scripts.Add(script);

            var compiledDirectory = Path.Combine(workspace.ModuleRoot, "ncs");
            if (Directory.Exists(compiledDirectory))
            {
                try
                {
                    foreach (var path in Directory.EnumerateFiles(
                                 compiledDirectory,
                                 "*.ncs",
                                 SearchOption.TopDirectoryOnly))
                    {
                        var script = Path.GetFileNameWithoutExtension(path);
                        if (!string.IsNullOrWhiteSpace(script))
                            scripts.Add(script);
                    }
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException)
                {
                    // Source scripts and current assignments remain available when compiled output
                    // is temporarily locked by a build or inaccessible on disk.
                }
            }

            foreach (var (_, field) in EventDefinitions)
            {
                var assigned = document.GetScript(field).Trim();
                if (assigned.Length > 0)
                    scripts.Add(assigned);
            }

            return new[] { string.Empty }
                .Concat(scripts.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        internal bool SetEventScript(ModuleEventRowViewModel row, string value) =>
            RunEdit($"Change {row.Label} script", () => Document.SetScript(row.FieldName, value));

        internal void OpenScript(string script) => _openScript?.Invoke(script);

        private void SetText(
            string description,
            string current,
            string? value,
            Action<string> mutation,
            string propertyName)
        {
            value ??= string.Empty;
            if (current == value)
                return;
            if (RunEdit(description, () => mutation(value)))
                OnPropertyChanged(propertyName);
        }

        private void SetNumber(
            string description,
            int current,
            decimal value,
            int minimum,
            int maximum,
            Action<int> mutation,
            string propertyName)
        {
            var number = decimal.ToInt32(decimal.Clamp(value, minimum, maximum));
            if (current == number)
                return;
            if (RunEdit(description, () => mutation(number)))
                OnPropertyChanged(propertyName);
        }

        private bool RunEdit(string description, Action mutation)
        {
            try
            {
                _session.Execute(description, mutation);
                AfterHistoryChange();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Module Properties edit failed ({description}): {ex.Message}");
                return false;
            }
        }

        [RelayCommand]
        private void AddHak()
        {
            if (string.IsNullOrWhiteSpace(SelectedAvailableHak))
                return;

            var names = Document.HakNames.ToList();
            if (names.Contains(SelectedAvailableHak, StringComparer.OrdinalIgnoreCase))
                return;
            names.Add(SelectedAvailableHak);
            ApplyHakList("Add HAK", names, names.Count - 1);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveHak))]
        private void RemoveHak()
        {
            if (SelectedHak == null)
                return;
            var index = Haks.IndexOf(SelectedHak);
            var names = Document.HakNames.ToList();
            if (index < 0 || index >= names.Count)
                return;
            names.RemoveAt(index);
            ApplyHakList("Remove HAK", names, Math.Min(index, names.Count - 1));
        }

        private bool CanRemoveHak() => SelectedHak != null;

        [RelayCommand(CanExecute = nameof(CanMoveHakUp))]
        private void MoveHakUp() => MoveHak(-1);

        private bool CanMoveHakUp() => SelectedHak != null && Haks.IndexOf(SelectedHak) > 0;

        [RelayCommand(CanExecute = nameof(CanMoveHakDown))]
        private void MoveHakDown() => MoveHak(1);

        private bool CanMoveHakDown() =>
            SelectedHak != null && Haks.IndexOf(SelectedHak) >= 0 && Haks.IndexOf(SelectedHak) < Haks.Count - 1;

        private void MoveHak(int offset)
        {
            if (SelectedHak == null)
                return;
            var index = Haks.IndexOf(SelectedHak);
            var target = index + offset;
            var names = Document.HakNames.ToList();
            if (index < 0 || target < 0 || target >= names.Count)
                return;
            (names[index], names[target]) = (names[target], names[index]);
            ApplyHakList(offset < 0 ? "Move HAK up" : "Move HAK down", names, target);
        }

        private void ApplyHakList(string description, IReadOnlyList<string> names, int selectedIndex)
        {
            if (!RunEdit(description, () => Document.SetHakNames(names)))
                return;

            RefreshHakRows(selectedIndex);
            QueueCustomContentReload();
        }

        partial void OnSelectedHakChanged(ModuleHakRow? value)
        {
            RemoveHakCommand.NotifyCanExecuteChanged();
            MoveHakUpCommand.NotifyCanExecuteChanged();
            MoveHakDownCommand.NotifyCanExecuteChanged();
        }

        private void RefreshCustomContentDiscovery()
        {
            ModuleCustomContentSnapshot? snapshot = null;
            try
            {
                snapshot = _customContent?.Discover();
            }
            catch (Exception ex)
            {
                CustomContentStatus = $"Could not read nwn.ini: {ex.Message}";
            }

            var profile = snapshot?.Profile ?? Domain.GameData.Resources.NwnIniProfile.Load();
            NwnIniPath = profile.IniPath;
            HakDirectory = profile.HakDirectory ?? "Not configured";
            TlkDirectory = profile.TlkDirectory ?? "Not configured";

            _allAvailableHaks = snapshot?.AvailableHaks ?? profile.EnumerateHakNames();

            CustomTlkChoices.Clear();
            CustomTlkChoices.Add(string.Empty);
            foreach (var name in snapshot?.AvailableTlks ?? profile.EnumerateTlkNames())
                CustomTlkChoices.Add(name);
            if (!string.IsNullOrWhiteSpace(Document.CustomTlk) &&
                !CustomTlkChoices.Contains(Document.CustomTlk, StringComparer.OrdinalIgnoreCase))
            {
                CustomTlkChoices.Add(Document.CustomTlk!);
            }

            StartingMovieChoices.Clear();
            StartingMovieChoices.Add(string.Empty);
            foreach (var name in snapshot?.AvailableMovies ?? profile.EnumerateMovieNames())
                StartingMovieChoices.Add(name);
            if (!string.IsNullOrWhiteSpace(Document.StartingMovie) &&
                !StartingMovieChoices.Contains(Document.StartingMovie, StringComparer.OrdinalIgnoreCase))
            {
                StartingMovieChoices.Add(Document.StartingMovie!);
            }

            RefreshHakRows();
            OnPropertyChanged(nameof(StartingMovie));
            OnPropertyChanged(nameof(SelectedCustomTlk));
            OnPropertyChanged(nameof(NwnIniPath));
            OnPropertyChanged(nameof(HakDirectory));
            OnPropertyChanged(nameof(TlkDirectory));
        }

        private void RefreshHakRows(int selectedIndex = -1)
        {
            var available = _allAvailableHaks.ToHashSet(StringComparer.OrdinalIgnoreCase);
            Haks.Clear();
            foreach (var name in Document.HakNames)
                Haks.Add(new ModuleHakRow(name, !available.Contains(name)));

            SelectedHak = selectedIndex >= 0 && selectedIndex < Haks.Count ? Haks[selectedIndex] : null;
            var assigned = Document.HakNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var previousAvailable = SelectedAvailableHak;
            var unassigned = _allAvailableHaks.Where(name => !assigned.Contains(name)).ToList();
            AvailableHaks.Clear();
            foreach (var name in unassigned)
                AvailableHaks.Add(name);
            SelectedAvailableHak = unassigned.Contains(previousAvailable, StringComparer.OrdinalIgnoreCase)
                ? previousAvailable
                : unassigned.FirstOrDefault();

            OnPropertyChanged(nameof(AssignedHakCount));
            OnPropertyChanged(nameof(MissingHakCount));
        }

        private void QueueCustomContentReload()
        {
            _customReloadCts?.Cancel();
            _customReloadCts?.Dispose();
            var cts = new CancellationTokenSource();
            _customReloadCts = cts;
            _ = ReloadCustomContentAfterDelayAsync(cts.Token);
        }

        private async Task ReloadCustomContentAfterDelayAsync(CancellationToken cancellationToken)
        {
            if (_customContent == null)
            {
                CustomContentStatus = "Resource reload is unavailable.";
                return;
            }

            try
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(true);
                IsReloadingCustomContent = true;
                CustomContentStatus = "Reloading assigned HAK and TLK resources...";
                var result = await _customContent.ReloadAsync(
                    Document.HakNames,
                    Document.CustomTlk,
                    cancellationToken).ConfigureAwait(true);
                CustomContentStatus = result.MissingHaks.Count == 0
                    ? $"Loaded {result.LoadedHakCount} HAK layers" +
                      (result.CustomTlk == null ? "." : $" and {result.CustomTlk}.tlk.")
                    : $"Loaded {result.LoadedHakCount} HAK layers; {result.MissingHaks.Count} assigned HAK(s) are missing.";
                RefreshCustomContentDiscovery();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CustomContentStatus = $"Reload failed: {ex.GetBaseException().Message}";
                _log.AppendLine($"Module custom-content reload failed: {ex.GetBaseException().Message}");
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                    IsReloadingCustomContent = false;
            }
        }

        [RelayCommand]
        private async Task Save() => await TrySaveAsync().ConfigureAwait(true);

        [RelayCommand(CanExecute = nameof(IsDirty))]
        private void Revert()
        {
            var customContentBefore = CustomContentSignature();
            _session.RevertToSaved();
            ReloadFields();
            if (customContentBefore != CustomContentSignature())
                QueueCustomContentReload();
            AfterHistoryChange();
        }

        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty)
                return true;

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts.ConfirmExternalChangeAsync(_session.FilePath).ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;
                    if (choice == ExternalChangeChoice.Reload)
                    {
                        var customContentBefore = CustomContentSignature();
                        _session.ReloadFromDisk();
                        ReloadFields();
                        if (customContentBefore != CustomContentSignature())
                            QueueCustomContentReload();
                        AfterHistoryChange();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    _session.RecordCurrentFileState();
                }

                var bytes = _session.ToBytes();
                if (!SaveService.TryWriteAtomicIfUnchanged(_session, bytes))
                {
                    _log.AppendLine("Module Properties save stopped because module.ifo changed while saving.");
                    return false;
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(bytes);
                AfterHistoryChange();
                Saved?.Invoke();
                _log.AppendLine($"Saved {_session.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Module Properties save failed: {ex.Message}");
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            var customContentBefore = CustomContentSignature();
            _session.Undo();
            ReloadFields();
            if (customContentBefore != CustomContentSignature())
                QueueCustomContentReload();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            var customContentBefore = CustomContentSignature();
            _session.Redo();
            ReloadFields();
            if (customContentBefore != CustomContentSignature())
                QueueCustomContentReload();
            AfterHistoryChange();
        }

        private string CustomContentSignature() =>
            string.Join('\u001f', Document.HakNames) + "\0" + (Document.CustomTlk ?? string.Empty);

        private void ReloadFields()
        {
            _refreshing = true;
            try
            {
                Variables = new VarTableSectionViewModel(RunEdit, Document.VarTable, Variables.GameCodeIndex);
                OnPropertyChanged(nameof(Variables));
                foreach (var row in Events)
                    row.NotifyValueChanged();
                foreach (var property in new[]
                         {
                             nameof(Name), nameof(Tag), nameof(MinutesPerHour), nameof(DawnHour), nameof(DuskHour),
                             nameof(StartingMonth), nameof(StartingDay), nameof(StartingHour), nameof(StartingYear),
                             nameof(XpScale), nameof(StartingMovie), nameof(Description), nameof(SelectedCustomTlk)
                         })
                {
                    OnPropertyChanged(property);
                }
                RefreshCustomContentDiscovery();
            }
            finally
            {
                _refreshing = false;
            }
        }

        internal void ApproveApplicationClose() => _closeApproved = true;

        public override bool OnClose()
        {
            if (!_closeApproved && IsDirty)
            {
                if (!_closePromptOpen)
                {
                    _closePromptOpen = true;
                    _ = ConfirmCloseAsync();
                }
                return false;
            }

            if (_disposed)
                return base.OnClose();
            _disposed = true;
            _customReloadCts?.Cancel();
            _customReloadCts?.Dispose();
            _session.Dispose();
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private async Task ConfirmCloseAsync()
        {
            try
            {
                var choice = await _prompts.ConfirmCloseAsync("Module Properties").ConfigureAwait(true);
                var approved = choice == UnsavedChangesChoice.Discard ||
                               choice == UnsavedChangesChoice.Save && await TrySaveAsync().ConfigureAwait(true);
                if (!approved)
                    return;

                if (choice == UnsavedChangesChoice.Discard)
                {
                    _session.ReloadFromDisk();
                    if (_customContent != null)
                    {
                        await _customContent.ReloadAsync(
                            Document.HakNames,
                            Document.CustomTlk).ConfigureAwait(true);
                    }
                }
                _closeApproved = true;
                CloseRequested?.Invoke(this);
            }
            finally
            {
                _closePromptOpen = false;
            }
        }

        private void AfterHistoryChange()
        {
            UpdateTitle();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void UpdateTitle() => Title = IsDirty ? "Module Properties *" : "Module Properties";

        private void ReadBuildConfiguration()
        {
            if (!File.Exists(HakConfigurationPath))
                return;

            try
            {
                using var json = JsonDocument.Parse(File.ReadAllBytes(HakConfigurationPath));
                if (json.RootElement.TryGetProperty("EnableChecksumChecking", out var checksum))
                    ChecksumChecking = checksum.ValueKind == JsonValueKind.True;
                if (json.RootElement.TryGetProperty("OutputPath", out var output) &&
                    output.GetString() is { Length: > 0 } value)
                {
                    PackOutput = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(HakConfigurationPath)!, value));
                }
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read {HakConfigurationPath}: {ex.Message}");
            }
        }

        private void OpenBuildConfiguration()
        {
            if (_actions.OpenFile != null)
            {
                _actions.OpenFile(HakConfigurationPath);
                return;
            }

            Process.Start(new ProcessStartInfo(HakConfigurationPath) { UseShellExecute = true });
        }

        private async Task CheckHakConflictsAsync()
        {
            if (_customContent?.ResourceIndex == null)
            {
                CustomContentStatus = "HAK conflict checking is unavailable.";
                return;
            }

            CustomContentStatus = "Checking assigned HAKs for conflicts...";
            var conflicts = await Task.Run(_customContent.ResourceIndex.FindHakConflicts).ConfigureAwait(true);
            CustomContentStatus = conflicts.Count == 0
                ? "No duplicate resources were found."
                : $"Found {conflicts.Count} duplicate resource(s); details were written to Output.";

            _log.AppendLine($"HAK conflict check: {conflicts.Count} duplicate resource(s).");
            foreach (var conflict in conflicts)
                _log.AppendLine($"  {conflict.Resource}: {string.Join(" > ", conflict.Layers)}");
        }
    }
}
