using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Tlk;

/// <summary>One lazily-created row in the virtual range displayed by the TLK grid.</summary>
public sealed class TlkEditorRowViewModel : ObservableObject
{
    private readonly TlkEditorDocumentViewModel _owner;

    internal TlkEditorRowViewModel(TlkEditorDocumentViewModel owner, int id)
    {
        _owner = owner;
        Id = id;
    }

    public int Id { get; }
    public uint StrRef => checked(TlkService.CustomTlkBase + (uint)Id);
    public string Text => _owner.GetText(Id) ?? string.Empty;
    public string Preview => Text.Replace('\r', ' ').Replace('\n', ' ');
    public int Length => Text.Length;
    public int UsageCount => _owner.UsageCountFor(Id);
    public bool IsBlank => !_owner.ContainsEntry(Id);
    public string State => IsBlank ? (UsageCount > 0 ? "Reserved blank" : "Blank") : "Text";

    internal void Refresh()
    {
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(UsageCount));
        OnPropertyChanged(nameof(IsBlank));
        OnPropertyChanged(nameof(State));
    }
}

/// <summary>
/// Read-only indexed collection whose row objects are allocated only as the virtualizing grid asks
/// for them. A weak cache keeps visible rows stable without turning a 193,000-row range into
/// 193,000 persistent view models.
/// </summary>
public sealed class TlkVirtualRowCollection : IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    private const int RowCachePruneThreshold = 512;
    private readonly TlkEditorDocumentViewModel _owner;
    private readonly Dictionary<int, WeakReference<TlkEditorRowViewModel>> _rows = new();
    private int[]? _filteredIds;
    private int _rangeMaximum;
    private int _rowRequests;

    internal TlkVirtualRowCollection(TlkEditorDocumentViewModel owner)
    {
        _owner = owner;
        _rangeMaximum = Math.Max(0, owner.MaxEntryId);
    }

    public int Count => _filteredIds?.Length ?? checked(_rangeMaximum + 1);
    public bool IsReadOnly => true;
    public bool IsFixedSize => true;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    public object? this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            var id = _filteredIds?[index] ?? index;
            return RowForId(id);
        }
        set => throw new NotSupportedException();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Number of row view models currently retained by the weak cache.</summary>
    public int CreatedRowCount => _rows.Count;

    internal TlkEditorRowViewModel RowForId(int id)
    {
        if (_rows.TryGetValue(id, out var weak))
        {
            if (weak.TryGetTarget(out var cachedRow))
                return cachedRow;
            _rows.Remove(id);
        }

        if (_rows.Count >= RowCachePruneThreshold && ++_rowRequests % 64 == 0)
            PruneCollectedRows();

        var row = new TlkEditorRowViewModel(_owner, id);
        _rows[id] = new WeakReference<TlkEditorRowViewModel>(row);
        return row;
    }

    private void PruneCollectedRows()
    {
        foreach (var id in _rows.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToArray())
            _rows.Remove(id);
    }

    internal bool ContainsId(int id) =>
        _filteredIds == null ? id >= 0 && id <= _rangeMaximum : Array.BinarySearch(_filteredIds, id) >= 0;

    internal int IndexOfId(int id)
    {
        if (_filteredIds == null)
            return id >= 0 && id <= _rangeMaximum ? id : -1;
        var index = Array.BinarySearch(_filteredIds, id);
        return index < 0 ? -1 : index;
    }

    internal void ResetRange(int maximum, int[]? filteredIds)
    {
        _rangeMaximum = Math.Max(0, maximum);
        _filteredIds = filteredIds;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    internal void RefreshRows(IEnumerable<int> ids)
    {
        foreach (var id in ids.Distinct())
        {
            if (_rows.TryGetValue(id, out var weak) && weak.TryGetTarget(out var row))
                row.Refresh();
        }
    }

    internal void RefreshCachedRows()
    {
        foreach (var weak in _rows.Values)
        {
            if (weak.TryGetTarget(out var row))
                row.Refresh();
        }
    }

    public IEnumerator GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
            yield return this[index];
    }

    public int IndexOf(object? value) => value is TlkEditorRowViewModel row ? IndexOfId(row.Id) : -1;
    public bool Contains(object? value) => IndexOf(value) >= 0;
    public void CopyTo(Array array, int index)
    {
        for (var source = 0; source < Count; source++)
            array.SetValue(this[source], index + source);
    }

    public int Add(object? value) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public void Insert(int index, object? value) => throw new NotSupportedException();
    public void Remove(object? value) => throw new NotSupportedException();
    public void RemoveAt(int index) => throw new NotSupportedException();
}

/// <summary>A single referenced location displayed below the selected TLK row.</summary>
public sealed record TlkUsageRowViewModel(
    string Source,
    string File,
    int Row,
    string RowLabel,
    string Column,
    uint StrRef);

/// <summary>Undoable, singleton SWLOR custom TLK document.</summary>
public partial class TlkEditorDocumentViewModel : Document, IEditorDocument
{
    private const string ClipboardRowPrefix = "SWLOR-TLK-V1:";
    private readonly ITlkEditorBackend _backend;
    private readonly OutputLogService _log;
    private readonly IEditorPromptService _prompts;
    private readonly Action? _afterSave;
    private readonly List<TlkHistoryEntry> _history = new();
    private readonly HashSet<int> _savedEntryIds = new();
    private readonly HashSet<int> _confirmedClears = new();
    private readonly HashSet<int> _confirmedReferencedWrites = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _busyOperationSync = new();
    private int _historyPosition;
    private int _savedPosition;
    private int _busyOperationCount;
    private TaskCompletionSource? _idleOperations;
    private bool _refreshingSelection;
    private bool _closeApproved;
    private bool _closePromptOpen;
    private bool _disposed;
    private Task<bool>? _activeSave;

    public TlkVirtualRowCollection Rows { get; }
    public ObservableCollection<TlkUsageRowViewModel> Usages { get; } = new();

    [ObservableProperty] private TlkEditorRowViewModel? _selectedRow;
    [ObservableProperty] private string _selectedText = string.Empty;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private string _goToValue = string.Empty;
    [ObservableProperty] private string _navigationStatus = string.Empty;
    [ObservableProperty] private string _referenceStatus = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public int EntryCount => _backend.Count;
    public int MaxEntryId => _backend.MaxEntryId;
    public int VisibleRowCount => Rows.Count;
    public int SelectedId => SelectedRow?.Id ?? 0;
    public uint SelectedStrRef => checked(TlkService.CustomTlkBase + (uint)SelectedId);
    public string SelectedIdDisplay => SelectedId.ToString();
    public string SelectedStrRefDisplay => SelectedStrRef.ToString();
    public bool HasSelectedRow => SelectedRow != null;
    public bool HasUsages => Usages.Count > 0;
    public bool IsDirty => _historyPosition != _savedPosition;
    public bool CanUndo => _historyPosition > 0;
    public bool CanRedo => _historyPosition < _history.Count;
    public string JsonPath => _backend.JsonPath;
    public string BinaryPath => _backend.BinaryPath;

    public event Action<TlkEditorDocumentViewModel>? Closed;
    public event Action<TlkEditorDocumentViewModel>? CloseRequested;
    public event Action<int>? SelectionNavigationRequested;

    public TlkEditorDocumentViewModel(
        ITlkEditorBackend backend,
        OutputLogService log,
        IEditorPromptService prompts,
        Action? afterSave = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _afterSave = afterSave;
        CaptureSavedEntryIds();
        Id = $"tlk-editor:{backend.JsonPath}";
        Rows = new TlkVirtualRowCollection(this);
        RefreshReferenceStatus();
        UpdateTitleAndState();
        SelectId(0, clearFilter: false);
    }

    internal bool ContainsEntry(int id) => _backend.ContainsEntry(id);
    internal string? GetText(int id) => _backend.GetText(id);
    internal int UsageCountFor(int id) => _backend.UsageCountFor(id);

    internal void ReportClipboardFailure(string operation, Exception exception) =>
        _log.AppendLine($"Could not {operation}: {exception.GetBaseException().Message}");

    partial void OnSelectedRowChanged(TlkEditorRowViewModel? value)
    {
        _refreshingSelection = true;
        try
        {
            SelectedText = value == null ? string.Empty : _backend.GetText(value.Id) ?? string.Empty;
            Usages.Clear();
            if (value != null)
            {
                foreach (var usage in _backend.UsagesOf(value.Id)
                             .OrderBy(usage => usage.ColumnName == TlkReferenceIndex.RepositoryTextColumnName ? 1 : 0)
                             .ThenBy(usage => usage.FileName, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(usage => usage.RowIndex))
                {
                    Usages.Add(new TlkUsageRowViewModel(
                        usage.ColumnName == TlkReferenceIndex.RepositoryTextColumnName
                            ? "Repository"
                            : "2DA",
                        usage.FileName,
                        usage.RowIndex,
                        usage.RowLabel ?? string.Empty,
                        usage.ColumnName,
                        usage.StrRef));
                }
            }
        }
        finally
        {
            _refreshingSelection = false;
        }

        NotifySelectionState();
    }

    partial void OnSelectedTextChanged(string value)
    {
        if (_refreshingSelection || SelectedRow == null)
            return;

        var id = SelectedRow.Id;
        if (_backend.ContainsEntry(id) && value.Length == 0)
        {
            _refreshingSelection = true;
            SelectedText = _backend.GetText(id) ?? string.Empty;
            _refreshingSelection = false;
            NavigationStatus = $"Use Clear row to blank TLK row {id}.";
            return;
        }
        if (!_backend.ContainsEntry(id) && value.Length > 0 && _backend.IsReferenced(id))
        {
            _refreshingSelection = true;
            SelectedText = string.Empty;
            _refreshingSelection = false;
            NavigationStatus =
                $"Blank row {id} is referenced. Use grid paste (Ctrl+V) so the change can be confirmed.";
            return;
        }
        if (value.Length > 0 && id > _backend.MaxEntryId &&
            !CanCreateThrough(id, new Dictionary<int, string> { [id] = value }))
        {
            _refreshingSelection = true;
            SelectedText = _backend.GetText(id) ?? string.Empty;
            _refreshingSelection = false;
            return;
        }

        ApplyChanges(
            $"Edit TLK row {id}",
            new[] { TlkValueChange.Set(id, _backend.ContainsEntry(id), _backend.GetText(id), value) });
    }

    partial void OnFilterTextChanged(string value) => RefreshFilter();

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    [RelayCommand]
    private void GoToRow()
    {
        if (!TryParseRow(GoToValue, out var id))
        {
            NavigationStatus = "Enter a raw row ID or a full custom StrRef.";
            return;
        }
        SelectId(id, clearFilter: true);
    }

    [RelayCommand]
    private Task FindFirstBlank() => RunBusyOperationAsync(FindFirstBlankCoreAsync);

    private async Task FindFirstBlankCoreAsync()
    {
        if (!await TryRefreshReferencesAsync().ConfigureAwait(true))
            return;
        if (!CanAllocateBlank())
            return;
        SelectId(_backend.FindFirstAvailableBlank(), clearFilter: true);
    }

    [RelayCommand]
    private Task FindNextBlank() => RunBusyOperationAsync(FindNextBlankCoreAsync);

    private async Task FindNextBlankCoreAsync()
    {
        var start = SelectedId;
        if (!await TryRefreshReferencesAsync().ConfigureAwait(true))
            return;
        if (!CanAllocateBlank())
            return;
        SelectId(_backend.FindNextAvailableBlank(start), clearFilter: true);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private Task ClearRow() => RunBusyOperationAsync(ClearRowCoreAsync);

    private async Task ClearRowCoreAsync()
    {
        var id = SelectedRow?.Id;
        if (!id.HasValue || !_backend.ContainsEntry(id.Value))
            return;
        if (!await TryRefreshReferencesAsync().ConfigureAwait(true))
            return;

        var clearConfirmed = false;
        if (_backend.IsReferenced(id.Value) || _backend.ReferenceWarnings.Count > 0)
        {
            var usages = FormatUsages(_backend.UsagesOf(id.Value));
            var incomplete = _backend.ReferenceWarnings.Count == 0
                ? string.Empty
                : $"\n\nWarning: {_backend.ReferenceWarnings.Count} reference file(s) could not be scanned, " +
                  "so additional references may exist.";
            var approved = await _prompts.ConfirmDestructiveAsync(
                $"Clear possibly referenced TLK row {id}?",
                $"This leaves row {id} blank." +
                (usages.Length == 0 ? string.Empty : $" Known references:\n\n{usages}") +
                incomplete,
                "Clear row anyway").ConfigureAwait(true);
            if (!approved)
                return;
            clearConfirmed = true;
        }

        ApplyChanges(
            $"Clear TLK row {id}",
            new[] { TlkValueChange.Clear(id.Value, _backend.GetText(id.Value)) });
        if (clearConfirmed)
            _confirmedClears.Add(id.Value);
    }

    /// <summary>Returns one text line per selected grid row, in row order.</summary>
    public string CopyRows(IEnumerable<int> rowIds) => string.Join(
        Environment.NewLine,
        rowIds.Distinct().Order().Select(id => EncodeClipboardRow(_backend.GetText(id) ?? string.Empty)));

    /// <summary>
    /// Applies a grid paste as one undo step. Newlines divide consecutive rows; the selected-row
    /// text box uses the platform's normal paste instead and therefore preserves those newlines.
    /// </summary>
    public Task<bool> PasteRowsAsync(string clipboardText) =>
        RunBusyOperationAsync(() => PasteRowsCoreAsync(clipboardText));

    private async Task<bool> PasteRowsCoreAsync(string clipboardText)
    {
        if (SelectedRow == null || clipboardText == null)
            return false;

        var normalizedText = clipboardText.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var lines = normalizedText
            .Split('\n')
            .ToList();
        if (lines.Count > 1 && lines[^1].Length == 0 && normalizedText.EndsWith('\n'))
            lines.RemoveAt(lines.Count - 1);
        if (lines.Count == 0)
            return false;

        // Grid-to-grid copy encodes only rows that need escaping, so an entry which itself contains
        // newlines round-trips without becoming several rows while ordinary copied rows remain
        // ordinary readable clipboard text.
        if (lines.Any(line => line.StartsWith(ClipboardRowPrefix, StringComparison.Ordinal)))
        {
            try
            {
                lines = lines.Select(line => line.StartsWith(ClipboardRowPrefix, StringComparison.Ordinal)
                    ? Encoding.UTF8.GetString(Convert.FromBase64String(line[ClipboardRowPrefix.Length..]))
                    : line).ToList();
            }
            catch (FormatException)
            {
                NavigationStatus = "Copied TLK row data is malformed and was not pasted.";
                return false;
            }
        }

        var start = SelectedRow.Id;
        if ((long)start + lines.Count - 1 > TlkFormatLimits.MaximumEntryId)
        {
            NavigationStatus = "Paste would exceed the valid custom TLK row range.";
            return false;
        }
        if (!await TryRefreshReferencesAsync().ConfigureAwait(true))
            return false;

        var plannedText = lines
            .Select((text, offset) => new KeyValuePair<int, string>(start + offset, text))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var highestNewId = plannedText
            .Where(pair => pair.Key > _backend.MaxEntryId && pair.Value.Length > 0)
            .Select(pair => (int?)pair.Key)
            .Max();
        if (highestNewId.HasValue && !CanCreateThrough(highestNewId.Value, plannedText))
            return false;

        var changes = new List<TlkValueChange>(lines.Count);
        var overwrites = new List<int>();
        var referenced = new List<int>();
        var cleared = new List<int>();
        var filledBlanks = new List<int>();
        for (var offset = 0; offset < lines.Count; offset++)
        {
            var id = start + offset;
            if (_backend.ContainsEntry(id))
            {
                overwrites.Add(id);
                if (lines[offset].Length == 0)
                    cleared.Add(id);
            }
            else if (lines[offset].Length > 0)
                filledBlanks.Add(id);
            if (_backend.IsReferenced(id))
                referenced.Add(id);
            changes.Add(TlkValueChange.Set(
                id, _backend.ContainsEntry(id), _backend.GetText(id), lines[offset]));
        }

        if (overwrites.Count > 0 || referenced.Count > 0 || _backend.ReferenceWarnings.Count > 0)
        {
            var preview = BuildPastePreview(start, lines, overwrites, referenced) +
                          (_backend.ReferenceWarnings.Count == 0
                              ? string.Empty
                              : $"\n\nReference coverage is incomplete: {_backend.ReferenceWarnings.Count} reference file(s) could not be scanned.");
            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Paste {lines.Count} TLK rows?",
                preview,
                "Paste rows").ConfigureAwait(true);
            if (!confirmed)
                return false;
            foreach (var id in cleared.Where(id => _backend.IsReferenced(id) || _backend.ReferenceWarnings.Count > 0))
                _confirmedClears.Add(id);
            foreach (var id in filledBlanks.Where(id => _backend.IsReferenced(id) || _backend.ReferenceWarnings.Count > 0))
                _confirmedReferencedWrites.Add(id);
        }

        ApplyChanges($"Paste {lines.Count} TLK rows at {start}", changes);
        EnsureRangeIncludes(start + lines.Count - 1);
        SelectId(start, clearFilter: true);
        NavigationStatus = $"Pasted {lines.Count} row(s), {start}-{start + lines.Count - 1}.";
        return true;
    }

    public void SelectStrRef(uint strRef)
    {
        if (strRef < TlkService.CustomTlkBase)
        {
            NavigationStatus = $"StrRef {strRef} belongs to the base-game TLK and cannot be edited here.";
            return;
        }
        var rawId = strRef - TlkService.CustomTlkBase;
        if (rawId > TlkFormatLimits.MaximumEntryId)
        {
            NavigationStatus = $"StrRef {strRef} is outside the supported SWLOR TLK range.";
            return;
        }
        SelectId((int)rawId, clearFilter: true);
    }

    public void SelectId(int id, bool clearFilter = true)
    {
        if (id < 0 || id > TlkFormatLimits.MaximumEntryId)
        {
            NavigationStatus = $"TLK row {id} is outside the valid custom StrRef range.";
            return;
        }

        if (clearFilter && !string.IsNullOrEmpty(FilterText))
            FilterText = string.Empty;
        EnsureRangeIncludes(id);
        var row = Rows.RowForId(id);
        if (ReferenceEquals(SelectedRow, row))
            OnSelectedRowChanged(row);
        else
            SelectedRow = row;
        GoToValue = id.ToString();
        NavigationStatus = $"Row {id} · StrRef {TlkService.CustomTlkBase + (uint)id}";
        SelectionNavigationRequested?.Invoke(id);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo()
    {
        if (!CanUndo)
            return;
        var entry = _history[--_historyPosition];
        ApplyHistory(entry.Changes, useNewValue: false);
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo()
    {
        if (!CanRedo)
            return;
        var entry = _history[_historyPosition++];
        ApplyHistory(entry.Changes, useNewValue: true);
    }

    public async Task<bool> TrySaveAsync()
    {
        if (_activeSave != null)
            return await _activeSave.ConfigureAwait(true);

        var operation = TrySaveCoreAsync();
        _activeSave = operation;
        try
        {
            return await operation.ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(_activeSave, operation))
                _activeSave = null;
        }
    }

    private Task<bool> TrySaveCoreAsync() => RunBusyOperationAsync(SaveCoreAsync);

    private async Task<bool> SaveCoreAsync()
    {
        try
        {
            if (!await TryRefreshReferencesAsync().ConfigureAwait(true))
                return false;
            if (!RevalidateAppendedEntries())
                return false;
            if (!await ConfirmNewlyReferencedClearsAsync().ConfigureAwait(true))
                return false;
            if (!await ConfirmNewlyReferencedWritesAsync().ConfigureAwait(true))
                return false;

            var overwrite = false;
            if (await Task.Run(_backend.HasExternalChange).ConfigureAwait(true))
            {
                var choice = await _prompts.ConfirmExternalChangeAsync(_backend.JsonPath).ConfigureAwait(true);
                if (choice == ExternalChangeChoice.Cancel)
                    return false;
                if (choice == ExternalChangeChoice.Reload)
                {
                    await Task.Run(_backend.Reload).ConfigureAwait(true);
                    ApplyReloadedState();
                    PublishAndRefreshLabels("reload");
                    _log.AppendLine($"Reloaded externally changed TLK {_backend.JsonPath}.");
                    return true;
                }
                overwrite = true;
            }

            await Task.Run(() => _backend.Save(overwrite)).ConfigureAwait(true);
            _savedPosition = _historyPosition;
            CaptureSavedEntryIds();
            UpdateTitleAndState();
            PublishAndRefreshLabels("save");
            _log.AppendLine($"Saved {_backend.JsonPath} and generated {_backend.BinaryPath}.");
            return true;
        }
        catch (TlkExternalChangeException)
        {
            _log.AppendLine("TLK save stopped because the source changed while saving.");
            return false;
        }
        catch (Exception ex)
        {
            _log.AppendLine($"TLK save failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    internal void ApproveApplicationClose() => _closeApproved = true;

    internal async Task WaitForActiveOperationAsync()
    {
        while (true)
        {
            Task? idle;
            lock (_busyOperationSync)
            {
                if (_busyOperationCount == 0)
                    return;
                idle = _idleOperations?.Task;
            }
            if (idle != null)
                await idle.ConfigureAwait(true);
        }
    }

    public override bool OnClose()
    {
        if (IsBusy)
        {
            if (!_closePromptOpen)
            {
                _closePromptOpen = true;
                _ = WaitForBusyThenCloseAsync();
            }
            return false;
        }

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
        Closed?.Invoke(this);
        return base.OnClose();
    }

    private async Task WaitForBusyThenCloseAsync()
    {
        await WaitForActiveOperationAsync().ConfigureAwait(true);

        if (_closeApproved || !IsDirty)
        {
            _closeApproved = true;
            _closePromptOpen = false;
            CloseRequested?.Invoke(this);
            return;
        }

        await ConfirmCloseAsync().ConfigureAwait(true);
    }

    private async Task ConfirmCloseAsync()
    {
        try
        {
            var choice = await _prompts.ConfirmCloseAsync("TLK Editor").ConfigureAwait(true);
            var approved = choice == UnsavedChangesChoice.Discard ||
                           choice == UnsavedChangesChoice.Save && await TrySaveAsync().ConfigureAwait(true);
            if (!approved)
                return;
            _closeApproved = true;
            CloseRequested?.Invoke(this);
        }
        finally
        {
            _closePromptOpen = false;
        }
    }

    private void ApplyChanges(string description, IEnumerable<TlkValueChange> requested)
    {
        var changes = requested
            .GroupBy(change => change.Id)
            .Select(group => group.Last())
            .Where(change => change.OldExists != change.NewExists ||
                             !string.Equals(change.OldText, change.NewText, StringComparison.Ordinal))
            .OrderBy(change => change.Id)
            .ToArray();
        if (changes.Length == 0)
            return;

        if (_historyPosition < _history.Count)
        {
            _history.RemoveRange(_historyPosition, _history.Count - _historyPosition);
            if (_savedPosition > _historyPosition)
                _savedPosition = -1;
        }

        ApplyHistory(changes, useNewValue: true, refreshState: false);
        _history.Add(new TlkHistoryEntry(description, changes));
        _historyPosition++;
        RefreshAfterEdit(changes.Select(change => change.Id));
    }

    private void ApplyHistory(
        IReadOnlyList<TlkValueChange> changes,
        bool useNewValue,
        bool refreshState = true)
    {
        foreach (var change in changes)
        {
            var exists = useNewValue ? change.NewExists : change.OldExists;
            var text = useNewValue ? change.NewText : change.OldText;
            if (exists)
                _backend.SetText(change.Id, text ?? string.Empty);
            else
                _backend.Clear(change.Id);
        }

        if (refreshState)
            RefreshAfterEdit(changes.Select(change => change.Id));
    }

    private void RefreshAfterEdit(IEnumerable<int> changedIds)
    {
        var ids = changedIds.ToArray();
        foreach (var id in ids.Where(_backend.ContainsEntry))
            _confirmedClears.Remove(id);
        foreach (var id in ids.Where(id => !_backend.ContainsEntry(id)))
            _confirmedReferencedWrites.Remove(id);
        // Keep the current filtered snapshot stable while text is being edited. Refiltering on
        // every keystroke can evict the selected row and redirect the remaining input elsewhere.
        // The next filter/exact change recomputes the snapshot explicitly.
        if (string.IsNullOrWhiteSpace(FilterText) && !Rows.ContainsId(MaxEntryId))
        {
            Rows.ResetRange(Math.Max(0, MaxEntryId), null);
            OnPropertyChanged(nameof(VisibleRowCount));
        }
        Rows.RefreshRows(ids);
        if (SelectedRow != null && ids.Contains(SelectedRow.Id))
        {
            _refreshingSelection = true;
            SelectedText = _backend.GetText(SelectedRow.Id) ?? string.Empty;
            _refreshingSelection = false;
        }
        UpdateTitleAndState();
        NotifySelectionState();
    }

    private void ApplyReloadedState()
    {
        var requestedId = Math.Min(SelectedId, Math.Max(0, _backend.MaxEntryId));
        _history.Clear();
        _historyPosition = 0;
        _savedPosition = 0;
        CaptureSavedEntryIds();
        RefreshReferenceStatus();
        RefreshFilter(keepSelection: false);
        if (Rows.ContainsId(requestedId))
            SelectId(requestedId, clearFilter: false);
        else if (SelectedRow != null)
            SelectId(SelectedRow.Id, clearFilter: false);
        UpdateTitleAndState();
    }

    private void RefreshReferenceStatus()
    {
        ReferenceStatus = _backend.ReferenceWarnings.Count == 0
            ? "TLK references indexed."
            : $"Reference scan skipped {_backend.ReferenceWarnings.Count} file(s); see Output.";
        foreach (var warning in _backend.ReferenceWarnings)
            _log.AppendLine($"TLK reference scan: {warning}");
    }

    private void RefreshFilter(bool keepSelection = true)
    {
        var selectedId = SelectedId;
        var ids = MatchingEntryIds(FilterText);
        var filtered = string.IsNullOrWhiteSpace(FilterText) ? null : ids;
        Rows.ResetRange(Math.Max(MaxEntryId, selectedId), filtered);
        OnPropertyChanged(nameof(VisibleRowCount));
        if (keepSelection && Rows.ContainsId(selectedId))
            SelectedRow = Rows.RowForId(selectedId);
        else if (filtered is { Length: > 0 })
            SelectedRow = Rows.RowForId(filtered[0]);
        else if (filtered is { Length: 0 })
            SelectedRow = null;
    }

    private int[] MatchingEntryIds(string value)
    {
        var query = value.Trim();
        if (query.Length == 0)
            return Array.Empty<int>();
        if (TryParseRow(query, out var id))
            return new[] { id };

        return _backend.Entries
            .Where(entry => entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Id)
            .Order()
            .ToArray();
    }

    private static bool TryParseRow(string? value, out int id)
    {
        id = -1;
        if (!uint.TryParse(value?.Trim(), out var number))
            return false;
        var raw = number >= TlkService.CustomTlkBase ? number - TlkService.CustomTlkBase : number;
        if (raw > TlkFormatLimits.MaximumEntryId)
            return false;
        id = (int)raw;
        return true;
    }

    private void EnsureRangeIncludes(int id)
    {
        if (Rows.ContainsId(id))
            return;
        Rows.ResetRange(Math.Max(Math.Max(0, MaxEntryId), id), null);
        OnPropertyChanged(nameof(VisibleRowCount));
    }

    private bool CanAllocateBlank()
    {
        if (_backend.ReferenceWarnings.Count == 0)
            return true;
        NavigationStatus =
            $"Blank search is unavailable because {_backend.ReferenceWarnings.Count} reference file(s) could not be indexed.";
        return false;
    }

    private async Task<bool> TryRefreshReferencesAsync()
    {
        try
        {
            await Task.Run(_backend.RefreshReferences).ConfigureAwait(true);
            RefreshReferenceStatus();
            Rows.RefreshCachedRows();
            if (SelectedRow != null)
                OnSelectedRowChanged(SelectedRow);
            return true;
        }
        catch (Exception ex)
        {
            NavigationStatus = "TLK references could not be refreshed; the operation was cancelled.";
            _log.AppendLine($"TLK reference refresh failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private async Task<bool> ConfirmNewlyReferencedClearsAsync()
    {
        var cleared = _savedEntryIds
            .Where(id =>
                !_backend.ContainsEntry(id) &&
                (_backend.IsReferenced(id) || _backend.ReferenceWarnings.Count > 0) &&
                !_confirmedClears.Contains(id))
            .Order()
            .ToArray();
        if (cleared.Length == 0)
            return true;

        var confirmed = await _prompts.ConfirmDestructiveAsync(
            "Save cleared TLK rows with possible references?",
            "These cleared rows are now referenced, or reference coverage is incomplete:\n\n" +
            string.Join(", ", cleared) +
            "\n\nSaving will leave those references without TLK text.",
            "Save anyway").ConfigureAwait(true);
        if (!confirmed)
            return false;
        foreach (var id in cleared)
            _confirmedClears.Add(id);
        return true;
    }

    private async Task<bool> ConfirmNewlyReferencedWritesAsync()
    {
        var filled = _backend.Entries
            .Select(entry => entry.Id)
            .Where(id =>
                !_savedEntryIds.Contains(id) &&
                _backend.ContainsEntry(id) &&
                (_backend.IsReferenced(id) || _backend.ReferenceWarnings.Count > 0) &&
                !_confirmedReferencedWrites.Contains(id))
            .Order()
            .ToArray();
        if (filled.Length == 0)
            return true;

        var confirmed = await _prompts.ConfirmDestructiveAsync(
            "Save newly populated TLK rows with possible references?",
            "These newly populated rows are now referenced, or reference coverage is incomplete:\n\n" +
            string.Join(", ", filled) +
            "\n\nSaving will replace blank rows that may already be reserved by other content.",
            "Save anyway").ConfigureAwait(true);
        if (!confirmed)
            return false;
        foreach (var id in filled)
            _confirmedReferencedWrites.Add(id);
        return true;
    }

    private void BeginBusyOperation()
    {
        var becameBusy = false;
        lock (_busyOperationSync)
        {
            if (_busyOperationCount++ == 0)
            {
                _idleOperations = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                becameBusy = true;
            }
        }

        if (becameBusy)
            IsBusy = true;
    }

    private void EndBusyOperation()
    {
        TaskCompletionSource? idle = null;
        lock (_busyOperationSync)
        {
            if (--_busyOperationCount == 0)
            {
                idle = _idleOperations;
                _idleOperations = null;
            }
        }

        if (idle != null)
        {
            IsBusy = false;
            idle.TrySetResult();
        }
    }

    private async Task RunBusyOperationAsync(Func<Task> operation)
    {
        BeginBusyOperation();
        try
        {
            await _operationGate.WaitAsync().ConfigureAwait(true);
            try
            {
                await operation().ConfigureAwait(true);
            }
            finally
            {
                _operationGate.Release();
            }
        }
        finally
        {
            EndBusyOperation();
        }
    }

    private async Task<T> RunBusyOperationAsync<T>(Func<Task<T>> operation)
    {
        BeginBusyOperation();
        try
        {
            await _operationGate.WaitAsync().ConfigureAwait(true);
            try
            {
                return await operation().ConfigureAwait(true);
            }
            finally
            {
                _operationGate.Release();
            }
        }
        finally
        {
            EndBusyOperation();
        }
    }

    private void CaptureSavedEntryIds()
    {
        _savedEntryIds.Clear();
        foreach (var entry in _backend.Entries)
            _savedEntryIds.Add(entry.Id);
        _confirmedClears.Clear();
        _confirmedReferencedWrites.Clear();
    }

    private bool RevalidateAppendedEntries()
    {
        var savedMaximum = _savedEntryIds.Count == 0 ? -1 : _savedEntryIds.Max();
        var highestNewId = _backend.Entries
            .Where(entry => entry.Id > savedMaximum && !_savedEntryIds.Contains(entry.Id))
            .Select(entry => (int?)entry.Id)
            .Max();
        if (!highestNewId.HasValue)
            return true;

        return CanCreateThrough(
            highestNewId.Value,
            new Dictionary<int, string>(),
            savedMaximum);
    }

    private bool CanCreateThrough(
        int highestNewId,
        IReadOnlyDictionary<int, string> plannedText,
        int? existingMaximum = null)
    {
        if (highestNewId <= (existingMaximum ?? _backend.MaxEntryId))
            return true;
        if (!CanAllocateBlank())
            return false;

        var blankId = _backend.FindFirstAvailableBlank();
        for (var id = blankId; id < highestNewId; id++)
        {
            if (_backend.ContainsEntry(id) || _backend.IsReferenced(id))
                continue;
            if (!plannedText.TryGetValue(id, out var text) || text.Length == 0)
            {
                NavigationStatus =
                    $"Use Blank row {id} before adding row {highestNewId}.";
                return false;
            }
        }

        return true;
    }

    private void NotifySelectionState()
    {
        OnPropertyChanged(nameof(SelectedId));
        OnPropertyChanged(nameof(SelectedStrRef));
        OnPropertyChanged(nameof(SelectedIdDisplay));
        OnPropertyChanged(nameof(SelectedStrRefDisplay));
        OnPropertyChanged(nameof(HasSelectedRow));
        OnPropertyChanged(nameof(HasUsages));
        ClearRowCommand.NotifyCanExecuteChanged();
    }

    private void UpdateTitleAndState()
    {
        Title = IsDirty ? "TLK Editor *" : "TLK Editor";
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(EntryCount));
        OnPropertyChanged(nameof(MaxEntryId));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private static string FormatUsages(IReadOnlyList<TlkEditorUsage> usages) => string.Join(
        Environment.NewLine,
        usages.Take(30).Select(usage =>
            $"{usage.FileName}: row {usage.RowIndex}" +
            (string.IsNullOrWhiteSpace(usage.RowLabel) ? string.Empty : $" ({usage.RowLabel})") +
            $", {usage.ColumnName}")) +
        (usages.Count > 30 ? $"{Environment.NewLine}…and {usages.Count - 30} more." : string.Empty);

    private static string BuildPastePreview(
        int start,
        IReadOnlyList<string> lines,
        IReadOnlyCollection<int> overwrites,
        IReadOnlyCollection<int> referenced)
    {
        var preview = lines.Take(24).Select((text, offset) =>
        {
            var id = start + offset;
            var flags = new List<string>();
            if (overwrites.Contains(id))
                flags.Add("OVERWRITES TEXT");
            if (referenced.Contains(id))
                flags.Add("REFERENCED");
            var sample = text.Replace('\r', ' ').Replace('\n', ' ');
            if (sample.Length > 70)
                sample = sample[..67] + "…";
            return $"{id}: {sample}" + (flags.Count == 0 ? string.Empty : $" [{string.Join(", ", flags)}]");
        });
        var textPreview = string.Join(Environment.NewLine, preview);
        if (lines.Count > 24)
            textPreview += $"{Environment.NewLine}…and {lines.Count - 24} more row(s).";
        return "Review the consecutive rows that will be written:\n\n" + textPreview;
    }

    private static string EncodeClipboardRow(string text) =>
        text.Length == 0 ||
        text.Contains('\r') ||
        text.Contains('\n') ||
        text.StartsWith(ClipboardRowPrefix, StringComparison.Ordinal)
            ? ClipboardRowPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
            : text;

    private void PublishAndRefreshLabels(string operation)
    {
        try
        {
            _backend.Publish();
            _afterSave?.Invoke();
        }
        catch (Exception ex)
        {
            // The disk transaction/reload already succeeded. Keep the document generation honest
            // and report the non-persistent cache refresh failure separately.
            _log.AppendLine(
                $"TLK {operation} succeeded, but refreshing open labels failed: " +
                ex.GetBaseException().Message);
        }
    }

    private sealed record TlkHistoryEntry(string Description, IReadOnlyList<TlkValueChange> Changes);

    private sealed record TlkValueChange(
        int Id,
        bool OldExists,
        string? OldText,
        bool NewExists,
        string? NewText)
    {
        public static TlkValueChange Set(int id, bool oldExists, string? oldText, string text) =>
            new(id, oldExists, oldText, text.Length > 0, text.Length > 0 ? text : null);

        public static TlkValueChange Clear(int id, string? oldText) =>
            new(id, true, oldText, false, null);
    }
}
