using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes1 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            BasicHeavyVibrobladeGA();
            BasicVibrobladeBA();
            BasicVibrobladeBS();
            BasicFinesseVibrobladeD();
            BasicHeavyVibrobladeGS();
            BasicVibrobladeLS();
            BasicFinesseVibrobladeR();
            BasicVibrobladeK();
            BasicFinesseVibrobladeSS();
            BasicBatonC();
            BasicBatonM();
            BasicBatonMS();
            BasicQuarterstaff();
            BasicFinesseVibrobladeK();
            BasicKnuckles();
            BasicPolearmH();
            BasicPolearmS();
            BasicBlasterPistol();
            BasicBlasterRifle();
            BasicDart();
            BasicShuriken();
            BasicThrowingAxe();

            return _builder.Build();
        }

        private void BasicHeavyVibrobladeGA()
        {
            _builder.Create(RecipeType.BasicHeavyVibrobladeGA, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .Resref("greataxe_b")
                .Component("ref_veldite", 9)
                .Component("wood", 5);
        }

        private void BasicVibrobladeBA()
        {
            _builder.Create(RecipeType.BasicVibrobladeBA, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("battleaxe_b")
                .Component("ref_veldite", 8)
                .Component("wood", 4);
        }

        private void BasicVibrobladeBS()
        {
            _builder.Create(RecipeType.BasicVibrobladeBS, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("bst_sword_b")
                .Component("ref_veldite", 7)
                .Component("wood", 4);
        }

        private void BasicFinesseVibrobladeD()
        {
            _builder.Create(RecipeType.BasicFinesseVibrobladeD, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("dagger_b")
                .Component("ref_veldite", 2)
                .Component("wood", 1);
        }
        private void BasicHeavyVibrobladeGS()
        {
            _builder.Create(RecipeType.BasicHeavyVibrobladeGS, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .Resref("greatsword_b")
                .Component("ref_veldite", 7)
                .Component("wood", 4);
        }
        private void BasicVibrobladeLS()
        {
            _builder.Create(RecipeType.BasicVibrobladeLS, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("longsword_b")
                .Component("ref_veldite", 6)
                .Component("wood", 3);
        }
        private void BasicFinesseVibrobladeR()
        {
            _builder.Create(RecipeType.BasicFinesseVibrobladeR, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("rapier_b")
                .Component("ref_veldite", 5)
                .Component("wood", 3);
        }
        private void BasicVibrobladeK()
        {
            _builder.Create(RecipeType.BasicVibrobladeK, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("katana_b")
                .Component("ref_veldite", 6)
                .Component("wood", 3);
        }
        private void BasicFinesseVibrobladeSS()
        {
            _builder.Create(RecipeType.BasicFinesseVibrobladeSS, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("shortsword_b")
                .Component("ref_veldite", 4)
                .Component("wood", 2);
        }
        private void BasicBatonC()
        {
            _builder.Create(RecipeType.BasicBatonC, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 1)
                .Resref("club_b")
                .Component("ref_veldite", 3)
                .Component("wood", 2);
        }
        private void BasicBatonM()
        {
            _builder.Create(RecipeType.BasicBatonM, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("mace_b")
                .Component("ref_veldite", 4)
                .Component("wood", 2);
        }
        private void BasicBatonMS()
        {
            _builder.Create(RecipeType.BasicBatonMS, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .Resref("morningstar_b")
                .Component("ref_veldite", 5)
                .Component("wood", 3);
        }
        private void BasicQuarterstaff()
        {
            _builder.Create(RecipeType.BasicQuarterstaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 1)
                .Resref("morningstar_b")
                .Component("ref_veldite", 3)
                .Component("wood", 2);
        }
        private void BasicFinesseVibrobladeK()
        {
            _builder.Create(RecipeType.BasicFinesseVibrobladeK, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .Resref("kukri_b")
                .Component("ref_veldite", 7)
                .Component("wood", 4);
        }
        private void BasicKnuckles()
        {
            _builder.Create(RecipeType.BasicKnuckles, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 1)
                .Resref("knuckles_b")
                .Component("ref_veldite", 4)
                .Component("wood", 2);
        }
        private void BasicPolearmH()
        {
            _builder.Create(RecipeType.BasicPolearmH, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .Resref("halberd_b")
                .Component("ref_veldite", 5)
                .Component("wood", 3);
        }
        private void BasicPolearmS()
        {
            _builder.Create(RecipeType.BasicPolearmS, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .Resref("spear_b")
                .Component("ref_veldite", 10)
                .Component("wood", 5);
        }
        private void BasicBlasterPistol()
        {
            _builder.Create(RecipeType.BasicBlasterPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .Resref("blaster_b")
                .Component("ref_veldite", 5)
                .Component("elec_destroyed", 3);
        }
        private void BasicBlasterRifle()
        {
            _builder.Create(RecipeType.BasicBlasterRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .Resref("rifle_b")
                .Component("ref_veldite", 7)
                .Component("elec_destroyed", 5);
        }
        private void BasicDart()
        {
            _builder.Create(RecipeType.BasicDart, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .Resref("dart_b")
                .Component("ref_veldite", 2)
                .Component("wood", 1);
        }
        private void BasicShuriken()
        {
            _builder.Create(RecipeType.BasicShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .Resref("shuriken_b")
                .Component("ref_veldite", 3)
                .Component("wood", 2);
        }
        private void BasicThrowingAxe()
        {
            _builder.Create(RecipeType.BasicThrowingAxe, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 1)
                .Resref("axe_b")
                .Component("ref_veldite", 4)
                .Component("wood", 2);
        }
    }
}