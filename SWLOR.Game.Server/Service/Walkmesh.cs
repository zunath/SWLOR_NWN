using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Walkmesh
    {
        private static readonly Dictionary<uint, List<Location>> _walkmeshesByArea = new Dictionary<uint, List<Location>>();
        private const int AreaBakeStep = 5;

        /// <summary>
        /// When the module loads, generate a list of walkable locations in each area.
        /// These locations can be used to spawn objects randomly throughout an area.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadWalkmeshes()
        {
            Console.WriteLine("Baking areas...");
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                BakeArea(area);
            }
            Console.WriteLine("Finished baking areas.");
        }

        // Area baking process
        // Run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        private static void BakeArea(uint area)
        {
            _walkmeshesByArea[area] = new List<Location>();

            const float MinDistance = 6.0f;
            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            var areaResref = GetResRef(area);

            var arraySizeX = width * (10 / AreaBakeStep);
            var arraySizeY = height * (10 / AreaBakeStep);

            for (var x = 0; x < arraySizeX; x++)
            {
                for (var y = 0; y < arraySizeY; y++)
                {
                    var checkLocation = Location(area, Vector3(x * AreaBakeStep, y * AreaBakeStep), 0.0f);
                    var material = GetSurfaceMaterial(checkLocation);
                    var isWalkable = Convert.ToInt32(Get2DAString("surfacemat", "Walk", material)) == 1;

                    // Location is not walkable if another object exists nearby.
                    var nearest = GetNearestObjectToLocation(checkLocation, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable | ObjectType.Trigger);
                    var distance = GetDistanceBetweenLocations(checkLocation, GetLocation(nearest));
                    if (GetIsObjectValid(nearest) && distance <= MinDistance)
                    {
                        isWalkable = false;
                    }

                    if (isWalkable)
                    {
                        var location = Location(
                            area,
                            new Vector3(x * AreaBakeStep,
                                y * AreaBakeStep,
                                GetGroundHeight(checkLocation)), 
                            0.0f);
                        _walkmeshesByArea[area].Add(location);
                    }
                }
            }

            Console.WriteLine("Area walkmesh up to date: " + GetName(area));
        }

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public static Location GetRandomLocation(uint area)
        {
            if (!_walkmeshesByArea.ContainsKey(area)) return Location(area, Vector3.Zero, 0.0f);

            var count = _walkmeshesByArea[area].Count;
            if (count <= 0) return Location(area, Vector3.Zero, 0.0f);

            var index = Random.Next(count);
            return _walkmeshesByArea[area][index];
        }

    }
}
