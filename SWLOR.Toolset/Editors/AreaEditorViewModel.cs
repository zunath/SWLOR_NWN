using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The composite area editor, docked as a document tab: the area's static properties (the
    /// .are file, schema-driven like BlueprintEditorViewModel) alongside one expandable section
    /// per placed-instance list in the paired .git file (Creatures/Placeables/Doors/Waypoints/
    /// Stores/Sounds/Triggers).
    /// </summary>
    /// <remarks>
    /// Owns two independent DocumentSessions - one per file - because the .are and .git files
    /// are separate nwn_gff documents with separate undo histories. Undo/Redo is deliberately
    /// split rather than merged into one combined stack: the area-properties group gets its own
    /// small Undo/Redo pair (mirroring BlueprintEditorViewModel), while the toolbar's primary
    /// Undo/Redo acts on the .git session, since instance placement/deletion is the editing this
    /// screen is mostly used for. Save writes whichever session(s) are dirty; the title's dirty
    /// marker reflects either session being dirty.
    /// </remarks>
    public partial class AreaEditorViewModel : Document
    {
        private static readonly (string Title, string ListFieldName, ResourceType BlueprintType)[] InstanceListConfigs =
        {
            ("Creatures", "Creature List", ResourceType.Utc),
            ("Placeables", "Placeable List", ResourceType.Utp),
            ("Doors", "Door List", ResourceType.Utd),
            ("Waypoints", "WaypointList", ResourceType.Utw),
            ("Stores", "StoreList", ResourceType.Utm),
            ("Sounds", "SoundList", ResourceType.Uts),
            ("Triggers", "TriggerList", ResourceType.Utt)
        };

        private readonly DocumentSession _areSession;
        private readonly DocumentSession _gitSession;
        private readonly OutputLogService _log;
        private readonly string _areResRef;

        public ObservableCollection<EditorGroup> AreaPropertyGroups { get; } = new();

        public ObservableCollection<InstanceListSectionViewModel> Sections { get; } = new();

        public bool IsDirty => _areSession.UndoStack.IsDirty || _gitSession.UndoStack.IsDirty;

        public AreaEditorViewModel(
            string areResRef,
            ModuleWorkspace workspace,
            LookupOptionProvider lookups,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log)
        {
            _log = log;
            _areResRef = areResRef;
            Id = $"area-editor:{areResRef}";

            var arePath = workspace.GetResourcePath(ResourceType.Area, areResRef);
            var gitPath = Path.Combine(workspace.ModuleRoot, "git", areResRef + ".git.json");

            _areSession = DocumentSession.Open(arePath);
            _gitSession = DocumentSession.Open(gitPath);

            var areContext = new EditorFieldContext(_areSession.Document, RunAreEdit);
            foreach (var group in AreSchema.Build().Groups)
            {
                var fields = group.Fields.Select(descriptor => CreateFieldViewModel(descriptor, areContext, lookups)).ToList();
                AreaPropertyGroups.Add(new EditorGroup(group.Title, fields));
            }

            foreach (var config in InstanceListConfigs)
            {
                Sections.Add(new InstanceListSectionViewModel(
                    config.Title, config.ListFieldName, config.BlueprintType,
                    _gitSession, workspace, RunGitEdit, gameCodeIndex, log));
            }

            UpdateTitle();
        }

        private static FieldViewModel CreateFieldViewModel(
            FieldDescriptor descriptor, EditorFieldContext context, LookupOptionProvider lookups)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, context),
                _ => new TextFieldViewModel(descriptor, context)
            };
        }

        private bool RunAreEdit(string description, Action mutation) => RunEdit(_areSession, description, mutation);

        private bool RunGitEdit(string description, Action mutation) => RunEdit(_gitSession, description, mutation);

        private bool RunEdit(DocumentSession session, string description, Action mutation)
        {
            try
            {
                using (session.Begin(description))
                    mutation();

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
            SaveSession(_areSession);
            SaveSession(_gitSession);
            AfterHistoryChange();
        }

        private void SaveSession(DocumentSession session)
        {
            if (!session.UndoStack.IsDirty)
                return;

            try
            {
                var bytes = session.Document.ToBytes();
                var temporaryPath = session.FilePath + ".tmp";
                File.WriteAllBytes(temporaryPath, bytes);
                File.Move(temporaryPath, session.FilePath, overwrite: true);

                session.UndoStack.MarkSaved();
                _log.AppendLine($"Saved {session.FilePath}.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
            }
        }

        /// <summary>Undo/redo for the area-properties (.are) group's own small history.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoAre))]
        private void UndoAre()
        {
            _areSession.UndoStack.Undo();
            RefreshAreaPropertyFields();
            AfterHistoryChange();
        }

        public bool CanUndoAre => _areSession.UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedoAre))]
        private void RedoAre()
        {
            _areSession.UndoStack.Redo();
            RefreshAreaPropertyFields();
            AfterHistoryChange();
        }

        public bool CanRedoAre => _areSession.UndoStack.CanRedo;

        /// <summary>Undo/redo for the instance lists (.git) - the toolbar's primary pair, since
        /// placing/moving/removing instances is the bulk of this screen's editing.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoInstances))]
        private void UndoInstances()
        {
            _gitSession.UndoStack.Undo();
            RefreshInstanceSections();
            AfterHistoryChange();
        }

        public bool CanUndoInstances => _gitSession.UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedoInstances))]
        private void RedoInstances()
        {
            _gitSession.UndoStack.Redo();
            RefreshInstanceSections();
            AfterHistoryChange();
        }

        public bool CanRedoInstances => _gitSession.UndoStack.CanRedo;

        public override bool OnClose()
        {
            _areSession.Dispose();
            _gitSession.Dispose();
            return base.OnClose();
        }

        private void RefreshAreaPropertyFields()
        {
            foreach (var group in AreaPropertyGroups)
            foreach (var field in group.Fields)
                field.RefreshFromDocument();
        }

        private void RefreshInstanceSections()
        {
            foreach (var section in Sections)
                section.RefreshFromDocument();
        }

        private void AfterHistoryChange()
        {
            UpdateTitle();
            UndoAreCommand.NotifyCanExecuteChanged();
            RedoAreCommand.NotifyCanExecuteChanged();
            UndoInstancesCommand.NotifyCanExecuteChanged();
            RedoInstancesCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanUndoAre));
            OnPropertyChanged(nameof(CanRedoAre));
            OnPropertyChanged(nameof(CanUndoInstances));
            OnPropertyChanged(nameof(CanRedoInstances));
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_areResRef} *" : _areResRef;
        }
    }
}
