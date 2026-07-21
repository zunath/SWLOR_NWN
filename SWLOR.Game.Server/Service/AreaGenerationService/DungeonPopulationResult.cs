using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Result of populating one generated dungeon instance, reported back to the consumer
    /// (e.g. the /genarea chat command) for confirmation messaging.
    /// </summary>
    public class DungeonPopulationResult
    {
        public int RoomsPopulated { get; set; }
        public int CreaturesSpawned { get; set; }
        public bool BossSpawned { get; set; }
        public string BossResref { get; set; } = string.Empty;
        public bool TreasurePlaced { get; set; }
        public uint TreasureContainer { get; set; } = OBJECT_INVALID;
        public int TreasureItemsSpawned { get; set; }
        public int ExitsPlaced { get; set; }
        public bool ExitPlaced => ExitsPlaced > 0;
        /// <summary>How many transitions were realized as real door objects (vs exit placeables).</summary>
        public int DoorsCreated { get; set; }
        /// <summary>How many "set dressing" decoration placeables have been spawned SO FAR (0 when
        /// the request disabled decorations, the theme has no curated palette, or density rolled
        /// zero). Decoration spawning is batched across scheduler ticks (see
        /// DungeonContentPlacer.PlaceDecorations), so this keeps climbing for a moment after
        /// Populate returns -- compare against <see cref="DecorationsPlanned"/> once
        /// <see cref="DecorationsSpawnComplete"/> reports true.</summary>
        public int DecorationsPlaced { get; set; }
        /// <summary>Total decoration placements the deterministic plan produced for this instance --
        /// the count <see cref="DecorationsPlaced"/> converges to as the batched spawn completes.</summary>
        public int DecorationsPlanned { get; set; }
        /// <summary>True once every batched decoration spawn tick has run (or the pass was skipped
        /// entirely). See DungeonContentPlacer.PlaceDecorations.</summary>
        public bool DecorationsSpawnComplete { get; set; }

        /// <summary>How many spawned decorations carried a non-1 per-instance visual scale in the
        /// plan (frontage scale jitter -- see PlannedDecoration.VisualScale).</summary>
        public int ScaleTransformsPlanned { get; set; }
        /// <summary>How many of those scales were verified applied on the live object (read back
        /// via GetObjectVisualTransform after SetObjectVisualTransform). The self-test asserts
        /// this converges 1:1 with <see cref="ScaleTransformsPlanned"/>.</summary>
        public int ScaleTransformsApplied { get; set; }

        /// <summary>How many spawned decorations carried a support anchor in the plan (frontage
        /// buildings on chasm-bearing tilesets -- see PlannedDecoration.GroundAnchor).</summary>
        public int GroundAnchorsPlanned { get; set; }
        /// <summary>How many of those anchors' live GetGroundHeight samples agreed with the plan's
        /// own GroundZ (within 0.5m) -- the live/offline grounding-parity check. The self-test
        /// asserts this converges 1:1 with <see cref="GroundAnchorsPlanned"/>.</summary>
        public int GroundAnchorsVerified { get; set; }
    }
}
