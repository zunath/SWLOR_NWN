using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class RangedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Pistols();
            Shurikens();
            Rifles();

            return _builder.Build();
        }

        private void Pistols()
        {
            // Basic Pistol
            _builder.Create(RecipeType.BasicPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("b_pistol")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 7)
                .Component("elec_ruined", 4);

            // Titan Pistol
            _builder.Create(RecipeType.TitanPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("tit_pistol")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 7)
                .Component("elec_flawed", 4);

            // Delta Pistol
            _builder.Create(RecipeType.DeltaPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("del_pistol")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 7)
                .Component("elec_good", 4);

            // Proto Pistol
            _builder.Create(RecipeType.ProtoPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("proto_pistol")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 7)
                .Component("elec_imperfect", 4);

            // Ophidian Pistol
            _builder.Create(RecipeType.OphidianPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("oph_pistol")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 7)
                .Component("elec_high", 4);
        }

        private void Shurikens()
        {
            // Basic Shuriken
            _builder.Create(RecipeType.BasicShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("b_shuriken")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Shuriken
            _builder.Create(RecipeType.TitanShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("tit_shuriken")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Shuriken
            _builder.Create(RecipeType.DeltaShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("del_shuriken")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Shuriken
            _builder.Create(RecipeType.ProtoShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("proto_shuriken")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Shuriken
            _builder.Create(RecipeType.OphidianShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("oph_shuriken")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);
        }

        private void Rifles()
        {
            // Basic Rifle
            _builder.Create(RecipeType.BasicRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("b_rifle")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 10)
                .Component("elec_ruined", 5);

            // Titan Rifle
            _builder.Create(RecipeType.TitanRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("tit_rifle")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 10)
                .Component("elec_flawed", 5);

            // Delta Rifle
            _builder.Create(RecipeType.DeltaRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("del_rifle")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 10)
                .Component("elec_good", 5);

            // Proto Rifle
            _builder.Create(RecipeType.ProtoRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("proto_rifle")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 10)
                .Component("elec_imperfect", 5);

            // Ophidian Rifle
            _builder.Create(RecipeType.OphidianRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("oph_rifle")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 5);
        }
    }
}