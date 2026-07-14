using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// Root shape of a saved Content Builder project ("File -> Save/Save As/Open"). Deliberately flat
    /// and readable rather than clever: AreaSettings mirrors every user-editable control on the Areas
    /// tab (see MainWindow.CaptureState/ApplyState), and Batch reuses AreaBatchFileEntry -- the same
    /// shape SWLOR.ProcgenReview's "--areas-file" contract already uses (see AreaBatchFile.cs) --
    /// rather than inventing a second, parallel batch format.
    ///
    /// Version has NO default initializer on purpose: a saved file always stamps it explicitly
    /// (ProjectFileService.CurrentVersion), so a JSON file with the "version" property entirely
    /// missing deserializes to 0, which ProjectFileService.ValidateJson reports as a distinct
    /// "missing version" error rather than silently being accepted as version 1.
    /// </summary>
    public sealed class ContentBuilderProjectFile
    {
        public int Version { get; set; }
        public AreaSettingsFile AreaSettings { get; set; }
        public List<AreaBatchFileEntry> Batch { get; set; } = new();
    }

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

        public int Seed { get; set; }

        /// <summary>"schematic" or "mapgraphics" -- MainWindow.SchematicModeKey/MapGraphicsModeKey.</summary>
        public string PreviewMode { get; set; } = string.Empty;
        public bool RoomOverlay { get; set; }
    }
}
