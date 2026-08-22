using SWLOR.Toolset.Domain.Editors.Items;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>An item blueprint document hosting the behavior-shaped item editor.</summary>
    /// <remarks>
    /// The ResRef row is editable, and a ResRef is the file name, so a save under a changed value
    /// writes the new file, removes the old, and rebinds the session rather than letting field and
    /// file drift apart. The application supplies the shared blueprint coordinator, which also
    /// rebuilds placed instances; the standalone fallback used by focused tests retains the older
    /// fail-closed reference check.
    /// </remarks>
    public partial class ItemDocumentViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly BlueprintSaveCoordinator? _saveCoordinator;

        /// <summary>(old resref, own file path) -> files still referencing that resref.</summary>
        private readonly Func<string, string, IReadOnlyList<string>>? _findReferences;

        /// <summary>
        /// (old resref, new resref) -> save result and the exact installed sidecar fingerprint.
        /// </summary>
        private readonly Func<string, string, CategorySaveResult>? _refileCategories;

        /// <summary>
        /// Old resref -> whether its category-folder membership (if any) can be carried over to the new
        /// resref. Checked in the rename preflight so a sidecar that cannot be saved refuses the rename
        /// instead of the save going ahead and leaving the sidecar naming a resref that no longer exists.
        /// </summary>
        private readonly Func<string, bool>? _canRefileCategories;
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
            Func<bool>? itemSourcesReady = null,
            ItemCostTableRanges? costTables = null,
            Func<JsonGffStruct, bool, RenderModel?>? resolveModel = null,
            ResourceIndex? resourceIndex = null,
            ArmorDyeSwatchService? armorDyeSwatches = null,
            ArmorPartCatalog? armorPartModels = null,
            Func<string, string, IReadOnlyList<string>>? findReferences = null,
            Func<string, bool>? canRefileCategories = null,
            Func<string, string, CategorySaveResult>? refileCategories = null,
            BlueprintSaveCoordinator? saveCoordinator = null,
            Sources.ObjectSourceSectionViewModel? placementSource = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            _findReferences = findReferences;
            _canRefileCategories = canRefileCategories;
            _refileCategories = refileCategories;
            _saveCoordinator = saveCoordinator;
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
                itemSourcesReady,
                isDirty: false,
                costTables,
                resolveModel: resolveModel,
                resourceIndex: resourceIndex,
                armorDyeSwatches: armorDyeSwatches,
                armorPartModels: armorPartModels,
                placementSource: placementSource,
                log: log);
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
                var invariantProblem = Editor.EnforceSaveInvariants();
                if (invariantProblem != null)
                {
                    _log.AppendLine(invariantProblem);
                    return false;
                }

                var targetResRef = Editor.TemplateResRef.Trim().ToLowerInvariant();
                if (!NwnResRef.IsCanonical(targetResRef))
                {
                    _log.AppendLine(
                        $"Cannot save {_resRef}: ResRef '{Editor.TemplateResRef}' must be " +
                        $"1-{NwnResRef.MaxLength} " +
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

                    // The builder accepted the generation that is on disk now. Adopt that exact
                    // generation so the rename's final capture rejects only a later external write.
                    _session.RecordCurrentFileState();
                }

                // The field may carry stray case or whitespace the shape check already tolerated
                // away; what is saved must be exactly what the file is named.
                if (Editor.TemplateResRef != targetResRef && !Editor.NormalizeResRef(targetResRef))
                    return false;

                if (_saveCoordinator != null)
                    return SaveWithCoordinator(targetResRef);

                using var moduleWriteLock =
                    ModuleWriteLock.AcquireForResourcePath(_session.FilePath);
                var renaming = !string.Equals(targetResRef, _resRef, StringComparison.Ordinal);
                var caseOnlyRename = renaming &&
                                     string.Equals(
                                         targetResRef,
                                         _resRef,
                                         StringComparison.OrdinalIgnoreCase);
                var newPath = _session.FilePath;
                if (renaming && !TryResolveRenameTarget(targetResRef, out newPath))
                    return false;
                if (renaming && !caseOnlyRename && IsStillReferenced(targetResRef))
                    return false;
                if (renaming && !caseOnlyRename && !CanRefileCategories(targetResRef))
                    return false;

                // Everything above - the reference sweep and the category preflight - reads the disk
                // and can take seconds. Another tool may have written the original in that window,
                // and a rename is about to DELETE it, so its fingerprint is checked and captured
                // here rather than trusting the check from before the scans.
                byte[]? originalContentHash = null;
                if (renaming &&
                    !_session.TryCaptureUnchangedFileContentHash(out originalContentHash))
                {
                    _log.AppendLine(
                        $"Cannot rename {_resRef}: the file changed on disk while the rename was being " +
                        "checked. Nothing was written - reload or save again to pick that change up.");
                    return false;
                }

                var oldPath = _session.FilePath;
                var oldResRef = _resRef;
                var moving = renaming &&
                             !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
                var changingPathSpelling = renaming &&
                                           !string.Equals(oldPath, newPath, StringComparison.Ordinal);
                var moduleRoot = changingPathSpelling
                    ? Directory.GetParent(Path.GetDirectoryName(oldPath)!)?.FullName
                      ?? throw new InvalidOperationException(
                          $"Could not determine the module root for '{oldPath}'.")
                    : null;
                var saveBytes = _session.ToBytes();
                using var renameRecovery = changingPathSpelling
                    ? ItemRenameRecovery.Begin(
                        moduleRoot!,
                        oldPath,
                        newPath,
                        saveBytes,
                        originalContentHash!)
                    : null;

                // A rename installs its destination with no-overwrite semantics: the existence
                // check above ran before the (potentially long) reference scan, and a blueprint
                // another process created in that window must fail this save rather than be
                // silently replaced and then orphaned by the delete below.
                if (caseOnlyRename && changingPathSpelling)
                {
                    if (!renameRecovery!.OriginalStillMatches())
                    {
                        _log.AppendLine(
                            $"Cannot normalize the filename for {_resRef}: {oldPath} changed while " +
                            "the save was being prepared. Nothing was written - reload and try again.");
                        return false;
                    }

                    // Windows keeps the existing directory-entry casing when a file is overwritten.
                    // Remove the old spelling first, under the recovery transaction's module lock,
                    // then create the canonical lowercase path as a new directory entry.
                    File.Delete(oldPath);
                    SaveService.WriteAtomicNew(newPath, saveBytes);
                }
                else if (renaming &&
                         !string.Equals(_session.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
                    SaveService.WriteAtomicNew(newPath, saveBytes);
                else if (!SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Cannot save {_session.FilePath}: the file changed on disk while the save " +
                        "was being prepared. Nothing was written - reload or save again.");
                    return false;
                }

                if (renaming)
                {
                    // The sidecar commits BEFORE the original is deleted, so a sidecar that turned
                    // unwritable since the preflight costs nothing: the destination is removed
                    // again and the original - still on disk, still filed - stands.
                    if (moving)
                    {
                        var refileResult = RefileCategories(
                            oldResRef,
                            targetResRef,
                            newPath);
                        if (!refileResult.Saved)
                            return false;
                        renameRecovery!.RecordCategoryGeneration(
                            refileResult.ContentSha256);
                    }

                    if (moving && !TryDeleteRenamedOriginal(oldPath, newPath, renameRecovery!))
                    {
                        // The delete failed after the sidecar already moved: put the sidecar back so
                        // it keeps naming the blueprint that actually exists.
                        RefileCategories(targetResRef, oldResRef, null);
                        return false;
                    }

                    renameRecovery?.Complete();
                    _session.MoveTo(newPath);
                    _resRef = targetResRef;
                    Id = $"item:{newPath}";
                    Editor.SetHeaderOwner(targetResRef);
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(saveBytes);
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

        private bool SaveWithCoordinator(string targetResRef)
        {
            var oldResRef = _resRef;
            var oldPath = _session.FilePath;
            var outcome = _saveCoordinator!.Save(
                _session,
                ResourceType.Uti,
                oldResRef,
                targetResRef);
            if (!outcome.Saved)
                return false;

            var savedBytes = _session.ToBytes();
            if (outcome.Renamed)
            {
                _resRef = targetResRef;
                Id = $"item:{_session.FilePath}";
                Editor.SetHeaderOwner(targetResRef);
            }

            _session.UndoStack.MarkSaved();
            _session.RecordCurrentFileState(savedBytes);
            AfterHistoryChange();
            if (outcome.Renamed)
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

            CatalogEntryChanged?.Invoke();
            Editor.RefreshSource();
            return true;
        }

        /// <summary>
        /// Moves the category membership onto the renamed blueprint, removing the just-written
        /// destination when the sidecar refuses. True when there is nothing to refile.
        /// </summary>
        private CategorySaveResult RefileCategories(
            string fromResRef,
            string toResRef,
            string? rollbackPath)
        {
            var result = _refileCategories?.Invoke(fromResRef, toResRef)
                         ?? CategorySaveResult.Ok();
            if (result.Saved)
                return result;

            if (rollbackPath != null)
            {
                try
                {
                    File.Delete(rollbackPath);
                }
                catch (Exception ex)
                {
                    _log.AppendLine(
                        $"Could not remove {rollbackPath} after the category update failed: {ex.Message}");
                }
            }

            _log.AppendLine(
                $"Cannot rename {fromResRef} to {toResRef}: its category could not be updated, so the " +
                "rename was rolled back rather than leaving the category naming a deleted blueprint.");
            return result;
        }

        /// <summary>
        /// Deletes the pre-rename file after the new one was written, rolling the new file back
        /// when the delete fails (another process holding the original open, denied permissions).
        /// Without the rollback the failed save would leave BOTH blueprints on disk, and every
        /// retry would then be refused because the target path already exists.
        /// </summary>
        private bool TryDeleteRenamedOriginal(
            string oldPath,
            string newPath,
            ItemRenameRecovery.Transaction renameRecovery)
        {
            try
            {
                if (!renameRecovery.OriginalStillMatches())
                {
                    File.Delete(newPath);
                    _log.AppendLine(
                        $"Cannot rename {_resRef}: {oldPath} changed before it could be deleted. " +
                        "The save was rolled back; reload the external change before retrying.");
                    return false;
                }

                File.Delete(oldPath);
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    File.Delete(newPath);
                }
                catch (Exception rollback)
                {
                    _log.AppendLine(
                        $"Could not roll back {newPath} after the failed rename: {rollback.Message}. " +
                        "Delete it by hand before retrying.");
                }

                _log.AppendLine(
                    $"Rename failed: could not delete {oldPath} ({ex.Message}). " +
                    "The save was rolled back; the blueprint keeps its original resref.");
                return false;
            }
        }

        /// <summary>
        /// Refuses the rename while other module or game-code files still name the old resref:
        /// the save would delete the file every one of those references points at. The scan runs
        /// only on an actual rename, so an ordinary save never pays for it.
        /// </summary>
        private bool IsStillReferenced(string targetResRef)
        {
            var references = _findReferences?.Invoke(_resRef, _session.FilePath)
                             ?? Array.Empty<string>();
            if (references.Count == 0)
                return false;

            var shown = string.Join(", ", references.Take(5));
            var more = references.Count > 5 ? $" (+{references.Count - 5} more)" : string.Empty;
            _log.AppendLine(
                $"Cannot rename {_resRef} to {targetResRef}: {references.Count} file(s) still " +
                $"reference '{_resRef}' - {shown}{more}. Update those references first, then rename.");
            return true;
        }

        /// <summary>
        /// Refuses the rename when the item's custom-category membership cannot be carried over to
        /// the new resref: a rename that goes ahead anyway would leave the sidecar naming a resref
        /// that no longer exists, and the renamed item would read as unfiled once the module is
        /// reopened. Runs only on an actual rename, mirroring <see cref="IsStillReferenced"/>.
        /// </summary>
        private bool CanRefileCategories(string targetResRef)
        {
            if (_canRefileCategories == null || _canRefileCategories(_resRef))
                return true;

            _log.AppendLine(
                $"Cannot rename {_resRef} to {targetResRef}: its category could not be updated. " +
                "Resolve the category conflict, then rename again.");
            return false;
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
