#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A resolved (content, tileset, layout) triple ready to drive a generation request.
    /// </summary>
    public class DungeonComposition
    {
        public DungeonDetail Content { get; set; }
        public DungeonTilesetProfile Tileset { get; set; }
        public DungeonLayoutProfile Layout { get; set; }

        /// <summary>
        /// Clones the layout template and stamps the tileset's accent terrain name when the
        /// layout wants accents and the tileset supports them; otherwise accents are disabled.
        /// </summary>
        public MacroLayoutParameters BuildLayoutParameters()
        {
            var parameters = Layout.Template.Clone();
            // Exterior solid inversion (see DungeonTilesetProfile.SolidTerrainOverride): stamp the
            // profile's declared solid so LayoutSolver.Solve keeps it instead of
            // defaulting to the tileset's GENERAL Default terrain. Empty for every interior profile --
            // Solve's own empty-means-Default stamp is unchanged there.
            parameters.SolidTerrain = Tileset.SolidTerrainOverride ?? string.Empty;
            // Platform apron (see LayoutPlatformApronPainter): a chasm-margin city composition
            // paves the frontage anchor ring so its towers stand on real platform surface -- the
            // hand-built composition. Needs BOTH declarations: frontage walls to stand on it and
            // chasm semantics to need it; every other composition's corner plan stays untouched.
            parameters.PlatformApron =
                Tileset.FrontageBuildings.Count > 0 && Tileset.ChasmTerrains.Count > 0;
            parameters.AccentTerrain =
                parameters.AccentDensity > 0 && !string.IsNullOrEmpty(Tileset.AccentTerrain)
                    ? Tileset.AccentTerrain
                    : string.Empty;
            if (parameters.AccentTerrain.Length == 0)
                parameters.AccentDensity = 0;
            // Channels have their own terrain slot (ChannelTerrain) because some tilesets have
            // verified channel/bank coverage against a terrain with no verified blob-patch coverage
            // (vmr01's Chasm) — fall back to AccentTerrain when the tileset never set it separately,
            // preserving the original single-terrain behavior for every other tileset.
            var channelSource = !string.IsNullOrEmpty(Tileset.ChannelTerrain) ? Tileset.ChannelTerrain : Tileset.AccentTerrain;
            parameters.ChannelTerrain =
                parameters.AccentChannels > 0 && !string.IsNullOrEmpty(channelSource)
                    ? channelSource
                    : string.Empty;
            if (parameters.ChannelTerrain.Length == 0)
                parameters.AccentChannels = 0;
            // Roads have no terrain slot of their own (see DungeonTilesetProfile.RoadCrosser's doc
            // comment) -- just the crosser name, gated the same "layout expresses intent via RoadLanes,
            // tileset caps it to verified support" shape as every other declared capability here.
            parameters.RoadCrosser = Tileset.RoadCrosser ?? string.Empty;
            if (parameters.RoadCrosser.Length == 0)
            {
                parameters.RoadLanes = 0;
            }
            else
            {
                // A road lane can only occupy FULLY-open tiles (LayoutRoadCarver never repaints
                // corners), and a 1-wide open lane has no fully-open tile at all -- its cells all
                // straddle the lane's own edges -- so on a road-declaring composition, 1-wide
                // corridors would confine every street to room interiors (measured on
                // fcx01/futcity_plaza at size 20: road-edge share 0.016 at width 1 vs ~0.10 at
                // width 2, against the hand-built fcx01 reference's 0.102). 2-wide lanes are also
                // what hand-built fcx01 streets are. Independent of MinimumOpeningWidth (a pathnode
                // WALKABILITY floor -- Cobble2's partially-open tiles genuinely walk fine at width 1,
                // see PathNodeOpeningWidthAudit); this is road GEOMETRY.
                parameters.CorridorWidth = Math.Max(parameters.CorridorWidth, 2);
            }
            parameters.CorridorWidth = Math.Max(parameters.CorridorWidth, Tileset.MinimumOpeningWidth);
            // Tunnel body/port crosser vocabulary: a tileset profile may declare an alternate crosser
            // family (e.g. tdc01's GreyCorridor body paired with the canonical Doorway port) that is
            // mechanically identical to the canonical Corridor/Doorway family LayoutTunnelCarver
            // defaults to, just under different names. Only takes effect for Tunnel-mode Corridor-type
            // layouts -- an Alley-mode (Streets-style) layout profile keeps its own vmr01-verified
            // vocabulary untouched -- and only when the tileset actually declared both halves of the
            // pair; MacroLayoutGenerator still re-probes the real shape inventory before dispatch
            // (see its Custom-mode downgrade), the same "tileset declares intent, generator re-verifies"
            // shape as every other tileset-declared capability here.
            if (parameters.CorridorCrosserType == CorridorCrosserType.Corridor &&
                !string.IsNullOrEmpty(Tileset.TunnelBodyCrosser) && !string.IsNullOrEmpty(Tileset.TunnelPortCrosser))
            {
                parameters.CorridorCrosserType = CorridorCrosserType.Custom;
                parameters.TunnelBodyCrosser = Tileset.TunnelBodyCrosser;
                parameters.TunnelPortCrosser = Tileset.TunnelPortCrosser;
            }
            // Unconditional pass-through: RoomsAndCorridorsLayout itself gates all district behavior
            // (and every extra RNG draw) behind CorridorMode == Tunnel, so stamping this even for a
            // layout profile that never uses Tunnel mode is inert.
            parameters.SecondaryOpenTerrain = Tileset.SecondaryOpenTerrain ?? string.Empty;
            // Shared reference is fine: FeatureTiles/SetPieces are never mutated after a tileset
            // profile is built, only read by the resolver/stamper.
            parameters.FeatureTiles = Tileset.FeatureTiles;
            parameters.SetPieces = Tileset.SetPieces;
            // Multi-tile OpenSetPiece stamping needs rooms bigger than the group footprint + margin +
            // one spare tile (see DungeonTilesetProfile.SetPieceRoomCornerFloor's doc comment) -- floor
            // the layout's room-size ceiling to the tileset's declared physical need, mirroring the
            // CorridorWidth/MinimumOpeningWidth floor above and the PoolRegions room-size floor below.
            // MinRoomCornerSize is floored 2 corners lower (never above the ceiling): if the layout
            // kept rolling its own small-room minimum, most rooms would still be too small to host any
            // stamp -- measured on fcx01/Complex at size 20, Min left at 3 placed a 2x2 group on only
            // 17/30 seeds (mean 2.4 group tiles/area) vs the same composition with Min floored to 5.
            // LayoutParameterConstraints.ClampToValid still applies its own empirically-measured
            // per-size ceiling afterward (e.g. corner 6 at size 20), so this floor never pushes a
            // composition past the measured-safe room size for its area dimensions.
            // Gated on configured SetPieces so a declared floor with nothing to stamp stays inert.
            if (Tileset.SetPieceRoomCornerFloor > 0 && Tileset.SetPieces.Count > 0)
            {
                parameters.MaxRoomCornerSize = Math.Max(parameters.MaxRoomCornerSize, Tileset.SetPieceRoomCornerFloor);
                parameters.MinRoomCornerSize = Math.Max(parameters.MinRoomCornerSize, Tileset.SetPieceRoomCornerFloor - 2);
                parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);
                // Room-supply scaling is only ever stamped inside the same gate: a set-piece-heavy
                // declaration without a corner floor or without configured SetPieces has nothing to
                // scale FOR (the room-count derivation is sized off the floored room envelope above).
                // Width/Height are not known here (LayoutSolver stamps them per attempt), so this only
                // records intent; MacroLayoutGenerator.Generate applies the actual derivation via
                // LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling once dimensions exist.
                parameters.SetPieceRoomSupplyScaling = Tileset.SetPieceRoomSupplyScaling;
            }
            // Contiguous building blocks: only meaningful with configured SetPieces (same gate shape
            // as the supply-scaling stamp above, minus the corner-floor requirement -- adjacency is a
            // placement rule, not a room-size need) and inert without a road-declaring composition
            // (LayoutGroupStamper's OpenSetPiece path is the only consumer).
            if (Tileset.SetPieces.Count > 0)
                parameters.BuildingBlockContiguity = Tileset.BuildingBlockContiguity;
            // Straight-avenue street routing (see LayoutRoadCarver): the tileset declares its
            // hand-built streets are straight boulevards; non-declaring compositions keep the
            // legacy lane geometry byte-for-byte.
            parameters.StraightStreetRouting = Tileset.StraightStreetRouting;
            parameters.ExitGroups = Tileset.ExitGroups;
            parameters.DoorSlotCrossers = Tileset.DoorSlotCrossers;
            parameters.ExcludedTiles = Tileset.ExcludedTiles;
            // Layout expresses intent (e.g. StandardLayoutProfiles.Complex's ElevationRegions), the
            // tileset profile caps it to verified support -- 0 on every profile except
            // BaseGameTilesetProfiles.Dungeon means this is a no-op everywhere else today.
            parameters.ElevationRegions = Math.Min(parameters.ElevationRegions, Tileset.MaxElevationRegions);
            // Depth pools reuse the tileset's own blob-patch AccentTerrain as the pool terrain (e.g.
            // tde01's "Lava") -- never enabled without one, and clamped to the tileset's own verified
            // pool-vocabulary cap, mirroring ElevationRegions/MaxElevationRegions immediately above.
            parameters.PoolTerrain =
                parameters.PoolRegions > 0 && !string.IsNullOrEmpty(Tileset.AccentTerrain)
                    ? Tileset.AccentTerrain
                    : string.Empty;
            parameters.PoolRegions = parameters.PoolTerrain.Length == 0
                ? 0
                : Math.Min(parameters.PoolRegions, Tileset.MaxPoolRegions);
            // Per-corner relief mirrors the elevation/pool clamp shape exactly; the blend terrain and
            // ramp-crosser name are pure tileset vocabulary (stamped unconditionally -- both are inert
            // whenever the passes that read them are inactive or the names never resolve).
            parameters.ReliefRegions = Math.Min(parameters.ReliefRegions, Tileset.MaxReliefRegions);
            parameters.ReliefBlendTerrain = Tileset.ReliefBlendTerrain ?? string.Empty;
            parameters.RampCrosser = Tileset.RampCrosser ?? string.Empty;
            // A pool's own room-scoped rim+interior+rim footprint (LayoutElevationPoolPainter's
            // MinOuterSpan, 3 tiles) needs a room at least MinOuterSpan+2 tiles wide/tall on the
            // placement axis (the mechanism's own 1-corner-inset room-boundary margin on top of that).
            // Empirically, a layout profile's nominal MaxRoomCornerSize ceiling (e.g. Complex's 5) is
            // rarely actually realized once RoomsAndCorridorsLayout's own placement-attempt "degrade"
            // and overlap rejection are in play, so floor generously (+2 tiles of headroom) rather than
            // to the bare minimum -- mirroring CorridorWidth's own floor against
            // Tileset.MinimumOpeningWidth immediately above. A no-op whenever PoolRegions ended up 0.
            if (parameters.PoolRegions > 0)
            {
                var floor = Layouts.LayoutElevationPoolPainter.MinOuterSpan + 4;
                parameters.MaxRoomCornerSize = Math.Max(parameters.MaxRoomCornerSize, floor);
                parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);
            }
            return parameters;
        }
    }
}
