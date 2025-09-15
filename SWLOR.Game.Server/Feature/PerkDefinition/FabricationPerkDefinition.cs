using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FabricationPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Synthesis();
            Touch();
            Abilities();
            FurnitureBlueprints();
            StructureBlueprints();
            FabricationEquipment();
            Research();
            ScientificNetworking();
            ResearchProjects();

            return _builder.Build();
        }

        private void Synthesis()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.RapidSynthesisFabrication)
                .Name("Rapid Synthesis (Fabrication)")

                .AddPerkLevel()
                .Description("Increases progress by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10);


            _builder.Create(PerkCategoryType.Fabrication, PerkType.CarefulSynthesisFabrication)
                .Name("Careful Synthesis (Fabrication)")

                .AddPerkLevel()
                .Description("Increases progress by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 30);
        }

        private void Touch()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.BasicTouchFabrication)
                .Name("Basic Touch (Fabrication)")

                .AddPerkLevel()
                .Description("Increases quality by 10. (90% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 5);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.StandardTouchFabrication)
                .Name("Standard Touch (Fabrication)")

                .AddPerkLevel()
                .Description("Increases quality by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 15);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.PreciseTouchFabrication)
                .Name("Precise Touch (Fabrication)")

                .AddPerkLevel()
                .Description("Increases quality by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 35);
        }

        private void Abilities()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.MastersMendFabrication)
                .Name("Master's Mend (Fabrication)")

                .AddPerkLevel()
                .Description("Restores item durability by 30.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.SteadyHandFabrication)
                .Name("Steady Hand (Fabrication)")

                .AddPerkLevel()
                .Description("Increases success rate of next synthesis ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 20);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.MuscleMemoryFabrication)
                .Name("Muscle Memory (Fabrication)")

                .AddPerkLevel()
                .Description("Increases success rate of next touch ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 40);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.VenerationFabrication)
                .Name("Veneration (Fabrication)")

                .AddPerkLevel()
                .Description("Reduces CP cost of Synthesis abilitites by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 25);

            _builder.Create(PerkCategoryType.Fabrication, PerkType.WasteNotFabrication)
                .Name("Waste Not (Fabrication)")

                .AddPerkLevel()
                .Description("Reduces loss of durability by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 8);
        }

        private void FurnitureBlueprints()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FurnitureBlueprints)
                .Name("Furniture Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Furniture blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.FurnitureBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Furniture blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)
                .GrantsFeat(FeatType.FurnitureBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Furniture blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)
                .GrantsFeat(FeatType.FurnitureBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Furniture blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)
                .GrantsFeat(FeatType.FurnitureBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Furniture blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40)
                .GrantsFeat(FeatType.FurnitureBlueprints5);
        }

        private void StructureBlueprints()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.StructureBlueprints)
                .Name("Structure Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Structure blueprints.")
                .Price(6)
                .RequirementSkill(SkillType.Fabrication, 20)
                .GrantsFeat(FeatType.StructureBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Structure blueprints.")
                .Price(6)
                .RequirementSkill(SkillType.Fabrication, 40)
                .GrantsFeat(FeatType.StructureBlueprints2);
        }

        private void FabricationEquipment()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FabricationEquipment)
                .Name("Fabrication Equipment")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Fabrication equipment.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 5)
                .GrantsFeat(FeatType.FabricationEquipment1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Fabrication equipment.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 15)
                .GrantsFeat(FeatType.FabricationEquipment2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Fabrication equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 25)
                .GrantsFeat(FeatType.FabricationEquipment3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Fabrication equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 35)
                .GrantsFeat(FeatType.FabricationEquipment4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Fabrication equipment.")
                .Price(5)
                .RequirementSkill(SkillType.Fabrication, 45)
                .GrantsFeat(FeatType.FabricationEquipment5);
        }

        private void Research()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.Research)
                .Name("Research")

                .AddPerkLevel()
                .Description("Grants ability to research tier 1 blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants ability to research tier 2 blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants ability to research tier 3 blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants ability to research tier 4 blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40)

                .AddPerkLevel()
                .Description("Grants ability to research tier 5 blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 50);
        }

        private void ScientificNetworking()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.ScientificNetworking)
                .Name("Scientific Networking")

                .AddPerkLevel()
                .Description("Blueprints are created with an additional licensed run.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 25)

                .AddPerkLevel()
                .Description("Blueprints are created with an additional licensed run.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 50);
        }

        private void ResearchProjects()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.ResearchProjects)
                .Name("Research Projects")

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent research jobs by 1, for a total of 2.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 25)

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent research jobs by 1, for a total of 3.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 50);
        }
    }
}
