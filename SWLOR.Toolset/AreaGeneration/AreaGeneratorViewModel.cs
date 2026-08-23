using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Native toolset workflow for previewing and creating deterministic generated areas.</summary>
public partial class AreaGeneratorViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan AutomaticPreviewDelay = TimeSpan.FromMilliseconds(300);
    private const string InitialStatusMessage = "The preview generates automatically when this window opens.";

    private enum StatusSource
    {
        Preview,
        Creation
    }

    public sealed record ThemeChoice(DungeonDetail Value)
    {
        public string Label => Value.DisplayName;
    }

    public sealed record TilesetChoice(DungeonTilesetProfile Value)
    {
        public string Label => Value.DisplayName;
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
    private readonly IAreaGeneratorBackgroundTaskRunner _backgroundTasks;
    private bool _loadingDefaults;
    private bool _adjustingRanges;
    private bool _automaticPreviewEnabled;
    private bool _showResRefValidation;
    private bool _disposed;
    private string _statusWithoutResRefValidation = InitialStatusMessage;
    private bool _statusWithoutResRefValidationIsError;
    private string _latestPreviewStatus = InitialStatusMessage;
    private bool _latestPreviewStatusIsError;
    private StatusSource _statusSource;
    private CancellationTokenSource? _automaticPreviewCancellation;
    private AreaGenerationDraft? _previewedDraft;

    private static readonly HashSet<string> GenerationInputProperties = new(StringComparer.Ordinal)
    {
        nameof(SelectedTheme),
        nameof(SelectedTilesetProfile),
        nameof(SelectedLayoutProfile),
        nameof(SelectedDecorationProfile),
        nameof(Width),
        nameof(Height),
        nameof(Seed),
        nameof(LayoutStyle),
        nameof(MinRooms),
        nameof(MaxRooms),
        nameof(MinRoomSize),
        nameof(MaxRoomSize),
        nameof(CorridorWidth),
        nameof(LoopFactorPercent),
        nameof(OpenFillPercent),
        nameof(EntranceCount),
        nameof(ExitCount),
        nameof(DoorTransitions),
        nameof(AccentEnabled),
        nameof(AccentDensityPercent),
        nameof(FeatureDensityPercent),
        nameof(ElevationRegions),
        nameof(EnableDecorations),
        nameof(DecorationDensityPercent)
    };

    public ObservableCollection<ThemeChoice> Themes { get; } = new();
    public ObservableCollection<TilesetChoice> TilesetProfiles { get; } = new();
    public ObservableCollection<LayoutChoice> LayoutProfiles { get; } = new();
    public ObservableCollection<DecorationChoice> DecorationProfiles { get; } = new();
    public IReadOnlyList<DungeonLayoutStyle> LayoutStyles { get; } = Enum.GetValues<DungeonLayoutStyle>();
    public IReadOnlyList<AreaPreviewMode> PreviewModes { get; } = Enum.GetValues<AreaPreviewMode>();
    public int MinimumDimension => LayoutStyleSizeFloor.For(LayoutStyle);
    public int MinimumRoomSizeBound => EffectiveRoomSizeBounds().Min;
    public int MaximumRoomSizeBound => Math.Min(
        EffectiveRoomSizeBounds().Max,
        AreaSettingsBounds.RoomSizeSliderAbsoluteMax);
    public int MinimumOpenFillPercent => LayoutStyle == DungeonLayoutStyle.OrganicCave
        ? (int)Math.Ceiling(LayoutParameterConstraints.MinSafeOpenFillTarget((int)Width, (int)Height) * 100)
        : AreaSettingsBounds.OrganicFillPercentMin;
    public int MaximumElevationRegions => SelectedTilesetProfile == null
        ? AreaSettingsBounds.ElevationRegionsMin
        : Math.Max(
            SelectedTilesetProfile.Value.MaxElevationRegions,
            SelectedTilesetProfile.Value.MaxReliefRegions);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private ThemeChoice? _selectedTheme;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private TilesetChoice? _selectedTilesetProfile;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private LayoutChoice? _selectedLayoutProfile;
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
    [ObservableProperty] private string _statusMessage = InitialStatusMessage;
    [ObservableProperty] private string _resRefError = string.Empty;
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = string.Empty;

    public bool HasResRefError => !string.IsNullOrEmpty(ResRefError);

    public event Action<string>? AreaCreated;

    public AreaGeneratorViewModel(
        AreaGenerationAuthoringService authoring,
        AreaGenerationPreviewRenderer renderer,
        TilesetCatalog tilesets,
        ModuleWorkspace workspace)
        : this(authoring, renderer, tilesets, workspace, new AreaGeneratorBackgroundTaskRunner())
    {
    }

    public AreaGeneratorViewModel(
        AreaGenerationAuthoringService authoring,
        AreaGenerationPreviewRenderer renderer,
        TilesetCatalog tilesets,
        ModuleWorkspace workspace,
        IAreaGeneratorBackgroundTaskRunner backgroundTasks)
    {
        _authoring = authoring;
        _renderer = renderer;
        _tilesets = tilesets;
        _workspace = workspace;
        _backgroundTasks = backgroundTasks ?? throw new ArgumentNullException(nameof(backgroundTasks));

        foreach (var theme in authoring.Definitions.Themes)
            Themes.Add(new ThemeChoice(theme));

        var availableTilesets = tilesets.GetTilesetNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in authoring.Definitions.TilesetProfiles.Values
                     .Where(profile => availableTilesets.Contains(profile.TilesetResref))
                     .OrderBy(profile => profile.DisplayName))
        {
            TilesetProfiles.Add(new TilesetChoice(profile));
        }

        SelectedTheme = Themes.FirstOrDefault();
        if (SelectedTilesetProfile == null)
            SelectedTilesetProfile = TilesetProfiles.FirstOrDefault();
        RefreshLayoutProfiles();

        if (TilesetProfiles.Count == 0)
            SetStatus("No generator tilesets are available from the current game-data index.", isError: true);

        PropertyChanged += OnGenerationInputChanged;
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
        OnPropertyChanged(nameof(MaximumElevationRegions));
        ElevationRegions = Math.Min(ElevationRegions, MaximumElevationRegions);
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
        if (!value)
            BusyMessage = string.Empty;

        GeneratePreviewCommand.NotifyCanExecuteChanged();
        CreateAreaCommand.NotifyCanExecuteChanged();
    }

    partial void OnResRefChanged(string value)
    {
        if (!_showResRefValidation)
            return;

        if (_statusSource == StatusSource.Creation)
            RestoreLatestPreviewStatus();

        ValidateResRef();
        ApplyStatus();
    }

    partial void OnResRefErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasResRefError));
    }

    partial void OnLayoutStyleChanged(DungeonLayoutStyle value)
    {
        OnPropertyChanged(nameof(MinimumDimension));
        var minimum = LayoutStyleSizeFloor.For(value);
        Width = Math.Max(Width, minimum);
        Height = Math.Max(Height, minimum);
        RefreshEffectiveRanges();
    }

    partial void OnWidthChanged(double value) => RefreshEffectiveRanges();

    partial void OnHeightChanged(double value) => RefreshEffectiveRanges();

    partial void OnMinRoomsChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value > MaxRooms)
            MaxRooms = value;
    }

    partial void OnMaxRoomsChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value < MinRooms)
            MinRooms = value;
    }

    partial void OnMinRoomSizeChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value > MaxRoomSize)
            MaxRoomSize = value;
    }

    partial void OnMaxRoomSizeChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value < MinRoomSize)
            MinRoomSize = value;
    }

    partial void OnAccentEnabledChanged(bool value)
    {
        if (!_loadingDefaults && value && SupportsBlobAccents() &&
            AccentDensityPercent < AreaSettingsBounds.AccentDensityPercentMin)
            AccentDensityPercent = AreaSettingsBounds.AccentDensityPercentMin;
    }

    partial void OnPreviewModeChanged(AreaPreviewMode value) => InvalidatePreviewDisplay();

    partial void OnShowRoomsChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnShowTransitionsChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnShowDecorationsChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnPreviewChanging(Bitmap? oldValue, Bitmap? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
            oldValue?.Dispose();
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
        AccentEnabled = (parameters.AccentDensity > 0 && !string.IsNullOrWhiteSpace(parameters.AccentTerrain)) ||
                        (parameters.AccentChannels > 0 && !string.IsNullOrWhiteSpace(parameters.ChannelTerrain)) ||
                        (parameters.PoolRegions > 0 && !string.IsNullOrWhiteSpace(parameters.PoolTerrain));
        AccentDensityPercent = Math.Round(parameters.AccentDensity * 100);
        FeatureDensityPercent = Math.Round(parameters.FeatureDensity * 100);
        ElevationRegions = Math.Max(parameters.ElevationRegions, parameters.ReliefRegions);
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
        RefreshEffectiveRanges();
        InvalidateAndRequestPreview("Composition changed. Updating the preview...");
    }

    private bool SupportsBlobAccents() =>
        SelectedTilesetProfile != null &&
        !string.IsNullOrWhiteSpace(SelectedTilesetProfile.Value.AccentTerrain);

    private bool CanGenerate() => !IsBusy &&
                                  SelectedTheme != null &&
                                  SelectedTilesetProfile != null &&
                                  SelectedLayoutProfile != null;

    private bool CanCreate() => CanGenerate() && _previewedDraft != null;

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GeneratePreview()
    {
        CancelAutomaticPreviewRequest();
        SetPreviewedDraft(null);
        BusyMessage = "Preparing generation settings...";
        IsBusy = true;
        SetStatus("Generating area...");
        try
        {
            var settings = BuildSettings();
            BusyMessage = "Solving the layout and placing transitions and decorations...";
            var draft = await _backgroundTasks.RunAsync(() => _authoring.Generate(settings)).ConfigureAwait(true);
            if (!draft.Result.Success)
            {
                Preview = null;
                SetStatus(draft.Result.FailureReason, isError: true);
                return;
            }

            var previewMode = PreviewMode;
            var showRooms = ShowRooms;
            var showTransitions = ShowTransitions;
            var showDecorations = ShowDecorations;
            BusyMessage = previewMode == AreaPreviewMode.MapGraphics
                ? "Rendering tiles and preview overlays..."
                : "Rendering the schematic preview...";
            var image = await _backgroundTasks.RunAsync(() => _renderer.Render(
                draft,
                previewMode,
                showRooms,
                showTransitions,
                showDecorations)).ConfigureAwait(true);
            BusyMessage = "Preparing the preview image...";
            Preview = ToBitmap(image);
            SetPreviewedDraft(draft);
            SetStatus(Describe(draft, image));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Area generation preview failed.");
            Preview = null;
            SetPreviewedDraft(null);
            SetStatus(ex.GetBaseException().Message, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateArea()
    {
        var draft = _previewedDraft;
        if (draft == null)
        {
            SetStatus(
                "Wait for the current settings to finish previewing before creating the area.",
                isError: true,
                source: StatusSource.Creation);
            return;
        }

        _showResRefValidation = true;
        if (!ValidateResRef())
        {
            ApplyStatus();
            return;
        }

        BusyMessage = "Writing the generated area into the open module...";
        IsBusy = true;
        SetStatus("Creating the previewed area...", source: StatusSource.Creation);
        string? createdResref = null;
        try
        {
            var resRef = ResRef;
            var displayName = DisplayName;
            var createResult = await _backgroundTasks.RunAsync(() =>
            {
                var success = GeneratedAreaWriter.TryCreate(
                    _workspace,
                    _tilesets,
                    draft,
                    resRef,
                    displayName,
                    out var error);
                return (Success: success, Error: error);
            }).ConfigureAwait(true);
            if (!createResult.Success)
            {
                SetStatus(createResult.Error, isError: true, source: StatusSource.Creation);
                return;
            }

            var normalized = resRef.Trim().ToLowerInvariant();
            SetStatus($"Created '{normalized}' in the open module.", source: StatusSource.Creation);
            createdResref = normalized;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Creating generated area {ResRef} failed.", ResRef);
            SetStatus(ex.GetBaseException().Message, isError: true, source: StatusSource.Creation);
        }
        finally
        {
            IsBusy = false;
        }

        if (createdResref != null)
            AreaCreated?.Invoke(createdResref);
    }

    private void OnGenerationInputChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_loadingDefaults || e.PropertyName == null ||
            !GenerationInputProperties.Contains(e.PropertyName))
        {
            return;
        }

        InvalidateAndRequestPreview("Settings changed. Updating the preview...");
    }

    /// <summary>
    /// Starts the automatic preview lifecycle once the window is visible. Keeping this out of the
    /// constructor avoids doing rendering work for view models that are prepared but never shown.
    /// </summary>
    public void EnableAutomaticPreview()
    {
        if (_automaticPreviewEnabled || _disposed)
            return;

        RandomizeInitialSeed();
        _automaticPreviewEnabled = true;
        if (CanGenerate())
        {
            SetStatus("Preparing the preview...");
            RequestAutomaticPreview();
        }
    }

    private void RandomizeInitialSeed()
    {
        var wasLoadingDefaults = _loadingDefaults;
        _loadingDefaults = true;
        try
        {
            Seed = NextRandomSeed();
        }
        finally
        {
            _loadingDefaults = wasLoadingDefaults;
        }
    }

    [RelayCommand]
    private void RandomizeSeed()
    {
        Seed = NextRandomSeed();
    }

    private int NextRandomSeed()
    {
        var seed = Random.Shared.Next(AreaSettingsBounds.MaxSeed + 1);
        if (seed == Seed)
            seed = seed == AreaSettingsBounds.MaxSeed ? 0 : seed + 1;

        return seed;
    }

    private void InvalidateAndRequestPreview(string status)
    {
        InvalidatePreview(status);
        RequestAutomaticPreview();
    }

    private void InvalidatePreview(string status)
    {
        Preview = null;
        SetPreviewedDraft(null);
        SetStatus(status);
    }

    private void InvalidatePreviewDisplay()
    {
        if (_loadingDefaults)
            return;

        if (Preview != null)
            InvalidatePreview("Preview display options changed. Updating the preview...");

        RequestAutomaticPreview();
    }

    private void RequestAutomaticPreview()
    {
        if (!_automaticPreviewEnabled || _loadingDefaults || _disposed || !CanGenerate())
            return;

        CancelAutomaticPreviewRequest();
        var request = new CancellationTokenSource();
        _automaticPreviewCancellation = request;
        _ = GeneratePreviewAfterDelay(request, request.Token);
    }

    private async Task GeneratePreviewAfterDelay(CancellationTokenSource request, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutomaticPreviewDelay, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || _disposed ||
            !ReferenceEquals(_automaticPreviewCancellation, request) || !CanGenerate())
        {
            return;
        }

        await GeneratePreview().ConfigureAwait(true);
    }

    private void CancelAutomaticPreviewRequest()
    {
        var request = _automaticPreviewCancellation;
        _automaticPreviewCancellation = null;
        if (request == null)
            return;

        request.Cancel();
        request.Dispose();
    }

    private (int Min, int Max) EffectiveRoomSizeBounds()
    {
        var width = Math.Clamp((int)Width, AreaSettingsBounds.WidthMin, AreaSettingsBounds.WidthMax);
        var height = Math.Clamp((int)Height, AreaSettingsBounds.HeightMin, AreaSettingsBounds.HeightMax);
        return LayoutParameterConstraints.RoomSizeBounds(LayoutStyle, width, height);
    }

    private void RefreshEffectiveRanges()
    {
        OnPropertyChanged(nameof(MinimumRoomSizeBound));
        OnPropertyChanged(nameof(MaximumRoomSizeBound));
        OnPropertyChanged(nameof(MinimumOpenFillPercent));
        if (_loadingDefaults || _adjustingRanges)
            return;

        _adjustingRanges = true;
        try
        {
            MinRoomSize = Math.Clamp(MinRoomSize, MinimumRoomSizeBound, MaximumRoomSizeBound);
            MaxRoomSize = Math.Clamp(MaxRoomSize, MinimumRoomSizeBound, MaximumRoomSizeBound);
            if (MinRoomSize > MaxRoomSize)
                MinRoomSize = MaxRoomSize;
            OpenFillPercent = Math.Max(OpenFillPercent, MinimumOpenFillPercent);
        }
        finally
        {
            _adjustingRanges = false;
        }
    }

    private bool ValidateResRef()
    {
        var normalized = (ResRef ?? string.Empty).Trim().ToLowerInvariant();
        ResRefError = NwnResRef.IsCanonical(normalized)
            ? string.Empty
            : $"ResRef must be 1-{NwnResRef.MaxLength} characters, lowercase letters/digits/underscore only.";
        return !HasResRefError;
    }

    private void SetStatus(
        string message,
        bool isError = false,
        StatusSource source = StatusSource.Preview)
    {
        if (source == StatusSource.Preview)
        {
            _latestPreviewStatus = message;
            _latestPreviewStatusIsError = isError;
        }

        _statusWithoutResRefValidation = message;
        _statusWithoutResRefValidationIsError = isError;
        _statusSource = source;

        ApplyStatus();
    }

    private void RestoreLatestPreviewStatus()
    {
        _statusWithoutResRefValidation = _latestPreviewStatus;
        _statusWithoutResRefValidationIsError = _latestPreviewStatusIsError;
        _statusSource = StatusSource.Preview;
    }

    private void ApplyStatus()
    {
        if (!HasResRefError)
        {
            StatusIsError = _statusWithoutResRefValidationIsError;
            StatusMessage = _statusWithoutResRefValidation;
            return;
        }

        StatusIsError = true;
        StatusMessage = _statusWithoutResRefValidationIsError
            ? $"{ResRefError}{Environment.NewLine}{_statusWithoutResRefValidation}"
            : ResRefError;
    }

    private void SetPreviewedDraft(AreaGenerationDraft? draft)
    {
        _previewedDraft = draft;
        CreateAreaCommand.NotifyCanExecuteChanged();
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
        if (buffer.RowBytes == stride)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                image.Pixels,
                0,
                buffer.Address,
                image.Pixels.Length);
            return bitmap;
        }

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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CancelAutomaticPreviewRequest();
        PropertyChanged -= OnGenerationInputChanged;
        Preview = null;
        SetPreviewedDraft(null);
    }
}
