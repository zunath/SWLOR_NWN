using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastGeneralPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DiseasedTouch();
            Clip();
            SpinningClaw();

            return _builder.Build();
        }

        private void DiseasedTouch()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.DiseasedTouch)
                .Name("Diseased Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 8 poison DMG and has a DC8 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 11 poison DMG and has a DC10 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 poison DMG and has a DC12 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 17 poison DMG and has a DC14 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 poison DMG and has a DC16 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45);
        }

        private void Clip()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.Clip)
                .Name("Clip")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 10 physical DMG and has a DC8 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 physical DMG and has a DC10 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and has a DC12 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG and has a DC14 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 18 physical DMG and has a DC16 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(45);
        }

        private void SpinningClaw()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.SpinningClaw)
                .Name("Spinning Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 8 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(5)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 12 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 15 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(25)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 18 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(35)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 21 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(45);
        }

    }
}
