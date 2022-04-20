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
                .Price(3)
                .GrantsFeat(FeatType.StarshipBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Ship and Module blueprints.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 10)
                .GrantsFeat(FeatType.StarshipBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Ship and Module blueprints.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 20)
                .GrantsFeat(FeatType.StarshipBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Ship and Module blueprints.")
                .Price(5)
                .RequirementSkill(SkillType.Engineering, 30)
                .GrantsFeat(FeatType.StarshipBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Ship and Module blueprints.")
                .Price(6)
                .RequirementSkill(SkillType.Engineering, 40)
                .GrantsFeat(FeatType.StarshipBlueprints5);
        }
    }
}
