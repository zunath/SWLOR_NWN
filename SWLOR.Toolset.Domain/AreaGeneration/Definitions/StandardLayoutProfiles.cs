#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Layout profiles modeled on hand-built SWLOR areas. Tileset-independent: any profile can
    /// compose with any tileset profile. AccentDensity expresses intent — the terrain name comes
    /// from the composed tileset profile (and is skipped where the tileset has none).
    /// </summary>
    public class StandardLayoutProfiles : IDungeonLayoutProfileListDefinition
    {
        public const string Organic = "organic";
        public const string Warren = "warren";
        public const string Packed = "packed";
        public const string Halls = "halls";
        public const string Labyrinth = "labyrinth";
        public const string Complex = "complex";
        public const string Streets = "streets";

        private readonly DungeonLayoutProfileBuilder _builder = new();

        public Dictionary<string, DungeonLayoutProfile> BuildLayoutProfiles()
        {
            // Winding blobby caverns with pools (reference: Korriban Caverns, Mon Cala caves).
            _builder.Create(Organic, "Organic Cavern")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.OrganicCave;
                    p.OpenFillTarget = 0.48;
                    p.SmoothingPasses = 4;
                    p.CorridorWidth = 2;
                    p.AccentDensity = 0.06;
                    // A single linear crossing (e.g. a stream cutting through a cavern floor) on top
                    // of the blob patches above; only takes effect where the composed tileset profile
                    // supplies an AccentTerrain (see DungeonComposition.BuildLayoutParameters).
                    p.AccentChannels = 1;
                });

            // Dense looping corridor maze with small chambers (reference: Veles Sewers).
            _builder.Create(Warren, "Corridor Warren")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.Warren;
                    p.CorridorWidth = 1;
                    p.LoopFactor = 0.3;
                    p.MinRooms = 3;
                    p.MaxRooms = 5;
                    p.MaxRoomCornerSize = 5;
                    p.AccentDensity = 0.05;
                    // AccentChannels intentionally NOT enabled here: WarrenLayout hard-caps chamber
                    // size at 5 corners, and a valid channel crossing needs 4 consecutive open corner
                    // rows/columns that avoid the chamber's own protected center-tile corners — in a
                    // 5-tall chamber, the two possible 4-row windows always include the center row
                    // (verified offline: 0 of 19 candidate windows survived the center-tile exclusion
                    // across sampled seeds). See LayoutAccentChannelCarver and BridgeChannelTests.
                    // FenceLines is NOT enabled here for the identical reason: a run needs length+2
                    // cells of contiguous fully-open CELLS along one axis (not corners), and Warren's
                    // CorridorWidth=1 open lanes are only 1 corner wide — a cell needs both its y and
                    // y+1 corner rows open to count as fully open, so a 1-wide lane contributes zero
                    // fully-open cells at all, leaving only the 5-corner-capped chambers (at most 4
                    // cells per axis) as candidate space (verified offline: 0 placements across 20
                    // seeds of the shipped Sewers/Warren composition, see FenceAndAlleyTests). See
                    // StandardLayoutProfiles.Halls for where FenceLines is wired instead.
                });

            // Wall-sharing packed rooms joined by door gaps (reference: facility interiors).
            _builder.Create(Packed, "Packed Rooms")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.PackedRooms;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 6;
                    p.LoopFactor = 0.25;
                    p.CorridorWidth = 1;
                });

            // Many varied chambers joined by broad looping halls (reference: crypt/temple interiors).
            _builder.Create(Halls, "Chambered Halls")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.RoomsAndCorridors;
                    p.MinRooms = 6;
                    p.MaxRooms = 9;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 6;
                    p.CorridorWidth = 2;
                    p.LoopFactor = 0.35;
                    // Rooms up to 6 corners (5 cells) plus 2-corner-wide open lanes (which, unlike
                    // Warren's 1-wide lanes, DO contribute fully-open cells along their own length)
                    // give LayoutFenceCarver's whole-grid scan plenty of candidate space — this is
                    // AncientRuin's (vmr01) production pairing (see AlienRuinDungeonDefinition), and
                    // vmr01 has verified Fence vocabulary; a no-op on any tileset without it.
                    p.FenceLines = 2;
                    // Channels are mode-agnostic (LayoutAccentChannelCarver operates purely on the
                    // primary-open-terrain corner grid, regardless of OpenLane/Tunnel) and this is
                    // AncientRuin's (vmr01) default pairing -- enabling it here is what actually
                    // exercises vmr01's Chasm channel/bank vocabulary (see StandardTilesetProfiles.
                    // AncientRuin.ChannelTerrain) in the shipped default composition. A no-op on any
                    // tileset without a channel terrain configured.
                    p.AccentChannels = 1;
                });

            // Discrete rooms joined by wall-embedded tunnel corridors with doorway junctions
            // (reference: czs220_maintlvl facility maintenance level). Tunnels traverse via crosser
            // edges, so pathnode-restricted tilesets (zsf01) work at their natural corridor width.
            _builder.Create(Complex, "Corridor Complex")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.RoomsAndCorridors;
                    p.CorridorMode = CorridorMode.Tunnel;
                    p.MinRooms = 6;
                    p.MaxRooms = 9;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 5;
                    p.LoopFactor = 0.3;
                    // Requests up to 2 raised floor/wall patches from LayoutElevationPainter; a no-op
                    // on any tileset profile whose MaxElevationRegions is 0 (every profile today except
                    // BaseGameTilesetProfiles.Dungeon -- see DungeonComposition.BuildLayoutParameters,
                    // which clamps this down, and LayoutElevationPainter, which independently re-probes
                    // the real tileset before ever painting a corner). Not set on Halls/Organic yet --
                    // Dungeon's own registered pairing is Complex/Halls/Organic, but only Complex is
                    // exercised with elevation by the current profile.
                    p.ElevationRegions = 2;
                    // Ramp lanes are a bonus on top of ElevationRegions' own placed blobs (no-op
                    // without at least one raised blob, and self-gated per-tileset regardless -- see
                    // MacroLayoutParameters.ElevationRamps/LayoutElevationPainter.TryAddRampLane).
                    p.ElevationRamps = true;
                    // Requests up to 2 depth pools from LayoutElevationPoolPainter; a no-op on any
                    // tileset profile whose MaxPoolRegions is 0 or AccentTerrain is empty (see
                    // DungeonComposition.BuildLayoutParameters).
                    p.PoolRegions = 2;
                    // Requests up to 2 per-corner relief regions from LayoutReliefPainter (heights
                    // refined corner-by-corner on top of whatever the two passes above painted); a
                    // no-op on any tileset profile whose MaxReliefRegions is 0 (see
                    // DungeonComposition.BuildLayoutParameters and LayoutReliefPainter's own
                    // capability gate).
                    p.ReliefRegions = 2;
                });

            // Exterior city blocks joined by wall-embedded Alley crosser tunnels instead of Corridor
            // (reference: vmr01 exterior "streets" feel). Identical shape/mechanics to Complex --
            // discrete plazas joined by wall-embedded tunnels with junction ports -- just carved with
            // CorridorCrosserType.Alley. Unlike LayoutFenceCarver, LayoutTunnelCarver does not probe
            // tileset capability itself (Alley edges are labeled purely from corner geometry, the same
            // as Corridor/Doorway), so this profile is only meant for vmr01, the one generation
            // tileset with verified Alley vocabulary (see AlleyCorridorTests) -- pairing it with a
            // tileset lacking Alley tiles would make every generation attempt fail tile resolution and
            // exhaust the request's retry budget. Additive: no existing profile pairing selects this by
            // default (see AlienRuinDungeonDefinition, which still defaults to Halls).
            _builder.Create(Streets, "City Streets")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.RoomsAndCorridors;
                    p.CorridorMode = CorridorMode.Tunnel;
                    p.CorridorCrosserType = CorridorCrosserType.Alley;
                    p.MinRooms = 6;
                    p.MaxRooms = 9;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 5;
                    p.LoopFactor = 0.3;
                });

            // Near-perfect winding maze with a handful of small chambers (reference: classic labyrinth).
            _builder.Create(Labyrinth, "Labyrinth")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.Labyrinth;
                    p.CorridorWidth = 1;
                    p.LoopFactor = 0.05;
                    p.MinRooms = 3;
                    p.MaxRooms = 4;
                    p.MinRoomCornerSize = 2;
                    p.MaxRoomCornerSize = 4;
                });

            return _builder.Build();
        }
    }
}
