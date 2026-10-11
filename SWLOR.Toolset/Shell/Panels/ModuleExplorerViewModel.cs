using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels.ExplorerHost;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Module Contents dock tool: the module's areas, dialogs and scripts, one tab each. The workflow -
    /// folder tree, search, folder commands, move undo/redo, drag-drop, creation filing and delete - is the
    /// shared <see cref="ModuleExplorerController"/>; this panel wires SWLOR's data and services into it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately NOT the blueprints. Creatures, placeables, items and the rest are what the Palette
    /// panel is for, and listing all 17,000 of them twice in the same window only makes the builder decide
    /// which of two trees to use. What is left is the three things the Aurora toolset kept separate from
    /// its palette for the same reason: areas, dialogs, scripts.
    /// </para>
    /// <para>
    /// SWLOR supplies module JSON enumeration and catalog names, conversation-graph dialogs and their
    /// dialogue-text search, <see cref="Domain.Categories.ModuleFolderSeeder"/>'s folder rules, the new-area
    /// wizard and script/dialog templates, <see cref="Services.ModuleResourceDeletionService"/>, its editors,
    /// the category sidecar through <see cref="CategoryService"/>, its prompts, the <c>moduleContentsTab</c>
    /// setting and the module mutation lock - one adapter each under <c>Shell/Panels/ExplorerHost</c>, plus
    /// the palette's adapters for the services both panels share.
    /// </para>
    /// </remarks>
    public class ModuleExplorerViewModel : Tool
    {
        private readonly SwlorExplorerContentSource _content;
        private readonly ModuleExplorerController _workflow;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            TilesetCatalog? tilesetCatalog = null,
            Services.IEditorPromptService? prompts = null,
            Settings.ToolsetSettings? settings = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            ArgumentNullException.ThrowIfNull(workspaceContext);
            ArgumentNullException.ThrowIfNull(properties);
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(log);

            Id = "ModuleExplorer";
            Title = "Module Contents";

            _content = new SwlorExplorerContentSource(workspaceContext);
            _workflow = new ModuleExplorerController(
                new ModuleExplorerHost(_content, new SwlorPaletteCategoryStore(categories))
                {
                    Organization = new SwlorExplorerOrganization(_content),
                    Search = new SwlorExplorerDialogueSearch(workspaceContext, editorService),
                    Creation = new SwlorExplorerCreation(workspaceContext, tilesetCatalog, prompts),
                    Editors = new SwlorExplorerEditors(editorService),
                    Deletion = new SwlorExplorerDeletion(workspaceContext, mutationLock),
                    Selection = new SwlorExplorerSelection(properties),
                    Prompts = prompts is null ? null : new SwlorPalettePrompts(prompts),
                    Settings = settings is null ? null : new SwlorExplorerSettings(settings),
                    WriteGate = mutationLock is null ? null : new SwlorPaletteWriteGate(mutationLock),
                    Log = new SwlorPaletteLog(log)
                });
        }

        /// <summary>The sections, in the order Aurora listed them: areas, dialogs, scripts.</summary>
        public static IReadOnlyList<ResourceType> Sections => SwlorExplorerContentSource.SectionTypes;

        /// <summary>The shared Module Contents workflow this panel hosts.</summary>
        public ModuleExplorerController Workflow => _workflow;

        public ObservableCollection<ExplorerNodeViewModel> Rows => _workflow.Rows;

        public ObservableCollection<ExplorerTabViewModel> Tabs => _workflow.Tabs;

        public ObservableCollection<ExplorerMoveTarget> MoveTargets => _workflow.MoveTargets;

        public ExplorerNodeViewModel? SelectedRow
        {
            get => _workflow.SelectedRow;
            set => _workflow.SelectedRow = value;
        }

        public ResourceType SelectedType
        {
            get => _workflow.SelectedType;
            set => _workflow.SelectedType = value;
        }

        public string Filter
        {
            get => _workflow.Filter;
            set => _workflow.Filter = value;
        }

        public string? StatusMessage => _workflow.StatusMessage;

        public bool IsDeletingResource => _workflow.IsDeletingResource;

        /// <summary>True while a dialogue-text scan is running.</summary>
        public bool IsSearchingDialogue => _workflow.IsSearchingContent;

        /// <summary>The new-area wizard while it is open, or null.</summary>
        public NewAreaViewModel? ActiveNewArea => _workflow.ActiveCreationForm as NewAreaViewModel;

        public string NewItemLabel => _workflow.NewItemLabel;

        public bool CanCreateSelectedType => _workflow.CanCreateSelectedType;

        public bool CanOpenSelectedType => _workflow.CanOpenSelectedType;

        public bool CanCompileSelectedType => _workflow.CanCompileSelectedType;

        public bool CanDeleteSelectedResource => _workflow.CanDeleteSelectedResource;

        public bool HasFolderSelected => _workflow.HasFolderSelected;

        public bool HasMoveTargets => _workflow.HasMoveTargets;

        public IAsyncRelayCommand NewItemCommand => _workflow.NewItemCommand;

        public IAsyncRelayCommand NewFolderCommand => _workflow.NewFolderCommand;

        public IAsyncRelayCommand RenameFolderCommand => _workflow.RenameFolderCommand;

        public IAsyncRelayCommand DeleteFolderCommand => _workflow.DeleteFolderCommand;

        public IAsyncRelayCommand DeleteSelectedResourceCommand => _workflow.DeleteSelectedResourceCommand;

        public IAsyncRelayCommand CompileSelectedCommand => _workflow.CompileSelectedCommand;

        public IRelayCommand OpenSelectedCommand => _workflow.OpenSelectedCommand;

        public IRelayCommand RemoveFromFolderCommand => _workflow.RemoveFromFolderCommand;

        public IRelayCommand UndoResourceMoveCommand => _workflow.UndoResourceMoveCommand;

        public IRelayCommand RedoResourceMoveCommand => _workflow.RedoResourceMoveCommand;

        public IRelayCommand<ExplorerNodeViewModel?> ToggleCommand => _workflow.ToggleCommand;

        public IRelayCommand<ExplorerTabViewModel?> SelectTabCommand => _workflow.SelectTabCommand;

        /// <summary>Builds the tree for the selected tab, forgetting the previous module's names and move history.</summary>
        public void Initialize()
        {
            _content.ResetCatalog();
            _workflow.Initialize();
        }

        /// <summary>Called once the background catalog publishes names, so rows can lead with them.</summary>
        public void RefreshFromCatalog(BlueprintCatalog catalog) => _content.UpdateCatalog(catalog);

        /// <summary>Rebuilds the tree, keeping which folders were open.</summary>
        public void Refresh() => _workflow.Refresh();

        /// <summary>Double-click: open a resource, or expand a folder.</summary>
        public void OpenSelectedItem() => _workflow.OpenSelectedItem();

        public bool CanDropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target) =>
            _workflow.CanDropResource(source, target);

        public bool DropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target) =>
            _workflow.DropResource(source, target);
    }
}
