using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public abstract class PlayerMigrationBase: IPlayerMigration
    {
        public abstract int Version { get; }
        public abstract void Migrate(uint player);
        public virtual void MigratePlayerData(Player player) { }

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

                if (!GetIsObjectValid(item))
                    continue;
                PlayerEquipmentStorage.Unequip(player, item, slot);
            }

            // Finish before the runner checkpoints this character; delayed work
            // could otherwise be skipped permanently after a disconnect.
            KatarAnimationRemap.RefreshEquipmentAnimations(player);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // HP
            dbPlayer.MaxHP = Stat.BaseHP;
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
            dbPlayer.CombatReadiness = 0;

            // Defenses
            foreach (var defense in Combat.GetDefenseDamageTypes())
            {
                dbPlayer.Defenses[defense] = 0;
            }

            // Resistances
            foreach (var resistance in Resistance.GetAllResistanceTypes())
            {
                dbPlayer.Resistances[resistance] = 0;
            }

            // Evasion
            dbPlayer.Evasion = 0;

            Stat.AdjustPlayerMaxHP(dbPlayer, player, 0);
            SetCurrentHitPoints(player, GetMaxHitPoints(player));
            dbPlayer.HP = GetCurrentHitPoints(player);
            DB.Set(dbPlayer);
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
