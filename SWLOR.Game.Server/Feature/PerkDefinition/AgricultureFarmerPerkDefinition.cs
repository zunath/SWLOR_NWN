using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Farmer line of Agriculture: ten purchases at Agriculture 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP, the
    /// schedule of the Chef and Angler lines. Unlike them it has no style choice and no capstone: three perks whose levels are
    /// bought as the rank allows. <see cref="PerkType.CropManagement"/> opens a crop tier at 5, 15, 25, 35 and 45 (the crop levels),
    /// <see cref="PerkType.BountifulHarvest"/> adds items to a harvest at 10, 30 and 50, and <see cref="PerkType.SeedSaver"/> gives
    /// a chance at the seed back at 20 and 40. Nothing here is a stat: the farming service reads the ranks the planter holds when
    /// they plant and when they harvest (<c>FarmingRules</c>).
    /// </summary>
    public class AgricultureFarmerPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CropManagement();
            BountifulHarvest();
            SeedSaver();

            return _builder.Build();
        }

        private void CropManagement()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.CropManagement)
                .Name("Crop Management")
                .Icon("ife_garlic_jeste")

                .AddPerkLevel()
                .Description("You can plant tier 1 crops in a garden plot and raise tier 1 livestock in a corral.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 5)

                .AddPerkLevel()
                .Description("You can plant crops and raise livestock up to tier 2.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 15)

                .AddPerkLevel()
                .Description("You can plant crops and raise livestock up to tier 3.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)

                .AddPerkLevel()
                .Description("You can plant crops and raise livestock up to tier 4.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 35)

                .AddPerkLevel()
                .Description("You can plant crops and raise livestock up to tier 5.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 45);
        }

        private void BountifulHarvest()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.BountifulHarvest)
                .Name("Bountiful Harvest")
                .Icon("ife_tomato_king")

                .AddPerkLevel()
                .Description("Each crop you harvest and each animal you butcher yields 1 more item.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10)

                .AddPerkLevel()
                .Description("Each crop you harvest and each animal you butcher yields 2 more items.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 30)

                .AddPerkLevel()
                .Description("Each crop you harvest and each animal you butcher yields 3 more items.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 50);
        }

        private void SeedSaver()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SeedSaver)
                .Name("Seed Saver")
                .Icon("ife_morbol_seedl")

                .AddPerkLevel()
                .Description("A crop you harvest has a 15% chance to also return its seed.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)

                .AddPerkLevel()
                .Description("A crop you harvest has a 30% chance to also return its seed.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 40);
        }
    }
}
