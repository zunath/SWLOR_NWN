using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>The Output panel: a running log of catalog build progress, workspace open timing, and external file-system change notifications.</summary>
    public partial class OutputViewModel : Tool
    {
        public ObservableCollection<string> Lines { get; }

        public OutputViewModel(OutputLogService log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            Lines = log.Lines;
            Id = "Output";
            Title = "Output";
        }
    }
}
