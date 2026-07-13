using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Normalizes <see cref="MacroLayoutParameters"/> so that every combination Content Builder's
    /// Advanced Settings sliders can produce is guaranteed generation-safe under the standard
    /// 6-attempt retry, instead of relying on callers to only ever offer safe values.
    ///
    /// Every bound here was measured empirically with a scratch probe harness (30+ single-attempt
    /// seeds per data point, see the July 2026 procedural-areas advanced-settings-hardening probe
    /// tables) rather than guessed, and each formula is documented with the evidence that justifies
    /// it. Formulas intentionally UNDER-approximate the true empirical safe ceiling (and OVER-
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
            // MinRooms/MaxRooms at all) and throws ArgumentOutOfRangeException -- confirmed by probe
            // Part 2b, reachable directly from the UI (Min Rooms slider tops out at 12, Max Rooms
            // slider bottoms out at 2, and the two sliders are otherwise uncoupled).
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
            // InvalidOperationException ("only 0 fit") in probe Part 2c, because the self-correction
            // widened every room to 10 corners with no further size cap applied. Swapping first means
            // the RoomSizeBounds cap below is what actually governs the outcome.
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
            // requirement; the Content Builder slider's own range (30-60%) offers values as low as
            // 30%, which measured a flat 0% single-attempt success at size 12 in probe Part 3 (not
            // merely low-probability -- structurally near-impossible, so no amount of 6-attempt retry
            // rescues it). See MinSafeOpenFillTarget for the measured breakpoints.
            if (parameters.Style == DungeonLayoutStyle.OrganicCave)
            {
                var minFill = MinSafeOpenFillTarget(parameters.Width, parameters.Height);
                if (parameters.OpenFillTarget < minFill)
                    parameters.OpenFillTarget = minFill;
            }

            // CorridorWidth: every layout style already applies Math.Max(1, CorridorWidth) internally,
            // but normalizing here too means RoomSizeBounds/other future consumers can trust the
            // parameters object directly without re-deriving that floor themselves. Probe Part 4 found
            // CorridorWidth has no measurable effect on success/failure at any tested size (the
            // dominant failure modes are room size and organic fill), so no per-style/per-size cap is
            // needed beyond this floor.
            if (parameters.CorridorWidth < 1) parameters.CorridorWidth = 1;

            // Entrances/Exits: clamp to the UI's own 1-3 range so non-UI callers (review specs,
            // /genarea) can't request more anchor points than TransitionAssignment's room-count-aware
            // placement was verified against. Probe Part 5b found EntranceCount=ExitCount=3 never
            // fails on its own merits once room size / organic fill are within bounds.
            parameters.EntranceCount = Math.Clamp(parameters.EntranceCount, 1, 3);
            parameters.ExitCount = Math.Clamp(parameters.ExitCount, 1, 3);
        }

        /// <summary>
        /// Largest room/chamber corner size (see <see cref="MacroLayoutParameters.MaxRoomCornerSize"/>)
        /// that reliably (>=95% single-attempt, verified by probe) generates for <paramref name="style"/>
        /// at the given area dimensions. The returned Min is always 2 (every style's own generator
        /// already floors MinRoomCornerSize at 2). Exposed so Content Builder can bound the Min/Max
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
        ///   MinRoomCornerSize instead (verified harmless in probe Part 7 only because
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
