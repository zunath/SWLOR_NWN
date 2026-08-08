using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// A trigger blueprint (.utt) open as a document tab, hosting the shared trigger editor. Revert
    /// unwinds every unsaved edit; Save writes the file. The same editor serves a placement, where
    /// the area editor owns the session instead.
    /// </summary>
    public partial class TriggerDocumentViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly BlueprintSaveCoordinator? _saveCoordinator;
        private string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;
        private int _selectedTabIndex;

        public TriggerEditorViewModel Editor { get; }
        public Sources.ObjectSourceSectionViewModel? Source { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;
        public string FilePath => _session.FilePath;
        public string ResRef => _resRef;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value)
                    return;
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<TriggerDocumentViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<TriggerDocumentViewModel>? CloseRequested;

        /// <summary>Raised after this resource is saved or reloaded so catalog views can re-index it.</summary>
        public event Action? CatalogEntryChanged;
        public event Action<TriggerDocumentViewModel, string, string>? Renamed;

        public TriggerDocumentViewModel(
            string filePath,
            string resRef,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<BehaviorTagScope, string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ChoicePreviewService? previews = null,
            BlueprintSaveCoordinator? saveCoordinator = null,
            Sources.ObjectSourceSectionViewModel? source = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            _saveCoordinator = saveCoordinator;
            Source = source;
            Id = $"trigger:{filePath}";
            _session = DocumentSession.Open(filePath);

            Editor = new TriggerEditorViewModel(
                _session.Document.Root, resRef, isInstance: false, RunEdit, gameCodeIndex, resolveTag,
                resolveChoices, previews, prompts, log);

            UpdateTitle();
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
                _log.AppendLine($"Edit failed ({description}): {ex.Message}");
                return false;
            }
        }

        [RelayCommand]
        private async Task Save() => await TrySaveAsync().ConfigureAwait(true);

        /// <summary>Unwinds every unsaved edit — the footer's Revert.</summary>
        [RelayCommand(CanExecute = nameof(IsDirty))]
        private void Revert()
        {
            // Back to the saved version on disk, not back to position zero. Save leaves the
            // history intact, so unwinding the whole stack also unwound edits already committed.
            _session.RevertToSaved();

            Editor.ReloadFromDocument();
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
                        _session.ReloadFromDisk();
                        Editor.ReloadFromDocument();
                        AfterHistoryChange();
                        CatalogEntryChanged?.Invoke();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    _session.RecordCurrentFileState();
                }

                if (!BlueprintResRef.TryNormalize(
                        _session, "TemplateResRef", out var targetResRef, out var problem))
                {
                    _log.AppendLine($"Cannot save {_resRef}: {problem}");
                    return false;
                }

                Editor.ReloadFromDocument();
                var renaming = !string.Equals(
                    targetResRef, _resRef, StringComparison.OrdinalIgnoreCase);
                if (renaming && _saveCoordinator == null)
                {
                    _log.AppendLine($"Cannot rename {_resRef}: no blueprint save coordinator is available.");
                    return false;
                }

                var oldResRef = _resRef;
                var oldPath = _session.FilePath;
                var saveBytes = _session.ToBytes();
                var outcome = _saveCoordinator?.Save(
                    _session, ResourceType.Utt, oldResRef, targetResRef);
                if (outcome != null && !outcome.Saved)
                    return false;
                if (outcome == null && !SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Save stopped because {_session.FilePath} changed while the save was being prepared.");
                    return false;
                }

                if (outcome?.Renamed == true)
                {
                    _resRef = targetResRef;
                    Id = $"trigger:{_session.FilePath}";
                    Editor.SetHeaderOwner(targetResRef);
                    Source?.SetResRef(targetResRef);
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(_session.ToBytes());
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                if (outcome?.Renamed == true)
                {
                    Renamed?.Invoke(this, oldResRef, oldPath);
                    _log.AppendLine(
                        $"Saved {oldPath} as {_session.FilePath} and updated " +
                        $"{outcome.UpdatedInstances} placed instance" +
                        $"{(outcome.UpdatedInstances == 1 ? string.Empty : "s")}.");
                }
                else
                {
                    _log.AppendLine($"Saved {_session.FilePath}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            _session.Undo();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            _session.Redo();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        /// <summary>Suppresses a second tab-level prompt after the window-level discard decision.</summary>
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
            Editor.Dispose();
            _session.Dispose();
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private async Task ConfirmCloseAsync()
        {
            try
            {
                var choice = await _prompts.ConfirmCloseAsync(Title ?? _resRef).ConfigureAwait(true);
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

        private void UpdateTitle() => Title = IsDirty ? $"{_resRef} *" : _resRef;
    }
}
