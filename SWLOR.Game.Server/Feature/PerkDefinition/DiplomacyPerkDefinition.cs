using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class DiplomacyPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new PerkBuilder();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CityManagement();
            Upkeep();
            CitySpecialization();

            return _builder.Build();
        }

        private void CityManagement()
        {
            _builder.Create(PerkCategoryType.Diplomacy, PerkType.CityManagement)
                .Name("City Management")

                .AddPerkLevel()
                .Description("Enables you to become mayor of a city. You can manage cities up to rank 2 (Village).")
                .Price(2)
                .RequirementSkill(SkillType.Diplomacy, 5)
                .GrantsFeat(FeatType.CityManagement1)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 3 (Township).")
                .Price(3)
                .RequirementSkill(SkillType.Diplomacy, 10)
                .GrantsFeat(FeatType.CityManagement2)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 4 (City).")
                .Price(4)
                .RequirementSkill(SkillType.Diplomacy, 15)
                .GrantsFeat(FeatType.CityManagement3)


                .AddPerkLevel()
                .Description("You can manage cities up to rank 5 (Metropolis).")
                .Price(5)
                .RequirementSkill(SkillType.Diplomacy, 20)
                .GrantsFeat(FeatType.CityManagement4);
        }

        private void Upkeep()
        {
            _builder.Create(PerkCategoryType.Diplomacy, PerkType.Upkeep)
                .Name("Upkeep")

                .AddPerkLevel()
                .Description("Weekly maintenance fees are reduced by 5%.")
                .Price(2)
                .RequirementSkill(SkillType.Diplomacy, 10)
                .GrantsFeat(FeatType.Upkeep1)

                .AddPerkLevel()
                .Description("Weekly maintenance fees are reduced by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Diplomacy, 20)
                .GrantsFeat(FeatType.Upkeep2);
        }

        private void CitySpecialization()
        {
            _builder.Create(PerkCategoryType.Diplomacy, PerkType.CitySpecialization)
                .Name("City Specialization")

                .AddPerkLevel()
                .Description("You can select a specialization for your city.")
                .Price(2)
                .RequirementSkill(SkillType.Diplomacy, 10)
                .GrantsFeat(FeatType.CitySpecialization);
        }
    }
}
