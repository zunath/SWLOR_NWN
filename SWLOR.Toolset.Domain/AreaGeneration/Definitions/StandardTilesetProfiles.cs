#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Tileset profiles for the verified generation tilesets. Lighting values are sampled from
    /// hand-built reference areas; accent terrains are only declared where the tileset has full
    /// (open, accent) corner coverage among resolver-usable tiles (verified offline).
    /// </summary>
    public class StandardTilesetProfiles : IDungeonTilesetProfileListDefinition
    {
        public const string Cavern = "cavern";
        public const string Sewers = "sewers";
        public const string Facility = "facility";
        public const string AncientRuin = "ancientruin";

        private readonly DungeonTilesetProfileBuilder _builder = new();

        public Dictionary<string, DungeonTilesetProfile> BuildTilesetProfiles()
        {
            // Sea Caves (reference: moncaladungeon1). Water accents fully covered.
            // Feature tiles: tdt01 open ('Floor') 1x1 groups (Portal/Chessboard deliberately excluded).
            _builder.Create(Cavern, "Sea Caves")
                .Tileset("tdt01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Water")
                .FeatureTile("Treasure01", 2)
                .FeatureTile("Treasure02", 2)
                .FeatureTile("Pillar01")
                .FeatureTile("Pillar02")
                .FeatureTile("Hot_Springs")
                .SetPiece("Platform01_2x3")
                .SetPiece("Platform02_2x2")
                // L-shaped 2x2 with a hole (TileIds contains -1): LayoutGroupStamper's OpenSetPiece
                // classifier now tolerates a hole slot as ordinary plan space, not a real member (see
                // TryClassify's hole handling) -- all three real members are Floor-cornered, matching
                // the primary OpenSetPiece rule the same way Platform01/02 do.
                .SetPiece("Platform03_2x2")
                .SetPiece("Pillar_1x2", 2)
                .SetPiece("WallSection01_1x2")
                .SetPiece("WallSection02_1x2")
                .SetPiece("BigDoor01", 1)
                .SetPiece("BigDoor02", 1)
                // Bridge gate: LayoutGroupStamper's CorridorInsert classifier splices this into a
                // Water channel span (LayoutAccentChannelCarver, active by default via Organic's
                // AccentChannels — see StandardLayoutProfiles.Organic).
                .SetPiece("BridgeDoor", 1)
                // Doorway-pair pass-through segment: LayoutGroupStamper's CorridorInsert classifier
                // splices this into a straight Corridor chain by rewriting the two flanking plan edges
                // to Doorway (see TryPlaceCorridorInsert's Doorway branch); a no-op under Cavern's
                // default OpenLane (Organic) pairing, exercisable via Complex (Tunnel mode).
                .SetPiece("Door_Trans", 1)
                // Corridor-terminal stairs: LayoutGroupStamper's CorridorStub classifier splices these
                // onto an existing Tunnel-mode chain as a dead-end cap; a no-op under Cavern's default
                // OpenLane (Organic) pairing, exercisable via Complex (Tunnel mode).
                .SetPiece("StairsDown01", 1)
                .SetPiece("StairsUp01", 1)
                // 2x2 stairs: all-Floor-cornered OpenSetPiece with one tolerated door slot (the tile's
                // own art carries the doorframe; no door object is spawned).
                .SetPiece("StairsDown_2x2")
                .SetPiece("StairsUp_2x2")
                .ExitGroup("Exit01")
                .ExitGroup("Exit02")
                .ExitGroup("Exit03");

            // Cavern's own bulk "set dressing" palette — mined from tdt01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tdt01'], 4 areas: barrows_door_corridor,
            // proof_barrows, etc). Strongest co-occurrence pair: plc_waterdrip + zep_fireflies001 (13
            // occurrences) -> CaveDripGlow vignette.
            _builder
                .Decoration("zep_water001", 4, DecorationContext.WallAdjacent)
                .Decoration("zep_fireflies001", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_tno_rockldg2", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_bushfern001", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_debri20", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_tno_cliff_1", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_debri19", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_bushfern001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_fireflies001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_water001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_boulder003", 2, DecorationContext.RoomCenter)
                .Decoration("zep_altar002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_colony9", 1, DecorationContext.DoorwayFlank)
                .Vignette("CaveDripGlow", 3)
                .VignetteMember("zep_water001", 0f, 0f)
                .VignetteMember("zep_fireflies001", 1.5f, 0.8f);

            // Sewers (reference: veles_sewers). Pit channel accents fully covered.
            // Feature tiles: tds01 open ('Floor') 1x1 groups (Portal/Chessboard deliberately excluded).
            _builder.Create(Sewers, "Sewers")
                .Tileset("tds01")
                .Placeholder("gen_placeholder3")
                .TileLighting(0, 2, 2, 2)
                .AccentTerrain("Pit")
                .FeatureTile("Treasure01", 2)
                .FeatureTile("Treasure02", 2)
                .FeatureTile("Pillar01")
                .FeatureTile("Pillar02")
                .FeatureTile("Pillar03")
                .FeatureTile("Camp")
                .SetPiece("Platform01_2x2")
                .SetPiece("Platform02_2x2")
                // L-shaped 2x2 with a hole (TileIds contains -1): see tdt01's Platform03_2x2 note above.
                .SetPiece("Platform03_2x2")
                .SetPiece("Pillar_1x2", 2)
                .SetPiece("WallSection01_2x1")
                .SetPiece("WallSection02_2x1")
                .SetPiece("CampWall")
                .SetPiece("BigDoor01", 1)
                .SetPiece("BigDoor02", 1)
                // Fence gate: LayoutGroupStamper's CorridorInsert classifier splices one of these into
                // a straight Fence run LayoutFenceCarver carves (see StandardLayoutProfiles.Warren,
                // this tileset's production layout pairing); a no-op when no Fence run exists.
                .SetPiece("FenceDoor01", 1)
                .SetPiece("FenceDoor02", 1)
                // Bridge gate: splices into a Pit channel span (LayoutAccentChannelCarver); a no-op
                // under Sewers' default Warren pairing (Warren deliberately never enables
                // AccentChannels — see StandardLayoutProfiles.Warren), exercisable via Organic (this
                // tileset's other verified channel pairing, see BridgeChannelTests).
                .SetPiece("BridgeDoor01", 1)
                // Corridor-terminal stairs: a no-op under Warren's OpenLane corridors, exercisable via
                // Complex (Tunnel mode).
                .SetPiece("StairsDown", 1)
                .SetPiece("StairsUp", 1)
                // 2x2 stairs: all-Floor-cornered OpenSetPiece with one tolerated door slot.
                .SetPiece("StairsDown_2x2")
                .SetPiece("StairsUp_2x2")
                .ExitGroup("Exit01")
                .ExitGroup("Exit02");

            // Sewers' own bulk palette — mined from tds01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tds01'], 4 areas). Strongest
            // co-occurrence pairs: plc_dustplume + x0_tomb (63) and _mdrn_pl_pall009 + _mdrn_pl_ration
            // (38) -> vignettes.
            _builder
                .Decoration("_mdrn_pl_crate08", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_conta52", 3, DecorationContext.WallAdjacent)
                .Decoration("swd_build007", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_generas", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_machin1", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_chrmtgr", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_fog001", 3, DecorationContext.CorridorSide)
                .Decoration("zep_fog002", 2, DecorationContext.CorridorSide)
                .Decoration("zep_water001", 2, DecorationContext.CorridorSide)
                .Decoration("structure_rubble", 2, DecorationContext.CorridorSide)
                .Decoration("structure_rubble", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_frcfw2", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_carpt04", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_ration", 1, DecorationContext.DoorwayFlank)
                .Vignette("SewerRationStash", 2)
                .VignetteMember("_mdrn_pl_ration", 0f, 0f)
                .VignetteMember("_mdrn_pl_pall009", 1.2f, 0.4f);

            // Sci-Fi Base (reference: czs220_maintlvl). No accent coverage. Every partially-open
            // corner combo on zsf01 carries a movement-restricted pathnode (H/I) — only fully-open
            // tiles are pathnode A — so 1-wide door gaps and corridors fail the engine path check.
            _builder.Create(Facility, "D20 SciFi Base CQ")
                .Tileset("zsf01")
                .Placeholder("gen_placeholder2")
                .TileLighting(4, 0, 2, 2)
                // Family AREA atmosphere -- the SWLOR standard windowless-interior .are tuple, mined
                // from the hand-built zsf01 exemplars: cz220shipbreaker, cz220shipbreakin,
                // czs220_maintlvl (3 of 9 module areas agree exactly on the full core tuple; the
                // runner-up tuple has 2). Locked night with dim blue-grey moon ambient, no skybox,
                // no fog, no wind. FogClipDist 45 is the modal value among the agreeing areas (2/3).
                .Atmosphere(a =>
                {
                    a.SkyBox = 0;
                    a.DayNightCycle = false;
                    a.IsNight = true;
                    a.SunAmbientColor = 0;
                    a.SunDiffuseColor = 0;
                    a.MoonAmbientColor = 2960685;
                    a.MoonDiffuseColor = 6457991;
                    a.SunFogAmount = 0;
                    a.SunFogColor = 0;
                    a.MoonFogAmount = 5;
                    a.MoonFogColor = 0;
                    a.SunShadows = false;
                    a.MoonShadows = false;
                    a.ShadowOpacity = 60;
                    a.WindPower = 0;
                    a.LightingScheme = 13;
                    a.FogClipDist = 45f;
                })
                .MinimumOpeningWidth(2)
                // zsf01's declared floor ("Floor2") has a single fully-open tile; 'floor' is the
                // terrain czs220_maintlvl builds its rooms from (3 diagonal variants + doorway tiles).
                .PrimaryOpenTerrain("floor")
                // Multi-terrain districts: 'Floor2' has full 16/16 corner coverage vs 'wall' plus
                // Doorway-junction tiles (TILE54 verified offline), so some rooms can be carved as
                // walled Floor2 districts joined to 'floor' rooms via Tunnel-mode corridors — this
                // unlocks Floor2's own separate ~13-tile vocabulary that was otherwise unreachable under
                // the single-open-terrain constraint. Only takes effect because Facility's default
                // layout profile (Complex) already uses Tunnel mode.
                .SecondaryOpenTerrain("Floor2")
                .SetPiece("Cell", 2)
                .SetPiece("Room", 2)
                .SetPiece("Bedroom")
                .SetPiece("2x1Room")
                .SetPiece("Transiton")
                // Corridor-terminal stairs (all-wall corners, single Corridor edge, no door slot):
                // exercisable in Facility's own default Complex (Tunnel mode) pairing.
                .SetPiece("StairsUP", 1)
                .SetPiece("StairsDOWN", 1);

            // Facility's own bulk palette — mined from zsf01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['zsf01'], 8 areas). Strongest
            // co-occurrence pair: _mdrn_pl_hwall33 + _mdrn_pl_lghtpl3 (8) -> vignette.
            _builder
                .Decoration("_mdrn_pl_contpan", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall33", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_divider", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_conso23", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_contr01", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_cabi002", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_clntnke", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_cryotk2", 2, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_engctr1", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_lghtpl3", 3, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_fac13xe", 1, DecorationContext.DoorwayFlank)
                .Vignette("FacilityLightPanel", 3)
                .VignetteMember("_mdrn_pl_hwall33", 0f, 0f)
                .VignetteMember("_mdrn_pl_lghtpl3", 0.8f, 1.2f);

            // Alien Ruin (reference: korr_crypt_zil). Chasm lacks coverage — no accents.
            // 'Plaza' carries 11 fully-open tile variants vs 4 on the declared 'Floor'.
            // Feature tiles: vmr01's nine curated 1x1 groups (Portal/Chessboard/Mosaic_Plaza_2x2
            // deliberately excluded). InteriorRubble/RuinedHouse are Floor-cornered: with no
            // SecondaryOpenTerrain their corner key never appears in a Plaza-only layout, so they were
            // previously placed zero times; once districts are active (SecondaryOpenTerrain("Floor")
            // below, composed with a Tunnel-mode layout) Floor rooms actually exist and these start
            // matching automatically — no resolver change needed, TileResolver's corner-key lookup is
            // already terrain-agnostic.
            _builder.Create(AncientRuin, "D20 Alien Ruins")
                .Tileset("vmr01")
                .Placeholder("gen_placeholder4")
                .TileLighting(31, 27, 10, 12)
                .PrimaryOpenTerrain("Plaza")
                // Channel-only accent: Chasm has verified bank (half-Chasm/half-Plaza, single Bridge
                // edge, e.g. TILE52) and span (all-Chasm, opposite Bridge pair, e.g. TILE47-51/98)
                // coverage against Plaza (LayoutAccentChannelCarver's v1 scope is always the primary
                // open terrain), but no verified blob-patch (LayoutAccentPainter) coverage against any
                // open terrain, so AccentTerrain stays empty and only ChannelTerrain is set.
                .ChannelTerrain("Chasm")
                // Multi-terrain districts: 'Floor' has full 16/16 corner coverage vs 'Wall' plus
                // Doorway-junction tiles (TILE6 verified offline), so some rooms can be carved as walled
                // Floor districts joined to Plaza rooms via Tunnel-mode corridors — this unlocks
                // Floor's ~50-tile vocabulary (including feature/group tiles like InteriorMosaic_2x2
                // that structurally require Floor corners, see SetPiece("Amphitheater_2x2") note below)
                // that was otherwise unreachable under the single-open-terrain constraint. Only takes
                // effect when composed with a Tunnel-mode layout profile (Streets; the shipped default
                // Halls pairing stays OpenLane and never activates districts).
                .SecondaryOpenTerrain("Floor")
                .FeatureTile("ExteriorFountain", 2)
                .FeatureTile("ExteriorOvergrownGarden", 2)
                .FeatureTile("ExteriorPool", 2)
                .FeatureTile("InteriorRubble", 2)
                .FeatureTile("RuinedHouse", 2)
                .FeatureTile("Exterior Pillar 1")
                .FeatureTile("Exterior Pillar 2")
                .FeatureTile("Exterior Dais 1")
                .FeatureTile("Exterior Dais 2")
                // InteriorMosaic_2x2's corners are 'Floor', not 'Plaza' — LayoutGroupStamper's
                // OpenSetPiece classifier now determines a piece's own open terrain and restricts
                // placement to rooms carved from that same terrain (LayoutRoom.OpenTerrain), so this
                // only ever stamps into a Floor district room (created when districts are active) and
                // is otherwise silently unplaceable, exactly like the feature tiles above.
                .SetPiece("InteriorMosaic_2x2")
                .SetPiece("Amphitheater_2x2")
                .SetPiece("Mosaic_Plaza_2x2")
                .SetPiece("ExteriorWalkway_2x2")
                .SetPiece("Exterior Platform 2x2")
                .SetPiece("InteriorHallDoor", 1)
                // Fence/Alley gates: LayoutGroupStamper's CorridorInsert classifier splices one of
                // these into a straight Fence run (LayoutFenceCarver) or Alley tunnel segment
                // (LayoutTunnelCarver with CorridorCrosserType.Alley, see
                // StandardLayoutProfiles.Streets); a no-op unless that run/mode is actually used.
                .SetPiece("InteriorFenceDoor", 1)
                .SetPiece("ExteriorFenceDoor", 1)
                .SetPiece("BigDoorAlley", 1)
                // Bridge gate: splices into a Chasm channel span; active by default via Halls'
                // AccentChannels (see StandardLayoutProfiles.Halls / StandardTilesetProfiles.
                // AncientRuin.ChannelTerrain).
                .SetPiece("BridgeDoor01", 1)
                // Corridor-terminal stairs: Corridor-crosser variants splice onto a Complex (Tunnel)
                // chain; Alley-crosser variants splice onto a Streets (Alley Tunnel) chain. Both are
                // a no-op under AncientRuin's default OpenLane (Halls) pairing.
                .SetPiece("InteriorStairsDown", 1)
                .SetPiece("InteriorStairsUp", 1)
                .SetPiece("ExteriorStairsDown", 1)
                .SetPiece("ExteriorStairsUp", 1)
                // 2x2 stairs/tower: all-Plaza-cornered OpenSetPiece, each with one tolerated door slot.
                .SetPiece("ExteriorStairsDown_2x2")
                .SetPiece("ExteriorStairsUp_2x2")
                .SetPiece("ExteriorRuinedTower_2x2")
                // WallAlcove: small enclosed wall chambers with a doorframe object but no Doorway
                // crosser vocabulary of their own (see LayoutGroupStamper.TryPlaceWallAlcove).
                .SetPiece("Room 1 2x2")
                .SetPiece("Room 2 2x2")
                .SetPiece("Room 3 2x2")
                .SetPiece("Room 4 2x2")
                .SetPiece("Room 5 2x2")
                .ExitGroup("ExteriorExit01")
                .ExitGroup("ExteriorExit02");

            // Ancient Ruin's own bulk palette — mined from vmr01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['vmr01'], 5 areas). Strongest
            // co-occurrence pair: _mdrn_pl_frcf002 + _mdrn_pl_frcfw (36, a force-field gate pairing)
            // -> vignette.
            _builder
                .Decoration("_mdrn_pl_hwall34", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_walls06", 3, DecorationContext.WallAdjacent)
                .Decoration("swd3_wall001", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall11", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall06", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_walls09", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_carpt04", 2, DecorationContext.CorridorSide)
                .Decoration("swd_flormh01", 1, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_pillr18", 2, DecorationContext.RoomCenter)
                .Decoration("swd_conta002", 1, DecorationContext.RoomCenter)
                .Decoration("swp_banner0001", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_frcfw", 1, DecorationContext.DoorwayFlank)
                .Vignette("RuinForcefieldGate", 2)
                .VignetteMember("_mdrn_pl_frcfw", 0f, 0f)
                .VignetteMember("_mdrn_pl_frcf001", 0f, 1.0f);

            return _builder.Build();
        }
    }
}
