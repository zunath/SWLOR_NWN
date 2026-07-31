using Avalonia;
using Avalonia.Controls;

namespace SWLOR.Toolset.Editors.Behaviors
{
    public sealed partial class SearchableChoicePickerView : UserControl
    {
        public static readonly StyledProperty<bool> CompactProperty =
            AvaloniaProperty.Register<SearchableChoicePickerView, bool>(nameof(Compact));

        public bool Compact
        {
            get => GetValue(CompactProperty);
            set => SetValue(CompactProperty, value);
        }

        public SearchableChoicePickerView()
        {
            InitializeComponent();
        }
    }
}
