using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class EspionageCraftingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefineCraftingPerks();
            return _builder.Build();
        }

        private void DefineCraftingPerks()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.ControlledMixing)
                .Name("Controlled Mixing")
                .Icon("icr_mixing")
                .Description("Applies to Espionage recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("On poison mixing recipes, a successful Fine touch restores 2 durability, no more than was spent. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingPoisonTouchDurabilityRestore, 2)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("On poison mixing recipes, a successful Fine touch restores 4 durability, no more than was spent. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingPoisonTouchDurabilityRestore, 4);

            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.TriggerTuning)
                .Name("Trigger Tuning")
                .Icon("icr_tuning")
                .Description("Applies to Espionage recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("On trap assembly recipes, successful Careful Synthesis reduces the next Rapid Synthesis durability cost by 3 within 2 accepted actions. Up to 2 triggers.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingPreparedRapidDurabilityReduction, 3)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("On trap assembly recipes, successful Careful Synthesis reduces the next Rapid Synthesis durability cost by 5 within 2 accepted actions. Up to 2 triggers.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingPreparedRapidDurabilityReduction, 5);

            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.CleanAssembly)
                .Name("Clean Assembly")
                .Icon("icr_clean")
                .Description("Applies to Espionage recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("Successful paid synthesis during Economical adds 10% of Basic Touch's unconditioned quality. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingEconomicalSynthesisQualityPercent, 10)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .Description("Successful paid synthesis during Economical adds 20% of Basic Touch's unconditioned quality. Up to 3 triggers per craft.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingEconomicalSynthesisQualityPercent, 20);

            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.ContingencyPlanning)
                .Name("Contingency Planning")
                .Icon("icr_contingency")
                .Description("Applies to Espionage recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(5)
                .RequirementSkill(SkillType.Espionage, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .RequirementAnyCompletedLine(PerkType.ControlledMixing, PerkType.TriggerTuning, PerkType.CleanAssembly)
                .Description("Once per craft, the first failed Rapid Synthesis replaces the next conditions with Reinforced, then Economical, if the craft remains active.")
                .IncreasesCraftingStat(SkillType.Espionage, StatType.CraftingFailedSynthesisConditionRecovery, 1);

        }
    }
}
