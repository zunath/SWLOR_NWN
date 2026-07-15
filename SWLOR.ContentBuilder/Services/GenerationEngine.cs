using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>Outcome of one GenerationEngine.Generate call.</summary>
    internal sealed class GenerationResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public TilesetModel Tileset { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }

        /// <summary>
        /// Count of DungeonDecorationPlanner.Plan's output for this generation (0 when decorations are
        /// disabled, the theme has no curated palette, or density rolled zero this seed). Decorations
        /// are placeables, not tiles, so they never render in the schematic/map preview -- this is the
        /// only place their effect is surfaced, via MainWindow's status/log line.
        /// </summary>
        public int PlannedDecorationCount { get; init; }
    }

    /// <summary>
    /// Content Builder's live Advanced-knob state, layered on top of a DungeonComposition's own
    /// composed MacroLayoutParameters (DungeonComposition.BuildLayoutParameters) -- the identical
    /// starting point the runtime facade and SWLOR.ProcgenReview use. Every field here is applied
    /// unconditionally (none are nullable "did the user touch this" flags): MainWindow loads its
    /// sliders FROM the composed parameters (see MainWindow.LoadLayoutProfileKnobs), so applying every
    /// current slider value back on top is a no-op for knobs the user never touched and correct by
    /// construction for the ones they did -- there is no separate partial-override bookkeeping to
    /// keep in sync. Percent-based knobs are stored as the slider's own integer percent (not a
    /// pre-divided fraction) so a composed-value -> slider -> parameter round trip is exact.
    /// </summary>
    internal sealed class LayoutKnobOverrides
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

        public void ApplyTo(MacroLayoutParameters parameters, DungeonTilesetProfile tileset)
        {
            parameters.Style = Style;
            parameters.MinRooms = MinRooms;
            parameters.MaxRooms = MaxRooms;
            parameters.MinRoomCornerSize = MinRoomCornerSize;
            parameters.MaxRoomCornerSize = MaxRoomCornerSize;
            parameters.CorridorWidth = CorridorWidth;
            parameters.LoopFactor = LoopFactorPercent / 100.0;
            parameters.OpenFillTarget = OpenFillTargetPercent / 100.0;
            parameters.EntranceCount = EntranceCount;
            parameters.ExitCount = ExitCount;
            parameters.DoorTransitions = DoorTransitions;

            var accentActive = AccentEnabled && tileset != null && !string.IsNullOrEmpty(tileset.AccentTerrain);
            parameters.AccentTerrain = accentActive ? tileset.AccentTerrain : string.Empty;
            parameters.AccentDensity = accentActive ? AccentDensityPercent / 100.0 : 0.0;

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

    /// <summary>
    /// Drives Content Builder's preview generation through the exact same composition + retry
    /// pipeline SWLOR.ProcgenReview and the runtime facade use (DungeonComposition.
    /// BuildLayoutParameters, then LayoutSolver.Solve's shared seed-derived retry loop). Previously
    /// this cloned the raw layout-profile Template directly, bypassing BuildLayoutParameters'
    /// SecondaryOpenTerrain/CorridorWidth-floor/ChannelTerrain/accent-terrain stamping entirely --
    /// that gap is exactly what caused Preview and the built review module to diverge.
    /// </summary>
    internal static class GenerationEngine
    {
        public static GenerationResult Generate(
            DungeonComposition composition,
            TilesetModel tileset,
            int width,
            int height,
            int seed,
            LayoutKnobOverrides overrides)
        {
            var baseParameters = composition.BuildLayoutParameters();
            overrides?.ApplyTo(baseParameters, composition.Tileset);

            var openTerrainOverride = composition.Tileset?.PrimaryOpenTerrain ?? string.Empty;
            var solved = LayoutSolver.Solve(baseParameters, tileset, width, height, seed, openTerrainOverride);

            // Decorations are pure/engine-free (DungeonDecorationPlanner.Plan) exactly like the layout
            // solver itself, so the preview can compute the SAME plan the runtime facade/ProcgenReview
            // would produce for this composition+seed -- without ever touching the schematic/map
            // render, which stays tile-only (see GenerationResult.PlannedDecorationCount doc comment).
            var plannedDecorationCount = 0;
            if (solved.Success && composition.Content != null && (overrides?.EnableDecorations ?? true))
            {
                var densityPercent = overrides?.DecorationDensityPercent ?? 100;
                plannedDecorationCount = DungeonDecorationPlanner.Plan(solved.Resolved, composition.Tileset, composition.Content, densityPercent).Count;
            }

            return new GenerationResult
            {
                Success = solved.Success,
                Layout = solved.Layout,
                Parameters = solved.Parameters,
                Tileset = tileset,
                Resolved = solved.Resolved,
                AttemptSeed = solved.AttemptSeed,
                FailureReason = solved.FailureReason,
                PlannedDecorationCount = plannedDecorationCount
            };
        }
    }
}
