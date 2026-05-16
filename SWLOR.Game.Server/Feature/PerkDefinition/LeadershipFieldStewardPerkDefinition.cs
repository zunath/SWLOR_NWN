using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

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
            _builder.Create(PerkCategoryType.Leadership, PerkType.WatchfulPresence)
                .Name("Watchful Presence")

                .AddPerkLevel()
                .Description("Nearby party members take 4% less physical and Force damage. SOC scaling can raise this to 5%.")
                .Price(2)
                .GrantsFeat(FeatType.WatchfulPresence1)

                .AddPerkLevel()
                .Description("Nearby party members take 6% less physical and Force damage. SOC scaling can raise this to 7%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .GrantsFeat(FeatType.WatchfulPresence2)

                .AddPerkLevel()
                .Description("Nearby party members take 8% less physical and Force damage. SOC scaling can raise this to 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .GrantsFeat(FeatType.WatchfulPresence3);
        }

        private void RousingShout()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.RousingShout)
                .Name("Rousing Shout")

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 10% of maximum HP for 12 seconds. SOC scaling can raise this to 13%. If the target is at or below 35% HP, they also take 10% less damage, scaling up to 12%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.RousingShout1)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 15% of maximum HP for 12 seconds. SOC scaling can raise this to 19%. If the target is at or below 35% HP, they also take 15% less damage, scaling up to 18%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.RousingShout2)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 20% of maximum HP for 15 seconds. SOC scaling can raise this to 25%. If the target is at or below 35% HP, they also take 20% less damage, scaling up to 25%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.RousingShout3);
        }

        private void SteadyFormation()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.SteadyFormation)
                .Name("Steady Formation")

                .AddPerkLevel()
                .Description("Nearby party members gain +3% evasion chance and +30 Mind and Mobility resistance. SOC scaling can raise these to +4% and +40.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .GrantsFeat(FeatType.SteadyFormation1)

                .AddPerkLevel()
                .Description("Nearby party members gain +5% evasion chance and +50 Mind and Mobility resistance. SOC scaling can raise these to +6% and +65.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .GrantsFeat(FeatType.SteadyFormation2);
        }

        private void BolsterResolve()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.BolsterResolve)
                .Name("Bolster Resolve")

                .AddPerkLevel()
                .Description("Nearby party members gain temporary HP equal to 8% of maximum HP for 12 seconds. SOC scaling can raise this to 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .GrantsFeat(FeatType.BolsterResolve1)

                .AddPerkLevel()
                .Description("Nearby party members gain temporary HP equal to 12% of maximum HP and take 12% less damage for 15 seconds. SOC scaling can raise these to 15% temporary HP and 15% damage reduction.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .GrantsFeat(FeatType.BolsterResolve2);
        }

        private void FieldRecovery()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.FieldRecovery)
                .Name("Field Recovery")

                .AddPerkLevel()
                .Description("Nearby party members restore 1 STM every 4 seconds. SOC scaling can raise this to 2 STM per tick.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.FieldRecovery1)

                .AddPerkLevel()
                .Description("Nearby party members restore 2 STM every 4 seconds. SOC scaling can raise this to 3 STM per tick.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .GrantsFeat(FeatType.FieldRecovery2);
        }

        private void CleanseOrder()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CleanseOrder)
                .Name("Cleanse Order")

                .AddPerkLevel()
                .Description("Removes one standard elemental or trauma ailment from nearby party members and grants temporary HP equal to 6% of maximum HP for 10 seconds. SOC scaling can raise temporary HP to 8%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.CleanseOrder1)

                .AddPerkLevel()
                .Description("Removes one major negative status effect from any resistance group from nearby party members and grants 12% damage reduction for 10 seconds. SOC scaling can raise this to 15%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.CleanseOrder2);
        }

        private void TriageProtocol()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.TriageProtocol)
                .Name("Triage Protocol")

                .AddPerkLevel()
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
            _builder.Create(PerkCategoryType.Leadership, PerkType.HoldTheLine)
                .Name("Hold the Line")

                .AddPerkLevel()
                .Description("For 20 seconds, nearby party members gain temporary HP equal to 25% of maximum HP, take 30% less damage, and become immune to Mind and Mobility effects. SOC scaling can raise these to 30% temporary HP and 35% damage reduction.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.HoldTheLine1);
        }

    }
}
