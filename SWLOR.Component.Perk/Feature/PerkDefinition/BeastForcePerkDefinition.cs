using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
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
    public class BeastForcePerkDefinition : IPerkListDefinition
    {
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly IStatService _statService;
                private readonly IBeastMasteryService _beastMastery;

        public BeastForcePerkDefinition(
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
            ForceTouch(builder);
            Innervate(builder);
            ForceLink(builder);

            return builder.Build();
        }

        private void ForceTouch(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastForce, PerkType.ForceTouch)
                .Name("Force Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 force DMG.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 force DMG.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 force DMG.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 24 force DMG.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 28 force DMG.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch5);
        }

        private void Innervate(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastForce, PerkType.Innervate)
                .Name("Innervate")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast restores 30 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate1)

                .AddPerkLevel()
                .Description("The beast restores 40 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate2)

                .AddPerkLevel()
                .Description("The beast restores 60 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate3)

                .AddPerkLevel()
                .Description("The beast restores 80 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate4)

                .AddPerkLevel()
                .Description("The beast restores 120 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate5);
        }

        private void ForceLink(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BeastForce, PerkType.ForceLink)
                .Name("Force Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink3);
        }
    }
}
