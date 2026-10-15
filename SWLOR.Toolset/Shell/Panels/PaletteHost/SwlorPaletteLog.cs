using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>The Output panel's log.</summary>
    internal sealed class SwlorPaletteLog : IPaletteLog
    {
        private readonly OutputLogService _log;

        public SwlorPaletteLog(OutputLogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Write(string message) => _log.AppendLine(message);
    }
}
