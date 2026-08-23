#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Atmosphere;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Frontage;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>Fluent builder for tileset profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonTilesetProfileBuilder
    {
        private readonly Dictionary<string, DungeonTilesetProfile> _profiles = new();
        private DungeonTilesetProfile _active;

        public DungeonTilesetProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonTilesetProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            _activeDecorationProfile = null;
            _activeVignette = null;
            _lastDecorationEntry = null;
            return this;
        }

        public DungeonTilesetProfileBuilder Tileset(string tilesetResref)
        {
            _active.TilesetResref = tilesetResref;
            return this;
        }

        public DungeonTilesetProfileBuilder Placeholder(string placeholderResref)
        {
            _active.PlaceholderResref = placeholderResref;
            return this;
        }

        public DungeonTilesetProfileBuilder TileLighting(int mainLight1, int mainLight2, int sourceLight1, int sourceLight2)
        {
            _active.Lighting = new DungeonTileLighting
            {
                MainLight1 = mainLight1,
                MainLight2 = mainLight2,
                SourceLight1 = sourceLight1,
                SourceLight2 = sourceLight2
            };
            return this;
        }

        /// <summary>
        /// Declares the terrain name this tileset uses for accent patches. Only set this after
        /// verifying full (open, accent) corner coverage among resolver-usable tiles.
        /// </summary>
        public DungeonTilesetProfileBuilder AccentTerrain(string terrainName)
        {
            _active.AccentTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares a separate terrain for accent CHANNEL/bank coverage when it differs from
        /// AccentTerrain (the blob-patch terrain). Only set this after verifying channel/bank tile
        /// coverage against the current PrimaryOpenTerrain (see LayoutAccentChannelCarver).
        /// </summary>
        public DungeonTilesetProfileBuilder ChannelTerrain(string terrainName)
        {
            _active.ChannelTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares the narrowest opening width (in corners) this tileset can path through. Set to
        /// 2 for tilesets whose partially-open corner combos all carry movement-restricted pathnodes.
        /// </summary>
        public DungeonTilesetProfileBuilder MinimumOpeningWidth(int width)
        {
            _active.MinimumOpeningWidth = width;
            return this;
        }

        /// <summary>
        /// Declares the room-size floor (in corners) this tileset's multi-tile OpenSetPiece groups
        /// need to ever stamp -- see DungeonTilesetProfile.SetPieceRoomCornerFloor. Compositions raise
        /// the layout profile's MaxRoomCornerSize to at least this whenever the profile also configures
        /// SetPieces.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPieceRoomCornerFloor(int cornerSize)
        {
            _active.SetPieceRoomCornerFloor = cornerSize;
            return this;
        }

        /// <summary>
        /// Declares this tileset as set-piece-heavy: compositions scale the layout profile's room
        /// counts with area so larger areas carry proportionally more stampable rooms -- see
        /// DungeonTilesetProfile.SetPieceRoomSupplyScaling for the measured rationale and why this
        /// is separate from SetPieceRoomCornerFloor. Declare only alongside SetPieceRoomCornerFloor
        /// and configured SetPieces.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPieceRoomSupplyScaling()
        {
            _active.SetPieceRoomSupplyScaling = true;
            return this;
        }

        /// <summary>
        /// Declares that this tileset's stamped buildings assemble into contiguous blocks walling the
        /// street network, the hand-built city pattern -- see
        /// DungeonTilesetProfile.BuildingBlockContiguity for the measured evidence and the exact
        /// placement-rule changes. Declare only alongside configured SetPieces on a road-declaring
        /// (RoadCrosser) tileset whose building groups carry mutually compatible perimeter corner
        /// labels (verified for fcx01: every Cobble-district tower group has uniform open-cornered,
        /// crosser-free perimeter faces, and the Cobble2 district's towers likewise agree with each
        /// other).
        /// </summary>
        public DungeonTilesetProfileBuilder BuildingBlockContiguity()
        {
            _active.BuildingBlockContiguity = true;
            return this;
        }

        /// <summary>
        /// Declares that this tileset's street lanes route as straight avenues (shortest, then
        /// fewest-turns paths) instead of the legacy first-found breadth-first geometry -- see
        /// DungeonTilesetProfile.StraightStreetRouting for the measured evidence. Declare on
        /// road-declaring city tilesets whose hand-built reference streets are straight
        /// boulevards; every non-declaring tileset keeps byte-identical lane geometry.
        /// </summary>
        public DungeonTilesetProfileBuilder StraightStreetRouting()
        {
            _active.StraightStreetRouting = true;
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.ElevationRegions request this tileset's real tile
        /// inventory has verified rim vocabulary for. Only set after verifying with
        /// LayoutElevationPainter's shape probe (TileResolver.HasHeightAwareCandidate) against the
        /// composed PrimaryOpenTerrain/solid terrain -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxElevationRegions(int count)
        {
            _active.MaxElevationRegions = count;
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.PoolRegions request this tileset's real tile
        /// inventory has verified depth-pool vocabulary for. Only set after verifying with
        /// LayoutElevationPoolPainter's shape probe (TileResolver.HasHeightAwareCandidate) against the
        /// composed PrimaryOpenTerrain/AccentTerrain pairing -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxPoolRegions(int count)
        {
            _active.MaxPoolRegions = count;
            return this;
        }

        /// <summary>
        /// Overrides the terrain used for open space. Only set after verifying full (open, solid)
        /// corner coverage for that terrain among resolver-usable tiles.
        /// </summary>
        /// <summary>
        /// Overrides the SOLID (wall) terrain for tilesets whose GENERAL Default is actually the
        /// walkable ground (the exterior inversion) -- see DungeonTilesetProfile.SolidTerrainOverride.
        /// Only set after verifying full 16/16 simple-tile coverage of PrimaryOpenTerrain against this
        /// solid AND that the fully-open terrain tile is pathnode A.
        /// </summary>
        public DungeonTilesetProfileBuilder SolidTerrainOverride(string terrainName)
        {
            _active.SolidTerrainOverride = terrainName;
            return this;
        }

        public DungeonTilesetProfileBuilder PrimaryOpenTerrain(string terrainName)
        {
            _active.PrimaryOpenTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares a second open terrain this tileset offers for multi-terrain districts. Only set
        /// after verifying full (secondary, solid) corner coverage AND Doorway-junction tiles for the
        /// secondary terrain among resolver-usable tiles (see MultiTerrainDistrictTests).
        /// </summary>
        public DungeonTilesetProfileBuilder SecondaryOpenTerrain(string terrainName)
        {
            _active.SecondaryOpenTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Adds a rare decorative "group" tile (treasure mound, pillar, hot spring, ...) this
        /// tileset can sprinkle into open room space, with a relative weight (default 1; e.g.
        /// treasure mounds are commonly weighted 2). TileResolver re-verifies the named group's
        /// structural eligibility at resolve time rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder FeatureTile(string groupName, int weight = 1,
            FeatureZoneDressing dressing = FeatureZoneDressing.None)
        {
            _active.FeatureTiles[groupName] = weight;
            if (dressing != FeatureZoneDressing.None)
                _active.FeatureTileDressings[groupName] = dressing;
            return this;
        }

        /// <summary>
        /// Adds a tileset "group" set piece (a WallRoom hanging off a Tunnel corridor, or an
        /// OpenSetPiece dropped into open room space) with a max-instances-per-area count (default 1).
        /// LayoutGroupStamper re-verifies the named group's structural eligibility at stamp time
        /// rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPiece(string groupName, int maxPerArea = 1)
        {
            _active.SetPieces[groupName] = maxPerArea;
            return this;
        }

        /// <summary>
        /// Adds a themed 1x1 "exit" group (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions. Call order is priority order: GroupExitPlanner
        /// tries each configured name in the order added here. GroupExitPlanner re-verifies the named
        /// group's structural eligibility at resolve time rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder ExitGroup(string groupName)
        {
            _active.ExitGroups.Add(groupName);
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.ReliefRegions request this tileset's real tile
        /// inventory has verified per-corner relief vocabulary for. Only set after verifying with
        /// LayoutReliefPainter's capability probes (TileResolver.HasHeightAwareCandidate for a lone
        /// raised open corner, or a flat open/blend flip) -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxReliefRegions(int count)
        {
            _active.MaxReliefRegions = count;
            return this;
        }

        /// <summary>
        /// Declares the "slope blend" terrain LayoutReliefPainter may flip individual open corners to
        /// -- see DungeonTilesetProfile.ReliefBlendTerrain. Only set after verifying full flat
        /// (open, blend) corner coverage among resolver-usable tiles.
        /// </summary>
        public DungeonTilesetProfileBuilder ReliefBlendTerrain(string terrainName)
        {
            _active.ReliefBlendTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares the alternate ramp-lane edge-crosser name this tileset's raised-tile family
        /// carries instead of the canonical "Ramp" (e.g. tdm01's "Slope") -- see
        /// DungeonTilesetProfile.RampCrosser.
        /// </summary>
        public DungeonTilesetProfileBuilder RampCrosser(string crosserName)
        {
            _active.RampCrosser = crosserName;
            return this;
        }

        /// <summary>
        /// Declares the edge-crosser name this tileset's road/route-marking tile family carves (e.g.
        /// fcx01's "Routes") -- see DungeonTilesetProfile.RoadCrosser. Only call this after verifying
        /// RoadVocabularyCheck.SupportsRoads(tileset, openTerrain, crosserName) returns true against
        /// the real tileset data, not merely that the crosser name appears in the .set CROSSER TYPES
        /// list.
        /// </summary>
        public DungeonTilesetProfileBuilder RoadCrosser(string crosserName)
        {
            _active.RoadCrosser = crosserName;
            return this;
        }

        /// <summary>
        /// Declares an alternate Tunnel-mode body/port crosser pair this tileset's district/palette
        /// carves instead of the canonical Corridor/Doorway names -- see
        /// DungeonTilesetProfile.TunnelBodyCrosser. Only call this after verifying the full shape
        /// inventory with TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, body,
        /// port), not merely that both names appear in the tileset's declared crosser list.
        /// </summary>
        public DungeonTilesetProfileBuilder TunnelCrossers(string bodyCrosser, string portCrosser)
        {
            _active.TunnelBodyCrosser = bodyCrosser;
            _active.TunnelPortCrosser = portCrosser;
            return this;
        }

        /// <summary>
        /// Declares one or more crosser names (beyond the canonical "Doorway"/"Bridge" pair) this
        /// tileset's real tile inventory uses for a door-implying crosser -- see
        /// DungeonTilesetProfile.DoorSlotCrossers. Only call this after confirming (via a direct
        /// TileResolver.HasCandidate/TunnelVocabularyCheck.SupportsTunnels probe passing the same names)
        /// that declaring it actually closes real tile-coverage gaps, not merely that the crosser name
        /// appears in the tileset's declared vocabulary.
        /// </summary>
        public DungeonTilesetProfileBuilder DoorSlotCrossers(params string[] crossers)
        {
            _active.DoorSlotCrossers.AddRange(crossers);
            return this;
        }

        /// <summary>
        /// Declares one or more physical tile IDs this profile must never place, regardless of how
        /// structurally valid the corner/edge/group data looks -- see
        /// DungeonTilesetProfile.ExcludedTiles for when to use this (confirmed placeholder/stub art,
        /// not a structural gap). Only call this after confirming the model itself is broken (dump
        /// the raw .mdl and verify a rendered mesh node has no real texture) -- TileResolver still
        /// trusts every OTHER tile's shape data blindly, so this is a deliberate art-only override,
        /// not a structural re-verification.
        /// </summary>
        public DungeonTilesetProfileBuilder ExcludedTiles(params int[] tileIds)
        {
            foreach (var tileId in tileIds)
                _active.ExcludedTiles.Add(tileId);
            return this;
        }

        /// <summary>
        /// Marks the active profile as a palette/district variant of an already-registered physical tileset
        /// resref (same .set file, different terrain composition) -- see
        /// DungeonTilesetProfile.IsPaletteVariant.
        /// </summary>
        public DungeonTilesetProfileBuilder PaletteVariant()
        {
            _active.IsPaletteVariant = true;
            return this;
        }

        /// <summary>
        /// Declares this tileset FAMILY's standard AREA atmosphere (see
        /// <see cref="DungeonAreaAtmosphere"/>), mirroring the layout builder's Configure shape.
        /// Only declare it when the family's hand-built module evidence is unambiguous (>= 3
        /// exemplar areas agreeing on the full core atmosphere tuple); cite the agreeing areas in a
        /// comment at the call site. Families without such evidence stay undeclared and keep their
        /// current defaults on both output paths.
        /// </summary>
        public DungeonTilesetProfileBuilder Atmosphere(Action<DungeonAreaAtmosphere> configure)
        {
            var atmosphere = new DungeonAreaAtmosphere();
            configure(atmosphere);
            _active.Atmosphere = atmosphere;
            return this;
        }

        /// <summary>
        /// Declares a NAMED alternate atmosphere on this tileset profile (see
        /// <see cref="DungeonTilesetProfile.AtmosphereProfiles"/>), selectable per theme
        /// (DungeonDetail.AtmosphereProfile) or per request, mirroring
        /// <see cref="DecorationProfile"/>'s selection rules exactly. Declare the standard
        /// <see cref="Atmosphere"/> first.
        /// </summary>
        public DungeonTilesetProfileBuilder AtmosphereProfile(string name, Action<DungeonAreaAtmosphere> configure)
        {
            var atmosphere = new DungeonAreaAtmosphere();
            configure(atmosphere);
            _active.AtmosphereProfiles[name] = atmosphere;
            return this;
        }

        private DungeonDecorationProfile _activeDecorationProfile;

        /// <summary>
        /// Starts a NAMED alternate decoration palette on this tileset profile (see
        /// <see cref="DungeonTilesetProfile.DecorationProfiles"/>): every subsequent
        /// <see cref="Decoration"/>/<see cref="Vignette"/>/<see cref="VignetteMember"/> call routes
        /// into it instead of the standard palette, until the next Create()/DecorationProfile() call.
        /// Declare the standard palette FIRST, then each named profile.
        /// </summary>
        public DungeonTilesetProfileBuilder DecorationProfile(string name, bool organicClutterRotation = false)
        {
            _activeDecorationProfile = new DungeonDecorationProfile
            {
                Name = name,
                OrganicClutterRotation = organicClutterRotation
            };
            _active.DecorationProfiles[name] = _activeDecorationProfile;
            _lastDecorationEntry = null;
            return this;
        }

        /// <summary>
        /// Declares this tileset family as following an urban placement grammar -- see
        /// <see cref="DungeonTilesetProfile.UrbanDressing"/>. Only declare it for families whose
        /// hand-built reference areas measurably follow the grammar (bearing alignment, clear road
        /// ribbons, facade rows); every non-declaring tileset's plan stays byte-identical.
        /// </summary>
        public DungeonTilesetProfileBuilder UrbanDressing()
        {
            _active.UrbanDressing = true;
            return this;
        }

        /// <summary>
        /// Adds a weighted decorative placeable to this tileset FAMILY's own bulk palette for one
        /// placement context — see <see cref="DungeonTilesetProfile.Decorations"/>. This is where the
        /// bulk of a generated area's visual dressing should live; theme definitions should only add a
        /// small handful of their own genuinely theme-flavored accents. Routes into the active NAMED
        /// decoration profile instead once <see cref="DecorationProfile"/> has been called.
        /// </summary>
        public DungeonTilesetProfileBuilder Decoration(string resref, int weight, DecorationContext context,
            DecorationRole role = DecorationRole.Fixture, bool allowOnRoadSurface = false,
            DecorationAnchoring anchoring = DecorationAnchoring.FreeStanding,
            DecorationSize size = DecorationSize.Medium)
        {
            var target = _activeDecorationProfile?.Decorations ?? _active.Decorations;
            _lastDecorationEntry = new DungeonDecorationEntry
            {
                Resref = resref,
                Weight = weight,
                Context = context,
                Role = role,
                AllowOnRoadSurface = allowOnRoadSurface,
                Anchoring = anchoring,
                Size = size
            };
            target.Add(_lastDecorationEntry);

            return this;
        }

        private DungeonDecorationEntry _lastDecorationEntry;

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry's district affinity (see
        /// <see cref="DungeonDecorationEntry.DistrictWeights"/>): the entry only places in rooms
        /// of the listed flavors, at the listed per-flavor weight. Omit entirely for an entry that
        /// serves every district at its base weight.
        /// </summary>
        public DungeonTilesetProfileBuilder Districts(params (DistrictFlavor Flavor, int Weight)[] weights)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("Districts() must follow a Decoration() call.");
            foreach (var (flavor, weight) in weights)
                _lastDecorationEntry.DistrictWeights[flavor] = weight;
            return this;
        }

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry's hard per-area placement cap (see
        /// <see cref="DungeonDecorationEntry.MaxPerArea"/>) -- derive it from the hand-built
        /// per-area p95 within the entry's district.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxPerArea(int cap)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("MaxPerArea() must follow a Decoration() call.");
            _lastDecorationEntry.MaxPerArea = cap;
            return this;
        }

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry's measured XY footprint radius in
        /// meters. Omit it to use the entry's conservative <see cref="DecorationSize"/> radius.
        /// </summary>
        public DungeonTilesetProfileBuilder FootprintRadius(float radius)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("FootprintRadius() must follow a Decoration() call.");
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            _lastDecorationEntry.FootprintRadius = radius;
            return this;
        }

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry as stackable cargo (see
        /// <see cref="DungeonDecorationEntry.StackHeight"/>): a committed pile/depot member may
        /// carry a second copy of itself directly above at this Z step -- the hand-built
        /// stacked-cargo pattern. Derive the height from the mined Z step of the resref's own
        /// hand-built elevated placements (which sit exactly one model height above their base
        /// copy), never from guesswork.
        /// </summary>
        public DungeonTilesetProfileBuilder Stackable(float height)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("Stackable() must follow a Decoration() call.");
            _lastDecorationEntry.StackHeight = height;
            return this;
        }

        /// <summary>
        /// Sets this tileset family's own evidence-derived decorative density (placeables per total
        /// area tile at 100% request density), overriding the composed theme's own base density --
        /// see <see cref="DungeonTilesetProfile.DecorationDensityPerTile"/>.
        /// </summary>
        public DungeonTilesetProfileBuilder DecorationDensity(double perTile)
        {
            _active.DecorationDensityPerTile = perTile;
            return this;
        }

        /// <summary>
        /// Adds a weighted structural building placeable to this tileset family's frontage system
        /// (see <see cref="DungeonTilesetProfile.FrontageBuildings"/>). Footprint dimensions are
        /// the measured model XY extents: faceWidth along the fronted face, depth into the margin.
        /// Only declare on urban families whose hand-built references wall their open space with
        /// building placeables.
        /// </summary>
        public DungeonTilesetProfileBuilder FrontageBuilding(string resref, int weight, float faceWidth, float depth,
            int maxPerArea = 0, float minSpacing = 0f, bool workhorse = false, string family = null,
            int familyMaxPerArea = 0)
        {
            _active.FrontageBuildings.Add(new BuildingFrontageEntry
            {
                Resref = resref,
                Weight = weight,
                FaceWidth = faceWidth,
                Depth = depth,
                MaxPerArea = maxPerArea,
                MinSameModelSpacing = minSpacing,
                DominantEligible = workhorse,
                FamilyKey = family,
                FamilyMaxPerArea = familyMaxPerArea
            });
            return this;
        }

        /// <summary>
        /// Enables the subtle per-instance visual-scale jitter on this family's frontage buildings
        /// (see <see cref="DungeonTilesetProfile.FrontageScaleJitter"/>).
        /// </summary>
        public DungeonTilesetProfileBuilder FrontageScaleJitter()
        {
            _active.FrontageScaleJitter = true;
            return this;
        }

        /// <summary>
        /// Declares a terrain label that renders as a bottomless drop in this family (see
        /// <see cref="DungeonTilesetProfile.ChasmTerrains"/>) -- activates the frontage
        /// footprint-support rule (FrontageSupportRule) against the resolved corner plan.
        /// </summary>
        public DungeonTilesetProfileBuilder ChasmTerrain(string terrain)
        {
            _active.ChasmTerrains.Add(terrain);
            return this;
        }

        /// <summary>
        /// Adds a weighted wall-mounted sign/holo placeable to this tileset family's facade-mount
        /// pass (see <see cref="DungeonTilesetProfile.FacadeMounts"/>). The height band is the
        /// mined per-resref Z band of the hand-built elevated placements.
        /// </summary>
        public DungeonTilesetProfileBuilder FacadeMount(string resref, int weight, float minHeight, float maxHeight)
        {
            _active.FacadeMounts.Add(new FacadeMountEntry
            {
                Resref = resref,
                Weight = weight,
                MinHeight = minHeight,
                MaxHeight = maxHeight
            });
            return this;
        }

        /// <summary>
        /// Adds a weighted street-dressing placeable to this tileset family's street pass (see
        /// <see cref="DungeonTilesetProfile.StreetDressings"/>): a flat road-marking plate laid on
        /// the lane surface, or a margin accent standing at the lane edge facing the street. Only
        /// declare on urban families with a <see cref="RoadCrosser"/> whose hand-built references
        /// dress their street stretches this way; maxPerArea counts against the shared per-area
        /// usage ledger (combined with any scatter-palette curation of the same resref).
        /// </summary>
        public DungeonTilesetProfileBuilder StreetDressing(string resref, int weight, StreetDressingKind kind,
            int maxPerArea = 0)
        {
            _active.StreetDressings.Add(new StreetDressingEntry
            {
                Resref = resref,
                Weight = weight,
                Kind = kind,
                MaxPerArea = maxPerArea
            });
            return this;
        }

        /// <summary>
        /// Declares this family's SIGNATURE composition (see
        /// <see cref="DungeonTilesetProfile.SignatureLayoutProfileKey"/>/<see cref="DungeonTilesetProfile.SignatureSize"/>):
        /// the layout pairing and scale that best reproduce the family's hand-built reference look.
        /// Purely authoring metadata; every other layout and size stays selectable.
        /// </summary>
        public DungeonTilesetProfileBuilder SignatureComposition(string layoutProfileKey, int size)
        {
            _active.SignatureLayoutProfileKey = layoutProfileKey;
            _active.SignatureSize = size;
            return this;
        }

        private DungeonVignette _activeVignette;

        /// <summary>
        /// Starts a new evidence-backed vignette grouping (see <see cref="DungeonVignette"/>) on this
        /// tileset profile. Follow with one or more <see cref="VignetteMember"/> calls. Routes into
        /// the active NAMED decoration profile once <see cref="DecorationProfile"/> has been called.
        /// </summary>
        public DungeonTilesetProfileBuilder Vignette(string key, int weight = 1)
        {
            _activeVignette = new DungeonVignette { Key = key, Weight = weight };
            (_activeDecorationProfile?.Vignettes ?? _active.Vignettes).Add(_activeVignette);
            return this;
        }

        /// <summary>
        /// Adds one placeable to the active vignette. Offsets are world units relative to the
        /// vignette's anchor tile, BEFORE the anchor's own wall-facing rotation is applied (see
        /// DungeonDecorationPlanner.PlaceVignette) — author offsets as if the anchor faces "north"
        /// (+Y) into the room.
        /// </summary>
        public DungeonTilesetProfileBuilder VignetteMember(string resref, float offsetX, float offsetY, float facingOffset = 0f)
        {
            _activeVignette.Members.Add(new DungeonVignetteMember
            {
                Resref = resref,
                OffsetX = offsetX,
                OffsetY = offsetY,
                FacingOffset = facingOffset
            });
            return this;
        }

        public Dictionary<string, DungeonTilesetProfile> Build()
        {
            return _profiles;
        }
    }
}
