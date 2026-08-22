using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public sealed class AreaGeneratorWindowRenderTests
{
    [AvaloniaTest]
    public void Window_LoadsItsCompiledXaml()
    {
        var window = new AreaGeneratorWindow();

        window.Title.Should().Contain("Area Generator");
        window.Content.Should().NotBeNull();

        window.Close();
    }

    [Test]
    public void EnablingAccentTerrain_SeedsAValidNonzeroDensity()
    {
        using var viewModel = CreateViewModel();
        viewModel.AccentDensityPercent = 0;

        viewModel.AccentEnabled = true;

        viewModel.AccentDensityPercent.Should().Be(AreaSettingsBounds.AccentDensityPercentMin);
    }

    [Test]
    public void SelectingACompositionInput_ReevaluatesTheGenerateCommand()
    {
        using var viewModel = CreateViewModel();
        var notifications = 0;
        viewModel.GeneratePreviewCommand.CanExecuteChanged += (_, _) => notifications++;
        var profile = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            Key = "command_probe",
            DisplayName = "Command Probe",
            TilesetResref = "unavailable_command_probe"
        });
        viewModel.TilesetProfiles.Add(profile);

        viewModel.SelectedTilesetProfile = profile;

        notifications.Should().BeGreaterThan(0);
    }

    [Test]
    public void ReliefOnlyComposition_PreservesItsDefaultInTheSharedHeightControl()
    {
        using var viewModel = CreateViewModel();
        var reliefProfile = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            Key = "relief_only",
            DisplayName = "Relief Only",
            TilesetResref = "unavailable_test_tileset",
            MaxElevationRegions = 0,
            MaxReliefRegions = 2
        });
        viewModel.TilesetProfiles.Add(reliefProfile);
        viewModel.SelectedTilesetProfile = reliefProfile;
        viewModel.SelectedLayoutProfile = viewModel.LayoutProfiles.Single(choice =>
            choice.Value.Key == StandardLayoutProfiles.Complex);

        viewModel.MaximumElevationRegions.Should().Be(2);
        viewModel.ElevationRegions.Should().Be(2);
    }

    private static AreaGeneratorViewModel CreateViewModel()
    {
        var resources = new ResourceIndex(null, Array.Empty<ResourceIndex.HakLayer>());
        resources.EnsureInitialized();
        var tilesets = new TilesetCatalog(resources);
        return new AreaGeneratorViewModel(
            new AreaGenerationAuthoringService(tilesets),
            new AreaGenerationPreviewRenderer(resources: null),
            tilesets,
            new ModuleWorkspace(CorpusLocator.ModuleDirectory));
    }
}
