using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastEvasionPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            EvasiveManeuver();
            Assault();
            Sniff();

            return _builder.Build();
        }

        private void EvasiveManeuver()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver1)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver2)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver3)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver4)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver5);
        }

        private void Assault()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Assault)
                .Name("Assault")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 10 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 22 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 26 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault5);
        }

        private void Sniff()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Sniff)
                .Name("Sniff")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 8.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff1)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 15.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff2)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 25.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff3);
        }
    }
}
