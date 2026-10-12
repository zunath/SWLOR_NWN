using Nwn.Authoring.Areas.Tiles;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Settings;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// The palette's preferences in SWLOR's existing <see cref="ToolsetSettings"/> keys:
    /// <c>palettePreviewSize</c>, <c>paletteCategoryProportion</c>, <c>paletteSelection</c> (a resource
    /// extension or <see cref="ToolsetSettings.TilesSelection"/>), <c>paletteShowsStandard</c> and
    /// <c>tilePaintMode</c>.
    /// </summary>
    internal sealed class SwlorPaletteSettings : IPaletteSettings
    {
        private readonly ToolsetSettings _settings;

        public SwlorPaletteSettings(ToolsetSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public double PreviewSize
        {
            get => _settings.PalettePreviewSize;
            set => _settings.PalettePreviewSize = value;
        }

        public double CategoryProportion
        {
            get => _settings.PaletteCategoryProportion;
            set => _settings.PaletteCategoryProportion = value;
        }

        public PaletteSelection? Selection
        {
            get
            {
                var selection = _settings.PaletteSelection;
                if (string.Equals(selection, ToolsetSettings.TilesSelection, StringComparison.OrdinalIgnoreCase))
                    return PaletteSelection.Tiles;

                return ResourceTypeExtensions.TryFromExtension(selection, out var type)
                    ? PaletteSelection.ForType(type)
                    : null;
            }
            set => _settings.PaletteSelection = value switch
            {
                { Mode: PaletteMode.Tiles } => ToolsetSettings.TilesSelection,
                { Type: { } type } => type.Extension(),
                _ => string.Empty
            };
        }

        public SharedPaletteSource Source
        {
            get => _settings.PaletteShowsStandard ? SharedPaletteSource.Standard : SharedPaletteSource.Custom;
            set => _settings.PaletteShowsStandard = value == SharedPaletteSource.Standard;
        }

        public TilePaintMode? TilePaintMode
        {
            get => Enum.TryParse<TilePaintMode>(_settings.TilePaintMode, ignoreCase: true, out var mode)
                ? mode
                : null;
            set => _settings.TilePaintMode = value?.ToString() ?? string.Empty;
        }
    }
}
