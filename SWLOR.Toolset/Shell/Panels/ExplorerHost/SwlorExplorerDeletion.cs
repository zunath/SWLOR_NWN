using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// SWLOR's logical delete through <see cref="ModuleResourceDeletionService"/>: an area's ARE/GIT/GIC
    /// plus its module.ifo registration, both forms of a dialog, or a script's NSS plus its NCS, under the
    /// shared module mutation lock's deletion reservation.
    /// </summary>
    internal sealed class SwlorExplorerDeletion : IModuleExplorerDeletion
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly ModuleMutationLock? _mutationLock;

        public SwlorExplorerDeletion(WorkspaceContext workspaceContext, ModuleMutationLock? mutationLock)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _mutationLock = mutationLock;
        }

        public bool CanDelete(ResourceType type) => type is ResourceType.Area or ResourceType.Dlg or ResourceType.Nss;

        public IModuleExplorerPreparedDeletion Prepare(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace
                ?? throw new InvalidOperationException("No module is open.");
            return new SwlorExplorerPreparedDeletion(ModuleResourceDeletionService.Prepare(workspace, type, resRef));
        }

        /// <summary>Without a mutation lock nothing else can hold the module, so the reservation always succeeds.</summary>
        public IDisposable? TryReserve() =>
            _mutationLock == null ? new SwlorExplorerNoReservation() : _mutationLock.TryBeginResourceDeletion();

        public void Deleted(ResourceType type, string resRef) => _workspaceContext.RemoveCatalogEntry(type, resRef);
    }
}
