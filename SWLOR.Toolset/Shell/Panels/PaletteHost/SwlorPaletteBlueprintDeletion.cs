using System.Security.Cryptography;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// One SWLOR blueprint file deletion, guarded by the module mutation lock, the module write lease and
    /// the fingerprint taken before the confirmation.
    /// </summary>
    /// <remarks>
    /// The write lease taken by <see cref="Commit"/> is held until disposal, so the palette's follow-up
    /// sidecar update runs under the same lease as the file delete.
    /// </remarks>
    internal sealed class SwlorPaletteBlueprintDeletion : IPaletteBlueprintDeletion
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly ModuleWorkspace _workspace;
        private readonly ResourceType _type;
        private readonly string _resRef;
        private readonly byte[] _expectedHash;
        private ModuleWriteLock? _heldLock;

        public SwlorPaletteBlueprintDeletion(
            WorkspaceContext workspaceContext,
            ModuleWorkspace workspace,
            ResourceType type,
            string resRef,
            string path,
            byte[] expectedHash)
        {
            _workspaceContext = workspaceContext;
            _workspace = workspace;
            _type = type;
            _resRef = resRef;
            Location = path;
            _expectedHash = expectedHash;
        }

        public string DisplayName => Path.GetFileName(Location);

        public string Location { get; }

        public bool IsCurrent => ReferenceEquals(_workspaceContext.Workspace, _workspace);

        public PaletteOperationResult Commit()
        {
            try
            {
                // The same guard SaveService's write paths check before touching disk, so the delete is
                // refused the instant a module-wide operation starts.
                ModuleMutationLock.ThrowIfModuleLocked();
                _heldLock = ModuleWriteLock.AcquireForResourcePath(Location);
            }
            catch (Exception ex)
            {
                return PaletteOperationResult.Failed(ex.Message);
            }

            try
            {
                if (!File.Exists(Location) ||
                    !SHA256.HashData(File.ReadAllBytes(Location))
                        .AsSpan()
                        .SequenceEqual(_expectedHash))
                {
                    throw new IOException(
                        $"{Path.GetFileName(Location)} changed while the delete confirmation was open. " +
                        "Reload the palette and try again.");
                }

                File.Delete(Location);
            }
            catch (Exception ex)
            {
                return PaletteOperationResult.Failed(ex.Message);
            }

            // Out of the catalog, or Explorer and Search keep listing a resource whose file is gone.
            _workspaceContext.RemoveCatalogEntry(_type, _resRef);
            return PaletteOperationResult.Ok();
        }

        public void Dispose()
        {
            _heldLock?.Dispose();
            _heldLock = null;
        }
    }
}
