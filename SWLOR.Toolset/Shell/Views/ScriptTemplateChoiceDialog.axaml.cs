using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Shell.Views
{
    /// <summary>Small modal picker for the source shape of a brand-new .nss file.</summary>
    public partial class ScriptTemplateChoiceDialog : Window
    {
        public ScriptTemplateChoiceDialog()
        {
            InitializeComponent();
        }

        private void OnCreateClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Confirm();

        private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

        private void OnTemplateDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e) => Confirm();

        private void Confirm()
        {
            if (TemplateList.SelectedItem is ScriptTemplateDefinition template)
                Close(template.Id);
        }

        public static Task<string?> ShowAsync(IReadOnlyList<ScriptTemplateDefinition> templates)
        {
            try
            {
                var owner = (Avalonia.Application.Current?.ApplicationLifetime
                    as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner == null || templates.Count == 0)
                    return Task.FromResult<string?>(null);

                var dialog = new ScriptTemplateChoiceDialog();
                dialog.TemplateList.ItemsSource = templates;
                dialog.TemplateList.SelectedItem = templates[0];
                dialog.Opened += (_, _) => dialog.TemplateList.Focus();
                return dialog.ShowDialog<string?>(owner);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }
    }
}
