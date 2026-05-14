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
                .Description("Nearby party members take 2% less physical and Force damage. SOC scaling can raise this to 3%.")
                .Price(2)
                .GrantsFeat(FeatType.WatchfulPresence1)

                .AddPerkLevel()
                .Description("Nearby party members take 3% less physical and Force damage. SOC scaling can raise this to 4%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 22)
                .GrantsFeat(FeatType.WatchfulPresence2)

                .AddPerkLevel()
                .Description("Nearby party members take 5% less physical and Force damage. SOC scaling can raise this to 6%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 45)
                .GrantsFeat(FeatType.WatchfulPresence3);
        }

        private void RousingShout()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.RousingShout)
                .Name("Rousing Shout")

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 4% of maximum HP for 10 seconds. SOC scaling can raise this to 6%. If the target is at or below 35% HP, they also take 6% less damage, scaling up to 8%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.RousingShout1)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 6% of maximum HP for 10 seconds. SOC scaling can raise this to 8%. If the target is at or below 35% HP, they also take 10% less damage, scaling up to 12%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 18)
                .GrantsFeat(FeatType.RousingShout2)

                .AddPerkLevel()
                .Description("Bolsters one living ally, granting temporary HP equal to 8% of maximum HP for 12 seconds. SOC scaling can raise this to 10%. If the target is at or below 35% HP, they also take 14% less damage, scaling up to 16%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 40)
                .GrantsFeat(FeatType.RousingShout3);
        }

        private void SteadyFormation()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.SteadyFormation)
                .Name("Steady Formation")

                .AddPerkLevel()
                .Description("Nearby party members gain +2% evasion chance and +20% Mind and Mobility resistance. SOC scaling can raise these to +3% and +25%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 8)
                .GrantsFeat(FeatType.SteadyFormation1)

                .AddPerkLevel()
                .Description("Nearby party members gain +3% evasion chance and +35% Mind and Mobility resistance. SOC scaling can raise these to +4% and +45%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 28)
                .GrantsFeat(FeatType.SteadyFormation2);
        }

        private void BolsterResolve()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.BolsterResolve)
                .Name("Bolster Resolve")

                .AddPerkLevel()
                .Description("Nearby party members gain temporary HP equal to 4% of maximum HP for 12 seconds. SOC scaling can raise this to 6%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 12)
                .GrantsFeat(FeatType.BolsterResolve1)

                .AddPerkLevel()
                .Description("Nearby party members gain temporary HP equal to 6% of maximum HP and take 8% less damage for 12 seconds. SOC scaling can raise these to 8% temporary HP and 10% damage reduction.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 35)
                .GrantsFeat(FeatType.BolsterResolve2);
        }

        private void FieldRecovery()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.FieldRecovery)
                .Name("Field Recovery")

                .AddPerkLevel()
                .Description("Nearby party members restore 1 STM every 6 seconds. SOC scaling can raise this to 2 STM per tick.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.FieldRecovery1)

                .AddPerkLevel()
                .Description("Nearby party members restore 2 STM every 6 seconds. SOC scaling can raise this to 4 STM per tick.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 38)
                .GrantsFeat(FeatType.FieldRecovery2);
        }

        private void CleanseOrder()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CleanseOrder)
                .Name("Cleanse Order")

                .AddPerkLevel()
                .Description("Removes one standard elemental or trauma ailment from nearby party members and grants temporary HP equal to 3% of maximum HP. SOC scaling can raise temporary HP to 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .GrantsFeat(FeatType.CleanseOrder1)

                .AddPerkLevel()
                .Description("Removes one major negative status effect from any resistance group and grants 10% damage reduction for 8 seconds. SOC scaling can raise this to 12%.")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 42)
                .GrantsFeat(FeatType.CleanseOrder2);
        }

        private void TriageProtocol()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.TriageProtocol)
                .Name("Triage Protocol")

                .AddPerkLevel()
                .Description("Field Steward shouts also grant +5% healing received for 8 seconds. SOC scaling can raise this to +7%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 30)
                .IncreasesStat(StatType.FieldStewardTriageProtocolLevel, 1)

                .AddPerkLevel()
                .Description("Field Steward shouts also grant +8% healing received and last 2 seconds longer. SOC scaling can raise the healing received bonus to +10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 48)
                .IncreasesStat(StatType.FieldStewardTriageProtocolLevel, 2)
                .IncreasesStat(StatType.FieldStewardDurationBonusSeconds, 2);
        }

        private void HoldTheLine()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.HoldTheLine)
                .Name("Hold the Line")

                .AddPerkLevel()
                .Description("For 20 seconds, nearby party members gain temporary HP equal to 10% of maximum HP, take 25% less damage, and become immune to Mind and Mobility effects. SOC scaling can raise these to 12% temporary HP and 30% damage reduction.")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 50)
                .GrantsFeat(FeatType.HoldTheLine1);
        }

    }
}
