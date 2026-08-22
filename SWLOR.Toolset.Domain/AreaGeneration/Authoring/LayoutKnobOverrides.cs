#nullable disable
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// The toolset's Advanced-layout state, layered on top of a composition's effective parameters.
    /// Every field is applied unconditionally because the UI loads its controls from those same
    /// parameters. Percent-based knobs remain integer percentages so round trips are exact.
    /// </summary>
    public sealed record LayoutKnobOverrides
    {
        public DungeonLayoutStyle Style { get; init; }
        public int MinRooms { get; init; }
        public int MaxRooms { get; init; }
        public int MinRoomCornerSize { get; init; }
        public int MaxRoomCornerSize { get; init; }
        public int CorridorWidth { get; init; }
        public int LoopFactorPercent { get; init; }
        public int OpenFillTargetPercent { get; init; }
        public int EntranceCount { get; init; }
        public int ExitCount { get; init; }
        public bool DoorTransitions { get; init; }
        public bool AccentEnabled { get; init; }
        public int AccentDensityPercent { get; init; }
        public int FeatureDensityPercent { get; init; }
        public int ElevationRegions { get; init; }

        /// <summary>
        /// Decorations are theme content (DungeonDetail.Decorations/DecorationBaseDensity), not a
        /// layout knob -- they never feed MacroLayoutParameters/ApplyTo below, so they can't affect
        /// map geometry. GenerationEngine.Generate reads these two directly to compute
        /// GenerationResult.PlannedDecorationCount from the resolved layout.
        /// </summary>
        public bool EnableDecorations { get; init; } = true;
        public int DecorationDensityPercent { get; init; } = 100;

        /// <summary>Named tileset decoration profile to dress with (see DungeonTilesetProfile.
        /// DecorationProfiles, e.g. fcx01's "ruined"); empty = the standard palette. Content, not a
        /// layout knob -- same contract as the two decoration knobs above.</summary>
        public string DecorationProfile { get; init; } = string.Empty;

        public void ApplyTo(MacroLayoutParameters parameters, DungeonTilesetProfile tileset)
        {
            parameters.Style = Style;
            parameters.MinRooms = MinRooms;
            parameters.MaxRooms = MaxRooms;
            parameters.MinRoomCornerSize = MinRoomCornerSize;
            parameters.MaxRoomCornerSize = MaxRoomCornerSize;
            var minimumCorridorWidth = tileset != null
                ? System.Math.Max(1, tileset.MinimumOpeningWidth)
                : 1;
            if (tileset != null && !string.IsNullOrEmpty(tileset.RoadCrosser))
                minimumCorridorWidth = System.Math.Max(minimumCorridorWidth, 2);
            parameters.CorridorWidth = System.Math.Max(CorridorWidth, minimumCorridorWidth);
            parameters.LoopFactor = LoopFactorPercent / 100.0;
            parameters.OpenFillTarget = OpenFillTargetPercent / 100.0;
            parameters.EntranceCount = EntranceCount;
            parameters.ExitCount = ExitCount;
            parameters.DoorTransitions = DoorTransitions;

            var blobAccentActive = AccentEnabled && tileset != null && !string.IsNullOrEmpty(tileset.AccentTerrain);
            parameters.AccentTerrain = blobAccentActive ? tileset.AccentTerrain : string.Empty;
            parameters.AccentDensity = blobAccentActive ? AccentDensityPercent / 100.0 : 0.0;

            var channelTerrain = tileset == null
                ? string.Empty
                : !string.IsNullOrEmpty(tileset.ChannelTerrain)
                    ? tileset.ChannelTerrain
                    : tileset.AccentTerrain;
            var channelActive = AccentEnabled && parameters.AccentChannels > 0 && !string.IsNullOrEmpty(channelTerrain);
            parameters.ChannelTerrain = channelActive ? channelTerrain : string.Empty;
            if (!channelActive)
                parameters.AccentChannels = 0;

            var poolActive = blobAccentActive && parameters.PoolRegions > 0;
            parameters.PoolTerrain = poolActive ? parameters.AccentTerrain : string.Empty;
            if (!poolActive)
                parameters.PoolRegions = 0;

            parameters.FeatureDensity = FeatureDensityPercent / 100.0;

            // Clamp to the tileset's own verified support (mirrors DungeonComposition.
            // BuildLayoutParameters' identical clamp) -- a slider dragged above what the current
            // tileset supports is silently capped rather than handed to LayoutElevationPainter raw.
            parameters.ElevationRegions = tileset != null
                ? System.Math.Min(ElevationRegions, tileset.MaxElevationRegions)
                : 0;

            // Per-corner relief rides the same "Elevation Regions" slider intent (one raised-terrain
            // knob in the UI), clamped independently to the tileset's own verified relief support --
            // a tileset with elevation-blob vocabulary but no per-corner relief vocabulary (or vice
            // versa) gets exactly the passes its caps declare, mirroring DungeonComposition.
            // BuildLayoutParameters. The blend-terrain/ramp-crosser names were already stamped by
            // BuildLayoutParameters and are never slider-driven, so nothing re-applies them here.
            parameters.ReliefRegions = tileset != null
                ? System.Math.Min(ElevationRegions, tileset.MaxReliefRegions)
                : 0;
        }
    }
}
