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

        /// <summary>Set when content changed mid-scan, so the stale result is followed by a fresh one.</summary>
        private bool _rescanWhenIdle;

        public PlaceableIndexService(WorkspaceContext workspaceContext)
        {
            _workspaceContext = workspaceContext;

            // A blueprint that was created, deleted, or saved with a different Appearance changes
            // both answers this service holds. Without this the first scan stood as permanently
            // current for the module root: usage counts stayed at whatever they were when the
            // editor first opened, and the placeable grid's default "used in module" filter kept
            // offering a model nothing used any more while omitting the one just placed.
            _workspaceContext.CatalogEntryRefreshed += (type, _) =>
            {
                if (type is ResourceType.Utp or ResourceType.Utc or ResourceType.Utd or ResourceType.Utw)
                    Invalidate();
            };

            _workspaceContext.WorkspaceOpened += Invalidate;
        }

        /// <summary>
        /// Drops the built indexes and rescans, if anything is listening. A scan already in flight is
        /// left to finish - it will publish, and this will then be re-run against the newer content.
        /// </summary>
        public void Invalidate()
        {
            // The workspace's tag index caches its own dictionaries for the life of the workspace,
            // so clearing only this service's handle would hand the next scan the same stale answer.
            _workspaceContext.Workspace?.TagIndex.Invalidate();

            lock (_gate)
            {
                _builtFor = null;

                // Mid-scan: the in-flight pass is about to claim the root as built. Let it, and
                // rebuild afterwards rather than racing it here.
                if (_building)
                {
                    _rescanWhenIdle = true;
                    return;
                }
            }

            EnsureBuilt();
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

                    bool rescan;
                    lock (_gate)
                    {
                        _builtFor = workspace.ModuleRoot;
                        _building = false;
                        rescan = _rescanWhenIdle;
                        _rescanWhenIdle = false;
                    }

                    Updated?.Invoke();

                    // Content changed while this pass was reading; what it published is already
                    // one edit behind.
                    if (rescan)
                        Invalidate();
                });
            });
        }
    }
}
