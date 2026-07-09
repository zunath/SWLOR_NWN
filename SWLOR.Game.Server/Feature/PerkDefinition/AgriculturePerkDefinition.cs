using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class AgriculturePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Cultivation();
            Horticulture();
            Botany();

            return _builder.Build();
        }

        private void Cultivation()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.Cultivation)
                .Name("Cultivation")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CultivationTrait)
                .Description("Crop harvest yield is increased by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 10)
                .IncreasesStat(StatType.HarvestYieldPercentBonus, 10)

                .AddPerkLevel()
                .Description("Crop harvest yield is increased by 20% total.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)
                .IncreasesStat(StatType.HarvestYieldPercentBonus, 20)

                .AddPerkLevel()
                .Description("Crop harvest yield is increased by 30% total.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 40)
                .IncreasesStat(StatType.HarvestYieldPercentBonus, 30);
        }

        private void Horticulture()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.Horticulture)
                .Name("Horticulture")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HorticultureTrait)
                .Description("Crop growth speed is increased by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 10)
                .IncreasesStat(StatType.CropGrowthSpeedPercentBonus, 10)

                .AddPerkLevel()
                .Description("Crop growth speed is increased by 20% total.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)
                .IncreasesStat(StatType.CropGrowthSpeedPercentBonus, 20)

                .AddPerkLevel()
                .Description("Crop growth speed is increased by 30% total.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 40)
                .IncreasesStat(StatType.CropGrowthSpeedPercentBonus, 30);
        }

        private void Botany()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.Botany)
                .Name("Botany")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BotanyTrait)
                .Description("Pristine harvest chance is increased by 3%.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 10)
                .IncreasesStat(StatType.PristineHarvestChancePercentBonus, 3)

                .AddPerkLevel()
                .Description("Pristine harvest chance is increased by 6% total.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)
                .IncreasesStat(StatType.PristineHarvestChancePercentBonus, 6)

                .AddPerkLevel()
                .Description("Pristine harvest chance is increased by 9% total.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 40)
                .IncreasesStat(StatType.PristineHarvestChancePercentBonus, 9);
        }
    }
}
