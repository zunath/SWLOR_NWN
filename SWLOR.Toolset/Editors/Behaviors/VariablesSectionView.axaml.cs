using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>The raw local-variable grid shown on every behavior editor's Variables tab.</summary>
    public partial class VariablesSectionView : UserControl
    {
        public VariablesSectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
