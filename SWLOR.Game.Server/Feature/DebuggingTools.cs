using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("testspace")]
        public static void DebugTestSpace()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var shipId = new Guid("B2840D9A-E2BA-4308-9ECA-6D49ED38EB4E");
            dbPlayer.Ships[shipId] = new PlayerShip
            {
                Name = "Mah Test Ship",
                Type = ShipType.LightEscort,
                Shield = 20,
                Hull = 20,
                Capacitor = 10,
                HighPowerModules = new Dictionary<string, PlayerShipModule>
                {
                    {
                        "1", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule
                        }
                    },
                    {
                        "2", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule,
                            RecastTime = DateTime.UtcNow.AddMinutes(5)
                        }
                    },
                    {
                        "3", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule,
                            RecastTime = new DateTime(2021, 3, 14, 23, 5, 9)
                        }
                    },
                },
                LowPowerModules = new Dictionary<string, PlayerShipModule>
                {
                    {
                        "4", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule
                        }
                    },
                    {
                        "5", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule,
                            RecastTime = new DateTime(2021, 3, 14, 18, 2, 3)
                        }
                    },
                    {
                        "6", new PlayerShipModule
                        {
                            Type = ShipModuleType.TestModule
                        }
                    },
                }
            };

            DB.Set(playerId, dbPlayer);

            Space.EnterSpaceMode(player, shipId);

            var location = GetLocation(GetWaypointByTag("Viscara_Orbit"));
            AssignCommand(player, () => ActionJumpToLocation(location));
        }
    }
}
