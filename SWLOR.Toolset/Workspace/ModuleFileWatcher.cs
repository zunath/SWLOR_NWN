using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Watches a module root, its resource directories, and the sibling conversation source
    /// directory for external changes and logs them to the Output panel. The packer's transient
    /// "packing" and "palette-refresh" directories and
    /// item-rename transaction directories, and the NWN toolset's temp# workspaces are excluded
    /// from recursive monitoring; packed .mod artifacts and this app's atomic-save .tmp files are
    /// filtered before reporting.
    /// </summary>
    public sealed class ModuleFileWatcher : IDisposable
    {
        private readonly OutputLogService _log;
        private readonly object _watchersLock = new();
        private readonly Dictionary<string, FileSystemWatcher> _watchers =
            new(StringComparer.OrdinalIgnoreCase);
        private string? _moduleRoot;
        private string? _packingDirectoryPrefix;
        private string? _conversationDataRoot;

        /// <summary>The catalog to keep in step with the disk, or null in a test with none.</summary>
        private readonly WorkspaceContext? _workspaceContext;

        /// <summary>Coalesces a burst of watcher errors into one rescan; UI-thread only, see ScheduleRescan.</summary>
        private Avalonia.Threading.DispatcherTimer? _rescanDebounceTimer;

        private static readonly TimeSpan RescanDebounceInterval = TimeSpan.FromSeconds(1);

        public ModuleFileWatcher(OutputLogService log, WorkspaceContext? workspaceContext = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _workspaceContext = workspaceContext;
        }

        /// <summary>
        /// Raised (debounced, on the UI thread) after a watcher reports an error. The most common cause
        /// is its native event buffer overflowing - e.g. a bulk Git checkout touching more files in one
        /// burst than the OS buffer holds - which means some create/delete/rename notifications were
        /// already dropped before this fired. The owner should treat this as "the catalog and indexes
        /// may be out of sync with disk" and re-run whatever full rescan it uses at startup, since
        /// patching individual entries from here on has nothing but incomplete event history to work
        /// from.
        /// </summary>
        public event Action? RescanRequested;

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
            var affectsPlacementIndex = AffectsPlacementIndex(path);
            var affectsScriptUsages = AffectsScriptUsages(path);
            var affectsPaletteChoices = TryResolvePalette(path, out var paletteResRef);
            var resolved = TryResolveResource(path, out var type, out var resRef);
            if (!affectsTagIndex && !affectsPlacementIndex && !affectsScriptUsages &&
                !affectsPaletteChoices && !resolved)
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (affectsPlacementIndex)
                    _workspaceContext.InvalidateGitIndexes();
                else if (affectsTagIndex)
                    _workspaceContext.InvalidateTagIndex();
                if (affectsPaletteChoices)
                    _workspaceContext.InvalidatePaletteChoices(paletteResRef);

                // Resolved scripted resources invalidate through Refresh/Remove below. GIT has no
                // ResourceType, but InvalidateGitIndexes above already covers its script slots.
                if (affectsScriptUsages &&
                    !affectsPlacementIndex &&
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
        /// Debounces a burst of watcher errors into a single <see cref="RescanRequested"/>. A bulk
        /// Git checkout can overflow every recursive watcher within milliseconds of each other; without
        /// coalescing, each one would kick off its own full catalog rebuild.
        /// </summary>
        /// <remarks>
        /// <see cref="DispatcherTimer"/> must be started and stopped from the UI thread, and
        /// <see cref="System.IO.FileSystemWatcher.Error"/> arrives on a watcher thread - the same
        /// thread-marshalling reason <see cref="SyncCatalog"/> posts before touching anything.
        /// </remarks>
        private void ScheduleRescan()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_rescanDebounceTimer == null)
                {
                    _rescanDebounceTimer = new Avalonia.Threading.DispatcherTimer
                    {
                        Interval = RescanDebounceInterval
                    };
                    _rescanDebounceTimer.Tick += (_, _) =>
                    {
                        _rescanDebounceTimer!.Stop();
                        RescanRequested?.Invoke();
                    };
                }

                // Restarting rather than letting an already-running timer fire is what coalesces a
                // burst of errors: the window keeps sliding until the errors stop for one full interval.
                _rescanDebounceTimer.Stop();
                _rescanDebounceTimer.Start();
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
        /// True only when changing this file can add, remove, reorder, or move a placed instance.
        /// ARE and blueprint files affect names/tags but are not inputs to the placement scan.
        /// </summary>
        public static bool AffectsPlacementIndex(string path) =>
            Path.GetFileName(path).EndsWith(".git.json", StringComparison.OrdinalIgnoreCase);

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
        /// Reads a module ITP JSON path as its palette resref. ITP is not a first-class
        /// <see cref="ResourceType"/>, so it needs an explicit watcher path.
        /// </summary>
        public static bool TryResolvePalette(string path, out string paletteResRef)
        {
            paletteResRef = string.Empty;
            var fileName = Path.GetFileName(path);
            if (!fileName.EndsWith(".itp.json", StringComparison.OrdinalIgnoreCase))
                return false;

            paletteResRef = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(fileName));
            return !string.IsNullOrWhiteSpace(paletteResRef);
        }

        /// <summary>
        /// Reads a module resource path as its type and resref - "…/utc/foo.utc.json" is a UTC named
        /// "foo", and "…/nss/on_enter.nss" is a script named "on_enter". False for anything that is not
        /// a module resource. Graph-native <c>*.conversation.json</c> files in the sibling source
        /// directory are resolved as dialogs; false is returned for everything else.
        /// </summary>
        public static bool TryResolveResource(string path, out ResourceType type, out string resRef)
        {
            type = default;
            resRef = string.Empty;

            var fileName = Path.GetFileName(path);
            const string conversationSuffix = ".conversation.json";
            if (fileName.EndsWith(conversationSuffix, StringComparison.OrdinalIgnoreCase))
            {
                type = ResourceType.Dlg;
                resRef = fileName[..^conversationSuffix.Length];
                return !string.IsNullOrEmpty(resRef);
            }

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
                _moduleRoot = Path.GetFullPath(moduleRoot);
                _packingDirectoryPrefix =
                    Path.Combine(_moduleRoot, "packing") + Path.DirectorySeparatorChar;
                _conversationDataRoot = ModuleWorkspace.ResolveConversationDataRoot(_moduleRoot);

                // FileSystemWatcher has no directory-exclusion filter. Watching the module root
                // recursively and discarding temp# events here still lets those events overflow the
                // native watcher buffer. Watch the root itself plus each accepted child instead, so
                // the NWN toolset's temp0/temp1/... workspaces never enter a recursive watcher.
                AddWatcher(_moduleRoot, includeSubdirectories: false);
                foreach (var directory in Directory.EnumerateDirectories(_moduleRoot))
                    AddTopLevelDirectoryWatcher(directory);
                AddWatcher(_conversationDataRoot, includeSubdirectories: true);

                _log.AppendLine($"Watching '{moduleRoot}' for external changes.");
            }
            catch (Exception ex)
            {
                Stop();
                _log.AppendLine($"Could not start file watcher on '{moduleRoot}': {ex.Message}");
            }
        }

        private void AddTopLevelDirectoryWatcher(string directory)
        {
            if (IsIgnoredTopLevelDirectory(directory))
                return;

            AddWatcher(directory, includeSubdirectories: true);
        }

        private void AddWatcher(string directory, bool includeSubdirectories)
        {
            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));

            lock (_watchersLock)
            {
                if (_moduleRoot == null ||
                    _watchers.ContainsKey(fullPath) ||
                    !Directory.Exists(fullPath))
                {
                    return;
                }

                var watcher = new FileSystemWatcher(fullPath)
                {
                    IncludeSubdirectories = includeSubdirectories,
                    // LastWrite belongs on recursive resource-directory watchers only. Listening for
                    // it on the root could still receive metadata churn from an ignored temp# child.
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                   (includeSubdirectories ? NotifyFilters.LastWrite : 0)
                };

                watcher.Changed += (_, e) => Report($"External change detected: {e.FullPath}", e.FullPath);
                watcher.Created += (_, e) =>
                {
                    if (!includeSubdirectories && Directory.Exists(e.FullPath))
                        TryAddTopLevelDirectoryWatcher(e.FullPath);

                    Report($"External file created: {e.FullPath}", e.FullPath);
                };
                watcher.Deleted += (_, e) =>
                {
                    if (!includeSubdirectories)
                        HandleTopLevelDirectoryRemoved(e.FullPath);

                    Report($"External file deleted: {e.FullPath}", deleted: true, e.FullPath);
                };
                watcher.Renamed += (_, e) =>
                {
                    if (!includeSubdirectories)
                    {
                        HandleTopLevelDirectoryRemoved(e.OldFullPath);
                        if (Directory.Exists(e.FullPath))
                            TryAddTopLevelDirectoryWatcher(e.FullPath);
                    }

                    // A rename is a delete and a create: the old resref leaves the catalog and the new
                    // one joins it, or Search keeps offering a name that no longer resolves.
                    Report($"External rename: {e.OldFullPath} -> {e.FullPath}", deleted: true, e.OldFullPath);
                    Report($"External rename: {e.OldFullPath} -> {e.FullPath}", e.FullPath);
                };
                watcher.Error += (_, e) =>
                {
                    _log.AppendLine($"File watcher error: {e.GetException().Message}");
                    ScheduleRescan();
                };

                _watchers.Add(fullPath, watcher);
                watcher.EnableRaisingEvents = true;
            }
        }

        private void HandleTopLevelDirectoryRemoved(string directory)
        {
            RemoveWatcher(directory);

            // A bare directory event cannot name every resource that vanished with it. Transient
            // pack/toolset directories never contained catalog resources, though, and scheduling a
            // recovery for their routine cleanup would reopen the workspace after every pack.
            if (!IsIgnoredTopLevelDirectory(directory))
                ScheduleRescan();
        }

        private void TryAddTopLevelDirectoryWatcher(string directory)
        {
            try
            {
                AddTopLevelDirectoryWatcher(directory);

                // A directory that arrives already populated - moved or renamed into the module in one
                // atomic operation - has files the new recursive watcher above was never told about: it
                // only observes changes from here on, and Report cannot resolve a bare directory-created
                // event as a resource. Reusing the debounced rescan machinery (originally added for a
                // watcher-buffer overflow) is what brings the catalog, Explorer and Search into line with
                // everything already inside it, without teaching this path to enumerate and refresh
                // entries one by one.
                if (!IsIgnoredTopLevelDirectory(directory) &&
                    Directory.Exists(directory) &&
                    Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    ScheduleRescan();
                }
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not watch new module directory '{directory}': {ex.Message}");
            }
        }

        private void RemoveWatcher(string directory)
        {
            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            FileSystemWatcher? watcher;

            lock (_watchersLock)
            {
                if (!_watchers.Remove(fullPath, out watcher))
                    return;
            }

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        private bool IsIgnoredTopLevelDirectory(string directory)
        {
            var directoryName = Path.GetFileName(
                directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return string.Equals(directoryName, "packing", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directoryName, "palette-refresh", StringComparison.OrdinalIgnoreCase) ||
                   directoryName.StartsWith(
                       ItemRenameRecovery.TransactionPrefix,
                       StringComparison.OrdinalIgnoreCase) ||
                   IsNwnToolsetTemporaryDirectoryName(directoryName);
        }

        /// <summary>
        /// True for the numbered temporary directory names created alongside module resources by
        /// the NWN toolset, such as temp0 and temp12.
        /// </summary>
        public static bool IsNwnToolsetTemporaryDirectoryName(string directoryName)
        {
            const string prefix = "temp";
            return directoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   directoryName.Length > prefix.Length &&
                   directoryName.AsSpan(prefix.Length).IndexOfAnyExceptInRange('0', '9') < 0;
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

        /// <summary>True for paths the NWN toolset, pack pipeline, or this app churn as part of
        /// normal work: numbered toolset workspaces, anything under the packer's transient
        /// directories (including the directories themselves), packed .mod artifacts, and
        /// atomic-save temporary/rollback files.</summary>
        private bool IsBuildNoise(string path)
        {
            if (_moduleRoot != null)
            {
                var relativePath = Path.GetRelativePath(_moduleRoot, Path.GetFullPath(path));
                var firstSeparator = relativePath.IndexOfAny(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var topLevelName = firstSeparator < 0
                    ? relativePath
                    : relativePath[..firstSeparator];
                if (IsIgnoredTopLevelDirectory(topLevelName))
                    return true;
            }

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
            // A rescan debounced just before Stop() (module closed, or Watch() about to point at a
            // different module) must not fire afterwards against whatever happens to be open by then.
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _rescanDebounceTimer?.Stop());

            FileSystemWatcher[] watchers;
            lock (_watchersLock)
            {
                _moduleRoot = null;
                _packingDirectoryPrefix = null;
                _conversationDataRoot = null;
                watchers = _watchers.Values.ToArray();
                _watchers.Clear();
            }

            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }

        public void Dispose() => Stop();
    }
}
