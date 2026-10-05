using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using SWLOR.Toolset.Domain.GameData.Resources;
using SharedModelPreviewControl = Nwn.Toolset.Avalonia.Viewport.ModelPreviewControl;

namespace SWLOR.Toolset.Viewport;

/// <summary>A SWLOR-owned window for inspecting native resources through the shared viewport.</summary>
public sealed class NativeModelPreviewWindow : Window
{
    private readonly NativeModelPreviewAdapter _adapter;
    private readonly TextBox _resRefBox = new() { Text = "pfa0_chest001", Width = 220 };
    private readonly TextBlock _status = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly SharedModelPreviewControl _viewport = new();

    public NativeModelPreviewWindow(NativeModelPreviewAdapter adapter)
    {
        _adapter = adapter;
        Title = "Native Model Preview · SWLOR Toolset";
        Width = 1000;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var load = new Button { Content = "Load", MinWidth = 80 };
        load.Click += async (_, _) => await LoadModelAsync();
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8) };
        header.Children.Add(new TextBlock { Text = "Model resref:", VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(_resRefBox);
        header.Children.Add(load);
        header.Children.Add(_status);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Background = Brushes.Black };
        layout.Children.Add(header);
        Grid.SetRow(_viewport, 1);
        layout.Children.Add(_viewport);
        Content = layout;
        Opened += async (_, _) => await LoadModelAsync();
    }

    public static Task ShowAsync(ResourceIndex resources)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (lifetime?.MainWindow is not Window owner)
            throw new InvalidOperationException("The desktop main window is unavailable.");
        return new NativeModelPreviewWindow(new NativeModelPreviewAdapter(resources)).ShowDialog(owner);
    }

    private async Task LoadModelAsync()
    {
        _status.Text = "Loading…";
        try
        {
            var resRef = _resRefBox.Text ?? string.Empty;
            var data = await Task.Run(() => _adapter.Load(resRef));
            _viewport.Textures = data.Textures;
            _viewport.Scene = data.Scene;
            var details = new List<string> { "Unlit", Path.GetFileName(data.ModelSourcePath) };
            if (data.MissingTextures.Count > 0) details.Add($"missing maps: {string.Join(", ", data.MissingTextures)}");
            if (data.UnsupportedMaterials.Count > 0) details.Add($"partial materials: {string.Join(", ", data.UnsupportedMaterials)}");
            _status.Text = string.Join(" · ", details);
        }
        catch (Exception exception) when (exception is FormatException or IOException or ArgumentException or NotSupportedException)
        {
            _status.Text = exception.Message;
        }
    }
}
