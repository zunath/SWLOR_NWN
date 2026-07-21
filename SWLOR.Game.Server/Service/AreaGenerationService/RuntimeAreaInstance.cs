using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Tracks areas created at runtime by the generation system.
    /// Generated areas are deliberately absent from Area.AreasByResref (their resrefs are
    /// engine-generated), and the boot-time Walkmesh bake never covers them, so walkable
    /// locations are served from the layout-derived points stored here.
    /// </summary>
    public class RuntimeAreaInstance
    {
        public string InstanceId { get; set; } = string.Empty;
        public uint Area { get; set; } = OBJECT_INVALID;
        public string OverrideName { get; set; } = string.Empty;
        public ResolvedLayout Layout { get; set; }
        public AreaGenerationRequest Request { get; set; }
        /// <summary>Positions at the center of fully-open room tiles, usable for spawns and jumps.</summary>
        public List<Vector3> WalkablePoints { get; set; } = new();
        /// <summary>Where players are delivered when the instance is torn down or lost.</summary>
        public Location ExitLocation { get; set; }
        /// <summary>Creatures/placeables spawned by content population (e.g. DungeonContentPlacer), tracked for teardown.</summary>
        public List<uint> SpawnedObjects { get; set; } = new();
    }
}
