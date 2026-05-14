using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class LeadershipVanguardCommandPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            RallyingStandard();
            PressTheAttack();
            CoordinatedFocus();
            MarkTarget();
            ChargeOrder();
            BreakMorale();
            CommandRadius();
            DecisiveCommand();

            return _builder.Build();
        }

        private void RallyingStandard()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.RallyingStandard)
                .Name("Rallying Standard")

                .AddPerkLevel()
                .Description("Nearby party members gain +2% physical and Force ability hit chance. SOC scaling can raise this to +3%.")
                .Price(2)
                .GrantsFeat(FeatType.RallyingStandard1)

                .AddPerkLevel()
                .Description("Nearby party members gain +3% physical and Force ability hit chance. SOC scaling can raise this to +4%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .GrantsFeat(FeatType.RallyingStandard2);
        }

        private void PressTheAttack()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.PressTheAttack)
                .Name("Press the Attack")

                .AddPerkLevel()
                .Description("Nearby party members deal +4% damage for 10 seconds. SOC scaling can raise this to +6%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.PressTheAttack1)

                .AddPerkLevel()
                .Description("Nearby party members deal +6% damage for 10 seconds. SOC scaling can raise this to +8%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.PressTheAttack2)

                .AddPerkLevel()
                .Description("Nearby party members gain +8% damage and +3% physical and Force ability hit chance for 12 seconds. SOC scaling can raise these to +10% damage and +4% hit chance.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.PressTheAttack3);
        }

        private void CoordinatedFocus()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus")

                .AddPerkLevel()
                .Description("Nearby party members gain +2% critical hit chance. SOC scaling can raise this to +3%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .GrantsFeat(FeatType.CoordinatedFocus1)

                .AddPerkLevel()
                .Description("Nearby party members gain +3% critical hit chance and +3% critical damage. SOC scaling can raise each bonus to +4%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .GrantsFeat(FeatType.CoordinatedFocus2)

                .AddPerkLevel()
                .Description("Nearby party members gain +4% critical hit chance and +5% critical damage. SOC scaling can raise each bonus to +6%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .GrantsFeat(FeatType.CoordinatedFocus3);
        }

        private void MarkTarget()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.MarkTarget)
                .Name("Mark Target")

                .AddPerkLevel()
                .Description("Marks one enemy for 12 seconds. Party members deal +4% damage to the marked target. SOC scaling can raise this to +6%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .GrantsFeat(FeatType.MarkTarget1)

                .AddPerkLevel()
                .Description("Marks one enemy for 12 seconds. Party members deal +6% damage to the marked target, and the target suffers -6% evasion chance. SOC scaling can raise these to +8% damage and -8% evasion.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .GrantsFeat(FeatType.MarkTarget2);
        }

        private void ChargeOrder()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.ChargeOrder)
                .Name("Charge Order")

                .AddPerkLevel()
                .Description("Nearby party members gain +8% movement speed and +20% Mobility resistance. SOC scaling can raise these to +10% and +25%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.ChargeOrder1)

                .AddPerkLevel()
                .Description("Nearby party members gain +12% movement speed and +35% Mobility resistance. SOC scaling can raise these to +15% and +45%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .GrantsFeat(FeatType.ChargeOrder2);
        }

        private void BreakMorale()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.BreakMorale)
                .Name("Break Morale")

                .AddPerkLevel()
                .Description("Nearby enemies suffer Flash for 12 seconds, reducing physical and Force ability hit chance by 8%. SOC scaling can raise the penalty to 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.BreakMorale1)

                .AddPerkLevel()
                .Description("Nearby enemies suffer Flash, reducing physical and Force ability hit chance by 12%, and Weakened, reducing Attack by 8%, for 12 seconds. SOC scaling can raise these penalties to 14% and 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.BreakMorale2);
        }

        private void CommandRadius()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CommandRadius)
                .Name("Command Radius")

                .AddPerkLevel()
                .Description("Vanguard Command shouts and auras have +2m range. Vanguard Command shout durations increase by 1 second per 10 SOC above 10, capped at +2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 30)
                .IncreasesStat(StatType.LeadershipCommandRadiusBonusMeters, 2)
                .IncreasesStat(StatType.VanguardCommandDurationBonusBaseSeconds, 0)
                .IncreasesStat(StatType.VanguardCommandDurationBonusMaximumSeconds, 2)

                .AddPerkLevel()
                .Description("Vanguard Command shouts and auras have +4m range. Vanguard Command shout buffs last 2 seconds longer. SOC scaling can raise this duration bonus to +4 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 48)
                .IncreasesStat(StatType.LeadershipCommandRadiusBonusMeters, 4)
                .IncreasesStat(StatType.VanguardCommandDurationBonusBaseSeconds, 2)
                .IncreasesStat(StatType.VanguardCommandDurationBonusMaximumSeconds, 4);
        }

        private void DecisiveCommand()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.DecisiveCommand)
                .Name("Decisive Command")

                .AddPerkLevel()
                .Description("For 20 seconds, nearby party members gain +14% damage, +8% physical and Force ability hit chance, and +8% critical hit chance. SOC scaling can raise these to +18%, +10%, and +10%.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.DecisiveCommand1);
        }

    }
}
