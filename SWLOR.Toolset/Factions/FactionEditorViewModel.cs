using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Factions
{
    public sealed partial class FactionListItemViewModel : ObservableObject
    {
        public JsonGffStruct Entry { get; }
        public int Id { get; }
        public bool IsStandard { get; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private FactionReferenceUsage _usage;

        public string UsageSummary => !Usage.IsKnown
            ? string.Empty
            : Usage.Total == 0
                ? "Not used by module objects"
                : $"{Usage.Total} module reference{(Usage.Total == 1 ? string.Empty : "s")}";

        public FactionListItemViewModel(
            JsonGffStruct entry,
            int id,
            string name,
            bool isStandard,
            FactionReferenceUsage usage)
        {
            Entry = entry;
            Id = id;
            _name = name;
            IsStandard = isStandard;
            _usage = usage;
        }

        partial void OnUsageChanged(FactionReferenceUsage value) =>
            OnPropertyChanged(nameof(UsageSummary));
    }

    public sealed partial class FactionRelationshipRowViewModel : ObservableObject
    {
        private static readonly IReadOnlyList<string> Options =
            new[] { "Hostile", "Neutral", "Friendly" };

        private readonly FactionEditorViewModel _owner;
        private bool _reloading;

        public int TargetId { get; }
        public string TargetName { get; }
        public IReadOnlyList<string> AttitudeOptions => Options;

        [ObservableProperty]
        private int _value;

        [ObservableProperty]
        private int _oppositeValue;

        [ObservableProperty]
        private string _selectedAttitude;

        public string Result => FactionTable.DescribeReputation(Value);

        public string OppositeSummary =>
            $"{TargetName} → {_owner.SelectedFactionName}: " +
            $"{FactionTable.DescribeReputation(OppositeValue)} ({OppositeValue})";

        public FactionRelationshipRowViewModel(
            FactionEditorViewModel owner,
            int targetId,
            string targetName,
            int value,
            int oppositeValue)
        {
            _owner = owner;
            TargetId = targetId;
            TargetName = targetName;
            _value = value;
            _oppositeValue = oppositeValue;
            _selectedAttitude = FactionTable.DescribeReputation(value);
        }

        partial void OnValueChanged(int value)
        {
            OnPropertyChanged(nameof(Result));
            if (_reloading)
                return;

            _owner.SetRelationship(this, Math.Clamp(value, 0, 100));
        }

        partial void OnOppositeValueChanged(int value) =>
            OnPropertyChanged(nameof(OppositeSummary));

        partial void OnSelectedAttitudeChanged(string value)
        {
            if (_reloading)
                return;

            var canonical = value switch
            {
                "Hostile" => FactionTable.DefaultHostileReputation,
                "Friendly" => FactionTable.DefaultFriendlyReputation,
                _ => FactionTable.DefaultNeutralReputation
            };
            _owner.SetRelationship(this, canonical);
        }

        [RelayCommand]
        private void MatchOpposite() => _owner.SetRelationship(this, OppositeValue);

        internal void Reload(int value, int oppositeValue)
        {
            _reloading = true;
            try
            {
                Value = value;
                OppositeValue = oppositeValue;
                SelectedAttitude = FactionTable.DescribeReputation(value);
                OnPropertyChanged(nameof(Result));
                OnPropertyChanged(nameof(OppositeSummary));
            }
            finally
            {
                _reloading = false;
            }
        }

        internal void RefreshOwnerName() =>
            OnPropertyChanged(nameof(OppositeSummary));
    }

    /// <summary>The simplified, directional editor for one module's <c>repute.fac</c>.</summary>
    public sealed partial class FactionEditorViewModel : ObservableObject, IDisposable
    {
        private readonly string _moduleRoot;
        private readonly string _factionPath;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly DocumentSession _session;
        private readonly FacDocument _fac;
        private readonly FactionTable _table;
        private readonly Dictionary<JsonGffStruct, int> _baselineIds =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<int, int> _removedParentByOriginalId = new();
        private readonly HashSet<string> _changedPaths = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyDictionary<int, FactionReferenceUsage> _usageByOriginalId;
        private bool _reloading;
        private bool _disposed;

        public ObservableCollection<FactionListItemViewModel> Factions { get; } = new();
        public ObservableCollection<FactionRelationshipRowViewModel> Relationships { get; } = new();
        public ObservableCollection<FactionListItemViewModel> ParentChoices { get; } = new();

        public IReadOnlyCollection<string> ChangedPaths => _changedPaths;
        public bool IsDirty => _session.UndoStack.IsDirty;
        public bool CanSave => IsDirty && !IsSaving;
        public bool CanUndo => _session.UndoStack.CanUndo && !IsSaving;
        public bool CanRedo => _session.UndoStack.CanRedo && !IsSaving;
        public bool CanRemoveFaction => SelectedFaction is { IsStandard: false } && !IsSaving;
        public bool IsNameReadOnly => SelectedFaction?.IsStandard != false;
        public string SelectedFactionName => SelectedFaction?.Name ?? "Faction";
        public bool HasSelectedRelationship => SelectedRelationship != null;
        public bool HasUsageSummary => !string.IsNullOrWhiteSpace(UsedBy);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRemoveFaction))]
        [NotifyPropertyChangedFor(nameof(IsNameReadOnly))]
        [NotifyPropertyChangedFor(nameof(SelectedFactionName))]
        private FactionListItemViewModel? _selectedFaction;

        [ObservableProperty]
        private string _factionName = string.Empty;

        [ObservableProperty]
        private string _parentName = "None";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUsageSummary))]
        private string _usedBy = string.Empty;

        [ObservableProperty]
        private bool _globalEffect;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedRelationship))]
        private FactionRelationshipRowViewModel? _selectedRelationship;

        [ObservableProperty]
        private string _selectedRelationshipSummary = string.Empty;

        [ObservableProperty]
        private int _editorPage;

        [ObservableProperty]
        private bool _isAddingFaction;

        [ObservableProperty]
        private string _newFactionName = string.Empty;

        [ObservableProperty]
        private FactionListItemViewModel? _selectedNewFactionParent;

        [ObservableProperty]
        private bool _isConfirmingRemove;

        [ObservableProperty]
        private string _removeHeadline = string.Empty;

        [ObservableProperty]
        private string _removeSummary = string.Empty;

        [ObservableProperty]
        private string _removeDestination = string.Empty;

        [ObservableProperty]
        private string _statusText = "Choose a faction to edit its starting attitudes.";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanUndo))]
        [NotifyPropertyChangedFor(nameof(CanRedo))]
        [NotifyPropertyChangedFor(nameof(CanRemoveFaction))]
        private bool _isSaving;

        public FactionEditorViewModel(
            string moduleRoot,
            IReadOnlyDictionary<int, FactionReferenceUsage> usage,
            OutputLogService log,
            IEditorPromptService prompts)
        {
            _moduleRoot = moduleRoot ?? throw new ArgumentNullException(nameof(moduleRoot));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _factionPath = Path.Combine(moduleRoot, "fac", "repute.fac.json");
            _session = DocumentSession.Open(_factionPath);
            _fac = new FacDocument(_session.Document);
            _table = new FactionTable(_fac);
            _usageByOriginalId = usage;
            CaptureBaseline();
            Rebuild(selectEntry: _fac.FactionList.FirstOrDefault());
        }

        partial void OnSelectedFactionChanged(FactionListItemViewModel? value)
        {
            if (_reloading || value == null)
                return;
            LoadSelectedFaction();
            RequestRemoveFactionCommand.NotifyCanExecuteChanged();
        }

        partial void OnFactionNameChanged(string value)
        {
            if (_reloading || SelectedFaction == null || IsNameReadOnly)
                return;

            if (!RunEdit($"Rename faction to {value}", () => _table.SetName(SelectedFaction.Id, value)))
            {
                _reloading = true;
                FactionName = SelectedFaction.Name;
                _reloading = false;
                return;
            }

            SelectedFaction.Name = value.Trim();
            OnPropertyChanged(nameof(SelectedFactionName));
            foreach (var relationship in Relationships)
                relationship.RefreshOwnerName();
            if (SelectedRelationship != null)
                SelectedRelationshipSummary = RelationshipSummary(SelectedRelationship);
        }

        partial void OnGlobalEffectChanged(bool value)
        {
            if (_reloading || SelectedFaction == null)
                return;

            RunEdit(
                $"Set {SelectedFaction.Name} global effect",
                () => _table.SetGlobalEffect(SelectedFaction.Id, value));
        }

        partial void OnSelectedRelationshipChanged(FactionRelationshipRowViewModel? value)
        {
            SelectedRelationshipSummary = value == null
                ? string.Empty
                : RelationshipSummary(value);
        }

        [RelayCommand]
        private void BeginAddFaction()
        {
            IsConfirmingRemove = false;
            IsAddingFaction = true;
            NewFactionName = string.Empty;
            SelectedNewFactionParent = ParentChoices.FirstOrDefault(parent => parent.Id == 2)
                                       ?? ParentChoices.FirstOrDefault();
            StatusText = "Choose a standard parent. The new faction starts with that parent's attitudes.";
        }

        [RelayCommand]
        private void CancelAddFaction()
        {
            IsAddingFaction = false;
            StatusText = "Faction creation cancelled.";
        }

        [RelayCommand]
        private void CreateFaction()
        {
            if (SelectedNewFactionParent == null)
            {
                StatusText = "Choose Hostile, Commoner, Merchant, or Defender as the parent.";
                return;
            }

            JsonGffStruct? newEntry = null;
            if (!RunEdit(
                    $"Add faction {NewFactionName}",
                    () =>
                    {
                        var id = _table.AddFaction(NewFactionName, SelectedNewFactionParent.Id);
                        newEntry = _table.EntryAt(id);
                    }))
            {
                return;
            }

            IsAddingFaction = false;
            Rebuild(newEntry);
            StatusText = $"Added {SelectedFactionName} from {ParentName}. Adjust its attitudes, then save.";
        }

        [RelayCommand(CanExecute = nameof(CanRemoveFaction))]
        private void RequestRemoveFaction()
        {
            if (SelectedFaction == null || SelectedFaction.IsStandard)
                return;

            IsAddingFaction = false;
            IsConfirmingRemove = true;
            var parentId = _table.Factions[SelectedFaction.Id].ParentId
                           ?? throw new InvalidOperationException("The selected custom faction has no parent.");
            var parent = _table.Factions[parentId];
            var usage = SelectedFaction.Usage;
            RemoveHeadline = $"Remove {SelectedFaction.Name}?";
            RemoveDestination = parent.Name;
            RemoveSummary = !usage.IsKnown
                ? $"Every blueprint and placed object using this faction will move to {parent.Name}. " +
                  "Larger faction IDs will be compacted safely."
                : usage.Total == 0
                    ? "Nothing currently uses this faction. Its relationships will be removed."
                    : $"{usage.BlueprintCount} blueprint{(usage.BlueprintCount == 1 ? string.Empty : "s")} and " +
                      $"{usage.PlacedObjectCount} placed object{(usage.PlacedObjectCount == 1 ? string.Empty : "s")} " +
                      $"will move to {parent.Name}. Larger faction IDs will be compacted safely.";
        }

        [RelayCommand]
        private void CancelRemoveFaction()
        {
            IsConfirmingRemove = false;
            StatusText = "Faction removal cancelled.";
        }

        [RelayCommand]
        private void ConfirmRemoveFaction()
        {
            if (SelectedFaction == null || SelectedFaction.IsStandard)
                return;

            var removed = SelectedFaction;
            var selectedId = removed.Id;
            var originalId = _baselineIds.TryGetValue(removed.Entry, out var baselineId)
                ? baselineId
                : (int?)null;
            var parentCurrentId = _table.Factions[selectedId].ParentId
                                  ?? throw new InvalidOperationException("The selected custom faction has no parent.");
            var parentEntry = _table.EntryAt(parentCurrentId);
            var parentOriginalId = _baselineIds.TryGetValue(parentEntry, out var baselineParentId)
                ? baselineParentId
                : parentCurrentId;

            if (!RunEdit(
                    $"Remove faction {removed.Name}",
                    () => _table.RemoveFaction(selectedId)))
            {
                return;
            }

            if (originalId.HasValue)
                _removedParentByOriginalId[originalId.Value] = parentOriginalId;

            IsConfirmingRemove = false;
            var next = _fac.FactionList[Math.Min(selectedId, _fac.FactionList.Count - 1)];
            Rebuild(next);
            StatusText = $"Removed {removed.Name}. Its module references will move to {RemoveDestination} when you save.";
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            var selected = SelectedFaction?.Entry;
            _session.Undo();
            Rebuild(ResolveSelection(selected));
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            var selected = SelectedFaction?.Entry;
            _session.Redo();
            Rebuild(ResolveSelection(selected));
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Revert()
        {
            _session.RevertToSaved();
            _removedParentByOriginalId.Clear();
            CaptureBaseline();
            Rebuild(_fac.FactionList.FirstOrDefault());
            AfterHistoryChange();
            StatusText = "Reverted to the last saved faction settings.";
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty)
                return true;
            if (IsSaving)
                return false;

            IsSaving = true;
            NotifyCommandsChanged();
            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts
                        .ConfirmExternalChangeAsync(_factionPath)
                        .ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;
                    if (choice == ExternalChangeChoice.Reload)
                    {
                        _session.ReloadFromDisk();
                        _removedParentByOriginalId.Clear();
                        _usageByOriginalId = UnknownUsage(_table.Count);
                        CaptureBaseline();
                        Rebuild(_fac.FactionList.FirstOrDefault());
                        AfterHistoryChange();
                        StatusText = "Reloaded the externally changed faction table.";
                        return true;
                    }

                    _session.RecordCurrentFileState();
                }

                var idMap = BuildOriginalIdMap();
                var facBytes = _session.ToBytes();
                StatusText = idMap.Any(pair => pair.Key != pair.Value)
                    ? "Saving factions and updating module references..."
                    : "Saving faction settings...";

                var changed = await Task.Run(() => SaveCore(facBytes, idMap)).ConfigureAwait(true);
                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(facBytes);
                foreach (var path in changed)
                    _changedPaths.Add(path);

                _usageByOriginalId = UnknownUsage(_table.Count);
                _removedParentByOriginalId.Clear();
                CaptureBaseline();
                Rebuild(ResolveSelection(SelectedFaction?.Entry));
                AfterHistoryChange();
                StatusText = changed.Count == 1
                    ? "Saved faction settings."
                    : $"Saved faction settings and updated {changed.Count - 1} referenced resource" +
                      $"{(changed.Count - 1 == 1 ? string.Empty : "s")}.";
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Faction save failed: {ex.Message}");
                StatusText = "Faction save failed. Nothing was partially written; see Output.";
                return false;
            }
            finally
            {
                IsSaving = false;
                NotifyCommandsChanged();
            }
        }

        public async Task<bool> TryCloseAsync()
        {
            if (!IsDirty)
                return true;

            var choice = await _prompts.ConfirmCloseAsync("Factions").ConfigureAwait(true);
            if (choice == UnsavedChangesChoice.Save)
                return await TrySaveAsync().ConfigureAwait(true);
            return choice == UnsavedChangesChoice.Discard;
        }

        internal void SetRelationship(FactionRelationshipRowViewModel row, int value)
        {
            if (SelectedFaction == null)
                return;

            value = Math.Clamp(value, 0, 100);
            if (_table.GetReputation(SelectedFaction.Id, row.TargetId) == value)
            {
                row.Reload(value, row.OppositeValue);
                SelectedRelationship = row;
                return;
            }

            if (!RunEdit(
                    $"Set {SelectedFaction.Name} attitude toward {row.TargetName}",
                    () => _table.SetReputation(SelectedFaction.Id, row.TargetId, value)))
            {
                row.Reload(
                    _table.GetReputation(SelectedFaction.Id, row.TargetId),
                    _table.GetReputation(row.TargetId, SelectedFaction.Id));
                return;
            }

            row.Reload(value, _table.GetReputation(row.TargetId, SelectedFaction.Id));
            SelectedRelationship = row;
            SelectedRelationshipSummary = RelationshipSummary(row);
        }

        private bool RunEdit(string description, Action mutation)
        {
            try
            {
                _session.Execute(description, mutation);
                AfterHistoryChange();
                StatusText = description + ".";
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Faction edit failed ({description}): {ex.Message}");
                StatusText = ex.Message;
                return false;
            }
        }

        private void Rebuild(JsonGffStruct? selectEntry)
        {
            _reloading = true;
            try
            {
                Factions.Clear();
                ParentChoices.Clear();
                var definitions = _table.Factions;
                for (var id = 0; id < definitions.Count; id++)
                {
                    var definition = definitions[id];
                    var entry = _table.EntryAt(id);
                    var item = new FactionListItemViewModel(
                        entry,
                        id,
                        definition.Name,
                        definition.IsStandard,
                        UsageFor(entry));
                    Factions.Add(item);
                    if (id is >= 1 and < FactionTable.StandardFactionCount)
                        ParentChoices.Add(item);
                }

                SelectedFaction = Factions.FirstOrDefault(item => ReferenceEquals(item.Entry, selectEntry))
                                  ?? Factions.FirstOrDefault();
            }
            finally
            {
                _reloading = false;
            }

            LoadSelectedFaction();
            NotifyCommandsChanged();
        }

        private void LoadSelectedFaction()
        {
            if (SelectedFaction == null)
                return;

            _reloading = true;
            try
            {
                var definition = _table.Factions[SelectedFaction.Id];
                FactionName = definition.Name;
                ParentName = definition.ParentId.HasValue
                    ? _table.Factions[definition.ParentId.Value].Name
                    : "None (standard faction)";
                UsedBy = SelectedFaction.Usage.IsKnown
                    ? $"{SelectedFaction.Usage.BlueprintCount} blueprints · " +
                      $"{SelectedFaction.Usage.PlacedObjectCount} placed objects"
                    : string.Empty;
                GlobalEffect = definition.GlobalEffect;

                Relationships.Clear();
                foreach (var target in _table.Factions.Where(faction => faction.Id != SelectedFaction.Id))
                {
                    Relationships.Add(new FactionRelationshipRowViewModel(
                        this,
                        target.Id,
                        target.Name,
                        _table.GetReputation(SelectedFaction.Id, target.Id),
                        _table.GetReputation(target.Id, SelectedFaction.Id)));
                }

                SelectedRelationship = Relationships.FirstOrDefault();
            }
            finally
            {
                _reloading = false;
            }

            OnPropertyChanged(nameof(SelectedFactionName));
            OnPropertyChanged(nameof(IsNameReadOnly));
            OnPropertyChanged(nameof(CanRemoveFaction));
            RequestRemoveFactionCommand.NotifyCanExecuteChanged();
        }

        private FactionReferenceUsage UsageFor(JsonGffStruct entry)
        {
            if (_baselineIds.TryGetValue(entry, out var originalId) &&
                _usageByOriginalId.TryGetValue(originalId, out var usage))
            {
                return usage;
            }

            return FactionReferenceUsage.Unknown;
        }

        private static IReadOnlyDictionary<int, FactionReferenceUsage> UnknownUsage(int factionCount) =>
            Enumerable.Range(0, factionCount).ToDictionary(
                id => id,
                _ => FactionReferenceUsage.Unknown);

        private Dictionary<int, int> BuildOriginalIdMap()
        {
            var current = _fac.FactionList;
            var map = new Dictionary<int, int>();
            foreach (var (entry, originalId) in _baselineIds)
            {
                var currentId = IndexOf(current, entry);
                if (currentId >= 0)
                {
                    map[originalId] = currentId;
                    continue;
                }

                if (!_removedParentByOriginalId.TryGetValue(originalId, out var parentOriginalId))
                {
                    throw new InvalidOperationException(
                        $"Removed faction {originalId} has no parent remapping plan.");
                }

                var parentEntry = _baselineIds.First(pair => pair.Value == parentOriginalId).Key;
                var parentCurrentId = IndexOf(current, parentEntry);
                if (parentCurrentId < 0)
                    throw new InvalidOperationException("A removed faction's parent is no longer available.");
                map[originalId] = parentCurrentId;
            }

            return map;
        }

        private IReadOnlyList<string> SaveCore(
            byte[] facBytes,
            IReadOnlyDictionary<int, int> idMap)
        {
            using var allowance = ModuleMutationLock.AllowModuleWrites();
            using var moduleWriteLock = ModuleWriteLock.Acquire(_moduleRoot);

            // The prompt above establishes which external generation the builder accepted, but
            // another process can save while this operation is waiting for the cross-process
            // module lease. Recheck under that lease before any stale bytes are staged.
            if (_session.HasExternalChange())
            {
                throw new IOException(
                    $"{_factionPath} changed while the faction save was waiting to write. Nothing was written.");
            }

            var rewrites = idMap.Any(pair => pair.Key != pair.Value)
                ? FactionReferenceRewriter.BuildRewrites(_moduleRoot, idMap)
                : Array.Empty<FactionReferenceRewrite>();
            var staged = new List<SaveService.StagedWrite>();
            try
            {
                staged.Add(SaveService.Stage(_factionPath, facBytes));
                foreach (var rewrite in rewrites)
                    staged.Add(SaveService.Stage(rewrite.Path, rewrite.Bytes));

                var externallyChanged = rewrites.FirstOrDefault(
                    rewrite => !rewrite.SourceMatchesCurrentFile());
                if (externallyChanged != null)
                {
                    throw new IOException(
                        $"{externallyChanged.Path} changed while faction references were being prepared. " +
                        "Nothing was written.");
                }

                SaveService.CommitAll(staged);
            }
            catch
            {
                foreach (var write in staged)
                    SaveService.Discard(write);
                throw;
            }

            _log.AppendLine($"Saved {_factionPath}.");
            foreach (var rewrite in rewrites)
            {
                _log.AppendLine(
                    $"Updated {rewrite.ChangedReferences} faction reference" +
                    $"{(rewrite.ChangedReferences == 1 ? string.Empty : "s")} in {rewrite.Path}.");
            }

            return new[] { _factionPath }
                .Concat(rewrites.Select(rewrite => rewrite.Path))
                .ToList();
        }

        private string RelationshipSummary(FactionRelationshipRowViewModel row)
        {
            var behavior = row.Result switch
            {
                "Hostile" => "starts hostile toward",
                "Friendly" => "starts friendly toward",
                _ => "starts neutral toward"
            };
            return $"{SelectedFactionName} {behavior} {row.TargetName}. " +
                   $"This is one-sided: {row.OppositeSummary}.";
        }

        private void CaptureBaseline()
        {
            _baselineIds.Clear();
            for (var id = 0; id < _fac.FactionList.Count; id++)
                _baselineIds[_fac.FactionList[id]] = id;
        }

        private JsonGffStruct? ResolveSelection(JsonGffStruct? preferred)
        {
            if (preferred != null && IndexOf(_fac.FactionList, preferred) >= 0)
                return preferred;
            return _fac.FactionList.FirstOrDefault();
        }

        private static int IndexOf(IReadOnlyList<JsonGffStruct> entries, JsonGffStruct entry)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (ReferenceEquals(entries[index], entry))
                    return index;
            }
            return -1;
        }

        private void AfterHistoryChange()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            SaveCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        private void NotifyCommandsChanged()
        {
            SaveCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            RequestRemoveFactionCommand.NotifyCanExecuteChanged();
            BeginAddFactionCommand.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _session.Dispose();
        }
    }
}
