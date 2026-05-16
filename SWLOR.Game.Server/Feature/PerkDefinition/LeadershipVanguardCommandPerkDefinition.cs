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
            CommandPresence();
            DecisiveCommand();

            return _builder.Build();
        }

        private void RallyingStandard()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.RallyingStandard)
                .Name("Rallying Standard")

                .AddPerkLevel()
                .Description("Nearby party members gain +3% physical and Force ability hit chance. SOC scaling can raise this to +4%.")
                .Price(2)
                .GrantsFeat(FeatType.RallyingStandard1)

                .AddPerkLevel()
                .Description("Nearby party members gain +5% physical and Force ability hit chance. SOC scaling can raise this to +6%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .GrantsFeat(FeatType.RallyingStandard2);
        }

        private void PressTheAttack()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.PressTheAttack)
                .Name("Press the Attack")

                .AddPerkLevel()
                .Description("Nearby party members deal +8% damage for 12 seconds. SOC scaling can raise this to +10%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.PressTheAttack1)

                .AddPerkLevel()
                .Description("Nearby party members deal +11% damage for 12 seconds. SOC scaling can raise this to +14%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.PressTheAttack2)

                .AddPerkLevel()
                .Description("Nearby party members gain +14% damage and +5% physical and Force ability hit chance for 15 seconds. SOC scaling can raise these to +18% damage and +7% hit chance.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.PressTheAttack3);
        }

        private void CoordinatedFocus()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus")

                .AddPerkLevel()
                .Description("Nearby party members gain +3% critical hit chance. SOC scaling can raise this to +4%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .GrantsFeat(FeatType.CoordinatedFocus1)

                .AddPerkLevel()
                .Description("Nearby party members gain +4% critical hit chance and +5% critical damage. SOC scaling can raise these to +5% and +7%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .GrantsFeat(FeatType.CoordinatedFocus2)

                .AddPerkLevel()
                .Description("Nearby party members gain +6% critical hit chance and +8% critical damage. SOC scaling can raise these to +7% and +10%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .GrantsFeat(FeatType.CoordinatedFocus3);
        }

        private void MarkTarget()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.MarkTarget)
                .Name("Mark Target")

                .AddPerkLevel()
                .Description("Marks one enemy for 15 seconds. Party members deal +8% damage to the marked target. SOC scaling can raise this to +10%. The mark requires a successful attack-roll check.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .GrantsFeat(FeatType.MarkTarget1)

                .AddPerkLevel()
                .Description("Marks one enemy for 15 seconds. Party members deal +12% damage to the marked target, and the target suffers -10% evasion chance. SOC scaling can raise these to +15% damage and -12% evasion. The mark requires a successful attack-roll check.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .GrantsFeat(FeatType.MarkTarget2);
        }

        private void ChargeOrder()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.ChargeOrder)
                .Name("Charge Order")

                .AddPerkLevel()
                .Description("Nearby party members gain +10% movement speed and +30 Mobility resistance. SOC scaling can raise these to +12% and +40.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.ChargeOrder1)

                .AddPerkLevel()
                .Description("Nearby party members gain +15% movement speed and +50 Mobility resistance. SOC scaling can raise these to +18% and +65.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .GrantsFeat(FeatType.ChargeOrder2);
        }

        private void BreakMorale()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.BreakMorale)
                .Name("Break Morale")

                .AddPerkLevel()
                .Description("Nearby enemies suffer Flash for 15 seconds, reducing physical and Force ability hit chance by 10%. SOC scaling can raise the penalty to 12%. This command applies reliably to valid nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.BreakMorale1)

                .AddPerkLevel()
                .Description("Nearby enemies suffer Flash, reducing physical and Force ability hit chance by 15%, and Weakened, reducing Attack by 12%, for 15 seconds. SOC scaling can raise these penalties to 18% and 15%. This command applies reliably to valid nearby enemies.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.BreakMorale2);
        }

        private void CommandPresence()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CommandRadius)
                .Name("Command Presence")

                .AddPerkLevel()
                .Description("All Leadership auras and area commands have +2m range. Non-capstone Leadership command buffs last 2 seconds longer. SOC scaling can raise the duration bonus to +3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 30)
                .IncreasesStat(StatType.LeadershipCommandRadiusBonusMeters, 2)
                .IncreasesStat(StatType.LeadershipCommandDurationBonusBaseSeconds, 2)
                .IncreasesStat(StatType.LeadershipCommandDurationBonusMaximumSeconds, 3)

                .AddPerkLevel()
                .Description("All Leadership auras and area commands have +4m range. Non-capstone Leadership command buffs last 4 seconds longer. SOC scaling can raise the duration bonus to +5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 48)
                .IncreasesStat(StatType.LeadershipCommandRadiusBonusMeters, 4)
                .IncreasesStat(StatType.LeadershipCommandDurationBonusBaseSeconds, 4)
                .IncreasesStat(StatType.LeadershipCommandDurationBonusMaximumSeconds, 5);
        }

        private void DecisiveCommand()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.DecisiveCommand)
                .Name("Decisive Command")

                .AddPerkLevel()
                .Description("For 20 seconds, nearby party members gain +24% damage, +10% physical and Force ability hit chance, +10% critical hit chance, and restore 1 STM every 3 seconds. SOC scaling can raise the bonuses to +30%, +12%, and +12%.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.DecisiveCommand1);
        }

    }
}
