using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class LeadershipFieldStewardPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            WatchfulPresence();
            RousingShout();
            SteadyFormation();
            BolsterResolve();
            FieldRecovery();
            CleanseOrder();
            TriageProtocol();
            HoldTheLine();

            return _builder.Build();
        }

        private void WatchfulPresence()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.WatchfulPresence)
                .Name("Watchful Presence")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) take 4% less physical and Force damage. SOC scaling can raise this to 5%.")
                .Price(2)
                .IncreasesStat(StatType.WatchfulPresenceAuraLevel, 1)
                .GrantsFeat(FeatType.WatchfulPresence1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) take 6% less physical and Force damage. SOC scaling can raise this to 7%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .IncreasesStat(StatType.WatchfulPresenceAuraLevel, 2)
                .GrantsFeat(FeatType.WatchfulPresence2)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) take 8% less physical and Force damage. SOC scaling can raise this to 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .IncreasesStat(StatType.WatchfulPresenceAuraLevel, 3)
                .GrantsFeat(FeatType.WatchfulPresence3);
        }

        private void RousingShout()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.RousingShout)
                .Name("Rousing Shout")

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 10% of maximum HP for 30 seconds. SOC scaling can raise this to 13%. If the target is at or below 35% HP, they also take 10% less physical and Force damage, scaling up to 12%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.RousingShout1)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 15% of maximum HP for 30 seconds. SOC scaling can raise this to 19%. If the target is at or below 35% HP, they also take 15% less physical and Force damage, scaling up to 18%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.RousingShout2)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 20% of maximum HP for 30 seconds. SOC scaling can raise this to 25%. If the target is at or below 35% HP, they also take 20% less physical and Force damage, scaling up to 25%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.RousingShout3);
        }

        private void SteadyFormation()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.SteadyFormation)
                .Name("Steady Formation")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +3% evasion chance, +30 Mind Resistance, and +30 Mobility Resistance. SOC scaling can raise these to +4% evasion chance, +40 Mind Resistance, and +40 Mobility Resistance.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .IncreasesStat(StatType.SteadyFormationAuraLevel, 1)
                .GrantsFeat(FeatType.SteadyFormation1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) gain +5% evasion chance, +50 Mind Resistance, and +50 Mobility Resistance. SOC scaling can raise these to +6% evasion chance, +65 Mind Resistance, and +65 Mobility Resistance.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .IncreasesStat(StatType.SteadyFormationAuraLevel, 2)
                .GrantsFeat(FeatType.SteadyFormation2);
        }

        private void BolsterResolve()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.BolsterResolve)
                .Name("Bolster Resolve")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BolsterResolveTrait)
                .Description("Field Steward recovery commands also grant party members within Leadership range (5m base) temporary HP equal to 8% of maximum HP for 30 seconds. SOC scaling can raise this to 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .IncreasesStat(StatType.LeadershipFieldStewardBolsterResolveRank, 1)

                .AddPerkLevel()
                .Description("Field Steward recovery commands also grant party members within Leadership range (5m base) temporary HP equal to 12% of maximum HP and 12% physical and Force damage reduction for 30 seconds. SOC scaling can raise these to 15% temporary HP and 15% physical and Force damage reduction.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .IncreasesStat(StatType.LeadershipFieldStewardBolsterResolveRank, 2);
        }

        private void FieldRecovery()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.FieldRecovery)
                .Name("Field Recovery")

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) restore 1 STM every 4 seconds. SOC scaling can raise this to 2 STM per tick.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .IncreasesStat(StatType.FieldRecoveryAuraLevel, 1)
                .GrantsFeat(FeatType.FieldRecovery1)

                .AddPerkLevel()
                .Description("Aura: Party members within Leadership range (5m base) restore 2 STM every 4 seconds. SOC scaling can raise this to 3 STM per tick.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .IncreasesStat(StatType.FieldRecoveryAuraLevel, 2)
                .GrantsFeat(FeatType.FieldRecovery2);
        }

        private void CleanseOrder()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.CleanseOrder)
                .Name("Cleanse Order")

                .AddPerkLevel()
                .Description("Removes one standard elemental or trauma ailment from party members within Leadership range (5m base) and grants temporary HP equal to 6% of maximum HP for 30 seconds. SOC scaling can raise temporary HP to 8%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.CleanseOrder1)

                .AddPerkLevel()
                .Description("Removes one major negative status effect from party members within Leadership range (5m base) and grants temporary HP equal to 12% of maximum HP for 30 seconds. SOC scaling can raise temporary HP to 15%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.CleanseOrder2);
        }

        private void TriageProtocol()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.TriageProtocol)
                .Name("Triage Protocol")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TriageProtocolTrait)
                .Description("Non-capstone Field Steward shouts also grant +8% healing received for their duration. SOC scaling can raise this to +10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 30)
                .IncreasesStat(StatType.FieldStewardTriageProtocolLevel, 1)

                .AddPerkLevel()
                .Description("Non-capstone Field Steward shouts also grant +12% healing received and last 3 seconds longer. SOC scaling can raise the healing received bonus to +15%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 48)
                .IncreasesStat(StatType.FieldStewardTriageProtocolLevel, 2)
                .IncreasesStat(StatType.FieldStewardDurationBonusSeconds, 3);
        }

        private void HoldTheLine()
        {
            _builder.Create(PerkCategoryType.LeadershipFieldSteward, PerkType.HoldTheLine)
                .Name("Hold the Line")

                .AddPerkLevel()
                .Description("For 45 seconds, party members within Leadership range (5m base), including you, gain temporary HP equal to 18% of maximum HP, take 18% less damage, and become immune to Mind and Mobility effects. SOC scaling can raise temporary HP and damage reduction to 22%.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.HoldTheLine1)
                .RequirementQuest(LeadershipCapstoneQuestDefinition.HoldTheLineMasteryQuestId);
        }

    }
}
