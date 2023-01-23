using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastTankPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BolsterArmor();
            Anger();
            FocusAttention();

            return _builder.Build();
        }

        private void BolsterArmor()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.BolsterArmor)
                .Name("Bolster Armor")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's physical defense by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor1)

                .AddPerkLevel()
                .Description("Increases the beast's physical defense by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor2)

                .AddPerkLevel()
                .Description("Increases the beast's physical defense by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor3)

                .AddPerkLevel()
                .Description("Increases the beast's physical defense by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor4)

                .AddPerkLevel()
                .Description("Increases the beast's physical defense by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor5);
        }

        private void Anger()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.Anger)
                .Name("Anger")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger1)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger2)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger3)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking the beast.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger4)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking the beast.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger5);
        }

        private void FocusAttention()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.FocusAttention)
                .Name("Focus Attention")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 10%.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention1)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 20%.")
                .Price(2)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention2)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 30%.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention3)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 40%.")
                .Price(3)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention4)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 50%.")
                .Price(3)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention5);
        }
    }
}
