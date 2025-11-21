using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class LeadershipPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CityManagement();
            Upkeep();
            GuildRelations();
            ShoutRange();
            RousingShout();
            Dedication();
            SoldiersSpeed();
            SoldiersStrike();
            Charge();
            SoldiersPrecision();
            ShockingShout();
            Rejuvenation();
            FrenziedShout();

            return _builder.Build();
        }

        private void CityManagement()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CityManagement)
                .Name("City Management")
                .RefundRequirement((player) =>
                {
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    // Not a citizen of any property.
                    if (string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) ||
                        dbPlayer.CitizenPropertyId == Guid.Empty.ToString())
                    {
                        return string.Empty;
                    }

                    var dbCity = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);

                    // Player is the owner of a city (aka their mayor)
                    if (dbCity.OwnerPlayerId == dbPlayer.Id)
                    {
                        return "You are the mayor of a city. You cannot refund this perk until you abdicate your position.";
                    }

                    // Player is currently running for election.
                    var dbElection = DB.Search(new DBQuery<Election>()
                        .AddFieldSearch(nameof(Election.PropertyId), dbCity.Id, false))
                        .SingleOrDefault();
                    if (dbElection != null && dbElection.CandidatePlayerIds.Contains(playerId))
                    {
                        return "You are currently running for election. You cannot refund this perk until you withdraw from the race.";
                    }

                    return string.Empty;
                })

                .AddPerkLevel()
                .Description("Enables you to become mayor of a city. You can manage cities up to rank 2 (Village).")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.CityManagement1)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 3 (Township).")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 10)
                .GrantsFeat(FeatType.CityManagement2)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 4 (City).")
                .Price(4)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.CityManagement3)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 5 (Metropolis).")
                .Price(5)
                .RequirementSkill(SkillType.Leadership, 20)
                .GrantsFeat(FeatType.CityManagement4);
        }

        private void Upkeep()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.Upkeep)
                .Name("Upkeep")

                .AddPerkLevel()
                .Description("Weekly maintenance fees are reduced by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 10)
                .GrantsFeat(FeatType.Upkeep1)

                .AddPerkLevel()
                .Description("Weekly maintenance fees are reduced by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 20)
                .GrantsFeat(FeatType.Upkeep2);
        }

        private void GuildRelations()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.GuildRelations)
                .Name("Guild Relations")

                .AddPerkLevel()
                .Description("Improves GP and credit rewards from guild tasks by 5%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)

                .AddPerkLevel()
                .Description("Improves GP and credit rewards from guild tasks by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 10)

                .AddPerkLevel()
                .Description("Improves GP and credit rewards from guild tasks by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 15)

                .AddPerkLevel()
                .Description("Improves GP and credit rewards from guild tasks by 20%.")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 20);
        }

        private void ShoutRange()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.ShoutRange)
                .Name("Shout Range")

                .AddPerkLevel()
                .Description("Increases the range of your shouts to 12.5 meters.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 25)

                .AddPerkLevel()
                .Description("Increases the range of your shouts to 15 meters.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 50)
                
                .TriggerPurchase(Ability.ReapplyPlayerAuraAOE)
                .TriggerRefund(Ability.ReapplyPlayerAuraAOE);
        }

        private void RousingShout()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.RousingShout)
                .Name("Rousing Shout")

                .AddPerkLevel()
                .Description("Revives an unconscious target with 1 HP.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.RousingShout)

                .AddPerkLevel()
                .Description("Revives an unconscious target with (SOC)% HP.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 30)

                .AddPerkLevel()
                .Description("Revives an unconscious target with (2*SOC)% HP.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 45);
        }

        private void Dedication()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.Dedication)
                .Name("Dedication")

                .AddPerkLevel()
                .Description("Improves XP gain of all party members by (10+SOC)%")
                .Price(1)
                .GrantsFeat(FeatType.Dedication)

                .AddPerkLevel()
                .Description("Improves XP gain of all party members by (10+2SOC)%")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 10)

                .AddPerkLevel()
                .Description("Improves XP gain of all party members by (10+3SOC)%")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 30);
        }

        private void SoldiersSpeed()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.SoldiersSpeed)
                .Name("Soldier's Speed")

                .AddPerkLevel()
                .Description("Improves evasion of other nearby party members by SOC/2.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.SoldiersSpeed)

                .AddPerkLevel()
                .Description("Improves evasion of other nearby members by SOC.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 20)

                .AddPerkLevel()
                .Description("Improves evasion of other nearby party members by 1.5*SOC.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 40);
        }

        private void SoldiersStrike()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.SoldiersStrike)
                .Name("Soldier's Strike")

                .AddPerkLevel()
                .Description("Improves Attack of other nearby party members by SOC.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 5)
                .GrantsFeat(FeatType.SoldiersStrike)

                .AddPerkLevel()
                .Description("Improves Attack of other nearby members by SOC*1.5.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 20)

                .AddPerkLevel()
                .Description("Improves Attack of other nearby party members by SOC*2.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 40);
        }

        private void Charge()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.Charge)
                .Name("Charge")

                .AddPerkLevel()
                .Description("Increases the movement speed of all nearby party members by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 10)
                .GrantsFeat(FeatType.Charge)

                .AddPerkLevel()
                .Description("Increases the movement speed of all nearby party members by 30%.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 35);
        }

        private void SoldiersPrecision()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.SoldiersPrecision)
                .Name("Soldier's Precision")

                .AddPerkLevel()
                .Description("Improves Accuracy of other nearby party members by SOC/2.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 10)
                .GrantsFeat(FeatType.SoldiersPrecision)

                .AddPerkLevel()
                .Description("Improves Accuracy of other nearby members by SOC.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 25)

                .AddPerkLevel()
                .Description("Improves Accuracy of other nearby party members by 1.5*SOC.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 45);
        }

        private void ShockingShout()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.ShockingShout)
                .Name("Shocking Shout")

                .AddPerkLevel()
                .Description("Attempts to stun all nearby enemies for 2 seconds with a will check of 12+SOC/2. (Max: 6 targets)")
                .Price(3)
                .RequirementSkill(SkillType.Leadership, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ShockingShout);
        }

        private void Rejuvenation()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.Rejuvenation)
                .Name("Rejuvenation")

                .AddPerkLevel()
                .Description("Grants 1 STM regeneration to other nearby party members every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)
                .GrantsFeat(FeatType.Rejuvenation)

                .AddPerkLevel()
                .Description("Grants 2 STM regeneration to other nearby party members every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 35)

                .AddPerkLevel()
                .Description("Grants 3 STM regeneration to other nearby party members every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 45);
        }

        private void FrenziedShout()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.FrenziedShout)
                .Name("Frenzied Shout")

                .AddPerkLevel()
                .Description("Reduces physical defense of all nearby enemies by SOC.")
                .Price(2)
                .GrantsFeat(FeatType.FrenziedShout)

                .AddPerkLevel()
                .Description("Reduces physical defense of all nearby enemies by SOC*1.5.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 15)

                .AddPerkLevel()
                .Description("Reduces physical defense of all nearby enemies by SOC*2.")
                .Price(2)
                .RequirementSkill(SkillType.Leadership, 35);
        }
    }
}
