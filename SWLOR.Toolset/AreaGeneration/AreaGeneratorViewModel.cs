using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Native toolset workflow for previewing and creating deterministic generated areas.</summary>
public partial class AreaGeneratorViewModel : ObservableObject
{
    public sealed record ThemeChoice(DungeonDetail Value)
    {
        public string Label => Value.DisplayName;
    }

    public sealed record TilesetChoice(DungeonTilesetProfile Value)
    {
        public string Label => $"{Value.DisplayName} ({Value.TilesetResref})";
    }

    public sealed record LayoutChoice(DungeonLayoutProfile Value)
    {
        public string Label => Value.DisplayName;
    }

    public sealed record DecorationChoice(string Key, string Label);

    private readonly AreaGenerationAuthoringService _authoring;
    private readonly AreaGenerationPreviewRenderer _renderer;
    private readonly TilesetCatalog _tilesets;
    private readonly ModuleWorkspace _workspace;
    private bool _loadingDefaults;

    public ObservableCollection<ThemeChoice> Themes { get; } = new();
    public ObservableCollection<TilesetChoice> TilesetProfiles { get; } = new();
    public ObservableCollection<LayoutChoice> LayoutProfiles { get; } = new();
    public ObservableCollection<DecorationChoice> DecorationProfiles { get; } = new();
    public IReadOnlyList<DungeonLayoutStyle> LayoutStyles { get; } = Enum.GetValues<DungeonLayoutStyle>();
    public IReadOnlyList<AreaPreviewMode> PreviewModes { get; } = Enum.GetValues<AreaPreviewMode>();

    [ObservableProperty] private ThemeChoice? _selectedTheme;
    [ObservableProperty] private TilesetChoice? _selectedTilesetProfile;
    [ObservableProperty] private LayoutChoice? _selectedLayoutProfile;
    [ObservableProperty] private DecorationChoice? _selectedDecorationProfile;
    [ObservableProperty] private AreaPreviewMode _previewMode = AreaPreviewMode.MapGraphics;
    [ObservableProperty] private bool _showRooms = true;
    [ObservableProperty] private bool _showTransitions = true;
    [ObservableProperty] private bool _showDecorations = true;
    [ObservableProperty] private string _resRef = string.Empty;
    [ObservableProperty] private string _displayName = "Generated Area";
    [ObservableProperty] private double _width = 16;
    [ObservableProperty] private double _height = 16;
    [ObservableProperty] private double _seed = 4242;
    [ObservableProperty] private DungeonLayoutStyle _layoutStyle = DungeonLayoutStyle.RoomsAndCorridors;
    [ObservableProperty] private double _minRooms = 4;
    [ObservableProperty] private double _maxRooms = 8;
    [ObservableProperty] private double _minRoomSize = 3;
    [ObservableProperty] private double _maxRoomSize = 7;
    [ObservableProperty] private double _corridorWidth = 1;
    [ObservableProperty] private double _loopFactorPercent = 25;
    [ObservableProperty] private double _openFillPercent = 45;
    [ObservableProperty] private double _entranceCount = 1;
    [ObservableProperty] private double _exitCount = 1;
    [ObservableProperty] private bool _doorTransitions = true;
    [ObservableProperty] private bool _accentEnabled = true;
    [ObservableProperty] private double _accentDensityPercent = 8;
    [ObservableProperty] private double _featureDensityPercent = 3;
    [ObservableProperty] private double _elevationRegions;
    [ObservableProperty] private bool _enableDecorations = true;
    [ObservableProperty] private double _decorationDensityPercent = 100;
    [ObservableProperty] private Bitmap? _preview;
    [ObservableProperty] private string _statusMessage = "Choose a composition, then generate a preview.";
    [ObservableProperty] private bool _isBusy;

    public event Action<string>? AreaCreated;

    public AreaGeneratorViewModel(
        AreaGenerationAuthoringService authoring,
        AreaGenerationPreviewRenderer renderer,
        TilesetCatalog tilesets,
        ModuleWorkspace workspace)
    {
        _authoring = authoring;
        _renderer = renderer;
        _tilesets = tilesets;
        _workspace = workspace;

        foreach (var theme in authoring.Definitions.Themes)
            Themes.Add(new ThemeChoice(theme));

        foreach (var profile in authoring.Definitions.TilesetProfiles.Values
                     .Where(profile => tilesets.TryGetTileset(profile.TilesetResref, out _))
                     .OrderBy(profile => profile.DisplayName))
        {
            TilesetProfiles.Add(new TilesetChoice(profile));
        }

        SelectedTheme = Themes.FirstOrDefault();
        if (SelectedTilesetProfile == null)
            SelectedTilesetProfile = TilesetProfiles.FirstOrDefault();
        RefreshLayoutProfiles();

        if (TilesetProfiles.Count == 0)
            StatusMessage = "No generator tilesets are available from the current game-data index.";
    }

    partial void OnSelectedThemeChanged(ThemeChoice? value)
    {
        if (_loadingDefaults || value == null)
            return;

        _loadingDefaults = true;
        SelectedTilesetProfile = TilesetProfiles.FirstOrDefault(choice =>
            choice.Value.Key.Equals(value.Value.TilesetProfileKey, StringComparison.OrdinalIgnoreCase))
            ?? SelectedTilesetProfile;
        RefreshLayoutProfiles(value.Value.LayoutProfileKey);
        _loadingDefaults = false;
        LoadCompositionDefaults();
    }

    partial void OnSelectedTilesetProfileChanged(TilesetChoice? value)
    {
        if (_loadingDefaults)
            return;

        RefreshLayoutProfiles(SelectedTheme?.Value.LayoutProfileKey);
        LoadCompositionDefaults();
    }

    partial void OnSelectedLayoutProfileChanged(LayoutChoice? value)
    {
        if (!_loadingDefaults)
            LoadCompositionDefaults();
    }

    partial void OnIsBusyChanged(bool value)
    {
        GeneratePreviewCommand.NotifyCanExecuteChanged();
        CreateAreaCommand.NotifyCanExecuteChanged();
    }

    private void RefreshLayoutProfiles(string? preferredKey = null)
    {
        var priorKey = preferredKey ?? SelectedLayoutProfile?.Value.Key;
        LayoutProfiles.Clear();

        TilesetModel? model = null;
        if (SelectedTilesetProfile != null &&
            _tilesets.TryGetTileset(SelectedTilesetProfile.Value.TilesetResref, out var definition))
        {
            model = TilesetSetParser.FromDefinition(SelectedTilesetProfile.Value.TilesetResref, definition);
        }

        foreach (var profile in _authoring.Definitions.LayoutProfiles.Values.OrderBy(profile => profile.DisplayName))
        {
            if (model == null || SelectedTilesetProfile == null ||
                LayoutSupportRules.Supports(SelectedTilesetProfile.Value, profile, model))
            {
                LayoutProfiles.Add(new LayoutChoice(profile));
            }
        }

        _loadingDefaults = true;
        SelectedLayoutProfile = LayoutProfiles.FirstOrDefault(choice =>
            choice.Value.Key.Equals(priorKey, StringComparison.OrdinalIgnoreCase))
            ?? LayoutProfiles.FirstOrDefault();
        _loadingDefaults = false;
    }

    private void LoadCompositionDefaults()
    {
        if (SelectedTheme == null || SelectedTilesetProfile == null || SelectedLayoutProfile == null)
            return;

        _loadingDefaults = true;
        var composition = new DungeonComposition
        {
            Content = SelectedTheme.Value,
            Tileset = SelectedTilesetProfile.Value,
            Layout = SelectedLayoutProfile.Value
        };
        var parameters = composition.BuildLayoutParameters();
        LayoutStyle = parameters.Style;
        MinRooms = parameters.MinRooms;
        MaxRooms = parameters.MaxRooms;
        MinRoomSize = parameters.MinRoomCornerSize;
        MaxRoomSize = parameters.MaxRoomCornerSize;
        CorridorWidth = parameters.CorridorWidth;
        LoopFactorPercent = Math.Round(parameters.LoopFactor * 100);
        OpenFillPercent = Math.Round(parameters.OpenFillTarget * 100);
        EntranceCount = parameters.EntranceCount;
        ExitCount = parameters.ExitCount;
        DoorTransitions = parameters.DoorTransitions;
        AccentEnabled = parameters.AccentDensity > 0 &&
                        !string.IsNullOrWhiteSpace(SelectedTilesetProfile.Value.AccentTerrain);
        AccentDensityPercent = Math.Round(parameters.AccentDensity * 100);
        FeatureDensityPercent = Math.Round(parameters.FeatureDensity * 100);
        ElevationRegions = parameters.ElevationRegions;
        EnableDecorations = true;
        DecorationDensityPercent = 100;

        DecorationProfiles.Clear();
        DecorationProfiles.Add(new DecorationChoice(string.Empty, "Standard palette"));
        foreach (var key in SelectedTilesetProfile.Value.DecorationProfiles.Keys.OrderBy(key => key))
            DecorationProfiles.Add(new DecorationChoice(key, key));
        SelectedDecorationProfile = DecorationProfiles.FirstOrDefault(choice =>
            choice.Key.Equals(SelectedTheme.Value.DecorationProfile, StringComparison.OrdinalIgnoreCase))
            ?? DecorationProfiles[0];
        _loadingDefaults = false;
        Preview = null;
        StatusMessage = "Composition changed. Generate a preview to solve the area.";
    }

    private bool CanGenerate() => !IsBusy &&
                                  SelectedTheme != null &&
                                  SelectedTilesetProfile != null &&
                                  SelectedLayoutProfile != null;

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GeneratePreview()
    {
        IsBusy = true;
        StatusMessage = "Generating area...";
        try
        {
            var settings = BuildSettings();
            var draft = await Task.Run(() => _authoring.Generate(settings));
            if (!draft.Result.Success)
            {
                Preview = null;
                StatusMessage = draft.Result.FailureReason;
                return;
            }

            var image = await Task.Run(() => _renderer.Render(
                draft,
                PreviewMode,
                ShowRooms,
                ShowTransitions,
                ShowDecorations));
            Preview = ToBitmap(image);
            StatusMessage = Describe(draft, image);
        }
        catch (Exception ex)
        {
            Preview = null;
            StatusMessage = ex.GetBaseException().Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task CreateArea()
    {
        IsBusy = true;
        StatusMessage = "Solving and creating area...";
        try
        {
            var settings = BuildSettings();
            var draft = await Task.Run(() => _authoring.Generate(settings));
            if (!draft.Result.Success)
            {
                StatusMessage = draft.Result.FailureReason;
                return;
            }

            if (!GeneratedAreaWriter.TryCreate(
                    _workspace,
                    _tilesets,
                    draft,
                    ResRef,
                    DisplayName,
                    out var error))
            {
                StatusMessage = error;
                return;
            }

            var normalized = ResRef.Trim().ToLowerInvariant();
            StatusMessage = $"Created '{normalized}' in the open module.";
            AreaCreated?.Invoke(normalized);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.GetBaseException().Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private AreaGenerationSettings BuildSettings()
    {
        var wholeValues = new[]
        {
            Width, Height, Seed, MinRooms, MaxRooms, MinRoomSize, MaxRoomSize, CorridorWidth,
            LoopFactorPercent, OpenFillPercent, EntranceCount, ExitCount, AccentDensityPercent,
            FeatureDensityPercent, ElevationRegions, DecorationDensityPercent
        };
        if (wholeValues.Any(value => !double.IsFinite(value) || value != Math.Truncate(value)))
            throw new InvalidOperationException("Generator numeric settings must be whole numbers.");

        return new AreaGenerationSettings
        {
            ThemeKey = SelectedTheme!.Value.ThemeKey,
            TilesetProfileKey = SelectedTilesetProfile!.Value.Key,
            LayoutProfileKey = SelectedLayoutProfile!.Value.Key,
            Width = (int)Width,
            Height = (int)Height,
            Seed = (int)Seed,
            Overrides = new LayoutKnobOverrides
            {
                Style = LayoutStyle,
                MinRooms = (int)MinRooms,
                MaxRooms = (int)MaxRooms,
                MinRoomCornerSize = (int)MinRoomSize,
                MaxRoomCornerSize = (int)MaxRoomSize,
                CorridorWidth = (int)CorridorWidth,
                LoopFactorPercent = (int)LoopFactorPercent,
                OpenFillTargetPercent = (int)OpenFillPercent,
                EntranceCount = (int)EntranceCount,
                ExitCount = (int)ExitCount,
                DoorTransitions = DoorTransitions,
                AccentEnabled = AccentEnabled,
                AccentDensityPercent = (int)AccentDensityPercent,
                FeatureDensityPercent = (int)FeatureDensityPercent,
                ElevationRegions = (int)ElevationRegions,
                EnableDecorations = EnableDecorations,
                DecorationDensityPercent = (int)DecorationDensityPercent,
                DecorationProfile = SelectedDecorationProfile?.Key ?? string.Empty
            }
        };
    }

    private static string Describe(AreaGenerationDraft draft, AreaPreviewImage image)
    {
        var resolved = draft.Result.Resolved!;
        var missing = image.MissingTileGraphics == 0
            ? string.Empty
            : $" {image.MissingTileGraphics} tile graphic(s) used schematic colors.";
        return $"Seed {draft.Result.AttemptSeed}: {resolved.Rooms.Count} rooms, " +
               $"{resolved.Transitions.Count} transitions, {draft.Result.PlannedDecorationCount} decorations.{missing}";
    }

    private static Bitmap ToBitmap(AreaPreviewImage image)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(image.Width, image.Height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        var stride = image.Width * 4;
        for (var y = 0; y < image.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                image.Pixels,
                y * stride,
                buffer.Address + y * buffer.RowBytes,
                stride);
        }
        return bitmap;
    }
}
