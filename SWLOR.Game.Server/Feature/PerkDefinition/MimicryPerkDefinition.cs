using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class MimicryPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CombatAnalyzer();
            AnalyzerMemory();
            PatternRecognition();

            return _builder.Build();
        }


        private void CombatAnalyzer()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.CombatAnalyzer)
                .Name("Combat Analyzer")
                .TriggerRefund(player => Mimicry.UnequipAllTechniques(player))

                .AddPerkLevel()
                .GrantsFeat(FeatType.CombatAnalyzerTrait)
                .Description("Grants a combat analyzer capable of recording enemy creature techniques. Unlocks technique learning and the Techniques window. Provides 2 technique slots and lets you replicate tier 1 techniques.")
                .Price(2)
                .RequirementSkill(SkillType.Mimicry, 0)

                .AddPerkLevel()
                .Description("Upgrades the combat analyzer, improving equipped technique potency and letting you replicate tier 2 techniques.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 15)
                .IncreasesStat(StatType.MimicryPotencyPercent, 5)

                .AddPerkLevel()
                .Description("Further upgrades the combat analyzer, improving equipped technique potency and letting you replicate tier 3 techniques.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 30)
                .IncreasesStat(StatType.MimicryPotencyPercent, 10)

                .AddPerkLevel()
                .Description("Maximizes the combat analyzer, improving equipped technique potency and letting you replicate tier 4 techniques.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 45)
                .IncreasesStat(StatType.MimicryPotencyPercent, 15);
        }


        private void AnalyzerMemory()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.AnalyzerMemory)
                .Name("Analyzer Memory")
                .TriggerRefund(player => Mimicry.EnforceSlotBudget(player))

                .AddPerkLevel()
                .GrantsFeat(FeatType.AnalyzerMemoryTrait)
                .Description("Expands your combat analyzer's memory, granting 1 additional technique slot.")
                .Price(2)
                .RequirementSkill(SkillType.Mimicry, 10)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Expands your combat analyzer's memory further, granting 2 additional technique slots in total.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 25)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Maximizes your combat analyzer's memory, granting 3 additional technique slots in total.")
                .Price(4)
                .RequirementSkill(SkillType.Mimicry, 40)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer);
        }


        private void PatternRecognition()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.PatternRecognition)
                .Name("Pattern Recognition")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PatternRecognitionTrait)
                .Description("Sharpens your ability to recognize enemy combat patterns, improving your technique learn chance by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Mimicry, 10)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Further sharpens your ability to recognize enemy combat patterns, improving your technique learn chance by 20% in total.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 30)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer);
        }

    }
}
