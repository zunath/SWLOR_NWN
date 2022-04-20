using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            WeaponFinesse();
            Mobility();
            Dash();

            return _builder.Build();
        }

        private void WeaponFinesse()
        {
            _builder.Create(PerkCategoryType.General, PerkType.WeaponFinesse)
                .Name("Weapon Finesse")

                .AddPerkLevel()
                .Description("You make melee attack rolls with your PER score if it is higher than your MGT score.")
                .Price(3)
                .GrantsFeat(FeatType.WeaponFinesse);
        }

        private void Mobility()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Mobility)
                .Name("Mobility")

                .AddPerkLevel()
                .Description("Improves your ability to avoid attacks of opportunity.")
                .Price(5)
                .GrantsFeat(FeatType.Mobility);
        }

        /// <summary>
        /// When a player enters the module, if they have the Dash perk,
        /// apply the movement speed changes.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void ApplyDash()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var dashLevel = Perk.GetEffectivePerkLevel(player, PerkType.Dash);
            CreaturePlugin.SetMovementRateFactor(player, 1.0f + dashLevel * 0.1f);
        }

        private void Dash()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Dash)
                .Name("Dash")

                .AddPerkLevel()
                .Description("Increases movement rate by 10%")
                .Price(2)

                .AddPerkLevel()
                .Description("Increases movement rate by 20%")
                .Price(3)

                .AddPerkLevel()
                .Description("Increases movement rate by 30%")
                .Price(3)

                .AddPerkLevel()
                .Description("Increases movement rate by 40%")
                .Price(4)

                .AddPerkLevel()
                .Description("Increases movement rate by 50%")
                .Price(4)

                .TriggerPurchase((player, type, level) =>
                {
                    CreaturePlugin.SetMovementRateFactor(player, 1.0f + level * 0.1f);
                })
                
                .TriggerRefund((player, type, level) =>
                {
                    CreaturePlugin.SetMovementRateFactor(player, 1.0f);
                });
        }
    }
}
