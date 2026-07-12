using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// How a generated area behaves across server restarts.
    /// Only Ephemeral is implemented; the other modes are reserved so consumer code
    /// can declare intent now and pick up the behavior when those strategies land.
    /// </summary>
    public enum AreaPersistenceMode
    {
        /// <summary>Instance vanishes on restart; players who were inside relocate to the recorded entrance location.</summary>
        Ephemeral = 0,
        /// <summary>Seed and config persist; the same layout regenerates deterministically after a restart.</summary>
        SeedPersisted = 1,
        /// <summary>The realized area is exported as a real module resource and survives restarts.</summary>
        FullExport = 2
    }

    public class AreaGenerationRequest
    {
        public string TilesetResref { get; set; } = "tdt01";
        /// <summary>Module area cloned as the shell for the generated grid. Must be registered in the module.</summary>
        public string PlaceholderResref { get; set; } = "gen_placeholder1";
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Fixed seed for deterministic output. Null picks a random seed per attempt.</summary>
        public int? Seed { get; set; }
        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;
        /// <summary>
        /// Bounded regeneration retries before the request fails cleanly. Path validation can
        /// reject a layout when a tileset's junction tiles have restricted path nodes (observed
        /// on zsf01, which has exactly one simple tile per corner class), so the budget must
        /// absorb several rejections.
        /// </summary>
        public int MaxAttempts { get; set; } = 6;
        public AreaPersistenceMode Persistence { get; set; } = AreaPersistenceMode.Ephemeral;
        public string DisplayName { get; set; } = "Generated Area";
        public string Tag { get; set; } = "GENERATED_AREA";
        /// <summary>Tile light color indices applied to every generated tile (theme-driven).</summary>
        public DungeonTileLighting Lighting { get; set; } = new();
    }

    public class AreaGenerationResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public uint Area { get; set; } = OBJECT_INVALID;
        public ResolvedLayout Layout { get; set; }
        public int SeedUsed { get; set; }
        public int AttemptsUsed { get; set; }
    }
}
