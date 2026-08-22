#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Whole-tileset capability probe for LayoutTunnelCarver's wall-embedded tunnel mode: true only
    /// when the tileset can resolve every tile SHAPE the carver can unconditionally emit, not merely
    /// whether the body/port crosser NAMES exist in its declared vocabulary. Mirrors
    /// LayoutAccentChannelCarver.CanCarve's "probe TileResolver.HasCandidate for the exact shapes,
    /// don't just check crosser presence" pattern, extended to tunnel carving's larger shape set.
    ///
    /// Two onboarded tilesets motivated this: Illithid Interior (tii01) declares both "Doorway" and
    /// "Corridor" (so a bare Crossers.Contains check passes) but has no tile for a solid cell carrying
    /// a Doorway edge together with two Corridor edges (a "T-with-port" junction, i.e. a corridor bend
    /// merging directly into a room's doorway port) -- verified below via <see cref="SupportsTunnels"/>
    /// returning false purely because of that one missing shape, confirmed against a direct probe
    /// (ZZProbeTunnelShapesTests during development) that isolated it as the ONLY failing shape out of
    /// the full inventory for tii01. Ruins (tdr01) declares "Alley" (so the Alley-vocabulary presence
    /// check passes) but has no side-open boundary tile carrying a lone Alley edge at all -- every
    /// tunnel needs at least one such tile to ever enter or exit a room, so this is a hard, deterministic
    /// blocker (not a probabilistic one like Illithid's), matching the "No matching tile ... Right=Alley"
    /// failure Ruins/Streets always hits.
    ///
    /// Shape inventory (every representative check below uses ONE orientation; TileResolver's own
    /// rotation search over all four orientations means one stands in for all four the carver could
    /// need -- the same reasoning LayoutAccentChannelCarver.CanCarve's doc comment gives):
    ///
    /// All-solid-corner tunnel-chain cells (this is the vocabulary LayoutTunnelCarver's own class doc
    /// already names: "all-solid tiles with Corridor edges in straight/L/T/X arrangements"):
    ///   - straight  [body,-,body,-]   -- REQUIRED: every chain needs to run in a straight line.
    ///   - turn      [body,body,-,-]   -- REQUIRED: BFS routing bends around obstacles constantly.
    ///   - T         [body,body,body,-] -- REQUIRED (per the carver's own doc comment): a later tunnel's
    ///     chain can pass through a cell an earlier tunnel already claimed, merging three body edges.
    ///   - X         [body,body,body,body] -- REQUIRED for the same reason, one merge further.
    ///
    /// Port-adapter cells (the chain cell immediately behind a room's boundary doorway -- it shares its
    /// port edge with the boundary cell via EdgeCrosserGrid's shared-edge storage, so it always carries
    /// the port crosser on one side PLUS whatever body edges its own chain routing/merges add):
    ///   - straight-with-port [port,-,body,-]      -- REQUIRED: the un-bent case.
    ///   - turn-with-port     [port,body,-,-]      -- REQUIRED: BFS can turn on its very first step.
    ///   - T-with-port        [port,body,body,-]   -- REQUIRED: this is tii01's exact missing shape --
    ///     a corridor bend merging directly into a doorway port. Confirmed empirically to be the ONLY
    ///     failing shape for tii01 (58-66% single-attempt / up to 98.5% six-attempt-retry failures);
    ///     every other onboarded tileset (13/13) plus all four original generation tilesets support it.
    ///   - X-with-port        [port,body,body,body] -- REQUIRED for symmetry with T-with-port (a rarer,
    ///     more extreme merge); every probed tileset that has T-with-port also has this, so requiring it
    ///     costs nothing in practice while closing the same class of gap one merge further.
    ///   - double-port opposite [port,-,port,-] and adjacent [port,port,-,-] -- REQUIRED: the rare case
    ///     where two rooms are exactly one solid cell apart (LayoutTunnelCarver.TryConnect's
    ///     chain.Count == 1 branch) shares a single solid cell between both rooms' ports with zero body
    ///     edges. A real unconditional carver output (not gated behind any config), so it's in scope
    ///     even though every probed tileset already happens to support it.
    ///
    /// Boundary port cell (the side-open tile LayoutTunnelCarver.TryAddPort actually carves a port
    /// onto -- room-side wall corners open, far-side corners solid, port edge on the far/solid side):
    ///   - REQUIRED, unconditionally: without at least one resolvable orientation, NO port can ever be
    ///     placed at all, so every tunnel deterministically fails resolution the instant one is
    ///     attempted. This is tdr01/Alley's exact gap.
    ///
    /// Pure body T/X (no port) turned out to be present on literally every probed tileset including
    /// ones that fail everywhere else (e.g. Barrows/tbw01, which has zero Doorway vocabulary at all) --
    /// they never independently changed a verdict in probing, but they're free to check and are already
    /// named in LayoutTunnelCarver's own doc header, so they stay in the required set rather than being
    /// downgraded to "optional": a hypothetical future tileset with a body-only vocabulary gap should
    /// still be caught here rather than silently offered.
    /// </summary>
    public static class TunnelVocabularyCheck
    {
        private const string CorridorCrosser = "Corridor";
        private const string DoorwayCrosser = "Doorway";
        private const string AlleyCrosser = "Alley";

        /// <summary>
        /// True when <paramref name="tileset"/> can resolve every shape a wall-embedded tunnel between
        /// two rooms carved from <paramref name="openTerrain"/> needs. Pass
        /// <see cref="TilesetModel.DefaultTerrain"/> for <paramref name="solidTerrain"/> (the same value
        /// LayoutSolver.Solve stamps onto MacroLayoutParameters.SolidTerrain every attempt).
        /// </summary>
        public static bool SupportsTunnels(
            TilesetModel tileset, string openTerrain, string solidTerrain, CorridorCrosserType crosserType,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null)
        {
            return SupportsTunnels(tileset, openTerrain, string.Empty, solidTerrain, crosserType,
                extraDoorSlotCrossers: extraDoorSlotCrossers);
        }

        /// <summary>
        /// Overload for multi-terrain districts: also verifies the boundary/port shape against
        /// <paramref name="secondaryOpenTerrain"/> (RoomsAndCorridorsLayout.SecondaryOpenTerrain), since
        /// LayoutTunnelCarver enumerates a room's ports on ITS OWN open terrain, and a secondary-terrain
        /// room needs its own resolvable boundary tile too. <paramref name="secondaryOpenTerrain"/> may
        /// be empty (no districts configured); the check is skipped entirely for
        /// <see cref="CorridorCrosserType.Alley"/> either way, since RoomsAndCorridorsLayout's own
        /// useDistricts gate never activates a secondary-terrain room under Alley mode (verified by
        /// MultiTerrainDistrictTests.AlleyCrosserType_NeverActivatesDistrictsEvenWithSecondaryOpenTerrainConfigured),
        /// so probing it would only produce a misleading false negative, never a true one.
        ///
        /// <paramref name="customBodyCrosser"/>/<paramref name="customPortCrosser"/> are read only when
        /// <paramref name="crosserType"/> is <see cref="CorridorCrosserType.Custom"/> -- pass a tileset
        /// profile's declared TunnelBodyCrosser/TunnelPortCrosser (see
        /// MacroLayoutParameters.TunnelBodyCrosser doc comment) to probe an alternate district-scoped
        /// crosser family (e.g. tdc01's "GreyCorridor" body paired with the canonical "Doorway" port).
        ///
        /// <paramref name="extraDoorSlotCrossers"/> is passed straight through to every
        /// TileResolver.HasCandidate probe below (see MacroLayoutParameters.DoorSlotCrossers) so a port
        /// crosser family whose real tiles all carry door slots (e.g. Barrows/tbw01's "door_corridor")
        /// registers as a candidate the same way a canonical Doorway/Bridge door-slot tile always has --
        /// without this, every probed shape that happens to land on a door-slot tile would report a
        /// false negative purely because TileResolver's admission gate excluded it, not because the
        /// tileset genuinely lacks the shape.
        /// </summary>
        public static bool SupportsTunnels(
            TilesetModel tileset, string openTerrain, string secondaryOpenTerrain, string solidTerrain,
            CorridorCrosserType crosserType, string customBodyCrosser = null, string customPortCrosser = null,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));

            var isAlley = crosserType == CorridorCrosserType.Alley;
            var isCustom = crosserType == CorridorCrosserType.Custom;
            var body = isCustom ? customBodyCrosser : isAlley ? AlleyCrosser : CorridorCrosser;
            var port = isCustom ? customPortCrosser : isAlley ? AlleyCrosser : DoorwayCrosser;

            if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(port)) return false;

            if (!tileset.Crossers.Contains(body, StringComparer.OrdinalIgnoreCase)) return false;
            if (!tileset.Crossers.Contains(port, StringComparer.OrdinalIgnoreCase)) return false;

            if (!SupportsBodyAndPortShapes(tileset, solidTerrain, body, port, extraDoorSlotCrossers)) return false;
            if (!SupportsBoundaryShape(tileset, openTerrain, solidTerrain, port, extraDoorSlotCrossers)) return false;

            if (!isAlley && !string.IsNullOrEmpty(secondaryOpenTerrain) &&
                !SupportsBoundaryShape(tileset, secondaryOpenTerrain, solidTerrain, port, extraDoorSlotCrossers))
                return false;

            return true;
        }

        private static bool SupportsBodyAndPortShapes(
            TilesetModel tileset, string solid, string body, string port,
            IReadOnlyCollection<string> extraDoorSlotCrossers)
        {
            bool Body(string top, string right, string bottom, string left) =>
                TileResolver.HasCandidate(tileset, solid, solid, solid, solid, top, right, bottom, left, extraDoorSlotCrossers);

            if (!Body(body, "", body, "")) return false; // straight
            if (!Body(body, body, "", "")) return false; // turn (L)
            if (!Body(body, body, body, "")) return false; // T (3-way body merge)
            if (!Body(body, body, body, body)) return false; // X (4-way body merge)

            if (!Body(port, "", body, "")) return false; // straight-with-port
            if (!Body(port, body, "", "")) return false; // turn-with-port
            if (!Body(port, body, body, "")) return false; // T-with-port (tii01's exact gap)
            if (!Body(port, body, body, body)) return false; // X-with-port

            if (!Body(port, "", port, "")) return false; // double-port, opposite walls
            if (!Body(port, port, "", "")) return false; // double-port, adjacent walls

            return true;
        }

        /// <summary>
        /// The side-open doorway tile LayoutTunnelCarver.TryAddPort's Left-wall case carves a port onto
        /// (used here as the single representative rotation): room-side corners (Right side, per that
        /// method's own EdgeSlot.Left branch) open, far-side (Left side) corners solid, port edge on the
        /// far/Left side. Corresponds to TL=solid, TR=open, BR=open, BL=solid in TileResolver's own
        /// corner-slot convention (cell (cx,cy): bl=(cx,cy), tl=(cx,cy+1), br=(cx+1,cy), tr=(cx+1,cy+1)).
        /// </summary>
        private static bool SupportsBoundaryShape(
            TilesetModel tileset, string open, string solid, string port,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null)
        {
            return TileResolver.HasCandidate(tileset, solid, open, open, solid, "", "", "", port, extraDoorSlotCrossers);
        }
    }
}
