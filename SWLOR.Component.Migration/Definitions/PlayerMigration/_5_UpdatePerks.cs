using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _5_UpdatePerks: PlayerMigrationBase
    {
        public _5_UpdatePerks(
            ILogger logger,
            IDatabaseService database,
            IStatService statService,
            IStatCalculationService statCalculationService,
            ISkillService skillService,
            ICombatService combatService,
            IPerkService perkService,
            IItemService itemService,
            ICreaturePluginService creaturePlugin)
            : base(logger, database, statService, statCalculationService, skillService, combatService, perkService, itemService, creaturePlugin)
        {
        }

        public override int Version => 5;
        public override void Migrate(uint player)
        {
            var rightHandWeapon = GetItemInSlot(InventorySlotType.RightHand, player);

            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
            StatService.ApplyAttacksPerRound(player, rightHandWeapon);

            var innerStrength = PerkService.GetPerkLevel(player, PerkType.InnerStrength);
            if (innerStrength > 0)
            {
                // Remove old one which only targeted gloves.
                CreaturePlugin.SetCriticalRangeModifier(player, 0, 0, true, BaseItemType.Gloves);

                // Apply new one which targets all weapons
                CreaturePlugin.SetCriticalRangeModifier(player, -innerStrength, 0, true);
            }
        }
    }
}
