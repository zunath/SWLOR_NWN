using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>A titled group of field view models.</summary>
    public sealed record EditorGroup(string Title, IReadOnlyList<FieldViewModel> Fields);

    /// <summary>
    /// The generic schema-driven blueprint editor, docked as a document tab. Every mutation
    /// flows through a one-step DocumentTransaction on the session's undo stack; Save writes
    /// the document bytes atomically and marks the stack clean.
    /// </summary>
    public partial class BlueprintEditorViewModel : Document
    {
        private readonly DocumentSession _session;
        private readonly EditorFieldContext _context;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public ObservableCollection<EditorGroup> Groups { get; } = new();

        public VarTableSectionViewModel? VarTableSection { get; private set; }

        public bool IsDirty => _session.UndoStack.IsDirty;

        /// <summary>This blueprint's resource type — lets the model preview resolve its appearance.</summary>
        public ResourceType BlueprintType { get; }

        /// <summary>The live (possibly unsaved) document root, for appearance-driven model preview.</summary>
        public JsonGffStruct DocumentRoot => _session.Document.Root;

        /// <summary>Raised after every edit/undo/redo/save so the preview can re-resolve the model.</summary>
        public event Action? DocumentChanged;

        public BlueprintEditorViewModel(
            string filePath,
            string resRef,
            ResourceType type,
            EditorSchema schema,
            LookupOptionProvider lookups,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts)
        {
            _log = log;
            _prompts = prompts;
            _gameCodeIndex = gameCodeIndex;
            _resRef = resRef;
            BlueprintType = type;
            Id = $"editor:{filePath}";
            _session = DocumentSession.Open(filePath);
            _context = new EditorFieldContext(_session.Document, RunEdit);

            foreach (var group in schema.Groups)
            {
                var fields = group.Fields.Select(descriptor => CreateFieldViewModel(descriptor, lookups)).ToList();
                Groups.Add(new EditorGroup(group.Title, fields));
            }

            if (schema.HasVarTable)
            {
                VarTableSection = new VarTableSectionViewModel(
                    _context, new VarTable(_session.Document.Root), gameCodeIndex);
            }

            UpdateTitle();
        }

        private FieldViewModel CreateFieldViewModel(FieldDescriptor descriptor, LookupOptionProvider lookups)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, _context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, _context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, _context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, _context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, _context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, _context),
                _ => new TextFieldViewModel(descriptor, _context)
            };
        }

        private bool RunEdit(string description, Action mutation)
        {
            try
            {
                using (_session.Begin(description))
                {
                    mutation();
                }

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
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        /// <summary>Saves this editor, returning false when the user cancels or the write fails.</summary>
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
                        RecreateVarTableSection();
                        RefreshAllFields();
                        AfterHistoryChange();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }
                }

                Services.SaveService.WriteAtomic(_session.FilePath, _session.Document.ToBytes());
                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState();
                AfterHistoryChange();
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
        private void Undo()
        {
            _session.UndoStack.Undo();
            RefreshAllFields();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            _session.UndoStack.Redo();
            RefreshAllFields();
            AfterHistoryChange();
        }

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<BlueprintEditorViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<BlueprintEditorViewModel>? CloseRequested;

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

        private void RecreateVarTableSection()
        {
            if (VarTableSection == null)
                return;

            VarTableSection = new VarTableSectionViewModel(
                _context, new VarTable(_session.Document.Root), _gameCodeIndex);
            OnPropertyChanged(nameof(VarTableSection));
        }

        private void RefreshAllFields()
        {
            foreach (var group in Groups)
            foreach (var field in group.Fields)
                field.RefreshFromDocument();

            VarTableSection?.RefreshFromDocument();
        }

        private void AfterHistoryChange()
        {
            UpdateTitle();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsDirty));
            DocumentChanged?.Invoke();
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_resRef} *" : _resRef;
        }
    }
}
