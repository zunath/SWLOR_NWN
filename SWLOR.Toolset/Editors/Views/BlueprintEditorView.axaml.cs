using Avalonia.Controls;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The schema-driven blueprint editor. Its Appearance tab hosts the shared appearance grid,
    /// which owns its own paging, so nothing here has to follow a scroll position any more.
    /// </summary>
    public partial class BlueprintEditorView : UserControl
    {
        public BlueprintEditorView()
        {
            InitializeComponent();
        }
    }
}
