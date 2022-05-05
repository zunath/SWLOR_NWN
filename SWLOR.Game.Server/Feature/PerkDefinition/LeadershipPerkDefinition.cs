using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class LeadershipPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new PerkBuilder();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CityManagement();
            Upkeep();
            GuildRelations();

            return _builder.Build();
        }

        private void CityManagement()
        {
            _builder.Create(PerkCategoryType.Leadership, PerkType.CityManagement)
                .Name("City Management")
                .RefundRequirement((player, perkType, effectivePerkLevel) =>
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
    }
}
