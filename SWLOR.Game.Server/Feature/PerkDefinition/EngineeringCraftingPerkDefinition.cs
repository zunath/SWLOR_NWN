using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class EngineeringCraftingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefineCraftingPerks();
            return _builder.Build();
        }

        private void DefineCraftingPerks()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CircuitEconomy)
                .Name("Circuit Economy")
                .Icon("icr_circuit")
                .Description("Applies to Engineering recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 5)
                .Description("A successful switch between synthesis and touch restores 1 CP when the prior work also succeeded. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingSwitchCPRestore, 1)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 20)
                .Description("A successful switch between synthesis and touch restores 2 CP when the prior work also succeeded. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingSwitchCPRestore, 2);

            _builder.Create(PerkCategoryType.Engineering, PerkType.PreciseCalibration)
                .Name("Precise Calibration")
                .Icon("icr_calibrate")
                .Description("Applies to Engineering recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 35)
                .Description("Precise Touch gains 15% more quality during Fine.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingFinePreciseQualityPercent, 15)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 45)
                .Description("Precise Touch gains 25% more quality during Fine.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingFinePreciseQualityPercent, 25);

            _builder.Create(PerkCategoryType.Engineering, PerkType.DiagnosticPlanning)
                .Name("Diagnostic Planning")
                .Icon("icr_diagnostic")
                .Description("Applies to Engineering recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 25)
                .Description("See 2 upcoming conditions. Reveals the committed forecast without rerolling it.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingForecastLength, 2)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 45)
                .Description("See 3 upcoming conditions. Reveals the committed forecast without rerolling it.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingForecastLength, 3);

            _builder.Create(PerkCategoryType.Engineering, PerkType.MasterEngineer)
                .Name("Master Engineer")
                .Icon("icr_mstengineer")
                .Description("Applies to Engineering recipes using material conditions.")
                .AddPerkLevel()
                .Price(5)
                .RequirementSkill(SkillType.Engineering, 50)
                .RequirementAnyCompletedLine(PerkType.CircuitEconomy, PerkType.PreciseCalibration, PerkType.DiagnosticPlanning)
                .Description("Once per craft, successful paid work during Economical refunds half its effective CP cost, capped at 6 CP.")
                .IncreasesCraftingStat(SkillType.Engineering, StatType.CraftingEconomicalCPRefundPercent, 50);

        }
    }
}
