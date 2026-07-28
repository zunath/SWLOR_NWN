using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>An item blueprint document hosting the behavior-shaped item editor.</summary>
    /// <remarks>
    /// The one document whose save can rename its file: the ResRef row is editable, and a resref
    /// IS the file name, so a save under a changed resref writes the new file, deletes the old,
    /// and rebinds the session rather than letting the field and the file drift apart.
    /// </remarks>
    public partial class ItemDocumentViewModel : Document, IEditorDocument
    {
        private static readonly Regex ResRefShape = new("^[a-z0-9_]{1,16}$", RegexOptions.Compiled);

        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public ItemEditorViewModel Editor { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;

        public string FilePath => _session.FilePath;

        public string ResRef => _resRef;

        public event Action<ItemDocumentViewModel>? Closed;

        public event Action<ItemDocumentViewModel>? CloseRequested;

        public event Action? CatalogEntryChanged;

        /// <summary>Raised after a save renamed the file: (document, old resref, old path).</summary>
        public event Action<ItemDocumentViewModel, string, string>? Renamed;

        public ItemDocumentViewModel(
            string filePath,
            string resRef,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            Func<int, BaseItemRow?>? baseItemRows = null,
            Func<JsonGffStruct, IconImage?>? renderIcon = null,
            Behaviors.ChoicePreviewService? choicePreviews = null,
            Func<int, BaseItemIconRow?>? baseItemIcons = null,
            Func<string, bool>? textureExists = null,
            Func<string, IReadOnlyList<Domain.Workspace.ItemSourceEntry>>? sourceLookup = null,
            Func<int, int?>? costTableMax = null,
            Func<JsonGffStruct, RenderModel?>? resolveModel = null,
            ResourceIndex? resourceIndex = null,
            ArmorDyeSwatchService? armorDyeSwatches = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            Id = $"item:{filePath}";
            _session = DocumentSession.Open(filePath);

            Editor = new ItemEditorViewModel(
                _session.Document.Root,
                resRef,
                RunEdit,
                gameCodeIndex,
                resolveChoices,
                baseItemRows,
                renderIcon,
                choicePreviews,
                prompts,
                baseItemIcons,
                textureExists,
                sourceLookup,
                isDirty: false,
                costTableMax,
                resolveModel: resolveModel,
                resourceIndex: resourceIndex,
                armorDyeSwatches: armorDyeSwatches);
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
            if (!IsDirty)
                return true;

            try
            {
                var targetResRef = Editor.TemplateResRef.Trim().ToLowerInvariant();
                if (!ResRefShape.IsMatch(targetResRef))
                {
                    _log.AppendLine(
                        $"Cannot save {_resRef}: ResRef '{Editor.TemplateResRef}' must be 1-16 " +
                        "characters of a-z, 0-9, or underscore.");
                    return false;
                }

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

                // The field may carry stray case or whitespace the shape check already tolerated
                // away; what is saved must be exactly what the file is named.
                if (Editor.TemplateResRef != targetResRef && !Editor.NormalizeResRef(targetResRef))
                    return false;

                var renaming = !string.Equals(targetResRef, _resRef, StringComparison.OrdinalIgnoreCase);
                var newPath = _session.FilePath;
                if (renaming && !TryResolveRenameTarget(targetResRef, out newPath))
                    return false;

                SaveService.WriteAtomic(newPath, _session.ToBytes());

                var oldPath = _session.FilePath;
                var oldResRef = _resRef;
                if (renaming)
                {
                    if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                        File.Delete(oldPath);
                    _session.MoveTo(newPath);
                    _resRef = targetResRef;
                    Id = $"item:{newPath}";
                    Editor.SetHeaderOwner(targetResRef);
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState();
                AfterHistoryChange();
                if (renaming)
                {
                    Renamed?.Invoke(this, oldResRef, oldPath);
                    _log.AppendLine($"Saved {oldPath} as {newPath}.");
                }
                else
                {
                    _log.AppendLine($"Saved {_session.FilePath}.");
                }
                CatalogEntryChanged?.Invoke();
                Editor.RefreshSource();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// The renamed file's path, refused when another blueprint already owns it. A Windows
        /// case-only rename of the same file is allowed through: it is the same blueprint.
        /// </summary>
        private bool TryResolveRenameTarget(string targetResRef, out string newPath)
        {
            var directory = Path.GetDirectoryName(_session.FilePath) ?? string.Empty;
            var fileName = Path.GetFileName(_session.FilePath);
            var suffix = fileName.StartsWith(_resRef, StringComparison.OrdinalIgnoreCase)
                ? fileName[_resRef.Length..]
                : ".uti.json";

            newPath = Path.Combine(directory, targetResRef + suffix);
            if (File.Exists(newPath) &&
                !string.Equals(newPath, _session.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                _log.AppendLine(
                    $"Cannot rename {_resRef} to {targetResRef}: another blueprint already uses that resref.");
                return false;
            }

            return true;
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
