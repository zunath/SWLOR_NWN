using System.Security.Cryptography;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// SWLOR's blueprint writes: JSON files under the module root, written through
    /// <see cref="SwlorFileWriteAccess"/> and published to the workspace catalog straight away.
    /// </summary>
    /// <remarks>
    /// Each write is one small atomic file, so create and copy run synchronously and hand the palette a
    /// completed task. A request already cancelled writes nothing.
    /// </remarks>
    internal sealed class SwlorPaletteBlueprintOperations : IPaletteBlueprintOperations
    {
        private const string NoModuleOpen = "no module is open";

        private readonly WorkspaceContext _workspaceContext;
        private readonly Func<Editors.EditorService>? _editorService;

        public SwlorPaletteBlueprintOperations(
            WorkspaceContext workspaceContext,
            Func<Editors.EditorService>? editorService)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _editorService = editorService;
        }

        /// <summary>Merchants stay uncreatable: their StoreList inventory is not exposed by the editor.</summary>
        public bool CanCreate(ResourceType type) => BlueprintTemplateFactory.Supports(type);

        /// <summary>Every module blueprint file can be deleted; the guards live in the deletion itself.</summary>
        public bool CanDelete(ResourceType type) => true;

        /// <summary>
        /// Writes a blueprint built from the type's editor schema plus whatever every real blueprint of
        /// that type carries (see <see cref="BlueprintTemplateFactory"/>), so it opens as a complete object.
        /// </summary>
        public Task<PaletteBlueprintCreation> CreateAsync(
            ResourceType type,
            string resRef,
            string name,
            CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<PaletteBlueprintCreation>(cancellationToken)
                : Task.FromResult(Create(type, resRef, name));

        public Task<PaletteBlueprintCopy> CopyAsync(
            ResourceType type,
            SharedPaletteSource source,
            string resRef,
            CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<PaletteBlueprintCopy>(cancellationToken)
                : Task.FromResult(Copy(type, source, resRef));

        private PaletteBlueprintCreation Create(ResourceType type, string resRef, string name)
        {
            if (_workspaceContext.Workspace is not { } workspace)
                return PaletteBlueprintCreation.Failed(NoModuleOpen);
            var path = workspace.GetResourcePath(type, resRef);
            if (File.Exists(path))
                return PaletteBlueprintCreation.AlreadyExists();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SwlorFileWriteAccess.Writer.WriteNewAtomic(
                    path, BlueprintTemplateFactory.CreateFileContent(type, resRef, name));
            }
            catch (Exception ex)
            {
                return PaletteBlueprintCreation.Failed(ex.Message);
            }

            // Into the catalog straight away. It is a persistent snapshot, so without this the new
            // blueprint is missing from Explorer and Search, and the palette shows its resref instead of
            // the name that was just typed.
            _workspaceContext.RefreshCatalogEntry(type, resRef);
            return PaletteBlueprintCreation.Created(path);
        }

        private PaletteBlueprintCopy Copy(ResourceType type, SharedPaletteSource source, string resRef)
        {
            if (_workspaceContext.Workspace is not { } workspace)
                return PaletteBlueprintCopy.Failed(NoModuleOpen);

            string copyResRef;
            string copyPath;
            try
            {
                copyResRef = BlueprintCopyFactory.NextResRef(workspace, type, resRef);
                copyPath = workspace.GetResourcePath(type, copyResRef);

                var original = source == SharedPaletteSource.Standard
                    ? workspace.LoadIndexedBlueprint(type, resRef)
                    : workspace.LoadBlueprint(type, resRef);
                var content = BlueprintCopyFactory.CreateFileContent(type, original.Document, copyResRef);

                Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
                SwlorFileWriteAccess.Writer.WriteNewAtomic(copyPath, content);
            }
            catch (Exception ex)
            {
                return PaletteBlueprintCopy.Failed(ex.Message);
            }

            _workspaceContext.RefreshCatalogEntry(type, copyResRef);
            return PaletteBlueprintCopy.Copied(copyResRef, copyPath);
        }

        /// <summary>
        /// Fingerprints the blueprint file before the confirmation, so the delete applies only to the
        /// generation the builder reviewed.
        /// </summary>
        public PaletteBlueprintDeletePreparation PrepareDelete(ResourceType type, string resRef)
        {
            if (_workspaceContext.Workspace is not { } workspace)
                return PaletteBlueprintDeletePreparation.Refused(NoModuleOpen, NoModuleOpen);
            var path = workspace.GetResourcePath(type, resRef);

            byte[] expectedBlueprintHash;
            try
            {
                expectedBlueprintHash = SHA256.HashData(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                return PaletteBlueprintDeletePreparation.Refused(
                    $"could not fingerprint its blueprint ({ex.Message}).", ex.Message);
            }

            return PaletteBlueprintDeletePreparation.Ready(new SwlorPaletteBlueprintDeletion(
                _workspaceContext, workspace, type, resRef, path, expectedBlueprintHash));
        }

        public bool IsOpenInEditor(ResourceType type, string resRef) =>
            _editorService?.Invoke().IsOpen(type, resRef) == true;

        public void OpenEditor(ResourceType type, string resRef) =>
            _editorService?.Invoke().TryOpenEditor(type, resRef);
    }
}
