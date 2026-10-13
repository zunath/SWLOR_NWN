using NWN.Native.API;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Native
{
    public static class CreatureNavigation
    {
        public static bool TryFindWalkableDestination(uint creature, Vector3 requested, out Vector3 destination)
        {
            destination = default;
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);
            var area = nativeCreature?.GetArea();
            if (area == null)
                return false;

            using var start = new Vector(requested.X, requested.Y, requested.Z);
            using var result = new Vector();
            // The NWNX vector-only wrapper returns the requested point on failure, so use the
            // native success result to avoid treating blocked points as valid destinations.
            if (area.ComputeSafeLocation(start, 0.5f, nativeCreature.m_pcPathfindInformation, 1, result) == 0)
                return false;

            destination = new Vector3(result.x, result.y, result.z);
            return true;
        }
    }
}
