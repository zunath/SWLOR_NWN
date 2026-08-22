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
            OverclockedAnalyzer();

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
                .RequirementSkill(SkillType.Mimicry, 0)

                .AddPerkLevel()
                .Description("Upgrades the combat analyzer, increasing equipped technique potency by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 15)
                .IncreasesStat(StatType.MimicryPotencyPercent, 5)

                .AddPerkLevel()
                .Description("Further upgrades the combat analyzer, increasing equipped technique potency by 10% in total.")
                .Price(3)
                .RequirementSkill(SkillType.Mimicry, 30)
                .IncreasesStat(StatType.MimicryPotencyPercent, 10)

                .AddPerkLevel()
                .Description("Maximizes the combat analyzer, increasing equipped technique potency by 15% in total.")
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


        private void OverclockedAnalyzer()
        {
            _builder.Create(PerkCategoryType.Mimicry, PerkType.OverclockedAnalyzer)
                .Name("Overclocked Analyzer")
                .TriggerRefund(player => Mimicry.EnforceSlotBudget(player))

                .AddPerkLevel()
                .GrantsFeat(FeatType.Overload)
                .Description("Overclocks the combat analyzer, granting 2 additional technique slots and an activated ability that briefly boosts your equipped techniques' potency and on-hit effect chance.")
                .Price(6)
                .RequirementSkill(SkillType.Mimicry, 50)
                .RequirementMustHavePerk(PerkType.CombatAnalyzer, 4);
        }

    }
}
