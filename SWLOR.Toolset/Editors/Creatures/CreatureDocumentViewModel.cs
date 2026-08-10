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
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>A UTC document and its linked stat/equipment UTIs saved as one logical unit.</summary>
    public partial class CreatureDocumentViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;
        private int _selectedTabIndex;

        public CreatureEditorViewModel Editor { get; }
        public Sources.ObjectSourceSectionViewModel? Source { get; }
        public bool IsDirty => _session.UndoStack.IsDirty;
        public bool CanUndo => _session.UndoStack.CanUndo;
        public bool CanRedo => _session.UndoStack.CanRedo;
        public string FilePath => _session.FilePath;
        public string ResRef { get; }
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

        public event Action<CreatureDocumentViewModel>? Closed;
        public event Action<CreatureDocumentViewModel>? CloseRequested;
        public event Action? CatalogEntryChanged;

        public CreatureDocumentViewModel(
            string filePath,
            string resRef,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            ResourceIndex? resourceIndex,
            Func<JsonGffStruct, RenderModel?>? resolveModel,
            Func<int, AppearanceRow?> appearance,
            Editors.Items.ArmorPartCatalog? armorParts,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>>? equipmentChoices,
            Func<string, CreatureEquipmentChoice?>? equipmentDetails,
            Editors.Behaviors.ChoicePreviewService? choicePreviews,
            Func<BehaviorChoice, string?>? previewAudio,
            Action<string>? openLootDefinition,
            IReadOnlyList<AppearanceOption>? appearanceOptions,
            ThumbnailService? appearanceThumbnails,
            Editors.Items.ArmorDyeSwatchService? colorPalettes = null,
            Func<string, string>? resolveItemName = null,
            Func<string, int, int, int,
                Task<IReadOnlyList<CreatureEquipmentChoice>>>? equipmentSearch = null,
            Func<IReadOnlyList<AppearanceOption>>? appearanceOptionsLoader = null,
            Func<int, string?>? abilityIcon = null,
            Sources.ObjectSourceSectionViewModel? source = null,
            TintMapCatalog? tintMapCatalog = null)
        {
            _log = log;
            _prompts = prompts;
            Source = source;
            ResRef = resRef;
            Id = $"creature:{filePath}";
            _session = DocumentSession.Open(filePath);
            Editor = new CreatureEditorViewModel(
                _session.Document.Root,
                filePath,
                resRef,
                RunEdit,
                gameCodeIndex,
                resolveChoices,
                resourceIndex,
                resolveModel,
                appearance,
                armorParts,
                equipmentChoices,
                equipmentDetails,
                choicePreviews,
                previewAudio,
                openLootDefinition,
                appearanceOptions,
                appearanceThumbnails,
                colorPalettes,
                resolveItemName,
                equipmentSearch,
                appearanceOptionsLoader,
                abilityIcon,
                log,
                tintMapCatalog,
                captureCoalesceOrigin: () => _session.UndoStack.CurrentAppliedEntry,
                runCoalescedEdit: RunCoalescedEdit);
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

        private bool RunCoalescedEdit(
            IDocumentEdit origin,
            string description,
            Action mutation)
        {
            try
            {
                var applied = _session.ExecuteCoalesced(origin, description, mutation);
                if (applied)
                    AfterHistoryChange();
                return applied;
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
            Editor.Equipment.ReloadSavedDocuments();
            Editor.ReloadFromDocument();
            AfterHistoryChange();
        }

        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty && !Editor.Loot.NeedsNormalization)
                return true;

            try
            {
                if (Editor.Loot.NeedsNormalization)
                    _session.Execute("Normalize loot rows", Editor.NormalizeForSave);

                if (!await AcceptExternalChangeAsync(_session, isMainDocument: true).ConfigureAwait(true))
                    return false;

                var equipment = Editor.Equipment.SaveParticipants();
                foreach (var item in equipment)
                {
                    if (!await AcceptExternalChangeAsync(item.Session, item.IsNew).ConfigureAwait(true))
                        return false;
                }

                var staged = new List<SaveService.StagedWrite>();
                var saved = new List<(DocumentSession Session, byte[] Bytes)>();
                using (ModuleWriteLock.AcquireForResourcePath(_session.FilePath))
                {
                    if (ChangedWhilePreparingGroupedSave(_session, isNew: false) ||
                        equipment.Any(item => ChangedWhilePreparingGroupedSave(item.Session, item.IsNew)))
                    {
                        return false;
                    }

                    try
                    {
                        var creatureBytes = _session.ToBytes();
                        staged.Add(SaveService.Stage(_session.FilePath, creatureBytes));
                        saved.Add((_session, creatureBytes));

                        foreach (var item in equipment)
                        {
                            var bytes = item.Session.ToBytes();
                            staged.Add(item.IsNew
                                ? SaveService.StageNew(item.Session.FilePath, bytes)
                                : SaveService.Stage(item.Session.FilePath, bytes));
                            saved.Add((item.Session, bytes));
                        }

                        SaveService.CommitAll(staged);
                    }
                    catch
                    {
                        foreach (var write in staged)
                            SaveService.Discard(write);
                        throw;
                    }
                }

                _session.UndoStack.MarkSaved();
                foreach (var (session, bytes) in saved)
                    session.RecordCurrentFileState(bytes);
                foreach (var item in equipment)
                {
                    var savedBytes = saved.Single(entry => ReferenceEquals(entry.Session, item.Session)).Bytes;
                    item.MarkSaved(savedBytes);
                }
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                _log.AppendLine(
                    equipment.Count == 0
                        ? $"Saved {_session.FilePath}."
                        : $"Saved {_session.FilePath} with {equipment.Count} linked item blueprint" +
                          (equipment.Count == 1 ? "." : "s."));
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        private bool ChangedWhilePreparingGroupedSave(DocumentSession session, bool isNew)
        {
            if (!session.HasExternalChange())
                return false;

            _log.AppendLine(isNew
                ? $"Cannot create {session.FilePath}: another file claimed that name while the grouped save was being prepared. Nothing was written."
                : $"Save stopped because {session.FilePath} changed while the grouped save was being prepared. Nothing was written.");
            return true;
        }

        private async Task<bool> AcceptExternalChangeAsync(
            DocumentSession session,
            bool isNew = false,
            bool isMainDocument = false)
        {
            if (!session.HasExternalChange())
                return true;

            if (isNew)
            {
                _log.AppendLine(
                    $"Cannot create {session.FilePath}: another file now uses that name. Nothing was written.");
                return false;
            }

            var choice = await _prompts.ConfirmExternalChangeAsync(session.FilePath).ConfigureAwait(true);
            if (choice == ExternalChangeChoice.Cancel)
                return false;
            if (choice == ExternalChangeChoice.Reload)
            {
                if (isMainDocument)
                {
                    session.ReloadFromDisk();
                    Editor.ReloadFromDocument();
                    AfterHistoryChange();
                    _log.AppendLine($"Reloaded {session.FilePath} after an external change.");
                    return false;
                }
                _log.AppendLine(
                    $"The linked item {session.FilePath} changed outside the toolset. Close and reopen the creature " +
                    "to reload the whole creature document safely.");
                return false;
            }

            session.RecordCurrentFileState();
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
                var choice = await _prompts.ConfirmCloseAsync(Title ?? ResRef).ConfigureAwait(true);
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

        private void UpdateTitle() => Title = IsDirty ? $"{ResRef} *" : ResRef;
    }
}
