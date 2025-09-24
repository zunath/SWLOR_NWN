using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class BeastGeneralPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        private readonly IStatService _statService;
        private readonly BeastMastery _beastMastery;

        public BeastGeneralPerkDefinition(IStatService statService, BeastMastery beastMastery)
        {
            _statService = statService;
            _beastMastery = beastMastery;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DiseasedTouch();
            Clip();
            SpinningClaw();
            BeastSpeed();

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
                .GrantsFeat(FeatType.DiseasedTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 11 poison DMG and has a DC10 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.DiseasedTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 poison DMG and has a DC12 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.DiseasedTouch3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 17 poison DMG and has a DC14 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.DiseasedTouch4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 poison DMG and has a DC16 Fortitude check to inflict Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.DiseasedTouch5);
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
                .GrantsFeat(FeatType.Clip1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 physical DMG and has a DC10 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.Clip2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and has a DC12 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.Clip3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG and has a DC14 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.Clip4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 18 physical DMG and has a DC16 Fortitude check to inflict Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.Clip5);
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
                .GrantsFeat(FeatType.SpinningClaw1)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 12 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.SpinningClaw2)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 15 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.SpinningClaw3)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 18 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.SpinningClaw4)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 21 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.SpinningClaw5);
        }

        private void BeastSpeed()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.BeastSpeed)
                .Name("Beast Speed")
                .GroupType(PerkGroupType.Beast)
                .TriggerPurchase((player) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, player);
                    if (!_beastMastery.IsPlayerBeast(beast))
                        return;

                    _statService.ApplyAttacksPerRound(beast, GetItemInSlot(InventorySlot.CreatureLeft));
                })
                .TriggerRefund((player) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, player);
                    if (!_beastMastery.IsPlayerBeast(beast))
                        return;

                    _statService.ApplyAttacksPerRound(beast, GetItemInSlot(InventorySlot.CreatureLeft));
                })

                .AddPerkLevel()
                .Description("The beast gains an additional attack per round.")
                .Price(3)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("The beast gains an additional attack per round.")
                .Price(3)
                .RequirementBeastLevel(30)

                .AddPerkLevel()
                .Description("The beast gains an additional attack per round.")
                .Price(3)
                .RequirementBeastLevel(45);
        }

    }
}
