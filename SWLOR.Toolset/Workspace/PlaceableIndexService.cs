using Avalonia.Threading;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Owns the two module-wide scans the placeable editor needs: which object tags exist, and how
    /// many blueprints use each appearance row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both cost a pass over thousands of files, so they run once per opened module on a background
    /// thread and every consumer starts with the empty index. That is why both indexes report
    /// whether they were actually built: an empty tag index must never be read as "this destination
    /// does not exist", and an empty usage index must not make the model grid look empty.
    /// </para>
    /// <para>
    /// <see cref="Updated"/> fires on the UI thread when a scan lands, so open editors can re-run
    /// their validation with the real answer.
    /// </para>
    /// </remarks>
    public sealed class PlaceableIndexService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly object _gate = new();
        private string? _builtFor;
        private bool _building;

        public PlaceableIndexService(WorkspaceContext workspaceContext)
        {
            _workspaceContext = workspaceContext;
        }

        public ModuleTagIndex? Tags { get; private set; }

        public PlaceableAppearanceUsageIndex Usage { get; private set; } = PlaceableAppearanceUsageIndex.Empty;

        /// <summary>Raised on the UI thread once a scan has replaced the empty indexes.</summary>
        public event Action? Updated;

        /// <summary>
        /// Starts the scan for the open module if it has not run yet. Safe to call from every editor
        /// that opens; only the first one does the work.
        /// </summary>
        public void EnsureBuilt()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            lock (_gate)
            {
                if (_building || string.Equals(_builtFor, workspace.ModuleRoot, StringComparison.OrdinalIgnoreCase))
                    return;

                _building = true;
            }

            Task.Run(() =>
            {
                ModuleTagIndex? tags;
                PlaceableAppearanceUsageIndex usage;

                try
                {
                    // Shares the workspace's tag index rather than scanning the module twice for
                    // the same answer; touching Tags is what warms its cache off the UI thread.
                    tags = workspace.TagIndex;
                    _ = tags.Tags;
                    usage = PlaceableAppearanceUsageIndex.Build(workspace);
                }
                catch (Exception)
                {
                    // A failed scan leaves both empty, which every consumer already treats as
                    // "cannot tell" rather than "nothing exists".
                    tags = null;
                    usage = PlaceableAppearanceUsageIndex.Empty;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    Tags = tags;
                    Usage = usage;

                    lock (_gate)
                    {
                        _builtFor = workspace.ModuleRoot;
                        _building = false;
                    }

                    Updated?.Invoke();
                });
            });
        }
    }
}
