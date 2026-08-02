using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// A single, app-wide log of lines shown in the Output panel: catalog build progress, workspace
    /// open timing, and external file-system change notifications. Appends always marshal onto the
    /// UI thread so any background caller (the catalog build, the file watcher) can log directly.
    /// Every line is also written to the console/debug output, so headless verification runs (no
    /// UI interaction) can still observe startup timing and progress.
    /// </summary>
    public sealed class OutputLogService
    {
        public ObservableCollection<string> Lines { get; } = new();

        public void AppendLine(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";

            Console.WriteLine(line);
            System.Diagnostics.Debug.WriteLine(line);

            if (Dispatcher.UIThread.CheckAccess())
            {
                Lines.Add(line);
            }
            else
            {
                Dispatcher.UIThread.Post(() => Lines.Add(line));
            }
        }
    }
}
