using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Browses one palette (.itp) tree for a blueprint type and lets the caller pick a leaf's resref
    /// to place (<c>onResRefChosen</c>), or cancel (<c>onCancelled</c>) - both close the host's popup.
    /// </summary>
    /// <remarks>
    /// Strictly read-only. Categories are organized in the Palette panel, whose authority is the
    /// toolset's own category sidecar; this browser reads the module's legacy .itp files only because
    /// they are what an area section's Add flow has to offer. Editing them here would write an
    /// arrangement the Palette panel never reads and the game or Aurora may later overwrite.
    /// </remarks>
    public partial class PaletteBrowserViewModel : ObservableObject
    {
        private Action<string> _onResRefChosen;
        private Action _onCancelled;

        public string Title { get; }
        public ObservableCollection<PaletteNodeViewModel> Nodes { get; } = new();

        [ObservableProperty]
        private PaletteNodeViewModel? _selectedNode;

        [ObservableProperty]
        private string? _statusMessage;

        public PaletteBrowserViewModel(
            string title,
            string itpPath,
            Action<string> onResRefChosen,
            Action onCancelled,
            OutputLogService log,
            Func<uint, string?>? resolveStrRef = null)
        {
            Title = title;
            _onResRefChosen = onResRefChosen;
            _onCancelled = onCancelled;

            try
            {
                foreach (var node in ItpDocument.Load(itpPath).Nodes.Where(n => n.DeleteMe != true))
                    Nodes.Add(new PaletteNodeViewModel(node, resolveStrRef));
            }
            catch (Exception ex)
            {
                log.AppendLine($"Failed to read palette '{itpPath}': {ex.Message}");
                StatusMessage = $"This palette could not be read: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates what choosing or cancelling this already-open browser completes. The same browser
        /// can move between the Properties Add flow and the 3D Place flow while retaining its selection.
        /// </summary>
        internal void RebindCompletionActions(Action<string> onResRefChosen, Action onCancelled)
        {
            _onResRefChosen = onResRefChosen ?? throw new ArgumentNullException(nameof(onResRefChosen));
            _onCancelled = onCancelled ?? throw new ArgumentNullException(nameof(onCancelled));
        }

        partial void OnSelectedNodeChanged(PaletteNodeViewModel? value) => StatusMessage = null;

        [RelayCommand]
        private void Choose()
        {
            if (SelectedNode is { IsLeaf: true, ResRef: { } resRef })
                _onResRefChosen(resRef);
            else
                StatusMessage = "Select a blueprint (leaf) node first.";
        }

        [RelayCommand]
        private void Cancel() => _onCancelled();
    }
}
