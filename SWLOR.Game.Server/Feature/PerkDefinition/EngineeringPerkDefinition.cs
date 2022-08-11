using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class EngineeringPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Synthesis();
            Touch();
            Abilities();
            StarshipBlueprints();
            EngineeringEquipment();
            EnhancementBlueprints();
            DroidEquipmentBlueprints();
            
            return _builder.Build();
        }

        private void Synthesis()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.RapidSynthesisEngineering)
                .Name("Rapid Synthesis (Engineering)")

                .AddPerkLevel()
                .Description("Increases progress by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10);


            _builder.Create(PerkCategoryType.Engineering, PerkType.CarefulSynthesisEngineering)
                .Name("Careful Synthesis (Engineering)")

                .AddPerkLevel()
                .Description("Increases progress by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 30);
        }

        private void Touch()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.BasicTouchEngineering)
                .Name("Basic Touch (Engineering)")

                .AddPerkLevel()
                .Description("Increases quality by 10. (90% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 5);

            _builder.Create(PerkCategoryType.Engineering, PerkType.StandardTouchEngineering)
                .Name("Standard Touch (Engineering)")

                .AddPerkLevel()
                .Description("Increases quality by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 15);

            _builder.Create(PerkCategoryType.Engineering, PerkType.PreciseTouchEngineering)
                .Name("Precise Touch (Engineering)")

                .AddPerkLevel()
                .Description("Increases quality by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 35);
        }

        private void Abilities()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.MastersMendEngineering)
                .Name("Master's Mend (Engineering)")

                .AddPerkLevel()
                .Description("Restores item durability by 30.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10);

            _builder.Create(PerkCategoryType.Engineering, PerkType.SteadyHandEngineering)
                .Name("Steady Hand (Engineering)")

                .AddPerkLevel()
                .Description("Increases success rate of next synthesis ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 20);

            _builder.Create(PerkCategoryType.Engineering, PerkType.MuscleMemoryEngineering)
                .Name("Muscle Memory (Engineering)")

                .AddPerkLevel()
                .Description("Increases success rate of next touch ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 40);

            _builder.Create(PerkCategoryType.Engineering, PerkType.VenerationEngineering)
                .Name("Veneration (Engineering)")

                .AddPerkLevel()
                .Description("Reduces CP cost of Synthesis abilitites by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 25);

            _builder.Create(PerkCategoryType.Engineering, PerkType.WasteNotEngineering)
                .Name("Waste Not (Engineering)")

                .AddPerkLevel()
                .Description("Reduces loss of durability by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 8);
        }

        private void StarshipBlueprints()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.StarshipBlueprints)
                .Name("Starship Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Ship and Module blueprints.")
                .Price(2)
                .GrantsFeat(FeatType.StarshipBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Ship and Module blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 10)
                .GrantsFeat(FeatType.StarshipBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Ship and Module blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20)
                .GrantsFeat(FeatType.StarshipBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Ship and Module blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)
                .GrantsFeat(FeatType.StarshipBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Ship and Module blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 40)
                .GrantsFeat(FeatType.StarshipBlueprints5);
        }


        private void EngineeringEquipment()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.EngineeringEquipment)
                .Name("Engineering Equipment")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Engineering equipment.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 5)
                .GrantsFeat(FeatType.EngineeringEquipment1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Engineering equipment.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 15)
                .GrantsFeat(FeatType.EngineeringEquipment2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Engineering equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 25)
                .GrantsFeat(FeatType.EngineeringEquipment3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Engineering equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 35)
                .GrantsFeat(FeatType.EngineeringEquipment4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Engineering equipment.")
                .Price(5)
                .RequirementSkill(SkillType.Engineering, 45)
                .GrantsFeat(FeatType.EngineeringEquipment5);
        }


        private void EnhancementBlueprints()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.EnhancementBlueprints)
                .Name("Enhancement Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Enhancement blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.EnhancementBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Enhancement blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10)
                .GrantsFeat(FeatType.EnhancementBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Enhancement blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20)
                .GrantsFeat(FeatType.EnhancementBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Enhancement blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)
                .GrantsFeat(FeatType.EnhancementBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Enhancement blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 40)
                .GrantsFeat(FeatType.EnhancementBlueprints5);
        }

        private void DroidEquipmentBlueprints()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.DroidEquipmentBlueprints)
                .Name("Droid Equipment Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Droid equipment blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.DroidEquipmentBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Droid equipment blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10)
                .GrantsFeat(FeatType.DroidEquipmentBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Droid equipment blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20)
                .GrantsFeat(FeatType.DroidEquipmentBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Droid equipment blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)
                .GrantsFeat(FeatType.DroidEquipmentBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Droid equipment blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 40)
                .GrantsFeat(FeatType.DroidEquipmentBlueprints5);
        }
    }
}
