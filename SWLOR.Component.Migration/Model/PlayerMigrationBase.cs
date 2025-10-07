using SWLOR.Component.Migration.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Model
{
    public abstract class PlayerMigrationBase: IPlayerMigration
    {
        protected readonly ILogger Logger;
        protected readonly IDatabaseService Database;
        protected readonly IStatService StatService;
        protected readonly IStatCalculationService StatCalculationService;
        protected readonly ISkillService SkillService;
        protected readonly ICombatService CombatService;
        protected readonly IPerkService PerkService;
        protected readonly IItemService ItemService;
        protected readonly ICreaturePluginService CreaturePlugin;

        protected PlayerMigrationBase(
            ILogger logger,
            IDatabaseService database,
            IStatService statService,
            IStatCalculationService statCalculationService,
            ISkillService skillService,
            ICombatService combatService,
            IPerkService perkService,
            IItemService itemService,
            ICreaturePluginService creaturePlugin)
        {
            Logger = logger;
            Database = database;
            StatService = statService;
            StatCalculationService = statCalculationService;
            SkillService = skillService;
            CombatService = combatService;
            PerkService = perkService;
            ItemService = itemService;
            CreaturePlugin = creaturePlugin;
        }
        public abstract int Version { get; }
        public abstract void Migrate(uint player);

        protected void RecalculateStats(uint player)
        {
            // Skipping: SP, AP, MGT, PER, VIT, WIL, AGI, SOC
            AssignCommand(player, () => ClearAllActions());

            // Unequip items
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var slot = (InventorySlotType)index;
                if (slot == InventorySlotType.CreatureArmor ||
                    slot == InventorySlotType.CreatureBite ||
                    slot == InventorySlotType.CreatureLeft ||
                    slot == InventorySlotType.CreatureRight)
                    continue;

                var item = GetItemInSlot(slot, player);

                AssignCommand(player, () => ActionUnequipItem(item));
            }

            // Unequip is on a delay, waiting one second should be fine given the initial load-in
            // is not usually that quick.
            DelayCommand(1f, () =>
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = Database.Get<Player>(playerId);

                // HP
                dbPlayer.MaxHP = StatService.BaseHP;
                dbPlayer.HP = GetMaxHitPoints(player);
                dbPlayer.HPRegen = 0;

                // FP
                dbPlayer.MaxFP = StatService.BaseFP;
                dbPlayer.FP = StatCalculationService.CalculateMaxFP(player);
                dbPlayer.FPRegen = 0;

                // STM
                dbPlayer.MaxStamina = StatService.BaseSTM;
                dbPlayer.Stamina = StatCalculationService.CalculateMaxSTM(player);
                dbPlayer.STMRegen = 0;

                // Crafting
                foreach (var (type, _) in SkillService.GetActiveCraftingSkills())
                {
                    dbPlayer.Craftsmanship[type] = 0;
                    dbPlayer.Control[type] = 0;
                    dbPlayer.CPBonus[type] = 0;
                }

                // Attack
                dbPlayer.Attack = 0;
                dbPlayer.ForceAttack = 0;

                // Defenses
                foreach (var defense in CombatService.GetAllDamageTypes())
                {
                    dbPlayer.Defenses[defense] = 0;
                }

                // Evasion
                dbPlayer.Evasion = 0;

                Database.Set(dbPlayer);
                StatService.AdjustPlayerMaxHP(dbPlayer, player, 0);
                SetCurrentHitPoints(player, GetMaxHitPoints(player));

                // Attacks
                StatService.ApplyAttacksPerRound(player, OBJECT_INVALID);
            });
        }

        protected void RefundPerk(uint player, PerkType perkType)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = Database.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(perkType))
                return;

            var perkLevel = dbPlayer.Perks[perkType];
            var perkDetail = PerkService.GetPerkDetails(perkType);
            var refundAmount = perkDetail.PerkLevels
                .Where(x => x.Key <= perkLevel)
                .Sum(x => x.Value.Price);

            dbPlayer.UnallocatedSP += refundAmount;
            dbPlayer.Perks.Remove(perkType);

            Database.Set(dbPlayer);

            Logger.Write<MigrationLogGroup>($"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP for perk '{perkType}'.");
            SendMessageToPC(player, $"Perk '{perkDetail.Name}' was automatically refunded. You reclaimed {refundAmount} SP.");
        }
    }
}
