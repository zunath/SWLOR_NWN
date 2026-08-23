using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    [Test]
    public void TilesetChoices_ShowOnlyTheVisualName()
    {
        var choice = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            DisplayName = "City Interior",
            TilesetResref = "tin01"
        });

        choice.Label.Should().Be("City Interior");
        choice.Label.Should().NotContain("tin01");
    }

    [AvaloniaTest]
    public void Window_LoadsItsCompiledXaml()
    {
        var window = new AreaGeneratorWindow();

        window.Title.Should().Contain("Area Generator");
        window.Content.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaTest]
    public void Footer_DoesNotShowTheRedundantErfExportNotice()
    {
        using var viewModel = CreateViewModel();
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.GetVisualDescendants().OfType<TextBlock>()
                .Should().NotContain(block => block.Text != null && block.Text.Contains("ERF Manager"));
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void OpeningWithoutAvailableTilesets_RandomizesTheSeedWithoutHidingTheDiagnostic()
    {
        using var viewModel = CreateViewModel();
        var seedBeforeOpening = viewModel.Seed;
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            viewModel.Seed.Should().NotBe(seedBeforeOpening);
            var randomizedSeed = viewModel.Seed;
            viewModel.EnableAutomaticPreview();
            viewModel.Seed.Should().Be(randomizedSeed, "the seed changes only once per window instance");
            viewModel.StatusMessage.Should().Be(
                "No generator tilesets are available from the current game-data index.");
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void AreaEditorsRemainReadableAtTheDefaultWindowSize()
    {
        using var viewModel = CreateViewModel();
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var width = window.FindControl<NumericUpDown>("WidthInput")!;
            var height = window.FindControl<NumericUpDown>("HeightInput")!;
            var seed = window.FindControl<NumericUpDown>("SeedInput")!;
            var resref = window.FindControl<TextBox>("ResRefInput")!;
            var displayName = window.FindControl<TextBox>("DisplayNameInput")!;

            new[] { width.Bounds.Width, height.Bounds.Width, resref.Bounds.Width, displayName.Bounds.Width }
                .Should().OnlyContain(controlWidth => controlWidth >= 180,
                    "each Area editor needs enough room for its value rather than only spinner buttons");
            width.GetVisualDescendants().OfType<TextBox>().Single().Bounds.Width.Should().BeGreaterThan(100);
            height.GetVisualDescendants().OfType<TextBox>().Single().Bounds.Width.Should().BeGreaterThan(100);
            seed.GetVisualDescendants().OfType<TextBox>().Single().Bounds.Width.Should().BeGreaterThan(70);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void PreviewModeUsesABuilderFacingLabel()
    {
        using var viewModel = CreateViewModel();
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var selector = window.FindControl<ComboBox>("PreviewModeSelector")!;
            selector.GetVisualDescendants().OfType<TextBlock>()
                .Should().Contain(block => block.Text == "Map graphics");
            selector.GetVisualDescendants().OfType<TextBlock>()
                .Should().NotContain(block => block.Text == "MapGraphics");
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void PreviewToolbarFitsInsideThePaneAtTheMinimumWindowWidth()
    {
        using var viewModel = CreateViewModel();
        var window = new AreaGeneratorWindow(viewModel)
        {
            Width = 920,
            Height = 650
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var pane = window.FindControl<Grid>("PreviewPane")!;
            var controls = new Control[]
            {
                window.FindControl<ComboBox>("PreviewModeSelector")!,
                window.FindControl<CheckBox>("ShowRoomsToggle")!,
                window.FindControl<CheckBox>("ShowTransitionsToggle")!,
                window.FindControl<CheckBox>("ShowDecorationsToggle")!,
                window.FindControl<Button>("GeneratePreviewButton")!
            };

            foreach (var control in controls)
            {
                var origin = control.TranslatePoint(new Point(0, 0), pane)!.Value;
                origin.X.Should().BeGreaterThanOrEqualTo(0);
                (origin.X + control.Bounds.Width).Should().BeLessThanOrEqualTo(
                    pane.Bounds.Width + 0.5,
                    $"{control.Name} must remain inside the preview pane at the minimum window width");
            }
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void SeedUsesRandomizeButtonInsteadOfSpinnerArrows()
    {
        using var viewModel = CreateViewModel();
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var seedInput = window.FindControl<NumericUpDown>("SeedInput")!;
            var randomize = window.FindControl<Button>("RandomizeSeedButton")!;
            seedInput.ShowButtonSpinner.Should().BeFalse();
            randomize.Content.Should().Be("Randomize");
            randomize.Command.Should().BeSameAs(viewModel.RandomizeSeedCommand);

            var previousSeed = viewModel.Seed;
            randomize.Command!.Execute(randomize.CommandParameter);

            viewModel.Seed.Should().NotBe(previousSeed);
            viewModel.Seed.Should().BeInRange(0, AreaSettingsBounds.MaxSeed);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public async Task OpeningTheWindow_RandomizesTheSeedAndGeneratesPreviewsAutomatically()
    {
        using var viewModel = CreateGeneratableViewModel();
        viewModel.PreviewMode = AreaPreviewMode.Schematic;
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            var seedBeforeOpening = viewModel.Seed;
            viewModel.TilesetProfiles.Should().NotBeEmpty();
            viewModel.GeneratePreviewCommand.CanExecute(null).Should().BeTrue();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            viewModel.Seed.Should().NotBe(seedBeforeOpening);
            viewModel.Seed.Should().BeInRange(0, AreaSettingsBounds.MaxSeed);
            viewModel.StatusMessage.Should().Be("Preparing the preview...");
            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);

            var firstPreview = viewModel.Preview;
            viewModel.Seed = viewModel.Seed == AreaSettingsBounds.MaxSeed ? 0 : viewModel.Seed + 1;
            viewModel.Preview.Should().BeNull("changing generation settings invalidates the old result immediately");

            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);

            viewModel.Preview.Should().NotBeSameAs(firstPreview);
            viewModel.StatusMessage.Should().StartWith("Seed ");
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public async Task GeneratingPreview_ShowsProgressWhileTheUiThreadRemainsResponsive()
    {
        var uiThreadId = Environment.CurrentManagedThreadId;
        var backgroundTasks = new GatedBackgroundTaskRunner();
        using var viewModel = CreateGeneratableViewModel(backgroundTasks);
        viewModel.PreviewMode = AreaPreviewMode.Schematic;
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            await backgroundTasks.FirstOperationStarted.WaitAsync(TimeSpan.FromSeconds(5));
            Dispatcher.UIThread.RunJobs();

            viewModel.IsBusy.Should().BeTrue();
            viewModel.BusyMessage.Should().Be(
                "Solving the layout and placing transitions and decorations...");
            window.FindControl<StackPanel>("GenerationProgressPanel")!.IsVisible.Should().BeTrue();
            window.FindControl<TextBlock>("GenerationProgressText")!.Text.Should().Be(viewModel.BusyMessage);
            window.FindControl<ProgressBar>("GenerationProgressBar")!.IsIndeterminate.Should().BeTrue();

            var uiPulse = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() => uiPulse.TrySetResult(true));
            await uiPulse.Task.WaitAsync(TimeSpan.FromSeconds(2));

            backgroundTasks.Release();
            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);

            backgroundTasks.WorkerThreadIds.Should().HaveCountGreaterThanOrEqualTo(2);
            backgroundTasks.WorkerThreadIds.Should().OnlyContain(threadId => threadId != uiThreadId);
            window.FindControl<StackPanel>("GenerationProgressPanel")!.IsVisible.Should().BeFalse();
        }
        finally
        {
            backgroundTasks.Release();
            if (viewModel.IsBusy)
            {
                await WaitUntilAsync(
                    () => !viewModel.IsBusy,
                    () => viewModel.StatusMessage);
            }

            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task DefaultBackgroundTaskRunner_RunsWorkOffTheCallingThread()
    {
        var callingThreadId = Environment.CurrentManagedThreadId;
        var runner = new AreaGeneratorBackgroundTaskRunner();

        var workerThreadId = await runner.RunAsync(() => Environment.CurrentManagedThreadId);

        workerThreadId.Should().NotBe(callingThreadId);
    }

    [AvaloniaTest]
    public async Task InvalidResRef_HighlightsTheEditorAndStatusUntilCorrected()
    {
        using var viewModel = CreateGeneratableViewModel();
        viewModel.PreviewMode = AreaPreviewMode.Schematic;
        var window = new AreaGeneratorWindow(viewModel);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);

            viewModel.ResRef.Should().BeEmpty("the visible generated_area text is only a watermark");
            await viewModel.CreateAreaCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();

            viewModel.HasResRefError.Should().BeTrue();
            viewModel.StatusIsError.Should().BeTrue();
            viewModel.ResRefError.Should().Be(
                "ResRef must be 1-16 characters, lowercase letters/digits/underscore only.");
            window.FindControl<TextBox>("ResRefInput")!.Classes.Should().Contain("validationError");
            window.FindControl<TextBlock>("ResRefErrorText")!.IsVisible.Should().BeTrue();
            window.FindControl<TextBlock>("StatusMessageText")!.Classes.Should().Contain("statusError");

            viewModel.Seed = viewModel.Seed == AreaSettingsBounds.MaxSeed ? 0 : viewModel.Seed + 1;
            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);

            viewModel.StatusIsError.Should().BeTrue(
                "automatic preview updates must not hide an active field validation error");
            viewModel.StatusMessage.Should().Be(viewModel.ResRefError);
            window.FindControl<TextBox>("ResRefInput")!.Classes.Should().Contain("validationError");

            var validSeed = viewModel.Seed;
            viewModel.Seed = 0.5;
            await WaitUntilAsync(
                () => !viewModel.IsBusy &&
                      viewModel.StatusMessage.Contains("Generator numeric settings must be whole numbers."),
                () => viewModel.StatusMessage);

            viewModel.StatusIsError.Should().BeTrue();
            viewModel.StatusMessage.Should().StartWith(viewModel.ResRefError);
            viewModel.StatusMessage.Should().Contain("Generator numeric settings must be whole numbers.");
            window.FindControl<TextBox>("ResRefInput")!.Classes.Should().Contain("validationError");

            viewModel.ResRef = "generated_area";
            Dispatcher.UIThread.RunJobs();

            viewModel.HasResRefError.Should().BeFalse();
            viewModel.StatusIsError.Should().BeTrue(
                "correcting one field must not hide an independent preview failure");
            viewModel.StatusMessage.Should().Be("Generator numeric settings must be whole numbers.");
            window.FindControl<TextBox>("ResRefInput")!.Classes.Should().NotContain("validationError");
            window.FindControl<TextBlock>("ResRefErrorText")!.IsVisible.Should().BeFalse();
            window.FindControl<TextBlock>("StatusMessageText")!.Classes.Should().Contain("statusError");

            viewModel.Seed = validSeed;
            await WaitUntilAsync(
                () => viewModel.Preview != null && !viewModel.IsBusy,
                () => viewModel.StatusMessage);
            Dispatcher.UIThread.RunJobs();

            viewModel.StatusIsError.Should().BeFalse();
            window.FindControl<TextBlock>("StatusMessageText")!.Classes.Should().NotContain("statusError");

            var existingAreaPath = Directory.EnumerateFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, "are"),
                "*.are.json").First();
            var existingResRef = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(existingAreaPath));
            viewModel.ResRef = existingResRef;
            await viewModel.CreateAreaCommand.ExecuteAsync(null);

            viewModel.StatusIsError.Should().BeTrue();
            viewModel.StatusMessage.Should().Contain($"An area named '{existingResRef}' already exists");

            viewModel.ResRef = "new_resref_test";
            Dispatcher.UIThread.RunJobs();

            viewModel.HasResRefError.Should().BeFalse();
            viewModel.StatusIsError.Should().BeFalse(
                "a creation error for the previous ResRef is stale after the field changes");
            viewModel.StatusMessage.Should().NotContain(existingResRef);
            window.FindControl<TextBlock>("StatusMessageText")!.Classes.Should().NotContain("statusError");
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public void EnablingAccentTerrain_SeedsAValidNonzeroDensity()
    {
        using var viewModel = CreateViewModel();
        var blobAccentProfile = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            Key = "blob_accent",
            DisplayName = "Blob Accent",
            TilesetResref = "unavailable_test_tileset",
            AccentTerrain = "Water"
        });
        viewModel.TilesetProfiles.Add(blobAccentProfile);
        viewModel.SelectedTilesetProfile = blobAccentProfile;
        viewModel.AccentEnabled = false;
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

    [Test]
    public void ChannelOnlyComposition_EnablesAccentsWithoutInventingBlobDensity()
    {
        using var viewModel = CreateViewModel();
        var channelProfile = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            Key = "channel_only",
            DisplayName = "Channel Only",
            TilesetResref = "unavailable_test_tileset",
            ChannelTerrain = "Chasm"
        });
        viewModel.TilesetProfiles.Add(channelProfile);
        viewModel.SelectedTilesetProfile = channelProfile;
        viewModel.SelectedLayoutProfile = viewModel.LayoutProfiles.Single(choice =>
            choice.Value.Key == StandardLayoutProfiles.Halls);

        viewModel.AccentEnabled.Should().BeTrue();
        viewModel.AccentDensityPercent.Should().Be(0);
    }

    [AvaloniaTest]
    public void ChannelLayout_OnBlobCapableTileset_PreservesZeroBlobDensityWhileLoadingDefaults()
    {
        using var viewModel = CreateViewModel();
        var mixedProfile = new AreaGeneratorViewModel.TilesetChoice(new DungeonTilesetProfile
        {
            Key = "mixed_accents",
            DisplayName = "Mixed Accents",
            TilesetResref = "unavailable_test_tileset",
            AccentTerrain = "Water",
            ChannelTerrain = "Pit"
        });
        viewModel.TilesetProfiles.Add(mixedProfile);
        viewModel.SelectedTilesetProfile = mixedProfile;
        viewModel.SelectedLayoutProfile = viewModel.LayoutProfiles.Single(choice =>
            choice.Value.Key == StandardLayoutProfiles.Halls);

        viewModel.AccentEnabled.Should().BeTrue();
        viewModel.AccentDensityPercent.Should().Be(0);

        var window = new AreaGeneratorWindow(viewModel);
        try
        {
            Dispatcher.UIThread.RunJobs();
            var density = window.FindControl<NumericUpDown>("AccentDensityInput")!;
            density.Minimum.Should().Be(0);
            density.Value.Should().Be(0);
            viewModel.AccentDensityPercent.Should().Be(0,
                "binding the channel-only default must not invent a blob accent pass");

            viewModel.AccentDensityPercent = 5;
            viewModel.AccentDensityPercent = 0;
            Dispatcher.UIThread.RunJobs();

            density.Value.Should().Be(0);
            viewModel.AccentDensityPercent.Should().Be(0,
                "zero explicitly disables blob painting even while the channel pass remains enabled");

            var parameters = new MacroLayoutParameters { AccentChannels = 1 };
            new LayoutKnobOverrides
            {
                AccentEnabled = viewModel.AccentEnabled,
                AccentDensityPercent = (int)viewModel.AccentDensityPercent
            }.ApplyTo(parameters, mixedProfile.Value);

            parameters.AccentDensity.Should().Be(0);
            parameters.ChannelTerrain.Should().Be("Pit");
            parameters.AccentChannels.Should().Be(1);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public void ChangingPreviewDisplayOptions_InvalidatesTheRenderedPreview()
    {
        using var viewModel = CreateViewModel();

        AssertInvalidatesPreview(viewModel, () => viewModel.PreviewMode = AreaPreviewMode.Schematic);
        AssertInvalidatesPreview(viewModel, () => viewModel.ShowRooms = false);
        AssertInvalidatesPreview(viewModel, () => viewModel.ShowTransitions = false);
        AssertInvalidatesPreview(viewModel, () => viewModel.ShowDecorations = false);
    }

    private static void AssertInvalidatesPreview(AreaGeneratorViewModel viewModel, Action changeDisplayOption)
    {
        viewModel.Preview = new WriteableBitmap(
            new PixelSize(1, 1),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        changeDisplayOption();

        viewModel.Preview.Should().BeNull();
        viewModel.StatusMessage.Should().Be(
            "Preview display options changed. Updating the preview...");
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

    private static AreaGeneratorViewModel CreateGeneratableViewModel(
        IAreaGeneratorBackgroundTaskRunner? backgroundTasks = null)
    {
        var resources = new ResourceIndex(
            baseLayer: null,
            new[]
            {
                new ResourceIndex.HakLayer(
                    "area-generator-auto-preview-test",
                    Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_t_minecave"))
            });
        resources.EnsureInitialized();
        var tilesets = new TilesetCatalog(resources);
        return backgroundTasks == null
            ? new AreaGeneratorViewModel(
                new AreaGenerationAuthoringService(tilesets),
                new AreaGenerationPreviewRenderer(resources: null),
                tilesets,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory))
            : new AreaGeneratorViewModel(
                new AreaGenerationAuthoringService(tilesets),
                new AreaGenerationPreviewRenderer(resources: null),
                tilesets,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                backgroundTasks);
    }

    private sealed class GatedBackgroundTaskRunner : IAreaGeneratorBackgroundTaskRunner
    {
        private readonly TaskCompletionSource<bool> _firstOperationStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _operationCount;

        public Task FirstOperationStarted => _firstOperationStarted.Task;
        public ConcurrentBag<int> WorkerThreadIds { get; } = new();

        public Task<T> RunAsync<T>(Func<T> operation)
        {
            return Task.Run(() =>
            {
                WorkerThreadIds.Add(Environment.CurrentManagedThreadId);
                if (Interlocked.Increment(ref _operationCount) == 1)
                {
                    _firstOperationStarted.TrySetResult(true);
                    _release.Task.GetAwaiter().GetResult();
                }

                return operation();
            });
        }

        public void Release() => _release.TrySetResult(true);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, Func<string> currentStatus)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail($"Timed out waiting for the automatic area preview. Status: {currentStatus()}");

            await Task.Delay(25);
        }
    }
}
