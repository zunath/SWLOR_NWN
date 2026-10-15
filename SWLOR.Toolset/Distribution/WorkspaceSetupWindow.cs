using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SWLOR.Toolset.Domain.Distribution;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Distribution;

/// <summary>First-run setup keeps editable workspaces outside the installed application's update directory.</summary>
public sealed class WorkspaceSetupWindow : Window
{
    private readonly ToolsetSettings _settings;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _download = new() { Content = "Create SWLOR workspace" };
    private readonly Button _open = new() { Content = "Open existing workspace" };
    private readonly Button _game = new() { Content = "Locate Neverwinter Nights" };
    private CancellationTokenSource _cancellation = new();
    private bool _busy;

    public WorkspaceSetupWindow(ToolsetSettings settings)
    {
        _settings = settings;
        Title = "SWLOR Toolset — Workspaces";
        Width = 560;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var cancel = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right };
        Content = new StackPanel
        {
            Margin = new Thickness(24), Spacing = 16,
            Children =
            {
                new TextBlock { Text = "Start building", FontSize = 22, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "Create an editable copy of SWLOR content, or open a workspace you already have. Your work stays separate from application updates.", TextWrapping = TextWrapping.Wrap },
                _download, _open, _game,
                new TextBlock { Text = "Neverwinter Nights supplies base-game models and tiles. SWLOR assets are downloaded separately and shared between workspaces.", TextWrapping = TextWrapping.Wrap },
                _status, cancel
            }
        };
        _download.Click += async (_, _) => await DownloadAsync();
        _open.Click += async (_, _) => await OpenAsync();
        _game.Click += async (_, _) =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select the Neverwinter Nights installation folder", AllowMultiple = false });
            var path = folders.FirstOrDefault()?.TryGetLocalPath();
            if (path != null) { _settings.NwnInstallOverride = path; _status.Text = "Game installation selected."; }
        };
        cancel.Click += (_, _) => { if (_busy) _cancellation.Cancel(); else Close(false); };
        Closing += (_, args) => { if (_busy) { args.Cancel = true; _cancellation.Cancel(); } };
        Closed += (_, _) => _cancellation.Dispose();
    }

    private async Task OpenAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select a workspace or Module folder", AllowMultiple = false });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (path == null) return;
        var module = Directory.Exists(Path.Combine(path, "Module")) ? Path.Combine(path, "Module") : path;
        if (!Directory.Exists(Path.Combine(module, "are")) || !Directory.Exists(Path.Combine(module, "utc")))
        { _status.Text = "This folder does not contain an unpacked module. Select its workspace or Module folder."; return; }
        _settings.ModuleRoot = module;
        Close(true);
    }

    private async Task DownloadAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose where to create a new SWLOR workspace", AllowMultiple = false });
        var parent = folders.FirstOrDefault()?.TryGetLocalPath();
        if (parent == null) return;
        var destination = Path.Combine(parent, "SWLOR Workspace " + DateTime.Now.ToString("yyyy-MM-dd HHmmss"));
        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
        _busy = true;
        _download.IsEnabled = _open.IsEnabled = _game.IsEnabled = false;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
            var download = new WorkspaceDownload(client, ToolsetFeed.ReadInstalledVersion(AppContext.BaseDirectory));
            _status.Text = "Checking available content...";
            var feed = await download.ReadFeedAsync(_cancellation.Token);
            await download.CreateAsync(feed, destination, WorkspaceDownload.AssetCacheDirectory,
                new Progress<string>(message => _status.Text = message), _cancellation.Token);
            _settings.ModuleRoot = Path.Combine(destination, "Module");
            _busy = false;
            Close(true);
        }
        catch (OperationCanceledException) { _status.Text = "Download cancelled. You can retry or open an existing workspace."; }
        catch (Exception exception) { _status.Text = "Workspace download failed: " + exception.Message; }
        finally { _busy = false; _download.IsEnabled = _open.IsEnabled = _game.IsEnabled = true; }
    }

    public static Task<bool> ShowAsync(Window owner, ToolsetSettings settings) =>
        new WorkspaceSetupWindow(settings).ShowDialog<bool>(owner);
}
