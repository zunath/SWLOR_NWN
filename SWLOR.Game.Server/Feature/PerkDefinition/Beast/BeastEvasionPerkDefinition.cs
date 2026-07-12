using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition.Beast
{
    public sealed class BeastEvasionPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            EvasiveManeuver();
            Assault();
            DistractingFeint();
            EvasiveChallenge();
            Sniff();
            QuickRecovery();
            UntouchableInstinct();

            return _builder.Build();
        }

        private void EvasiveManeuver()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast gains +6% evasion chance for 3 minutes.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver1)

                .AddPerkLevel()
                .Description("The beast gains +10% evasion chance for 3 minutes.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver2)

                .AddPerkLevel()
                .Description("The beast gains +14% evasion chance for 3 minutes.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver3);
        }

        private void Assault()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Assault)
                .Name("Assault")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +10 physical DMG and grants +6% evasion chance for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +20 physical DMG and grants +10% evasion chance for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +32 physical DMG and grants +14% evasion chance for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault3);
        }

        private void DistractingFeint()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.DistractingFeint)
                .Name("Distracting Feint")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack reduces target hit chance and evasion chance by 4% for 15 seconds and generates 350 base Enmity plus VIT scaling.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.DistractingFeint1)

                .AddPerkLevel()
                .Description("The beast's next attack reduces target hit chance and evasion chance by 8% for 15 seconds and generates 500 base Enmity plus VIT scaling.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.DistractingFeint2)

                .AddPerkLevel()
                .Description("The beast's next attack reduces target hit chance and evasion chance by 12% for 15 seconds and generates 650 base Enmity plus VIT scaling.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.DistractingFeint3);
        }

        private void EvasiveChallenge()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.EvasiveChallenge)
                .Name("Evasive Challenge")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast goads a single target, gains +8% evasion chance for 30 seconds, and restores 1 STM the next time it evades during the effect.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveChallenge1)

                .AddPerkLevel()
                .Description("The beast goads the selected target and enemies within 5m, removes movement slow, gains +14% evasion chance for 30 seconds, and restores 1 STM the next time it evades during the effect.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveChallenge2);
        }

        private void Sniff()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Sniff)
                .Name("Sniff")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SniffTrait)
                .Description("The beast gains +5% rare item find chance after combat.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .IncreasesStat(StatType.RareItemFindChance, 5)

                .AddPerkLevel()
                .Description("The beast gains +10% rare item find chance after combat.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .IncreasesStat(StatType.RareItemFindChance, 10)

                .AddPerkLevel()
                .Description("The beast gains +15% rare item find chance after combat.")
                .Price(3)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .IncreasesStat(StatType.RareItemFindChance, 15);
        }

        private void QuickRecovery()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.QuickRecovery)
                .Name("Quick Recovery")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickRecoveryTrait)
                .Description("When the beast evades an attack, it has a 15% chance to restore 1 STM.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .IncreasesStat(StatType.AvoidedAttackStaminaRestoreChance, 15)
                .IncreasesStat(StatType.AvoidedAttackStaminaRestore, 1)

                .AddPerkLevel()
                .Description("When the beast evades an attack, it has a 25% chance to restore 1 STM.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .IncreasesStat(StatType.AvoidedAttackStaminaRestoreChance, 25)
                .IncreasesStat(StatType.AvoidedAttackStaminaRestore, 1);
        }

        private void UntouchableInstinct()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.UntouchableInstinct)
                .Name("Untouchable Instinct")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast gains +20% evasion chance, +30% enmity generation, and 20% movement speed for 30 seconds.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.UntouchableInstinct1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.UntouchableInstinctMasteryQuestId);
        }

    }
}
