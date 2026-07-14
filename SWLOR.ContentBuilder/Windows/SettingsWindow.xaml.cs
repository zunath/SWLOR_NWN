using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

using SWLOR.ContentBuilder.Models;
using SWLOR.ContentBuilder.Services;

namespace SWLOR.ContentBuilder.Windows
{
    /// <summary>
    /// Modal Settings dialog (File -> Settings...): NWN user directory + NWN game install directory
    /// pickers, plus a read-only "derived paths" panel showing what those resolve to via nwn.ini's
    /// [Alias] section (see NwnIniAliasResolver). OK stamps <see cref="Result"/> with the edited
    /// settings for the caller to persist (see MainWindow, which calls SettingsService.UpdateCurrent);
    /// Cancel/closing any other way leaves <see cref="Result"/> null so the caller discards the
    /// in-progress edit entirely.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ObservableCollection<AliasRow> _aliasRows = new();

        private TextBox _userDirectoryBox;
        private TextBox _installDirectoryBox;

        public ContentBuilderSettings Result { get; private set; }

        public SettingsWindow(ContentBuilderSettings current)
        {
            InitializeComponent();
            BuildContent(current);
        }

        private sealed class AliasRow
        {
            public string Alias { get; init; }
            public string Path { get; init; }
            public string Status { get; init; }
        }

        private void BuildContent(ContentBuilderSettings current)
        {
            var (_, userDirGroup) = AddGroup(RootStack, "NWN User Directory (contains nwn.ini)");
            _userDirectoryBox = AddPathRow(userDirGroup, current.NwnUserDirectory, () => BrowseFolder(_userDirectoryBox));
            _userDirectoryBox.TextChanged += (_, _) => RefreshAliasRows();

            var (_, installDirGroup) = AddGroup(RootStack, "NWN Game Install Directory (contains data\\nwn_base.key)");
            _installDirectoryBox = AddPathRow(installDirGroup, current.NwnGameInstallDirectory, () => BrowseFolder(_installDirectoryBox));

            var (_, aliasGroup) = AddGroup(RootStack, "Derived Paths (from nwn.ini's [Alias] section)");
            var aliasGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                Height = 200,
                ItemsSource = _aliasRows,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
            aliasGrid.Columns.Add(new DataGridTextColumn { Header = "Alias", Binding = new Binding(nameof(AliasRow.Alias)), Width = new DataGridLength(90) });
            aliasGrid.Columns.Add(new DataGridTextColumn { Header = "Resolved Path", Binding = new Binding(nameof(AliasRow.Path)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            aliasGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new Binding(nameof(AliasRow.Status)), Width = new DataGridLength(110) });
            aliasGroup.Children.Add(aliasGrid);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var okButton = new Button { Content = "OK", Width = 84, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(0, 4, 0, 4), IsDefault = true };
            var cancelButton = new Button { Content = "Cancel", Width = 84, Padding = new Thickness(0, 4, 0, 4), IsCancel = true };
            okButton.Click += (_, _) =>
            {
                Result = new ContentBuilderSettings
                {
                    Version = SettingsService.CurrentVersion,
                    NwnUserDirectory = _userDirectoryBox.Text.Trim(),
                    NwnGameInstallDirectory = _installDirectoryBox.Text.Trim()
                };
                DialogResult = true;
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            RootStack.Children.Add(buttonPanel);

            RefreshAliasRows();
        }

        private static (GroupBox Box, StackPanel Content) AddGroup(Panel parent, string header)
        {
            var content = new StackPanel { Margin = new Thickness(6) };
            var box = new GroupBox { Header = header, Margin = new Thickness(0, 0, 0, 10), Content = content };
            parent.Children.Add(box);
            return (box, content);
        }

        private TextBox AddPathRow(Panel parent, string initialValue, Action onBrowse)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var box = new TextBox { Text = initialValue ?? string.Empty, Margin = new Thickness(0, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center };
            Grid.SetColumn(box, 0);
            row.Children.Add(box);

            var browse = new Button { Content = "Browse...", Padding = new Thickness(8, 2, 8, 2) };
            browse.Click += (_, _) => onBrowse();
            Grid.SetColumn(browse, 1);
            row.Children.Add(browse);

            parent.Children.Add(row);
            return box;
        }

        /// <summary>
        /// Uses .NET 8 WPF's built-in Microsoft.Win32.OpenFolderDialog (no System.Windows.Forms
        /// dependency needed) so both path pickers behave like the standard Windows folder browser.
        /// </summary>
        private void BrowseFolder(TextBox target)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Directory.Exists(target.Text) ? target.Text : null
            };

            if (dialog.ShowDialog(this) == true)
                target.Text = dialog.FolderName;
        }

        private void RefreshAliasRows()
        {
            _aliasRows.Clear();
            var userDirectory = _userDirectoryBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(userDirectory))
            {
                foreach (var alias in NwnIniAliasResolver.WellKnownAliases)
                    _aliasRows.Add(new AliasRow { Alias = alias, Path = "(no user directory set)", Status = "(not found)" });
                return;
            }

            foreach (var resolution in NwnIniAliasResolver.Resolve(userDirectory))
            {
                var status = !resolution.Exists
                    ? "(not found)"
                    : resolution.FoundInIni ? "ini" : "fallback";
                _aliasRows.Add(new AliasRow
                {
                    Alias = resolution.Alias,
                    Path = string.IsNullOrEmpty(resolution.Path) ? "(not found)" : resolution.Path,
                    Status = status
                });
            }
        }
    }
}
