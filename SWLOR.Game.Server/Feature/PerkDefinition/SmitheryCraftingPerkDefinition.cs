using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class SmitheryCraftingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefineCraftingPerks();
            return _builder.Build();
        }

        private void DefineCraftingPerks()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.TemperedStrikes)
                .Name("Tempered Strikes")
                .Icon("icr_temper")
                .Description("Applies to Smithery recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 5)
                .Description("Successful Workable synthesis restores 2 durability, no more than was spent. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingWorkDurabilityRestore, 2)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 20)
                .Description("Successful Workable synthesis restores 3 durability, no more than was spent. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingWorkDurabilityRestore, 3);

            _builder.Create(PerkCategoryType.Smithery, PerkType.ResilientWork)
                .Name("Resilient Work")
                .Icon("icr_resilient")
                .Description("Applies to Smithery recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 15)
                .Description("The first failed Rapid Synthesis grants 25% of its previewed progress. Costs are spent and the work chain still breaks.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingFailedSynthesisProgressPercent, 25)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 35)
                .Description("The first failed Rapid Synthesis grants 40% of its previewed progress. Costs are spent and the work chain still breaks.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingFailedSynthesisProgressPercent, 40);

            _builder.Create(PerkCategoryType.Smithery, PerkType.FinishingWork)
                .Name("Finishing Work")
                .Icon("icr_finish")
                .Description("Applies to Smithery recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 25)
                .Description("A Reinforced touch after successful synthesis gains 15% more quality. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingChainedTouchQualityPercent, 15)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 45)
                .Description("A Reinforced touch after successful synthesis gains 25% more quality. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingChainedTouchQualityPercent, 25);

            _builder.Create(PerkCategoryType.Smithery, PerkType.MasterSmith)
                .Name("Master Smith")
                .Icon("icr_mstsmith")
                .Description("Applies to Smithery recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(5)
                .RequirementSkill(SkillType.Smithery, 50)
                .RequirementAnyCompletedLine(PerkType.TemperedStrikes, PerkType.ResilientWork, PerkType.FinishingWork)
                .Description("Once per craft, successful Workable synthesis grants +30% quality to the next attempted touch within 2 accepted actions.")
                .IncreasesCraftingStat(SkillType.Smithery, StatType.CraftingWorkTouchOpportunityPercent, 30);

        }
    }
}
