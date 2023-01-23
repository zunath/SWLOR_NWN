using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastDamagePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Bite();
            FlameBreath();
            ShockingSlash();

            return _builder.Build();
        }

        private void Bite()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.Bite)
                .Name("Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 24 physical DMG.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 28 physical DMG.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite5);
        }

        private void FlameBreath()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.FlameBreath)
                .Name("Flame Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 fire DMG to all targets within a cone in front of the beast.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath1)

                .AddPerkLevel()
                .Description("Deals 12 fire DMG to all targets within a cone in front of the beast.")
                .Price(2)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath2)

                .AddPerkLevel()
                .Description("Deals 16 fire DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Burning.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath3)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Burning.")
                .Price(3)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath4)

                .AddPerkLevel()
                .Description("Deals 24 fire DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Burning.")
                .Price(3)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath5);
        }

        private void ShockingSlash()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.ShockingSlash)
                .Name("Shocking Slash")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 electrical DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash1)

                .AddPerkLevel()
                .Description("Deals 12 electrical DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash2)

                .AddPerkLevel()
                .Description("Deals 16 electrical DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Shock.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash3)

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Shock.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash4)

                .AddPerkLevel()
                .Description("Deals 24 electrical DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Shock.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash5);
        }
    }
}
