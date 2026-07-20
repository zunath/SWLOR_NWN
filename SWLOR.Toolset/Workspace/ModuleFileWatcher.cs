namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Watches a module root directory (recursively) for external changes and logs them to the
    /// Output panel. This package is read-only (no file writes anywhere but settings.json), so
    /// this is purely informational for now - reload prompts land with instance editing in a
    /// later work package.
    /// </summary>
    public sealed class ModuleFileWatcher : IDisposable
    {
        private readonly OutputLogService _log;
        private FileSystemWatcher? _watcher;

        public ModuleFileWatcher(OutputLogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Watch(string moduleRoot)
        {
            Stop();

            try
            {
                _watcher = new FileSystemWatcher(moduleRoot)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                };

                _watcher.Changed += (_, e) => _log.AppendLine($"External change detected: {e.FullPath}");
                _watcher.Created += (_, e) => _log.AppendLine($"External file created: {e.FullPath}");
                _watcher.Deleted += (_, e) => _log.AppendLine($"External file deleted: {e.FullPath}");
                _watcher.Renamed += (_, e) => _log.AppendLine($"External rename: {e.OldFullPath} -> {e.FullPath}");
                _watcher.Error += (_, e) => _log.AppendLine($"File watcher error: {e.GetException().Message}");

                _watcher.EnableRaisingEvents = true;
                _log.AppendLine($"Watching '{moduleRoot}' for external changes.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not start file watcher on '{moduleRoot}': {ex.Message}");
            }
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
