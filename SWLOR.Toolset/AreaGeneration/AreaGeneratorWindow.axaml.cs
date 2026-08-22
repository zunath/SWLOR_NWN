using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.AreaGeneration;

public partial class AreaGeneratorWindow : Window
{
    public AreaGeneratorWindow()
    {
        InitializeComponent();
    }

    public AreaGeneratorWindow(AreaGeneratorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Closing += OnClosing;
        viewModel.AreaCreated += resref => Close(resref);
    }

    public static async Task<string?> ShowAsync(
        AreaGenerationAuthoringService authoring,
        AreaGenerationPreviewRenderer renderer,
        TilesetCatalog tilesets,
        ModuleWorkspace workspace)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null)
            return null;

        var viewModel = new AreaGeneratorViewModel(authoring, renderer, tilesets, workspace);
        return await new AreaGeneratorWindow(viewModel).ShowDialog<string?>(owner).ConfigureAwait(true);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is AreaGeneratorViewModel { IsBusy: true })
            e.Cancel = true;
    }
}
