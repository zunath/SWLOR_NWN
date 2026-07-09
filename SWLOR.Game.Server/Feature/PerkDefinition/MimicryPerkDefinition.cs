using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
            TechniquePotency();
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
                .Description("Grants a combat analyzer capable of recording enemy creature techniques. Unlocks technique learning and the Techniques window. Provides 2 technique slots.")
                .Price(2)
                .RequirementSkill(SkillType.Mimicry, 0);
        }


        private void TechniquePotency()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.TechniquePotency)
                .Name("Technique Potency")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TechniquePotencyTrait)
                .Description("Improves the potency of your equipped techniques. (Rank 1)")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 5)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Improves the potency of your equipped techniques. (Rank 2)")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 15)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Improves the potency of your equipped techniques. (Rank 3)")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 25)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Improves the potency of your equipped techniques. (Rank 4)")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 35)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer);
        }


        private void AnalyzerMemory()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.AnalyzerMemory)
                .Name("Analyzer Memory")
                .TriggerRefund(player => Mimicry.EnforceSlotBudget(player))

                .AddPerkLevel()
                .GrantsFeat(FeatType.AnalyzerMemoryTrait)
                .Description("Expands your combat analyzer's memory, granting 2 additional technique slots.")
                .Price(2)
                .RequirementSkill(SkillType.Mimicry, 10)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Expands your combat analyzer's memory further, granting 4 additional technique slots in total.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 25)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer)

                .AddPerkLevel()
                .Description("Maximizes your combat analyzer's memory, granting 6 additional technique slots in total.")
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
