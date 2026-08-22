using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Workspace;
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
        private readonly BlueprintSaveCoordinator? _saveCoordinator;
        private string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public MerchantEditorViewModel Editor { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;
        public bool IsBusy => Editor.IsUpdatingInstances;
        public bool CanUndo => !IsBusy && _session.UndoStack.CanUndo;
        public bool CanRedo => !IsBusy && _session.UndoStack.CanRedo;
        public string FilePath => _session.FilePath;
        public string ResRef => _resRef;

        public event Action<MerchantDocumentViewModel>? Closed;
        public event Action<MerchantDocumentViewModel>? CloseRequested;
        public event Action? CatalogEntryChanged;
        public event Action<MerchantDocumentViewModel, string, string>? Renamed;

        public MerchantDocumentViewModel(
            string filePath,
            string resRef,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<BehaviorChoice>? baseItems = null,
            Func<string, MerchantItemDefinition?>? loadItem = null,
            Func<string, int, int, int, CancellationToken,
                Task<IReadOnlyList<MerchantItemDefinition>>>? searchItems = null,
            MerchantInstanceService? instances = null,
            BlueprintSaveCoordinator? saveCoordinator = null,
            Action<string, Action<Bitmap>>? requestItemPreview = null,
            Action<string>? openItem = null,
            Action<string, MerchantInstancePlacement>? goToInstance = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _resRef = resRef;
            _saveCoordinator = saveCoordinator;
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
                instances,
                requestItemPreview,
                openItem,
                goToInstance);
            Editor.PropertyChanged += OnEditorPropertyChanged;
            UpdateTitle();
        }

        private bool RunEdit(string description, Action mutation)
        {
            if (IsBusy)
                return false;

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

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save() => await TrySaveAsync().ConfigureAwait(true);

        private bool CanSave() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanRevert))]
        private void Revert()
        {
            _session.RevertToSaved();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        private bool CanRevert() => !IsBusy && IsDirty;

        public async Task<bool> TrySaveAsync()
        {
            if (IsBusy)
            {
                _log.AppendLine(
                    $"Save deferred for {_session.FilePath}: placed merchant instances are still updating.");
                return false;
            }

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
                        Editor.InvalidatePlacedInstances();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    _session.RecordCurrentFileState();
                }

                if (!Editor.PrepareForSave())
                    return false;

                if (!BlueprintResRef.TryNormalize(
                        _session, "ResRef", out var targetResRef, out var problem))
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
                    _session, ResourceType.Utm, oldResRef, targetResRef);
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
                    Id = $"merchant:{_session.FilePath}";
                    Editor.SetHeaderOwner(targetResRef);
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(_session.ToBytes());
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                Editor.InvalidatePlacedInstances();
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
            if (IsBusy)
                return;

            _session.Undo();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            if (IsBusy)
                return;

            _session.Redo();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        internal void ApproveApplicationClose() => _closeApproved = true;

        public override bool OnClose()
        {
            if (IsBusy)
                return false;

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
            Editor.PropertyChanged -= OnEditorPropertyChanged;
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

        private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MerchantEditorViewModel.IsUpdatingInstances))
                return;

            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            SaveCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        private void UpdateTitle() => Title = IsDirty ? $"{_resRef} *" : _resRef;
    }
}
