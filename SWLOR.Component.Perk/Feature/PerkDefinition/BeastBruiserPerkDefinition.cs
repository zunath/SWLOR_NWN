using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class BeastBruiserPerkDefinition : IPerkListDefinition
    {
        private readonly IRandomService _random;
        private readonly IServiceProvider _serviceProvider;

        public BeastBruiserPerkDefinition(
            IRandomService random, 
            IServiceProvider serviceProvider)
        {
            _random = random;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();

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
