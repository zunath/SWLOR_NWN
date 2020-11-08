using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FabricationPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            FurnitureBlueprints(builder);
            StructureBlueprints(builder);
            StarshipBlueprints(builder);
            TurretBlueprints(builder);
            ReactorBlueprints(builder);
            PlatingBlueprints(builder);
            MiningBlueprints(builder);
            DroidBlueprints(builder);

            return builder.Build();
        }

        private void FurnitureBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.FurnitureBlueprints)
                .Name("Furniture Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Furniture blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Furniture blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Furniture blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Furniture blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Furniture blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void StructureBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.StructureBlueprints)
                .Name("Structure Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Structure blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Structure blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Structure blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Structure blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Structure blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void StarshipBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.StarshipBlueprints)
                .Name("Starship Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Starship blueprints.")
                .Price(5)
                .RequirementSkill(SkillType.Fabrication, 25)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Starship blueprints.")
                .Price(7)
                .RequirementSkill(SkillType.Fabrication, 35)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Starship blueprints.")
                .Price(8)
                .RequirementSkill(SkillType.Fabrication, 45);
        }

        private void TurretBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.TurretBlueprints)
                .Name("Turret Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Turret blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Turret blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Turret blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Turret blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Turret blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void ReactorBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.ReactorBlueprints)
                .Name("Reactor Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Reactor blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Reactor blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Reactor blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Reactor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Reactor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void PlatingBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.PlatingBlueprints)
                .Name("Plating Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Plating blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Plating blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Plating blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Plating blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Plating blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void MiningBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.MiningBlueprints)
                .Name("Mining Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Mining blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Mining blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Mining blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Mining blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Mining blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

        private void DroidBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Fabrication, PerkType.DroidBlueprints)
                .Name("Droid Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Droid blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Droid blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Droid blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Droid blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Droid blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40);
        }

    }
}
