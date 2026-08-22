#nullable disable
namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Single source of truth for the area generator's static numeric ranges. Dynamic bounds that
    /// depend on the current style, tileset, or dimensions
    /// (LayoutStyleSizeFloor, LayoutParameterConstraints.RoomSizeBounds/MinSafeOpenFillTarget) are
    /// NOT here -- those are read directly from their own authoritative source at validation time.
    /// </summary>
    public static class AreaSettingsBounds
    {
        public const int WidthMin = 8;
        public const int WidthMax = 32;
        public const int HeightMin = 8;
        public const int HeightMax = 32;

        public const int MinRoomsMin = 2;
        public const int MinRoomsMax = 12;
        public const int MaxRoomsMin = 2;
        public const int MaxRoomsMax = 16;

        public const int MinRoomSizeMin = 2;
        public const int MinRoomSizeMax = 10;
        public const int MaxRoomSizeMin = 3;
        public const int MaxRoomSizeMax = 12;

        public const int CorridorWidthMin = 1;
        public const int CorridorWidthMax = 3;

        public const int LoopFactorPercentMin = 0;
        public const int LoopFactorPercentMax = 100;

        public const int OrganicFillPercentMin = 30;
        public const int OrganicFillPercentMax = 60;

        public const int AccentDensityPercentMin = 1;
        public const int AccentDensityPercentMax = 20;

        public const int FeatureDensityPercentMin = 0;
        public const int FeatureDensityPercentMax = 15;

        public const int ElevationRegionsMin = 0;
        public const int ElevationRegionsMax = 3;

        public const int EntrancesMin = 1;
        public const int EntrancesMax = 3;
        public const int ExitsMin = 1;
        public const int ExitsMax = 3;

        /// <summary>Percent of DungeonDetail.DecorationBaseDensity applied by the decoration pass;
        /// matches the decoration planner's 0-200 contract.</summary>
        public const int DecorationDensityPercentMin = 0;
        public const int DecorationDensityPercentMax = 200;

        /// <summary>Same ceiling MainWindow applies to the Max Room Size slider (RoomSizeSliderAbsoluteMax).</summary>
        public const int RoomSizeSliderAbsoluteMax = 12;

        /// <summary>Largest accepted deterministic seed.</summary>
        public const int MaxSeed = int.MaxValue - 1000;
    }
}
