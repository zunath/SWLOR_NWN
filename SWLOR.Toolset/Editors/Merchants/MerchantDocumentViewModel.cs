using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>A UTM document hosted by the dedicated merchant editor.</summary>
    public partial class MerchantDocumentViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public MerchantEditorViewModel Editor { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;
        public bool CanUndo => _session.UndoStack.CanUndo;
        public bool CanRedo => _session.UndoStack.CanRedo;
        public string FilePath => _session.FilePath;
        public string ResRef => _resRef;

        public event Action<MerchantDocumentViewModel>? Closed;
        public event Action<MerchantDocumentViewModel>? CloseRequested;
        public event Action? CatalogEntryChanged;

        public MerchantDocumentViewModel(
            string filePath,
            string resRef,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<BehaviorChoice>? baseItems = null,
            Func<string, MerchantItemDefinition?>? loadItem = null,
            Func<string, IReadOnlyList<MerchantItemDefinition>>? searchItems = null,
            MerchantInstanceService? instances = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _resRef = resRef;
            Id = $"merchant:{filePath}";
            _session = DocumentSession.Open(filePath);
            Editor = new MerchantEditorViewModel(
                _session.Document.Root,
                resRef,
                RunEdit,
                resolveChoices,
                baseItems,
                loadItem,
                searchItems,
                instances);
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

        [RelayCommand(CanExecute = nameof(IsDirty))]
        private void Revert()
        {
            _session.RevertToSaved();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty && !Editor.NeedsSaveNormalization)
                return true;

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts
                        .ConfirmExternalChangeAsync(_session.FilePath)
                        .ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;

                    if (choice == ExternalChangeChoice.Reload)
                    {
                        _session.ReloadFromDisk();
                        Editor.ReloadFromDocument();
                        AfterHistoryChange();
                        CatalogEntryChanged?.Invoke();
                        await Editor.RefreshPlacedInstancesAsync().ConfigureAwait(true);
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    _session.RecordCurrentFileState();
                }

                if (!Editor.PrepareForSave())
                    return false;

                var saveBytes = _session.ToBytes();
                if (!SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Save stopped because {_session.FilePath} changed while the save was being prepared.");
                    return false;
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(saveBytes);
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                await Editor.RefreshPlacedInstancesAsync().ConfigureAwait(true);
                _log.AppendLine($"Saved {_session.FilePath}.");
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
                               choice == UnsavedChangesChoice.Save &&
                               await TrySaveAsync().ConfigureAwait(true);
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
