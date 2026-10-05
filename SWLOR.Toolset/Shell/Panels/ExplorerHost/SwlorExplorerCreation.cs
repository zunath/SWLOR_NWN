using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// SWLOR's "New ..." for Module Contents. Areas open the new-area wizard (it writes the ARE/GIT/GIC
    /// triplet and registers it in module.ifo); dialogs are written as conversation graphs and scripts from
    /// a chosen template, through SWLOR's atomic module writer.
    /// </summary>
    internal sealed class SwlorExplorerCreation : IModuleExplorerCreation
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly IEditorPromptService? _prompts;

        public SwlorExplorerCreation(
            WorkspaceContext workspaceContext,
            TilesetCatalog? tilesetCatalog,
            IEditorPromptService? prompts)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _tilesetCatalog = tilesetCatalog;
            _prompts = prompts;
        }

        /// <summary>
        /// Every kind here can be created. Dialogs were the exception while they had no editor - a blank
        /// DLG the toolset could not open was an unusable resource - and the conversation editor lifted that.
        /// </summary>
        public bool CanCreate(ResourceType type) => type is ResourceType.Area or ResourceType.Dlg or ResourceType.Nss;

        public ModuleExplorerCreationMode Mode(ResourceType type) =>
            type == ResourceType.Area ? ModuleExplorerCreationMode.Form : ModuleExplorerCreationMode.NamePrompt;

        public string ToResRef(ResourceType type, string name) => ModuleResourceTemplateFactory.ToResRef(name);

        /// <summary>A dialog's resref is taken by either its graph or a legacy DLG of the same name.</summary>
        public bool Exists(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return false;

            return File.Exists(PathFor(workspace, type, resRef)) ||
                   (type == ResourceType.Dlg && File.Exists(workspace.GetResourcePath(ResourceType.Dlg, resRef)));
        }

        public async Task<ModuleExplorerCreationOptions?> ChooseOptionsAsync(ResourceType type)
        {
            if (type != ResourceType.Nss)
                return ModuleExplorerCreationOptions.None;

            if (_prompts == null)
                return null;

            var templateId = await _prompts.PromptForScriptTemplateAsync(ModuleResourceTemplateFactory.ScriptTemplates)
                .ConfigureAwait(true);
            return string.IsNullOrWhiteSpace(templateId) ? null : new ModuleExplorerCreationOptions(templateId);
        }

        public PaletteOperationResult Create(
            ResourceType type, string resRef, string name, ModuleExplorerCreationOptions options)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return PaletteOperationResult.Failed(string.Empty);

            var path = PathFor(workspace, type, resRef);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SwlorFileWriteAccess.Writer.WriteNewAtomic(
                    path,
                    type == ResourceType.Dlg
                        ? ConversationGraphTemplateFactory.CreateFileContent(resRef, name)
                        : ModuleResourceTemplateFactory.CreateFileContent(type, resRef, name, options.TemplateId));
            }
            catch (Exception ex)
            {
                return PaletteOperationResult.Failed(ex.Message);
            }

            return PaletteOperationResult.Ok();
        }

        public IAreaCreationFormState? OpenForm(ResourceType type, ModuleExplorerFormCallbacks callbacks)
        {
            ArgumentNullException.ThrowIfNull(callbacks);
            var workspace = _workspaceContext.Workspace;
            if (type != ResourceType.Area || workspace == null)
                return null;

            return new NewAreaViewModel(
                workspace,
                _tilesetCatalog,
                callbacks.Created,
                callbacks.Cancelled,
                callbacks.CanWrite);
        }

        public void Created(ResourceType type, string resRef)
        {
            _workspaceContext.RefreshCatalogEntry(type, resRef);
            if (type == ResourceType.Area)
                _workspaceContext.InvalidatePlacementIndex();
        }

        private static string PathFor(Domain.Workspace.ModuleWorkspace workspace, ResourceType type, string resRef) =>
            type == ResourceType.Dlg
                ? workspace.GetConversationGraphPath(resRef)
                : workspace.GetResourcePath(type, resRef);
    }
}
