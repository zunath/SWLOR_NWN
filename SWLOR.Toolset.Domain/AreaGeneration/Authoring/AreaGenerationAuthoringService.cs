#nullable enable
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Resolves generator definitions and toolset-indexed tileset data, then produces a deterministic
    /// draft suitable for preview or direct creation in the open module.
    /// </summary>
    public sealed class AreaGenerationAuthoringService
    {
        private readonly TilesetCatalog _tilesets;

        public DefinitionCatalog Definitions { get; }

        public AreaGenerationAuthoringService(
            TilesetCatalog tilesets,
            DefinitionCatalog? definitions = null)
        {
            _tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
            Definitions = definitions ?? new DefinitionCatalog();
        }

        public AreaGenerationDraft Generate(AreaGenerationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var theme = Definitions.Themes.FirstOrDefault(candidate =>
                candidate.ThemeKey.Equals(settings.ThemeKey, StringComparison.OrdinalIgnoreCase));
            if (theme == null)
                throw new ArgumentException($"Unknown area theme '{settings.ThemeKey}'.", nameof(settings));

            var tilesetKey = string.IsNullOrWhiteSpace(settings.TilesetProfileKey)
                ? theme.TilesetProfileKey
                : settings.TilesetProfileKey;
            if (!Definitions.TilesetProfiles.TryGetValue(tilesetKey, out var tilesetProfile))
                throw new ArgumentException($"Unknown tileset profile '{tilesetKey}'.", nameof(settings));

            var layoutKey = string.IsNullOrWhiteSpace(settings.LayoutProfileKey)
                ? theme.LayoutProfileKey
                : settings.LayoutProfileKey;
            if (!Definitions.LayoutProfiles.TryGetValue(layoutKey, out var layoutProfile))
                throw new ArgumentException($"Unknown layout profile '{layoutKey}'.", nameof(settings));

            var style = settings.Overrides?.Style ?? layoutProfile.Template.Style;
            ValidateDimensions(settings.Width, settings.Height, theme, style);
            if (settings.Seed is < 0 or > AreaSettingsBounds.MaxSeed)
                throw new ArgumentOutOfRangeException(nameof(settings), "Seed is outside the supported range.");

            if (!_tilesets.TryGetTileset(tilesetProfile.TilesetResref, out var definition))
            {
                throw new InvalidOperationException(
                    $"Tileset '{tilesetProfile.TilesetResref}' for profile '{tilesetProfile.DisplayName}' is unavailable.");
            }

            var tileset = TilesetSetParser.FromDefinition(tilesetProfile.TilesetResref, definition);
            var composition = new DungeonComposition
            {
                Content = theme,
                Tileset = tilesetProfile,
                Layout = layoutProfile
            };
            var result = GenerationEngine.Generate(
                composition,
                tileset,
                settings.Width,
                settings.Height,
                settings.Seed,
                settings.Overrides);

            return new AreaGenerationDraft(settings, composition, tileset, result);
        }

        private static void ValidateDimensions(
            int width,
            int height,
            DungeonDetail theme,
            DungeonLayoutStyle style)
        {
            if (width is < AreaSettingsBounds.WidthMin or > AreaSettingsBounds.WidthMax ||
                height is < AreaSettingsBounds.HeightMin or > AreaSettingsBounds.HeightMax)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Width and height must each be between {AreaSettingsBounds.WidthMin} and {AreaSettingsBounds.WidthMax}.");
            }

            if (width < theme.MinSize || height < theme.MinSize ||
                width > theme.MaxSize || height > theme.MaxSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Theme '{theme.DisplayName}' supports sizes {theme.MinSize}-{theme.MaxSize}.");
            }

            var styleFloor = LayoutStyleSizeFloor.For(style);
            if (width < styleFloor || height < styleFloor)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Layout style '{style}' requires width and height of at least {styleFloor}.");
            }
        }
    }
}
