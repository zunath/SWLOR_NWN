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

        public ModuleFileWatcher(OutputLogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
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
                _watcher.Deleted += (_, e) => Report($"External file deleted: {e.FullPath}", e.FullPath);
                _watcher.Renamed += (_, e) => Report($"External rename: {e.OldFullPath} -> {e.FullPath}", e.FullPath, e.OldFullPath);
                _watcher.Error += (_, e) => _log.AppendLine($"File watcher error: {e.GetException().Message}");

                _watcher.EnableRaisingEvents = true;
                _log.AppendLine($"Watching '{moduleRoot}' for external changes.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not start file watcher on '{moduleRoot}': {ex.Message}");
            }
        }

        private void Report(string message, params string[] paths)
        {
            if (paths.Any(IsBuildNoise))
                return;

            _log.AppendLine(message);
        }

        /// <summary>True for paths the pack pipeline or this app churn as part of normal
        /// builds: anything under the packer's "packing" working directory (including the
        /// directory itself), packed .mod artifacts, and atomic-save .tmp files.</summary>
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
                || path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
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
