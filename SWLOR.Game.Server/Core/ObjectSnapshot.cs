using System.Collections.Generic;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Core
{
    /// <summary>Finishes native searches before gameplay callbacks can start another search.</summary>
    public static class ObjectSnapshot
    {
        public static IReadOnlyList<uint> InShape(Shape shape, float size, Location location,
            bool lineOfSight = false, ObjectType objectFilter = ObjectType.Creature)
        {
            return Capture(
                () => GetFirstObjectInShape(shape, size, location, lineOfSight, objectFilter),
                () => GetNextObjectInShape(shape, size, location, lineOfSight, objectFilter),
                GetIsObjectValid);
        }

        public static IReadOnlyList<uint> InArea(uint area, ObjectType objectFilter)
        {
            return Capture(() => GetFirstObjectInArea(area, objectFilter),
                () => GetNextObjectInArea(area, objectFilter), GetIsObjectValid);
        }

        public static IReadOnlyList<uint> Players()
        {
            return Capture(() => GetFirstPC(), () => GetNextPC(), GetIsObjectValid);
        }

        public static IReadOnlyList<uint> Capture(Func<uint> first, Func<uint> next, Func<uint, bool> isValid)
        {
            var objects = new List<uint>();
            var visited = new HashSet<uint>();
            for (var obj = first(); isValid(obj); obj = next())
            {
                if (!visited.Add(obj))
                    throw new InvalidOperationException($"Native object search repeated object {obj:X8} instead of advancing.");

                objects.Add(obj);
            }

            return objects;
        }
    }
}
