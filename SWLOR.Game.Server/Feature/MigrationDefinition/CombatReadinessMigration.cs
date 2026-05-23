using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class CombatReadinessMigration
    {
        public static void ResetCombatReadiness(Player dbPlayer)
        {
            dbPlayer.CombatReadiness = 0;
        }

        public static void MigratePlayer(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            ResetCombatReadiness(dbPlayer);
            dbPlayer.CombatReadiness = CalculateEquippedCombatReadiness(player);

            DB.Set(dbPlayer);
        }

        private static int CalculateEquippedCombatReadiness(uint creature)
        {
            var amount = 0;

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item))
                    continue;

                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) != ItemPropertyType.CombatReadiness)
                        continue;

                    amount += GetItemPropertyCostTableValue(ip);
                }
            }

            return amount;
        }
    }
}
