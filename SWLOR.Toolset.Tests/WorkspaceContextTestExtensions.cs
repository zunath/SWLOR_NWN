using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    internal static class WorkspaceContextTestExtensions
    {
        /// <summary>
        /// Opens <paramref name="moduleRoot"/> and then waits for every background scan
        /// <see cref="WorkspaceContext.Open"/> starts (the catalog build, placement index warm-up,
        /// and transition-tag index). Those scans read module files with a share mode that blocks
        /// deletion, so a test that removes its scratch module right after opening must let them
        /// finish first, otherwise teardown intermittently fails with an IOException.
        /// </summary>
        public static void OpenAndSettle(this WorkspaceContext context, string moduleRoot)
        {
            context.Open(moduleRoot);
            context.WaitForBackgroundScans();
        }

        public static void WaitForBackgroundScans(this WorkspaceContext context)
        {
            var workspace = context.Workspace;
            if (workspace == null)
                return;

            context.Catalog?.BuildTask.GetAwaiter().GetResult();
            workspace.PlacementIndex.WarmAsync().GetAwaiter().GetResult();
            workspace.TagIndex.GetTransitionDestinationTagsAsync().GetAwaiter().GetResult();
        }
    }
}
