using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// Every user-editable Areas-tab setting: Composition dropdowns (by profile KEY, not display
    /// name), every Basic/Advanced knob, transitions, seed, and the preview toolbar. Field names are
    /// the plain domain concept, not the private slider-field names MainWindow uses internally --
    /// LoopFactorPercent/OrganicFillPercent/etc. are the slider's own integer percent (0-100), the
    /// same convention LayoutKnobOverrides already uses, so no fraction math leaks into the file.
    /// </summary>
    public sealed class AreaSettingsFile
    {
        public string ThemeKey { get; set; } = string.Empty;
        public string TilesetProfileKey { get; set; } = string.Empty;
        public string LayoutProfileKey { get; set; } = string.Empty;

        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>DungeonLayoutStyle name (e.g. "RoomsAndCorridors"). Parsed/validated manually by
        /// ProjectFileService rather than typed as the enum so an unknown value produces a clean,
        /// specific error message instead of a raw JSON converter exception.</summary>
        public string Style { get; set; } = string.Empty;

        public int MinRooms { get; set; }
        public int MaxRooms { get; set; }
        public int MinRoomSize { get; set; }
        public int MaxRoomSize { get; set; }
        public int CorridorWidth { get; set; }
        public int LoopFactorPercent { get; set; }
        public int OrganicFillPercent { get; set; }

        public bool AccentEnabled { get; set; }
        public int AccentDensityPercent { get; set; }
        public int FeatureDensityPercent { get; set; }
        public int ElevationRegions { get; set; }

        public int Entrances { get; set; }
        public int Exits { get; set; }
        public bool DoorTransitions { get; set; }

        /// <summary>Optional, defaults true/100: a v1 project file saved before decorations existed
        /// has neither property in its JSON and loads with these same defaults (see
        /// ProjectFileService.ValidateAndClampAreaSettings) -- the project file format version does
        /// not need to change for this addition.</summary>
        public bool DecorationsEnabled { get; set; } = true;
        public int DecorationDensityPercent { get; set; } = 100;

        /// <summary>Optional, defaults empty (the standard palette): named tileset decoration
        /// profile (see DungeonTilesetProfile.DecorationProfiles). An older project without the
        /// property loads as Standard; an unknown name falls back to Standard on apply.</summary>
        public string DecorationProfile { get; set; } = string.Empty;

        public int Seed { get; set; }

        /// <summary>"schematic" or "mapgraphics" -- MainWindow.SchematicModeKey/MapGraphicsModeKey.</summary>
        public string PreviewMode { get; set; } = string.Empty;
        public bool RoomOverlay { get; set; }
    }
}
