using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
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
