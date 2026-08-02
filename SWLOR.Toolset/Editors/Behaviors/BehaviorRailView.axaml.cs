using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// The behavior list every behavior-shaped editor puts down its left side.
    /// </summary>
    /// <remarks>
    /// The rows and the command are properties rather than bindings against a shared view-model
    /// interface, because each editor owns a differently typed <c>ChooseBehaviorCommand</c> and the
    /// toolset compiles its bindings.
    /// </remarks>
    public partial class BehaviorRailView : UserControl
    {
        public static readonly StyledProperty<IEnumerable?> ItemsProperty =
            AvaloniaProperty.Register<BehaviorRailView, IEnumerable?>(nameof(Items));

        public static readonly StyledProperty<ICommand?> ChooseCommandProperty =
            AvaloniaProperty.Register<BehaviorRailView, ICommand?>(nameof(ChooseCommand));

        /// <summary>The rail's rows: headings, dividers, and behaviors.</summary>
        public IEnumerable? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        /// <summary>Invoked with the picked <see cref="Domain.Editors.Behaviors.IBehaviorDescriptor"/>.</summary>
        public ICommand? ChooseCommand
        {
            get => GetValue(ChooseCommandProperty);
            set => SetValue(ChooseCommandProperty, value);
        }

        public BehaviorRailView()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
