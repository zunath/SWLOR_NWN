using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.GameCode;
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
        private readonly string _resRef;

        public ObservableCollection<EditorGroup> Groups { get; } = new();

        public VarTableSectionViewModel? VarTableSection { get; }

        public bool IsDirty => _session.UndoStack.IsDirty;

        public BlueprintEditorViewModel(
            string filePath,
            string resRef,
            EditorSchema schema,
            LookupOptionProvider lookups,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log)
        {
            _log = log;
            _resRef = resRef;
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
        private void Save()
        {
            try
            {
                var bytes = _session.Document.ToBytes();
                var temporaryPath = _session.FilePath + ".tmp";
                File.WriteAllBytes(temporaryPath, bytes);
                File.Move(temporaryPath, _session.FilePath, overwrite: true);

                _session.UndoStack.MarkSaved();
                AfterHistoryChange();
                _log.AppendLine($"Saved {_session.FilePath}.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
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

        public override bool OnClose()
        {
            _session.Dispose();
            return base.OnClose();
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
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_resRef} *" : _resRef;
        }
    }
}
