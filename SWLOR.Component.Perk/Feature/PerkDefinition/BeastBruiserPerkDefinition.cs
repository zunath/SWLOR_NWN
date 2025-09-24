using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class BeastBruiserPerkDefinition : IPerkListDefinition
    {
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly IStatService _statService;
                private readonly IBeastMasteryService _beastMastery;

        public BeastBruiserPerkDefinition(
            IRandomService random, 
            IPerkService perkService, 
            IStatService statService,
            IBeastMasteryService beastMastery)
        {
            _random = random;
            _perkService = perkService;
            _statService = statService;
            _beastMastery = beastMastery;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            PoisonBreath(builder);
            IceBreath(builder);
            EnduranceLink(builder);

            return builder.Build();
        }

        private void PoisonBreath(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastBruiser, PerkType.PoisonBreath)
                .Name("Poison Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath1)

                .AddPerkLevel()
                .Description("Deals 12 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath2)

                .AddPerkLevel()
                .Description("Deals 16 poison DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Poison.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath3)

                .AddPerkLevel()
                .Description("Deals 20 poison DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Poison.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath4)

                .AddPerkLevel()
                .Description("Deals 24 poison DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Poison.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath5);
        }

        private void IceBreath(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastBruiser, PerkType.IceBreath)
                .Name("Ice Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath1)

                .AddPerkLevel()
                .Description("Deals 12 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath2)

                .AddPerkLevel()
                .Description("Deals 16 ice DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Freezing.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath3)

                .AddPerkLevel()
                .Description("Deals 20 ice DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Freezing.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath4)

                .AddPerkLevel()
                .Description("Deals 24 ice DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Freezing.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath5);
        }



        private void EnduranceLink(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastBruiser, PerkType.EnduranceLink)
                .Name("Endurance Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink3);
        }
    }
}
