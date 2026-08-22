using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Archives
{
    public partial class ErfArchiveWindow : Window
    {
        private static readonly FilePickerFileType ErfFileType = new("Aurora ERF archive")
        {
            Patterns = new[] { "*.erf" },
            MimeTypes = new[] { "application/octet-stream" }
        };

        private readonly ErfArchiveViewModel? _viewModel;

        public ErfArchiveWindow()
        {
            InitializeComponent();
        }

        public ErfArchiveWindow(ErfArchiveViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            Closing += OnClosing;
            Closed += (_, _) => viewModel.Dispose();
        }

        public static Task ShowAsync(ErfArchiveService service, ToolsetSettings settings)
        {
            var owner = (Avalonia.Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null)
                return Task.CompletedTask;

            var viewModel = new ErfArchiveViewModel(service, settings);
            return new ErfArchiveWindow(viewModel).ShowDialog(owner);
        }

        private async void OnBrowseArchiveClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            string? path;
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select ERF file",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { ErfFileType }
                }).ConfigureAwait(true);
                path = files.FirstOrDefault()?.TryGetLocalPath();
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Could not open the file picker: {ex.GetBaseException().Message}";
                return;
            }

            if (path == null)
                return;

            try
            {
                await _viewModel.LoadArchiveAsync(path).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Could not load '{path}': {ex.GetBaseException().Message}";
            }
        }

        private async void OnOpenRecentClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                await _viewModel.LoadRecentArchiveAsync().ConfigureAwait(true);
        }

        private void OnArchiveDragOver(object? sender, DragEventArgs e)
        {
            var file = e.DataTransfer.TryGetFiles()?.FirstOrDefault();
            var path = file?.TryGetLocalPath();
            e.DragEffects = path != null &&
                            string.Equals(Path.GetExtension(path), ".erf", StringComparison.OrdinalIgnoreCase)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnArchiveDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            if (_viewModel == null)
                return;

            var path = e.DataTransfer.TryGetFiles()?.FirstOrDefault()?.TryGetLocalPath();
            if (path != null &&
                string.Equals(Path.GetExtension(path), ".erf", StringComparison.OrdinalIgnoreCase))
            {
                await _viewModel.LoadArchiveAsync(path).ConfigureAwait(true);
            }
        }

        private async void OnImportClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                using (ModuleMutationLock.AllowModuleWrites())
                    await _viewModel.ImportAsync().ConfigureAwait(true);
            }
        }

        private async void OnExportClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            string? path;
            try
            {
                var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save ERF As",
                    SuggestedFileName = "swlor-assets.erf",
                    DefaultExtension = "erf",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = new[] { ErfFileType }
                }).ConfigureAwait(true);
                path = destination?.TryGetLocalPath();
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Could not open the file picker: {ex.GetBaseException().Message}";
                return;
            }

            if (path == null)
                return;

            try
            {
                await _viewModel.ExportAsync(path).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Could not export to '{path}': {ex.GetBaseException().Message}";
            }
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_viewModel?.IsBusy == true)
                e.Cancel = true;
        }

        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.CanClose != false)
                Close();
        }
    }
}
