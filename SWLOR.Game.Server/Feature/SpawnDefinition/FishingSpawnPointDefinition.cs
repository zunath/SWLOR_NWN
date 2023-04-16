using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SpawnService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class FishingSpawnPointDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            FishingPoints();

            return _builder.Build();
        }

        private void CreateFishingPoint(string tableId, FishingLocationType location)
        {
            _builder.Create(tableId)
                .AddSpawn(ObjectType.Placeable, "fish_point")
                .RespawnDelay(90 + Random.Next(30))
                .SpawnAction(spawn =>
                {
                    var fishResrefList = Fishing.GetFishAvailableAtLocation(location);
                    var description = $"Equip a fishing rod, load some bait, and click this to begin fishing.\n\n" +
                                      "You spot the following fish in this location:\n\n";

                    foreach (var resref in fishResrefList)
                    {
                        var itemName = Cache.GetItemNameByResref(resref);
                        description += itemName + "\n";
                    }

                    SetLocalInt(spawn, Fishing.FishingPointLocationVariable, (int)location);
                    SetDescription(spawn, description);
                });
        }

        private void FishingPoints()
        {
            CreateFishingPoint("FP_VISC_CAVERN", FishingLocationType.ViscaraCavern);
            CreateFishingPoint("FP_VISC_DEEPWOODS", FishingLocationType.ViscaraDeepwoods);
            CreateFishingPoint("FP_VISC_EASTERN_SWAMPLANDS", FishingLocationType.ViscaraEasternSwamplands);
            CreateFishingPoint("FP_VISC_LAKE", FishingLocationType.ViscaraLake);
            CreateFishingPoint("FP_VISC_LAKE_GROUNDS", FishingLocationType.ViscaraLakeGrounds);
            CreateFishingPoint("FP_VISC_MOUNTAIN_VALLEY", FishingLocationType.ViscaraMountainValley);
            CreateFishingPoint("FP_VISC_WESTERN_SWAMPLANDS", FishingLocationType.ViscaraWesternSwamplands);
            CreateFishingPoint("FP_VISC_WILDLANDS", FishingLocationType.ViscaraWildlands);
            CreateFishingPoint("FP_VISC_WILDWOODS", FishingLocationType.ViscaraWildwoods);
            CreateFishingPoint("FP_MONC_CORAL_ISLES_INNER", FishingLocationType.MonCalaCoralIslesInner);
            CreateFishingPoint("FP_MONC_CORAL_ISLES_OUTER", FishingLocationType.MonCalaCoralIslesOuter);
            CreateFishingPoint("FP_MONC_DAC_CITY_SURFACE", FishingLocationType.MonCalaDacCitySurface);
            CreateFishingPoint("FP_MONC_SHARPTOOTH_JUNGLE_SOUTH", FishingLocationType.MonCalaSharptoothJungleSouth);
            CreateFishingPoint("FP_MONC_SHARPTOOTH_JUNGLE_CAVES", FishingLocationType.MonCalaSharptoothJungleCaves);
            CreateFishingPoint("FP_MONC_SUNKENHEDGE_SWAMPS", FishingLocationType.MonCalaSunkenhedgeSwamps);
            CreateFishingPoint("FP_HUTL_CLONING_TEST_SITE", FishingLocationType.HutlarCloningTestSite);
            CreateFishingPoint("FP_HUTL_QION_FOOTHILLS", FishingLocationType.HutlarQionFoothills);
            CreateFishingPoint("FP_HUTL_QION_TUNDRA", FishingLocationType.HutlarQionTundra);
            CreateFishingPoint("FP_HUTL_QION_VALLEY", FishingLocationType.HutlarQionValley);
            CreateFishingPoint("FP_TAT_BABY_SARLACC_CAVE", FishingLocationType.TatooineBabySarlaccCave);
            CreateFishingPoint("FP_TAT_TRAIDER_CAVE_MAIN_FLOOR", FishingLocationType.TatooineTuskenRaiderCaveMainFloor);
            CreateFishingPoint("FP_DATH_DESERT_WEST_SIDE", FishingLocationType.DathomirDesertWestSide);
            CreateFishingPoint("FP_DATH_GROTTO_CAVERNS", FishingLocationType.DathomirGrottoCaverns);
            CreateFishingPoint("FP_DATH_GROTTOS", FishingLocationType.DathomirGrottos);
            CreateFishingPoint("FP_DATH_MOUNTAINS", FishingLocationType.DathomirMountains);
            CreateFishingPoint("FP_DATH_TRIBE_VILLAGE", FishingLocationType.DathomirTribeVillage);
            CreateFishingPoint("FP_DANTOOINE_LAKE", FishingLocationType.DantooineLake);
            CreateFishingPoint("FP_DANTOOINE_MOUNTAIN_JUNGLES", FishingLocationType.DantooineMountainJungles);
            CreateFishingPoint("FP_DANTOOINE_CANYON", FishingLocationType.DantooineCanyon);
            CreateFishingPoint("FP_DANTOOINE_SOUTH_PLAINS", FishingLocationType.DantooineSouthFields);
            CreateFishingPoint("FP_DAN_FORSAKEN_JUNGLES", FishingLocationType.DantooineForsakenJungles);
        }

    }
}
