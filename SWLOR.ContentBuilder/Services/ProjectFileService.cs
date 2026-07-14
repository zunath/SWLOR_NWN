using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.ContentBuilder.Models;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// UI-free serialization + validation for Content Builder project files (File -> Save/Save
    /// As/Open). Takes/returns the plain <see cref="ContentBuilderProjectFile"/> DTO -- MainWindow is
    /// responsible for capturing that DTO from its controls and applying a validated one back onto
    /// them; this class never touches a WPF control.
    ///
    /// Load validation is all-or-nothing: <see cref="ValidateJson"/> parses and checks the ENTIRE file
    /// (area settings + every batch entry) before returning. On success it returns a candidate whose
    /// numeric fields have already been clamped through the same authoritative sources the UI itself
    /// clamps through (LayoutParameterConstraints, LayoutStyleSizeFloor, RoomSizeBounds,
    /// AreaSettingsBounds' slider Minimum/Maximum literals) -- callers apply it to the UI verbatim. On
    /// failure it returns a single message describing what's wrong and leaves the caller free to
    /// discard the candidate entirely without having touched any live state.
    /// </summary>
    internal static class ProjectFileService
    {
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        public sealed class ValidationResult
        {
            public bool Success { get; private init; }
            public ContentBuilderProjectFile File { get; private init; }
            public string Error { get; private init; }

            public static ValidationResult Ok(ContentBuilderProjectFile file) => new() { Success = true, File = file };
            public static ValidationResult Fail(string error) => new() { Success = false, Error = error };
        }

        public static string Serialize(ContentBuilderProjectFile file) =>
            JsonConvert.SerializeObject(file, SerializerSettings);

        public static void Save(ContentBuilderProjectFile file, string path) =>
            File.WriteAllText(path, Serialize(file));

        public static ValidationResult LoadAndValidate(string path, DefinitionCatalog catalog)
        {
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Could not read \"{path}\":\n{ex.Message}");
            }

            return ValidateJson(json, catalog);
        }

        /// <summary>Parses and validates a project file's JSON text without touching disk. Exposed
        /// separately from <see cref="LoadAndValidate"/> so tests can round-trip an in-memory string.</summary>
        public static ValidationResult ValidateJson(string json, DefinitionCatalog catalog)
        {
            ContentBuilderProjectFile candidate;
            try
            {
                candidate = JsonConvert.DeserializeObject<ContentBuilderProjectFile>(json, SerializerSettings);
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"File is not a valid Content Builder project (malformed JSON or wrong shape):\n{ex.Message}");
            }

            if (candidate == null)
                return ValidationResult.Fail("File is empty or its root is not a project object.");

            if (candidate.Version == 0)
                return ValidationResult.Fail("File is missing its \"version\" field.");

            if (candidate.Version != CurrentVersion)
                return ValidationResult.Fail($"Unsupported project file version {candidate.Version} (this build supports version {CurrentVersion}).");

            if (candidate.AreaSettings == null)
                return ValidationResult.Fail("File is missing its \"areaSettings\" section.");

            var settingsError = ValidateAndClampAreaSettings(candidate.AreaSettings, catalog);
            if (settingsError != null) return ValidationResult.Fail(settingsError);

            candidate.Batch ??= new List<AreaBatchFileEntry>();
            for (var i = 0; i < candidate.Batch.Count; i++)
            {
                var entryError = ValidateAndClampBatchEntry(candidate.Batch[i], catalog, i);
                if (entryError != null) return ValidationResult.Fail(entryError);
            }

            return ValidationResult.Ok(candidate);
        }

        // --------------------------------------------------------------
        // Area settings
        // --------------------------------------------------------------

        private static string ValidateAndClampAreaSettings(AreaSettingsFile s, DefinitionCatalog catalog)
        {
            var theme = catalog.Themes.FirstOrDefault(t => t.ThemeKey == s.ThemeKey);
            if (theme == null)
                return $"Unknown theme key \"{s.ThemeKey}\".";

            if (!catalog.TilesetProfiles.TryGetValue(s.TilesetProfileKey ?? string.Empty, out var tileset))
                return $"Unknown tileset profile key \"{s.TilesetProfileKey}\".";

            if (!catalog.LayoutProfiles.TryGetValue(s.LayoutProfileKey ?? string.Empty, out var layout))
                return $"Unknown layout profile key \"{s.LayoutProfileKey}\".";

            if (!LayoutSupportRules.Supports(tileset, layout))
                return $"Layout profile \"{layout.DisplayName}\" is not supported on tileset profile \"{tileset.DisplayName}\".";

            if (!Enum.TryParse<DungeonLayoutStyle>(s.Style, out var style))
                return $"Unknown layout style \"{s.Style}\".";
            s.Style = style.ToString();

            if (s.Width < 0) return $"Width must not be negative (found {s.Width}).";
            if (s.Height < 0) return $"Height must not be negative (found {s.Height}).";
            var sizeFloor = LayoutStyleSizeFloor.For(style);
            s.Width = Math.Clamp(s.Width, sizeFloor, AreaSettingsBounds.WidthMax);
            s.Height = Math.Clamp(s.Height, sizeFloor, AreaSettingsBounds.HeightMax);

            if (s.MinRooms < 0) return $"Min Rooms must not be negative (found {s.MinRooms}).";
            if (s.MaxRooms < 0) return $"Max Rooms must not be negative (found {s.MaxRooms}).";
            s.MinRooms = Math.Clamp(s.MinRooms, AreaSettingsBounds.MinRoomsMin, AreaSettingsBounds.MinRoomsMax);
            s.MaxRooms = Math.Clamp(s.MaxRooms, AreaSettingsBounds.MaxRoomsMin, AreaSettingsBounds.MaxRoomsMax);
            if (s.MinRooms > s.MaxRooms) (s.MinRooms, s.MaxRooms) = (s.MaxRooms, s.MinRooms);

            if (s.MinRoomSize < 0) return $"Min Room Size must not be negative (found {s.MinRoomSize}).";
            if (s.MaxRoomSize < 0) return $"Max Room Size must not be negative (found {s.MaxRoomSize}).";
            var (_, maxRoomSizeCeiling) = LayoutParameterConstraints.RoomSizeBounds(style, s.Width, s.Height);
            var effectiveCeiling = Math.Min(maxRoomSizeCeiling, AreaSettingsBounds.RoomSizeSliderAbsoluteMax);
            s.MaxRoomSize = Math.Clamp(s.MaxRoomSize, AreaSettingsBounds.MaxRoomSizeMin, effectiveCeiling);
            s.MinRoomSize = Math.Clamp(s.MinRoomSize, AreaSettingsBounds.MinRoomSizeMin, s.MaxRoomSize);

            if (s.CorridorWidth < 0) return $"Corridor Width must not be negative (found {s.CorridorWidth}).";
            s.CorridorWidth = Math.Clamp(s.CorridorWidth, AreaSettingsBounds.CorridorWidthMin, AreaSettingsBounds.CorridorWidthMax);

            if (s.LoopFactorPercent < 0) return $"Loop Factor must not be negative (found {s.LoopFactorPercent}).";
            s.LoopFactorPercent = Math.Clamp(s.LoopFactorPercent, AreaSettingsBounds.LoopFactorPercentMin, AreaSettingsBounds.LoopFactorPercentMax);

            if (s.OrganicFillPercent < 0) return $"Organic Fill must not be negative (found {s.OrganicFillPercent}).";
            var minFillPercent = (int)Math.Round(LayoutParameterConstraints.MinSafeOpenFillTarget(s.Width, s.Height) * 100);
            s.OrganicFillPercent = Math.Clamp(s.OrganicFillPercent, minFillPercent, AreaSettingsBounds.OrganicFillPercentMax);

            if (s.AccentDensityPercent < 0) return $"Accent Density must not be negative (found {s.AccentDensityPercent}).";
            s.AccentDensityPercent = Math.Clamp(s.AccentDensityPercent, AreaSettingsBounds.AccentDensityPercentMin, AreaSettingsBounds.AccentDensityPercentMax);
            if (s.AccentEnabled && string.IsNullOrEmpty(tileset.AccentTerrain))
                s.AccentEnabled = false; // mirrors MainWindow.UpdateAccentAvailability's auto-uncheck.

            if (s.FeatureDensityPercent < 0) return $"Feature Density must not be negative (found {s.FeatureDensityPercent}).";
            s.FeatureDensityPercent = Math.Clamp(s.FeatureDensityPercent, AreaSettingsBounds.FeatureDensityPercentMin, AreaSettingsBounds.FeatureDensityPercentMax);

            if (s.ElevationRegions < 0) return $"Elevation Regions must not be negative (found {s.ElevationRegions}).";
            s.ElevationRegions = Math.Clamp(s.ElevationRegions, AreaSettingsBounds.ElevationRegionsMin, AreaSettingsBounds.ElevationRegionsMax);

            if (s.Entrances < 0) return $"Entrances must not be negative (found {s.Entrances}).";
            if (s.Exits < 0) return $"Exits must not be negative (found {s.Exits}).";
            s.Entrances = Math.Clamp(s.Entrances, AreaSettingsBounds.EntrancesMin, AreaSettingsBounds.EntrancesMax);
            s.Exits = Math.Clamp(s.Exits, AreaSettingsBounds.ExitsMin, AreaSettingsBounds.ExitsMax);

            if (s.DecorationDensityPercent < 0) return $"Decoration Density must not be negative (found {s.DecorationDensityPercent}).";
            s.DecorationDensityPercent = Math.Clamp(s.DecorationDensityPercent, AreaSettingsBounds.DecorationDensityPercentMin, AreaSettingsBounds.DecorationDensityPercentMax);

            if (s.Seed < 0) return $"Seed must not be negative (found {s.Seed}).";
            s.Seed = Math.Clamp(s.Seed, 0, AreaSettingsBounds.MaxSeed);

            if (s.PreviewMode != "schematic" && s.PreviewMode != "mapgraphics")
                return $"Unknown preview mode \"{s.PreviewMode}\" (expected \"schematic\" or \"mapgraphics\").";

            return null;
        }

        // --------------------------------------------------------------
        // Batch
        // --------------------------------------------------------------

        private static string ValidateAndClampBatchEntry(AreaBatchFileEntry entry, DefinitionCatalog catalog, int index)
        {
            var label = $"Batch entry {index + 1}";

            if (entry == null)
                return $"{label} is missing.";

            var theme = catalog.Themes.FirstOrDefault(t => t.ThemeKey == entry.ThemeKey);
            if (theme == null)
                return $"{label}: unknown theme key \"{entry.ThemeKey}\".";

            var tilesetKey = string.IsNullOrEmpty(entry.TilesetKey) ? theme.TilesetProfileKey : entry.TilesetKey;
            if (!catalog.TilesetProfiles.TryGetValue(tilesetKey, out var tileset))
                return $"{label}: unknown tileset profile key \"{tilesetKey}\".";

            var layoutKey = string.IsNullOrEmpty(entry.LayoutKey) ? theme.LayoutProfileKey : entry.LayoutKey;
            if (!catalog.LayoutProfiles.TryGetValue(layoutKey, out var layout))
                return $"{label}: unknown layout profile key \"{layoutKey}\".";

            if (!LayoutSupportRules.Supports(tileset, layout))
                return $"{label}: layout profile \"{layout.DisplayName}\" is not supported on tileset profile \"{tileset.DisplayName}\".";

            if (entry.Seed < 0)
                return $"{label}: seed must not be negative (found {entry.Seed}).";
            entry.Seed = Math.Clamp(entry.Seed, 0, AreaSettingsBounds.MaxSeed);

            if (entry.Size <= 0)
                return $"{label}: size must be a positive integer (found {entry.Size}).";

            if (entry.DecorationDensityPercent < 0)
                return $"{label}: decoration density must not be negative (found {entry.DecorationDensityPercent}).";
            entry.DecorationDensityPercent = Math.Clamp(entry.DecorationDensityPercent, AreaSettingsBounds.DecorationDensityPercentMin, AreaSettingsBounds.DecorationDensityPercentMax);

            if (entry.Parameters == null)
                return $"{label}: missing generation parameters.";

            // MacroLayoutParameters is already the full EFFECTIVE, engine-facing parameter set --
            // LayoutParameterConstraints.ClampToValid IS the authoritative normalizer MacroLayoutGenerator
            // itself falls back on, so batch entries reuse it directly instead of re-deriving a second
            // clamp over the same fields.
            LayoutParameterConstraints.ClampToValid(entry.Parameters);

            return null;
        }
    }
}
