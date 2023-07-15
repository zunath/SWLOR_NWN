using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public abstract class PlayerMigrationBase: IPlayerMigration
    {
        public abstract int Version { get; }
        public abstract void Migrate(uint player);

        protected void RecalculateStats(uint player)
        {
            // Skipping: SP, AP, MGT, PER, VIT, WIL, AGI, SOC
            AssignCommand(player, () => ClearAllActions());

            // Unequip items
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var slot = (InventorySlot)index;
                if (slot == InventorySlot.CreatureArmor ||
                    slot == InventorySlot.CreatureBite ||
                    slot == InventorySlot.CreatureLeft ||
                    slot == InventorySlot.CreatureRight)
                    continue;

                var item = GetItemInSlot(slot, player);

                AssignCommand(player, () => ActionUnequipItem(item));
            }

            // Unequip is on a delay, waiting one second should be fine given the initial load-in
            // is not usually that quick.
            DelayCommand(1f, () =>
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                // HP
                dbPlayer.MaxHP = Stat.BaseHP;
                dbPlayer.HP = GetMaxHitPoints(player);
                dbPlayer.HPRegen = 0;

                // FP
                dbPlayer.MaxFP = Stat.BaseFP;
                dbPlayer.FP = Stat.GetMaxFP(player, dbPlayer);
                dbPlayer.FPRegen = 0;

                // STM
                dbPlayer.MaxStamina = Stat.BaseSTM;
                dbPlayer.Stamina = Stat.GetMaxStamina(player, dbPlayer);
                dbPlayer.STMRegen = 0;

                // Crafting
                foreach (var (type, _) in Skill.GetActiveCraftingSkills())
                {
                    dbPlayer.Craftsmanship[type] = 0;
                    dbPlayer.Control[type] = 0;
                    dbPlayer.CPBonus[type] = 0;
                }

                // Attack
                dbPlayer.Attack = 0;
                dbPlayer.ForceAttack = 0;

                // Defenses
                foreach (var defense in Combat.GetAllDamageTypes())
                {
                    dbPlayer.Defenses[defense] = 0;
                }

                // Evasion
                dbPlayer.Evasion = 0;

                DB.Set(dbPlayer);
                Stat.AdjustPlayerMaxHP(dbPlayer, player, 0);
                SetCurrentHitPoints(player, GetMaxHitPoints(player));

                // Attacks
                Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
            });
        }

        protected void RefundPerk(uint player, PerkType perkType)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(perkType))
                return;

            var perkLevel = dbPlayer.Perks[perkType];
            var perkDetail = Perk.GetPerkDetails(perkType);
            var refundAmount = perkDetail.PerkLevels
                .Where(x => x.Key <= perkLevel)
                .Sum(x => x.Value.Price);

            dbPlayer.UnallocatedSP += refundAmount;
            dbPlayer.Perks.Remove(perkType);

            DB.Set(dbPlayer);

            Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP for perk '{perkType}'.");
            SendMessageToPC(player, $"Perk '{perkDetail.Name}' was automatically refunded. You reclaimed {refundAmount} SP.");
        }
    }
}
