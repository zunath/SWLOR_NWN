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
    }

    public static class RuntimeAreaRegistry
    {
        private static readonly Dictionary<string, RuntimeAreaInstance> _instancesById = new();
        private static readonly Dictionary<uint, string> _instanceIdsByArea = new();

        public static void Register(RuntimeAreaInstance instance)
        {
            _instancesById[instance.InstanceId] = instance;
            _instanceIdsByArea[instance.Area] = instance.InstanceId;
        }

        public static void Unregister(string instanceId)
        {
            if (!_instancesById.TryGetValue(instanceId, out var instance))
                return;

            _instanceIdsByArea.Remove(instance.Area);
            _instancesById.Remove(instanceId);
        }

        public static bool TryGetByArea(uint area, out RuntimeAreaInstance instance)
        {
            instance = null;
            return _instanceIdsByArea.TryGetValue(area, out var id) &&
                   _instancesById.TryGetValue(id, out instance);
        }

        public static bool TryGetById(string instanceId, out RuntimeAreaInstance instance)
        {
            return _instancesById.TryGetValue(instanceId, out instance);
        }

        public static IEnumerable<RuntimeAreaInstance> GetAll()
        {
            return _instancesById.Values;
        }

        /// <summary>
        /// Returns a random walkable location inside a generated area, or an invalid location
        /// (area origin) when the area is not a registered generated instance.
        /// </summary>
        public static Location GetRandomWalkableLocation(uint area)
        {
            if (!TryGetByArea(area, out var instance) || instance.WalkablePoints.Count == 0)
                return Location(area, System.Numerics.Vector3.Zero, 0.0f);

            var point = instance.WalkablePoints[Random.Next(instance.WalkablePoints.Count)];
            return Location(area, point, 0.0f);
        }
    }
}
