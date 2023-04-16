using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("elec_ruined", 2);

            // Titan Pistol
            _builder.Create(RecipeType.TitanPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("tit_pistol")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("elec_flawed", 2);

            // Sith Pistol
            _builder.Create(RecipeType.SithPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("sith_pistol")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3);

            // Delta Pistol
            _builder.Create(RecipeType.DeltaPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("del_pistol")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("elec_good", 2);

            // Proto Pistol
            _builder.Create(RecipeType.ProtoPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("proto_pistol")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 2);

            // Ophidian Pistol
            _builder.Create(RecipeType.OphidianPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("oph_pistol")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 4)
                .Component("elec_high", 2);

            // Chiro Pistol
            _builder.Create(RecipeType.ChiroPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("chi_pistol")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1);

            // Titan Shuriken
            _builder.Create(RecipeType.TitanShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("tit_shuriken")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Sith Shuriken
            _builder.Create(RecipeType.SithShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("sith_shuriken")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Shuriken
            _builder.Create(RecipeType.DeltaShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("del_shuriken")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Proto Shuriken
            _builder.Create(RecipeType.ProtoShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("proto_shuriken")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Ophidian Shuriken
            _builder.Create(RecipeType.OphidianShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("oph_shuriken")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Chiro Shuriken
            _builder.Create(RecipeType.ChiroShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("chi_shuriken")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("elec_ruined", 3);

            // Titan Rifle
            _builder.Create(RecipeType.TitanRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("tit_rifle")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3);

            // Sith Rifle
            _builder.Create(RecipeType.SithRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("sith_rifle")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2);

            // Delta Rifle
            _builder.Create(RecipeType.DeltaRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("del_rifle")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Proto Rifle
            _builder.Create(RecipeType.ProtoRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("proto_rifle")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("elec_imperfect", 3);

            // Ophidian Rifle
            _builder.Create(RecipeType.OphidianRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("oph_rifle")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 3);

            // Chiro Rifle
            _builder.Create(RecipeType.ChiroRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("chi_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Bol Rifle
            _builder.Create(RecipeType.BolRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("bol_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("bol_leather", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }
    }
}