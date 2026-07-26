using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;
using SWLOR.Toolset.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>A door blueprint document hosting the shared blueprint/placement door editor.</summary>
    public partial class DoorDocumentViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public DoorEditorViewModel Editor { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;

        public event Action<DoorDocumentViewModel>? Closed;

        public event Action<DoorDocumentViewModel>? CloseRequested;

        public event Action? CatalogEntryChanged;

        public DoorDocumentViewModel(
            string filePath,
            string resRef,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<BehaviorTagScope, string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<DoorAppearanceChoice>? appearances = null,
            ResourceIndex? resourceIndex = null,
            Func<JsonGffStruct, RenderModel?>? resolveModel = null,
            ThumbnailService? thumbnails = null,
            ChoicePreviewService? choicePreviews = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            Id = $"door:{filePath}";
            _session = DocumentSession.Open(filePath);

            Editor = new DoorEditorViewModel(
                _session.Document.Root,
                resRef,
                isInstance: false,
                RunEdit,
                gameCodeIndex,
                resolveTag,
                resolveChoices,
                appearances,
                resourceIndex,
                resolveModel,
                thumbnails: thumbnails,
                choicePreviews: choicePreviews);
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
            while (_session.UndoStack.CanUndo)
                _session.Undo();

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
                }

                SaveService.WriteAtomic(_session.FilePath, _session.ToBytes());
                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState();
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
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
            Editor.SetDirty(IsDirty);
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
