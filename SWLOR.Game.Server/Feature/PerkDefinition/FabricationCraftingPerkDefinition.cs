using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class FabricationCraftingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefineCraftingPerks();
            return _builder.Build();
        }

        private void DefineCraftingPerks()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.StructuralBracing)
                .Name("Structural Bracing")
                .Icon("icr_bracing")
                .Description("Applies to Fabrication recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 10)
                .Description("The first 2 uses of Master's Mend restore 5 additional durability, capped by maximum durability.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingMendDurabilityBonus, 5)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 20)
                .Description("The first 2 uses of Master's Mend restore 10 additional durability, capped by maximum durability.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingMendDurabilityBonus, 10);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.ReinforcedAssembly)
                .Name("Reinforced Assembly")
                .Icon("icr_reinforce")
                .Description("Applies to Fabrication recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 15)
                .Description("Waste Not protects 1 additional work actions and lasts 1 additional accepted actions.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingWasteNotWindowBonus, 1)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 35)
                .Description("Waste Not protects 2 additional work actions and lasts 2 additional accepted actions.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingWasteNotWindowBonus, 2);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.MeasuredConstruction)
                .Name("Measured Construction")
                .Icon("icr_measured")
                .Description("Applies to Fabrication recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 30)
                .Description("Careful Synthesis costs 1 less CP for its first 3 attempts. Applied before percentage discounts; minimum base cost 1.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingCarefulCPReduction, 1)
                .AddPerkLevel()
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 45)
                .Description("Careful Synthesis costs 2 less CP for its first 3 attempts. Applied before percentage discounts; minimum base cost 1.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingCarefulCPReduction, 2);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.MasterBuilder)
                .Name("Master Builder")
                .Icon("icr_mstbuilder")
                .Description("Applies to Fabrication recipes using material conditions. Rank II replaces rank I.")
                .AddPerkLevel()
                .Price(5)
                .RequirementSkill(SkillType.Fabrication, 50)
                .RequirementAnyCompletedLine(PerkType.StructuralBracing, PerkType.ReinforcedAssembly, PerkType.MeasuredConstruction)
                .Description("Once per craft, work that would fail through durability exhaustion leaves 1 durability. Completion takes priority. Cannot protect an aborted craft.")
                .IncreasesCraftingStat(SkillType.Fabrication, StatType.CraftingDurabilityFailureProtection, 1);

        }
    }
}
