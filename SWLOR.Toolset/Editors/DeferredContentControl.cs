using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// A content host whose template is not realized until its surface is active. Avalonia keeps
    /// ordinary hidden ContentControls in the visual tree, so complex editors otherwise pay to
    /// construct every behavior panel even though the writer can see only one.
    /// </summary>
    public sealed class DeferredContentControl : ContentControl
    {
        public static readonly StyledProperty<object?> DeferredContentProperty =
            AvaloniaProperty.Register<DeferredContentControl, object?>(nameof(DeferredContent));

        public static readonly StyledProperty<IDataTemplate?> DeferredTemplateProperty =
            AvaloniaProperty.Register<DeferredContentControl, IDataTemplate?>(nameof(DeferredTemplate));

        public static readonly StyledProperty<bool> IsContentActiveProperty =
            AvaloniaProperty.Register<DeferredContentControl, bool>(nameof(IsContentActive));

        private bool _refreshing;

        public object? DeferredContent
        {
            get => GetValue(DeferredContentProperty);
            set => SetValue(DeferredContentProperty, value);
        }

        public IDataTemplate? DeferredTemplate
        {
            get => GetValue(DeferredTemplateProperty);
            set => SetValue(DeferredTemplateProperty, value);
        }

        public bool IsContentActive
        {
            get => GetValue(IsContentActiveProperty);
            set => SetValue(IsContentActiveProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (_refreshing)
                return;

            if (change.Property == DeferredContentProperty
                || change.Property == DeferredTemplateProperty
                || change.Property == IsContentActiveProperty)
            {
                RefreshContent();
            }
        }

        private void RefreshContent()
        {
            _refreshing = true;
            try
            {
                if (IsContentActive)
                {
                    ContentTemplate = DeferredTemplate;
                    Content = DeferredContent;
                }
                else
                {
                    Content = null;
                    ContentTemplate = null;
                }
            }
            finally
            {
                _refreshing = false;
            }
        }
    }
}
