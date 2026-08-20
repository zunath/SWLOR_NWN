using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

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
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.RallyingStandard)
                .Name("Rallying Standard")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +3% physical and Force ability hit chance. SOC scaling can raise this to +4%.")
                .Price(2)
                .IncreasesStat(StatType.RallyingStandardAuraLevel, 1)
                .GrantsFeat(FeatType.RallyingStandard1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +5% physical and Force ability hit chance. SOC scaling can raise this to +6%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .IncreasesStat(StatType.RallyingStandardAuraLevel, 2)
                .GrantsFeat(FeatType.RallyingStandard2);
        }

        private void PressTheAttack()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.PressTheAttack)
                .Name("Press the Attack")

                .AddPerkLevel()
                .Description("Party members within Leadership range (5m base) deal +6% damage for 30 seconds. SOC scaling can raise this to +8%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.PressTheAttack1)

                .AddPerkLevel()
                .Description("Party members within Leadership range (5m base) deal +8% damage for 30 seconds. SOC scaling can raise this to +10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.PressTheAttack2)

                .AddPerkLevel()
                .Description("Party members within Leadership range (5m base) gain +10% damage and +5% physical and Force ability hit chance for 30 seconds. SOC scaling can raise these to +12% damage and +7% hit chance.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.PressTheAttack3);
        }

        private void CoordinatedFocus()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +3% critical hit chance. SOC scaling can raise this to +4%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .IncreasesStat(StatType.CoordinatedFocusAuraLevel, 1)
                .GrantsFeat(FeatType.CoordinatedFocus1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +4% critical hit chance and +5% critical damage. SOC scaling can raise these to +5% and +7%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .IncreasesStat(StatType.CoordinatedFocusAuraLevel, 2)
                .GrantsFeat(FeatType.CoordinatedFocus2)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +6% critical hit chance and +8% critical damage. SOC scaling can raise these to +7% and +10%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .IncreasesStat(StatType.CoordinatedFocusAuraLevel, 3)
                .GrantsFeat(FeatType.CoordinatedFocus3);
        }

        private void MarkTarget()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.MarkTarget)
                .Name("Mark Target")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MarkTargetTrait)
                .Description("When a Vanguard Command offensive command affects an enemy, party members within Leadership range (5m base) gain +8% damage for 30 seconds. SOC scaling can raise this to +10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .IncreasesStat(StatType.LeadershipVanguardMarkTargetRank, 1)

                .AddPerkLevel()
                .Description("When a Vanguard Command offensive command affects an enemy, party members within Leadership range (5m base) gain +12% damage and +10% Ability Accuracy for 30 seconds. SOC scaling can raise these to +15% damage and +12% Ability Accuracy.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .IncreasesStat(StatType.LeadershipVanguardMarkTargetRank, 2);
        }

        private void ChargeOrder()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.ChargeOrder)
                .Name("Charge Order")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +10% movement speed and +30 Mobility Resistance. SOC scaling can raise these to +12% movement speed and +40 Mobility Resistance.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .IncreasesStat(StatType.ChargeOrderAuraLevel, 1)
                .GrantsFeat(FeatType.ChargeOrder1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +15% movement speed and +50 Mobility Resistance. SOC scaling can raise these to +18% movement speed and +65 Mobility Resistance.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .IncreasesStat(StatType.ChargeOrderAuraLevel, 2)
                .GrantsFeat(FeatType.ChargeOrder2);
        }

        private void BreakMorale()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.BreakMorale)
                .Name("Break Morale")

                .AddPerkLevel()
                .Description("Enemies within Leadership range (5m base) suffer Flash for 30 seconds, reducing physical and Force ability hit chance by 10%. SOC scaling can raise the penalty to 12%. This command applies reliably to valid enemies within Leadership range (5m base).")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.BreakMorale1)

                .AddPerkLevel()
                .Description("Enemies within Leadership range (5m base) suffer Flash, reducing physical and Force ability hit chance by 15%, and Weakened, reducing Attack by 12%, for 30 seconds. SOC scaling can raise these penalties to 18% and 15%. This command applies reliably to valid enemies within Leadership range (5m base).")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.BreakMorale2);
        }

        private void CommandPresence()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.CommandRadius)
                .Name("Command Presence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandRadiusTrait)
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
                .IncreasesStat(StatType.LeadershipCommandDurationBonusMaximumSeconds, 5)
                .TriggerPurchase(AbilityTargeting.RefreshClientTargeting)
                .TriggerRefund(AbilityTargeting.RefreshClientTargeting);
        }

        private void DecisiveCommand()
        {
            _builder.Create(PerkCategoryType.LeadershipVanguardCommand, PerkType.DecisiveCommand)
                .Name("Decisive Command")

                .AddPerkLevel()
                .Description("For 45 seconds, party members within Leadership range (5m base), including you, gain +12% damage, +6% physical and Force ability hit chance, +6% critical hit chance, and restore 1 STM every 3 seconds. SOC scaling can raise the bonuses to +15%, +8%, and +8%.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.DecisiveCommand1)
                .RequirementQuest(LeadershipCapstoneQuestDefinition.DecisiveCommandMasteryQuestId);
        }

    }
}
