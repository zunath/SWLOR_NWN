using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastBalancedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Claw();
            BolsterAttack();
            Hasten();

            return _builder.Build();
        }

        private void Claw()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Claw)
                .Name("Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 8 physical DMG and has a DC8 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 11 physical DMG and has a DC10 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and has a DC12 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 17 physical DMG and has a DC14 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 physical DMG and has a DC16 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw5);
        }

        private void BolsterAttack()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.BolsterAttack)
                .Name("Bolster Attack")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack1)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack2)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack3)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack4)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack5);
        }

        private void Hasten()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Hasten)
                .Name("Hasten")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants one additional attack to the beast for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten1)

                .AddPerkLevel()
                .Description("Grants two additional attacks to the beast for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten2)

                .AddPerkLevel()
                .Description("Grants two additional attacks to the beast and one additional attack to the Beastmaster for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten3);
        }
    }
}