using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>One prepared <see cref="ModuleResourceDeletionPlan"/>, committed on a worker thread.</summary>
    internal sealed class SwlorExplorerPreparedDeletion : IModuleExplorerPreparedDeletion
    {
        private readonly ModuleResourceDeletionPlan _plan;

        public SwlorExplorerPreparedDeletion(ModuleResourceDeletionPlan plan)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        /// <summary>
        /// The delete owns the shared module-operation lock. Its worker alone may perform the guarded
        /// filesystem writes while every editor, save and open route stays blocked until the panel finishes
        /// its catalog cleanup.
        /// </summary>
        public ModuleExplorerDeletionResult Commit()
        {
            using var allowance = ModuleMutationLock.AllowModuleWrites();
            var result = ModuleResourceDeletionService.Commit(_plan);
            return new ModuleExplorerDeletionResult(result.DeletedPaths, result.CleanupWarnings);
        }
    }
}
