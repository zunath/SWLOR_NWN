using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>The identity strip above every behavior editor's tabs.</summary>
    public partial class BehaviorEditorHeader : UserControl
    {
        public static readonly StyledProperty<string?> BehaviorNameProperty =
            AvaloniaProperty.Register<BehaviorEditorHeader, string?>(nameof(BehaviorName));

        public static readonly StyledProperty<string?> KindProperty =
            AvaloniaProperty.Register<BehaviorEditorHeader, string?>(nameof(Kind));

        public static readonly StyledProperty<string?> OwnerProperty =
            AvaloniaProperty.Register<BehaviorEditorHeader, string?>(nameof(Owner));

        public static readonly StyledProperty<bool> IsDirtyProperty =
            AvaloniaProperty.Register<BehaviorEditorHeader, bool>(nameof(IsDirty));

        /// <summary>The selected behavior's name, which is what the object actually is.</summary>
        public string? BehaviorName
        {
            get => GetValue(BehaviorNameProperty);
            set => SetValue(BehaviorNameProperty, value);
        }

        /// <summary>"blueprint" or "instance".</summary>
        public string? Kind
        {
            get => GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }

        /// <summary>The file this object lives in — its own ResRef, or its area's.</summary>
        public string? Owner
        {
            get => GetValue(OwnerProperty);
            set => SetValue(OwnerProperty, value);
        }

        public bool IsDirty
        {
            get => GetValue(IsDirtyProperty);
            set => SetValue(IsDirtyProperty, value);
        }

        public BehaviorEditorHeader()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
