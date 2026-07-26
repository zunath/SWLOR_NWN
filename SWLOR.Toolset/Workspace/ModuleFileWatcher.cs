using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Watches a module root directory (recursively) for external changes and logs them to the
    /// Output panel. Build noise is filtered out: the packer's transient "packing" working
    /// directory, the packed .mod artifact it rewrites, and this app's own atomic-save .tmp
    /// files would otherwise flood the log with thousands of lines per pack.
    /// </summary>
    public sealed class ModuleFileWatcher : IDisposable
    {
        private readonly OutputLogService _log;
        private FileSystemWatcher? _watcher;
        private string? _packingDirectoryPrefix;

        /// <summary>The catalog to keep in step with the disk, or null in a test with none.</summary>
        private readonly WorkspaceContext? _workspaceContext;

        public ModuleFileWatcher(OutputLogService log, WorkspaceContext? workspaceContext = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _workspaceContext = workspaceContext;
        }

        /// <summary>
        /// Brings the catalog into line with a resource file that changed outside the toolset.
        /// </summary>
        /// <remarks>
        /// These handlers only logged. After the initial catalog build, a resource created, edited,
        /// renamed or deleted by anything else - a git pull being the ordinary case - left Search, the
        /// Explorer and the palette showing the old state, and a row for a deleted file failed when it
        /// was opened. The catalog already knew how to refresh and remove single entries; nothing was
        /// calling it.
        /// <para>
        /// Marshalled to the UI thread because the refresh raises events that catalog-backed panels
        /// handle, and these arrive on a watcher thread.
        /// </para>
        /// </remarks>
        private void SyncCatalog(string path, bool deleted)
        {
            if (_workspaceContext == null)
                return;

            var affectsTagIndex = AffectsTagIndex(path);
            var affectsScriptUsages = AffectsScriptUsages(path);
            var resolved = TryResolveResource(path, out var type, out var resRef);
            if (!affectsTagIndex && !affectsScriptUsages && !resolved)
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (affectsTagIndex)
                    _workspaceContext.InvalidateTagIndex();

                // Resolved scripted resources invalidate through Refresh/Remove below. GIT has no
                // ResourceType, so its placed-instance script slots need the direct path.
                if (affectsScriptUsages &&
                    (!resolved || !Domain.Script.ScriptUsageIndex.ScriptedTypes.Contains(type)))
                {
                    _workspaceContext.InvalidateScriptUsages();
                }

                if (resolved)
                {
                    if (deleted)
                        _workspaceContext.RemoveCatalogEntry(type, resRef);
                    else
                        _workspaceContext.RefreshCatalogEntry(type, resRef);
                }
            });
        }

        /// <summary>
        /// True when changing this file can alter a resolved behavior tag. Paired GITs carry placed
        /// door/waypoint tags; UTD/UTW blueprints supply their fallbacks; UTI supplies door-key tags.
        /// </summary>
        public static bool AffectsTagIndex(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.EndsWith(".git.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".are.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".utd.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".uti.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".utw.json", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when changing this file can alter Find Usages or the script picker's usage counts.
        /// Includes paired GIT files, which carry placed-instance overrides but have no ResourceType.
        /// </summary>
        public static bool AffectsScriptUsages(string path)
        {
            if (Path.GetFileName(path).EndsWith(".git.json", StringComparison.OrdinalIgnoreCase))
                return true;

            return TryResolveResource(path, out var type, out _) &&
                   Domain.Script.ScriptUsageIndex.ScriptedTypes.Contains(type);
        }

        /// <summary>
        /// Reads a module resource path as its type and resref - "…/utc/foo.utc.json" is a UTC named
        /// "foo", and "…/nss/on_enter.nss" is a script named "on_enter". False for anything that is not
        /// a module resource, which is most of what the recursive watcher sees.
        /// </summary>
        public static bool TryResolveResource(string path, out ResourceType type, out string resRef)
        {
            type = default;
            resRef = string.Empty;

            var fileName = Path.GetFileName(path);
            if (fileName.EndsWith(".nss", StringComparison.OrdinalIgnoreCase))
            {
                type = ResourceType.Nss;
                resRef = Path.GetFileNameWithoutExtension(fileName);
                return !string.IsNullOrEmpty(resRef);
            }

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return false;

            // "foo.utc.json" -> "foo.utc" -> extension "utc", ResRef "foo".
            var withoutJson = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(withoutJson).TrimStart('.');
            if (string.IsNullOrEmpty(extension) ||
                !ResourceTypeExtensions.TryFromExtension(extension, out type))
            {
                return false;
            }

            resRef = Path.GetFileNameWithoutExtension(withoutJson);
            return !string.IsNullOrEmpty(resRef);
        }

        public void Watch(string moduleRoot)
        {
            Stop();

            try
            {
                _packingDirectoryPrefix =
                    Path.Combine(Path.GetFullPath(moduleRoot), "packing") + Path.DirectorySeparatorChar;

                _watcher = new FileSystemWatcher(moduleRoot)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                };

                _watcher.Changed += (_, e) => Report($"External change detected: {e.FullPath}", e.FullPath);
                _watcher.Created += (_, e) => Report($"External file created: {e.FullPath}", e.FullPath);
                _watcher.Deleted += (_, e) => Report(
                    $"External file deleted: {e.FullPath}", deleted: true, e.FullPath);
                _watcher.Renamed += (_, e) =>
                {
                    // A rename is a delete and a create: the old resref leaves the catalog and the new
                    // one joins it, or Search keeps offering a name that no longer resolves.
                    Report($"External rename: {e.OldFullPath} -> {e.FullPath}", deleted: true, e.OldFullPath);
                    Report($"External rename: {e.OldFullPath} -> {e.FullPath}", e.FullPath);
                };
                _watcher.Error += (_, e) => _log.AppendLine($"File watcher error: {e.GetException().Message}");

                _watcher.EnableRaisingEvents = true;
                _log.AppendLine($"Watching '{moduleRoot}' for external changes.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not start file watcher on '{moduleRoot}': {ex.Message}");
            }
        }

        private void Report(string message, params string[] paths) => Report(message, false, paths);

        private void Report(string message, bool deleted, params string[] paths)
        {
            if (paths.Any(IsBuildNoise))
                return;

            _log.AppendLine(message);

            foreach (var path in paths)
                SyncCatalog(path, deleted);
        }

        /// <summary>True for paths the pack pipeline or this app churn as part of normal
        /// builds: anything under the packer's "packing" working directory (including the
        /// directory itself), packed .mod artifacts, and atomic-save temporary/rollback files.</summary>
        private bool IsBuildNoise(string path)
        {
            if (_packingDirectoryPrefix != null)
            {
                if (path.StartsWith(_packingDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
                    return true;

                var packingDirectory = _packingDirectoryPrefix.TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(path, packingDirectory, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return path.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".save-backup", StringComparison.OrdinalIgnoreCase);
        }

        public void Stop()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        public void Dispose() => Stop();
    }
}
