using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class AgricultureCraftingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefineCraftingPerks();
            return _builder.Build();
        }

        private void DefineCraftingPerks()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.FlavorLayering)
                .Name("Flavor Layering")
                .Icon("icr_flavor")
                .Description("Applies to Agriculture recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 5)
                .Description("A successful Fine touch that increases quality adds 10% of Basic Synthesis's unconditioned progress. Up to 3 triggers; may finish the item.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingFineTouchProgressPercent, 10)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 20)
                .Description("A successful Fine touch that increases quality adds 20% of Basic Synthesis's unconditioned progress. Up to 3 triggers; may finish the item.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingFineTouchProgressPercent, 20);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.PatientPreparation)
                .Name("Patient Preparation")
                .Icon("icr_patient")
                .Description("Applies to Agriculture recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 15)
                .Description("A Fine Master's Mend that restores at least 10 durability preserves Fine for the next action. 1 use(s) per craft.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingFineMendPreservationUses, 1)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 35)
                .Description("A Fine Master's Mend that restores at least 10 durability preserves Fine for the next action. 2 use(s) per craft.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingFineMendPreservationUses, 2);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.EfficientPreparation)
                .Name("Efficient Preparation")
                .Icon("icr_efficient")
                .Description("Applies to Agriculture recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 30)
                .Description("Successful Careful Synthesis discounts the next Basic Touch by 1 CP within 2 accepted actions, minimum base cost 1. Up to 2 triggers.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingPreparedTouchCPReduction, 1)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 45)
                .Description("Successful Careful Synthesis discounts the next Basic Touch by 2 CP within 2 accepted actions, minimum base cost 1. Up to 2 triggers.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingPreparedTouchCPReduction, 2);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.PerfectTiming)
                .Name("Perfect Timing")
                .Icon("icr_timing")
                .Description("Applies to Agriculture recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(5)
                .RequirementSkill(SkillType.Agriculture, 50)
                .RequirementAnyCompletedLine(PerkType.FlavorLayering, PerkType.PatientPreparation, PerkType.EfficientPreparation)
                .Description("Once per craft, a successful Fine touch that reaches maximum quality adds one Basic Synthesis's unconditioned progress. May finish the item.")
                .IncreasesCraftingStat(SkillType.Agriculture, StatType.CraftingMaximumQualityProgress, 1);

        }
    }
}
