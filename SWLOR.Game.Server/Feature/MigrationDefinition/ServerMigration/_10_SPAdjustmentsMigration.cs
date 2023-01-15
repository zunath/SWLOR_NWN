using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _10_SPAdjustmentsMigration : ServerMigrationBase, IServerMigration
    {
        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            // Force - Universal Price Changes
            {(PerkType.ForcePush, 1), 2},
            {(PerkType.ForcePush, 2), 2},
            {(PerkType.ForcePush, 3), 3},
            {(PerkType.ForcePush, 4), 3},

            {(PerkType.ThrowLightsaber, 1), 3},
            {(PerkType.ThrowLightsaber, 2), 3},
            {(PerkType.ThrowLightsaber, 3), 3},

            {(PerkType.ForceStun, 1), 3},
            {(PerkType.ForceStun, 2), 4},
            {(PerkType.ForceStun, 3), 4},

            {(PerkType.BattleInsight, 1), 3},
            {(PerkType.BattleInsight, 2), 3},

            {(PerkType.MindTrick, 1), 3},
            {(PerkType.MindTrick, 2), 3},

            {(PerkType.Premonition, 1), 3},
            {(PerkType.Premonition, 2), 3},

            {(PerkType.ThrowRock, 1), 2},
            {(PerkType.ThrowRock, 2), 3},
            {(PerkType.ThrowRock, 3), 4},
            {(PerkType.ThrowRock, 4), 5},
            {(PerkType.ThrowRock, 5), 6},

            {(PerkType.ForceInspiration, 1), 4},
            {(PerkType.ForceInspiration, 2), 4},
            {(PerkType.ForceInspiration, 3), 4},

            // Force - Light Side Price Changes
            {(PerkType.ForceHeal, 1), 2},
            {(PerkType.ForceHeal, 2), 2},
            {(PerkType.ForceHeal, 3), 3},
            {(PerkType.ForceHeal, 4), 3},
            {(PerkType.ForceHeal, 5), 4},

            {(PerkType.ForceBurst, 1), 4},
            {(PerkType.ForceBurst, 2), 5},
            {(PerkType.ForceBurst, 3), 5},
            {(PerkType.ForceBurst, 4), 6},

            {(PerkType.ForceMind, 1), 4},
            {(PerkType.ForceMind, 2), 6},

            {(PerkType.Benevolence, 1), 3},
            {(PerkType.Benevolence, 2), 3},
            {(PerkType.Benevolence, 3), 3},

            {(PerkType.ForceValor, 1), 3},
            {(PerkType.ForceValor, 2), 3},
            
            // Force - Dark Side Price Changes
            {(PerkType.ForceDrain, 1), 2},
            {(PerkType.ForceDrain, 2), 2},
            {(PerkType.ForceDrain, 3), 3},
            {(PerkType.ForceDrain, 4), 3},
            {(PerkType.ForceDrain, 5), 4},

            {(PerkType.ForceLightning, 1), 4},
            {(PerkType.ForceLightning, 2), 5},
            {(PerkType.ForceLightning, 3), 5},
            {(PerkType.ForceLightning, 4), 6},

            {(PerkType.ForceBody, 1), 4},
            {(PerkType.ForceBody, 2), 6},

            {(PerkType.CreepingTerror, 1), 3},
            {(PerkType.CreepingTerror, 2), 3},
            {(PerkType.CreepingTerror, 3), 3},

            {(PerkType.ForceRage, 1), 3},
            {(PerkType.ForceRage, 2), 3},
        };

        public int Version => 10;
        public void Migrate()
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);

            var dbPlayers = DB.Search(dbQuery
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                var refundAmount = 0;

                // Calculate the refund amount first.
                foreach (var ((type, level), sp) in _refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type) && dbPlayer.Perks[type] >= level)
                    {
                        refundAmount += sp;
                    }
                }

                // Then remove the perks being refunded.
                foreach (var ((type, _), _) in _refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type))
                    {
                        dbPlayer.Perks.Remove(type);
                    }
                }

                if (refundAmount > 0)
                {
                    dbPlayer.UnallocatedSP += refundAmount;

                    Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");

                    DB.Set(dbPlayer);
                }
            }
        }
    }
}
