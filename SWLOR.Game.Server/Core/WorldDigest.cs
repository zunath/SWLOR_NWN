using System.Collections.Generic;
using System.Globalization;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// What a boot-only handler would change if a reload ran it again, read from the engine: the prices and store flags of
    /// every store item, the ids on the dock waypoints, the spawned objects per area, the objects that carry a property id, the
    /// weather, sky and fog per area, and the number of objects per area. A reload must leave each as the old build left it
    /// (<see cref="ReloadDigests"/>). Engine order is stable while no frame runs, so each is hashed in that order. Read only
    /// when hot reload is on.
    /// </summary>
    public static class WorldDigest
    {
        // The locals and tags the boot-only handlers write; kept here as text so the check names nothing of their domains.
        private const string SpawnIdVariable = "SPAWN_ID";
        private const string PropertyIdVariable = "PROPERTY_ID";
        private const string DockPointTag = "STARSHIP_DOCKPOINT";
        private const string DockPointIdVariable = "STARSHIP_DOCKPOINT_ID";
        private const string StoreItemVariable = "STORE_SERVICE_IS_STORE_ITEM";

        /// <summary>The world digests, in a fixed order.</summary>
        public static IReadOnlyList<ReloadDigestLine> Capture()
        {
            var stores = new WorldDigestAccumulator("world.storeItems");
            var docks = new WorldDigestAccumulator("world.dockIds");
            var spawns = new WorldDigestAccumulator("world.spawnsPerArea");
            var properties = new WorldDigestAccumulator("world.propertyObjects");
            var weather = new WorldDigestAccumulator("world.weatherPerArea");
            var objects = new WorldDigestAccumulator("world.objectsPerArea");

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                weather.Add(1, resref, ((int)GetWeather(area)).ToString(CultureInfo.InvariantCulture),
                    ((int)GetSkyBox(area)).ToString(CultureInfo.InvariantCulture),
                    GetFogAmount(FogType.Sun, area).ToString(CultureInfo.InvariantCulture),
                    GetFogAmount(FogType.Moon, area).ToString(CultureInfo.InvariantCulture),
                    ((int)GetFogColor(FogType.Sun, area)).ToString(CultureInfo.InvariantCulture),
                    ((int)GetFogColor(FogType.Moon, area)).ToString(CultureInfo.InvariantCulture));

                var areaProperty = GetLocalString(area, PropertyIdVariable);
                if (!string.IsNullOrEmpty(areaProperty))
                    properties.Add(1, "area", resref, areaProperty);

                var spawned = 0;
                var count = 0;
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    count++;
                    var type = GetObjectType(obj);
                    if (!string.IsNullOrEmpty(GetLocalString(obj, SpawnIdVariable)))
                        spawned++;

                    var propertyId = GetLocalString(obj, PropertyIdVariable);
                    if (!string.IsNullOrEmpty(propertyId))
                        properties.Add(1, ((int)type).ToString(CultureInfo.InvariantCulture), GetResRef(obj), propertyId);

                    if (type == ObjectType.Waypoint && GetTag(obj) == DockPointTag)
                        docks.Add(1, resref, GetLocalString(obj, DockPointIdVariable));

                    if (type == ObjectType.Store)
                    {
                        for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                        {
                            stores.Add(1, resref, GetTag(obj), GetResRef(item),
                                ItemPlugin.GetBaseGoldPieceValue(item).ToString(CultureInfo.InvariantCulture),
                                GetLocalBool(item, StoreItemVariable) ? "1" : "0");
                        }
                    }
                }

                spawns.Add(spawned, resref, spawned.ToString(CultureInfo.InvariantCulture));
                objects.Add(count, resref, count.ToString(CultureInfo.InvariantCulture));
            }

            return new[] { stores.Line(), docks.Line(), spawns.Line(), properties.Line(), weather.Line(), objects.Line() };
        }
    }
}
