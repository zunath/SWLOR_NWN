#nullable disable
using System;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Normalizes <see cref="MacroLayoutParameters"/> so that every combination the Area Generator's
    /// Advanced Settings sliders can produce is guaranteed generation-safe under the standard
    /// 6-attempt retry, instead of relying on callers to only ever offer safe values.
    ///
    /// Every bound here was measured empirically with a probe harness (30+ single-attempt seeds per
    /// data point in the July 2026 parameter-safety measurements) rather than guessed, and each
    /// formula is documented with the evidence that justifies it. Formulas intentionally
    /// UNDER-approximate the true empirical safe ceiling (and OVER-
    /// approximate any empirical safe floor) so a formula that's slightly conservative is preferred
    /// over one that's exactly tight to the measured 95% cutoff.
    /// </summary>
    public static class LayoutParameterConstraints
    {
        /// <summary>
        /// True if <see cref="ClampToValid"/> would change any field on <paramref name="parameters"/>.
        /// A pure value check over local copies -- no <see cref="MacroLayoutParameters"/> allocation --
        /// so <see cref="MacroLayoutGenerator.Generate"/> can decide whether it needs to clone the
        /// caller's object before clamping without ever allocating just to find out. Must mirror
        /// <see cref="ClampToValid"/>'s conditions exactly; the two are intentionally kept next to each
        /// other in this file to make that easy to verify at a glance.
        /// </summary>
        public static bool NeedsClamping(MacroLayoutParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var width = parameters.Width;
            var height = parameters.Height;
            var floor = LayoutStyleSizeFloor.For(parameters.Style);
            if (width < floor || height < floor) return true;

            var minRooms = parameters.MinRooms;
            var maxRooms = parameters.MaxRooms;
            if (minRooms > maxRooms) return true;
            if (minRooms < 2) return true;

            var minSize = parameters.MinRoomCornerSize;
            var maxSize = parameters.MaxRoomCornerSize;
            if (minSize > maxSize) return true;
            if (minSize < 2) return true;

            var (_, maxRoomSize) = RoomSizeBounds(parameters.Style, width, height);
            if (maxSize > maxRoomSize) return true;

            if (parameters.Style == DungeonLayoutStyle.OrganicCave &&
                parameters.OpenFillTarget < MinSafeOpenFillTarget(width, height))
                return true;

            if (parameters.CorridorWidth < 1) return true;

            if (parameters.EntranceCount < 1 || parameters.EntranceCount > 3) return true;
            if (parameters.ExitCount < 1 || parameters.ExitCount > 3) return true;

            return false;
        }

        /// <summary>
        /// Normalizes <paramref name="parameters"/> IN PLACE. Callers that must not mutate a shared
        /// parameters object should pass a clone (see MacroLayoutGenerator.Generate, which reuses its
        /// existing Alley-downgrade clone for this purpose). When every field is already valid this
        /// makes no changes and consumes no RNG (RNG is untouched entirely -- this method never rolls
        /// randomness).
        /// </summary>
        public static void ClampToValid(MacroLayoutParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // Raise Width/Height to the style's empirically measured minimum before anything else --
            // every other bound below is a function of the (now-floored) dimensions.
            var floor = LayoutStyleSizeFloor.For(parameters.Style);
            if (parameters.Width < floor) parameters.Width = floor;
            if (parameters.Height < floor) parameters.Height = floor;

            // Room counts: swap first so Min<=Max always holds. Un-swapped Min>Max reaches
            // System.Random.Next(min, max+1) in every style but PackedRooms (which never rolls
            // MinRooms/MaxRooms at all) and throws ArgumentOutOfRangeException -- confirmed by the
            // inverted room-count probe, reachable directly from the UI (Min Rooms tops out at 12,
            // Max Rooms bottoms out at 2, and the two sliders are otherwise uncoupled).
            if (parameters.MinRooms > parameters.MaxRooms)
                (parameters.MinRooms, parameters.MaxRooms) = (parameters.MaxRooms, parameters.MinRooms);
            parameters.MinRooms = Math.Max(2, parameters.MinRooms);
            parameters.MaxRooms = Math.Max(parameters.MinRooms, parameters.MaxRooms);

            // Room sizes: swap first for the identical reason. Unlike room counts, every style's own
            // generator already self-corrects a Min>Max ordering (each computes
            // maxSize = Math.Max(minSize, MaxRoomCornerSize)), but that self-correction silently
            // widens the EFFECTIVE room size to Min instead of honoring the user's stated Max -- e.g.
            // Min Room Size=10/Max Room Size=3 (both directly reachable: the sliders are 2-10 and
            // 3-12 respectively) at RoomsAndCorridors' 11x11 size floor produced a real
            // InvalidOperationException ("only 0 fit") in the inverted room-size probe, because the
            // self-correction widened every room to 10 corners with no further size cap applied.
            // Swapping first means the RoomSizeBounds cap below governs the outcome.
            if (parameters.MinRoomCornerSize > parameters.MaxRoomCornerSize)
                (parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize) = (parameters.MaxRoomCornerSize, parameters.MinRoomCornerSize);
            parameters.MinRoomCornerSize = Math.Max(2, parameters.MinRoomCornerSize);
            parameters.MaxRoomCornerSize = Math.Max(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);

            var (_, maxRoomSize) = RoomSizeBounds(parameters.Style, parameters.Width, parameters.Height);
            parameters.MaxRoomCornerSize = Math.Min(parameters.MaxRoomCornerSize, maxRoomSize);
            parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);

            // OrganicCave: OpenFillTarget has a hard safe floor that rises steeply as the area shrinks
            // toward its size floor -- at the 12x12 floor, only fill>=~58% reliably percolates past
            // OrganicCaveLayout.MinComponentSize (8 corners) and the >=2-spacious-seed-point
            // requirement; the Area Generator slider's own range (30-60%) offers values as low as
            // 30%, which measured a flat 0% single-attempt success at size 12 in the fill-target
            // probe (not merely low-probability -- structurally near-impossible, so no amount of
            // 6-attempt retry rescues it). See MinSafeOpenFillTarget for the measured breakpoints.
            if (parameters.Style == DungeonLayoutStyle.OrganicCave)
            {
                var minFill = MinSafeOpenFillTarget(parameters.Width, parameters.Height);
                if (parameters.OpenFillTarget < minFill)
                    parameters.OpenFillTarget = minFill;
            }

            // CorridorWidth: every layout style already applies Math.Max(1, CorridorWidth) internally,
            // but normalizing here too means RoomSizeBounds/other future consumers can trust the
            // parameters object directly without re-deriving that floor themselves. Corridor-width
            // probes found CorridorWidth has no measurable effect on success/failure at any tested
            // size (the dominant failure modes are room size and organic fill), so no
            // per-style/per-size cap is needed beyond this floor.
            if (parameters.CorridorWidth < 1) parameters.CorridorWidth = 1;

            // Entrances/Exits: clamp to the UI's own 1-3 range so direct API callers can't request
            // more anchor points than TransitionAssignment's room-count-aware placement was verified
            // against. Transition-count probes found EntranceCount=ExitCount=3 never fails on its own
            // merits once room size / organic fill are within bounds.
            parameters.EntranceCount = Math.Clamp(parameters.EntranceCount, 1, 3);
            parameters.ExitCount = Math.Clamp(parameters.ExitCount, 1, 3);
        }

        /// <summary>
        /// The area (in tiles) every layout profile's hardcoded room counts and every tileset
        /// profile's SetPiece budgets were tuned against (see LayoutGroupStamper.EffectiveMaxCount,
        /// which shares this baseline). Room-supply scaling only activates ABOVE it, so tuned
        /// behavior at the machinery's usual 16-24 test sizes is untouched even for a profile that
        /// declares scaling.
        /// </summary>
        public const int RoomSupplyBaselineTiles = 20 * 20;

        /// <summary>
        /// True if <see cref="ApplySetPieceRoomSupplyScaling"/> would change any field on
        /// <paramref name="parameters"/>. Mirrors that method's conditions exactly (the same
        /// check-before-clone contract as <see cref="NeedsClamping"/>/<see cref="ClampToValid"/>)
        /// so MacroLayoutGenerator.Generate never allocates a clone just to find out.
        /// </summary>
        public static bool NeedsSetPieceRoomSupplyScaling(MacroLayoutParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (!parameters.SetPieceRoomSupplyScaling) return false;
            if (parameters.Width * parameters.Height <= RoomSupplyBaselineTiles) return false;

            var derived = ScaledRoomEnvelope(parameters);
            return derived.MaxRooms > parameters.MaxRooms || derived.MinRooms > parameters.MinRooms ||
                   derived.MaxSize > parameters.MaxRoomCornerSize || derived.MinSize > parameters.MinRoomCornerSize;
        }

        /// <summary>
        /// Scales the room envelope (MinRooms/MaxRooms and MinRoomCornerSize/MaxRoomCornerSize) up
        /// with area for a set-piece-heavy composition (see
        /// MacroLayoutParameters.SetPieceRoomSupplyScaling) IN PLACE -- callers pass a clone, exactly
        /// like <see cref="ClampToValid"/>. Never lowers any field, stays inside
        /// <see cref="RoomSizeBounds"/>' measured-safe size cap, and consumes no RNG. No-op at or
        /// below the 20x20 tuning baseline.
        ///
        /// Why the envelope and not budgets: measured on fcx01 at 32x32 (20 seeds/district, July 2026
        /// city-density pass), raising every SetPiece budget substantially moved group-tile share
        /// 0.0398 -> 0.0393 (flat) because every layout style's room supply is constant in area --
        /// Halls/Complex hardcode MinRooms=6/MaxRooms=9 and PackedRooms reports at most MaxRooms
        /// (default 8) leaves to the stamper -- while LayoutGroupStamper needs one
        /// SetPieceRoomCornerFloor-sized room per stamped building (footprint + margin + spare
        /// center tile all inside ONE room). Scaling COUNTS alone measured 0.052 group share (still
        /// site-starved: a corner-size-7 room hosts exactly one 2x2 stamp, and 3x3+ towers need more);
        /// scaling counts AND sizes together measured the 0.10+ shares CityBlockDensityTests pins,
        /// because plaza-sized rooms host several buildings apiece -- the hand-built fcx01 pattern.
        /// </summary>
        public static void ApplySetPieceRoomSupplyScaling(MacroLayoutParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (!parameters.SetPieceRoomSupplyScaling) return;
            if (parameters.Width * parameters.Height <= RoomSupplyBaselineTiles) return;

            var derived = ScaledRoomEnvelope(parameters);
            parameters.MaxRoomCornerSize = Math.Max(parameters.MaxRoomCornerSize, derived.MaxSize);
            parameters.MinRoomCornerSize = Math.Max(parameters.MinRoomCornerSize, derived.MinSize);
            parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);
            parameters.MaxRooms = Math.Max(parameters.MaxRooms, derived.MaxRooms);
            parameters.MinRooms = Math.Max(parameters.MinRooms, derived.MinRooms);
            parameters.MinRooms = Math.Min(parameters.MinRooms, parameters.MaxRooms);
        }

        /// <summary>
        /// Area-derived room envelope for a set-piece-heavy composition.
        ///
        /// Sizes: the ceiling grows by one corner per 4 corners of area min-dimension past the 20x20
        /// baseline (32x32 -> +3), capped by <see cref="RoomSizeBounds"/>' measured-safe per-size
        /// ceiling, so plaza rooms get big enough to host several stamped buildings (an 8x8-tile room
        /// hosts four 2x2-footprint stamps; a corner-size-7 room hosts exactly one) and the 3x3/4x3
        /// tower groups become placeable at all. The floor rides 4 corners under the ceiling -- deep
        /// enough to keep a small/large room mix, high enough that most rooms stay stampable.
        ///
        /// Counts: how many rooms of the MIDPOINT derived size, each with its 1-corner solid gap, the
        /// interior corner grid can theoretically host. RoomsAndCorridors' random rectangle packing
        /// realizes roughly 70-90% of this in practice (measured at 32x32), which lands generated
        /// group-tile density at hand-built fcx01's own building spacing rather than wall-to-wall
        /// towers, so the theoretical count is used directly as MaxRooms rather than being
        /// discounted. MinRooms rides at 2/3 of MaxRooms so small targets stay possible (the styles
        /// roll targetCount from the range; placing fewer than target is graceful, never a failure).
        /// </summary>
        private static (int MinSize, int MaxSize, int MinRooms, int MaxRooms) ScaledRoomEnvelope(
            MacroLayoutParameters parameters)
        {
            var minDim = Math.Min(parameters.Width, parameters.Height);
            var (_, sizeCap) = RoomSizeBounds(parameters.Style, parameters.Width, parameters.Height);

            // Contiguous-block (street-canyon) compositions grow the ceiling from a 16-corner origin
            // instead of the 20-corner tuning baseline, but never past 10 corners: block assembly
            // needs at least one several-buildings room by the low-20s sizes (measured at 24x24:
            // origin 20 left the ceiling at 8 corners and building share at 0.12-0.24 vs the
            // 0.17-0.28 hand-built band; origin 16 raises it to 9 there and the 24x24 10-seed mean to
            // 0.215 -- the hand-built band mean exactly), while the 10-corner cap keeps 32x32 from
            // over-densifying (uncapped, origin 16 yields 11-corner rooms there and 0.28-0.36 shares,
            // past the band ceiling; capped at 10 it reproduces the measured-in-band 0.23-0.28).
            var growthOrigin = parameters.BuildingBlockContiguity ? 16 : 20;
            var maxSize = Math.Min(sizeCap, parameters.MaxRoomCornerSize + Math.Max(0, (minDim - growthOrigin) / 4));
            if (parameters.BuildingBlockContiguity)
                maxSize = Math.Min(maxSize, Math.Max(10, parameters.MaxRoomCornerSize));
            maxSize = Math.Max(maxSize, parameters.MaxRoomCornerSize);
            // Contiguous-block (street-canyon) compositions ride the floor 2 corners under the
            // ceiling instead of 4: block assembly needs plaza-sized rooms almost everywhere (a
            // seed whose BSP rolls mostly small rooms measured 0.128 building share vs the 0.17
            // hand-built band floor -- July 2026 street-canyon pass), and the small/large mix the
            // wider floor preserves matters less when adjacency lets several groups share one room.
            var minFloorDrop = parameters.BuildingBlockContiguity ? 3 : 4;
            var minSize = Math.Max(parameters.MinRoomCornerSize, maxSize - minFloorDrop);

            var midpointSize = (minSize + maxSize) / 2;
            var perRoom = (midpointSize + 1) * (midpointSize + 1);
            var usable = (parameters.Width - 1) * (parameters.Height - 1);
            var theoretical = Math.Max(2, usable / perRoom);

            // MinRooms rides one under MaxRooms rather than the 2/3 first tried: RoomsAndCorridors
            // stops PLACING at the rolled targetCount, so a wide Min..Max roll range directly costs
            // realized rooms (measured at 32x32 futcity_plaza/Complex: Min 7/Max 11 realized 7.7
            // rooms/area; Min 10/Max 11 realized 9.9). Placing fewer than target is graceful, so a
            // tight-high roll never risks generation failure -- only unrealized intent.
            return (minSize, maxSize, Math.Max(2, theoretical - 1), theoretical);
        }

        /// <summary>
        /// Largest room/chamber corner size (see <see cref="MacroLayoutParameters.MaxRoomCornerSize"/>)
        /// that reliably (>=95% single-attempt, verified by probe) generates for <paramref name="style"/>
        /// at the given area dimensions. The returned Min is always 2 (every style's own generator
        /// already floors MinRoomCornerSize at 2). Exposed so the Area Generator can bound the Min/Max
        /// Room Size sliders identically to what <see cref="ClampToValid"/> enforces, rather than
        /// letting the UI offer values the engine will silently override.
        ///
        /// Formulas (probe evidence, single-attempt success rate over 30-60 seeds per point, verified
        /// against both the default 4-8 room-count range and the worst-measured case of
        /// MinRooms=MaxRooms=12):
        /// - RoomsAndCorridors (vmr01 Halls pairing): Max(2, min(W,H)/3). Measured safe ceiling per
        ///   size (11->3, 14->4, 17->5, 20->6/7, 23->8, 26->9, 29->10, 32->11); floor division by 3
        ///   matches or under-shoots every measured point (e.g. size 26 measures safe up to 9 while
        ///   the formula yields 8), so it's a safe under-approximation everywhere.
        /// - PackedRooms (zsf01 pairing): Max(2, (min(W,H)-3)/2), which is the exact BSP root-split
        ///   feasibility condition (root rect corner-width = size-2 must satisfy
        ///   size-2 >= roomSize*2+1). Measured as a hard cliff (100% up to the formula's value, 0%
        ///   one step above) at every tested size 11-32, so the formula is exact, not merely safe.
        /// - Warren / Labyrinth: hard-capped at 5 / 4 respectively -- the same hardcap each layout
        ///   already applies internally (WarrenLayout, LabyrinthLayout.MaxChamberCornerSize) via
        ///   Math.Min(MaxRoomCornerSize, 5/4). Exposed here defensively too because that internal cap
        ///   only fires through Math.Max(minSize, Math.Min(max,5/4)) -- a MinRoomCornerSize above the
        ///   hardcap (reachable: the Min Room Size slider goes up to 10) silently overrides it to
        ///   MinRoomCornerSize instead (verified harmless by the chamber-size probe only because
        ///   CarveChambers additionally clamps each room rect against Width/Height directly; capping
        ///   here keeps the exposed bound honest about what the layout was designed for).
        /// - OrganicCave: room-size knobs are structurally unused (rooms are sampled from the smoothed
        ///   cave, not rectangles), so no cap applies.
        /// </summary>
        public static (int Min, int Max) RoomSizeBounds(DungeonLayoutStyle style, int width, int height)
        {
            var minDim = Math.Min(width, height);

            var max = style switch
            {
                DungeonLayoutStyle.RoomsAndCorridors => Math.Max(2, minDim / 3),
                DungeonLayoutStyle.PackedRooms => Math.Max(2, (minDim - 3) / 2),
                DungeonLayoutStyle.Warren => 5,
                DungeonLayoutStyle.Labyrinth => 4,
                _ => int.MaxValue
            };

            return (2, max);
        }

        /// <summary>
        /// Smallest <see cref="MacroLayoutParameters.OpenFillTarget"/> (0..1) that reliably generates
        /// for OrganicCave at the given area dimensions. Breakpoints measured by probe (single-attempt
        /// success rate, 40-60 seeds per point): minDim 12-13 needs >=60%, 14-19 needs >=50%, 20-27
        /// needs >=40%, 28+ needs >=35%. Every breakpoint boundary (13, 14, 19, 20, 27, 28) was
        /// directly measured at >=98% success, so these are not interpolated guesses. Required fill
        /// falls as the area grows because OrganicCaveLayout.MinComponentSize (8 corners) and the
        /// >=2-spacious-seed-point room requirement are fixed absolute thresholds that a larger grid
        /// clears at a lower fill fraction.
        /// </summary>
        public static double MinSafeOpenFillTarget(int width, int height)
        {
            var minDim = Math.Min(width, height);

            if (minDim <= 13) return 0.60;
            if (minDim <= 19) return 0.50;
            if (minDim <= 27) return 0.40;
            return 0.35;
        }
    }
}
