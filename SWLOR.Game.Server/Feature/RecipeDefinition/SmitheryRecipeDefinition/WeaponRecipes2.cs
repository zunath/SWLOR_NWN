using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes2 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            HeavyVibrobladeGA1();
            VibrobladeBA1();
            VibrobladeBS1();
            FinesseVibrobladeD1();
            HeavyVibrobladeGS1();
            VibrobladeLS1();
            FinesseVibrobladeR1();
            VibrobladeK1();
            FinesseVibrobladeSS1();
            BatonC1();
            BatonM1();
            BatonMS1();
            Quarterstaff1();
            FinesseVibrobladeK1();
            Knuckles1();
            PolearmH1();
            PolearmS1();
            BlasterPistol1();
            BlasterRifle1();
            Dart1();
            Shuriken1();
            ThrowingAxe1();

            return _builder.Build();
        }

        private void HeavyVibrobladeGA1()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGA1, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .Resref("greataxe_1")
                .Component("ref_scordspar", 9)
                .Component("fine_wood", 5);
        }

        private void VibrobladeBA1()
        {
            _builder.Create(RecipeType.VibrobladeBA1, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("battleaxe_1")
                .Component("ref_scordspar", 8)
                .Component("fine_wood", 4);
        }

        private void VibrobladeBS1()
        {
            _builder.Create(RecipeType.VibrobladeBS1, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("bst_sword_1")
                .Component("ref_scordspar", 7)
                .Component("fine_wood", 4);
        }

        private void FinesseVibrobladeD1()
        {
            _builder.Create(RecipeType.FinesseVibrobladeD1, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("dagger_1")
                .Component("ref_scordspar", 1)
                .Component("fine_wood", 1);
        }
        private void HeavyVibrobladeGS1()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGS1, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .Resref("greatsword_1")
                .Component("ref_scordspar", 7)
                .Component("fine_wood", 4);
        }
        private void VibrobladeLS1()
        {
            _builder.Create(RecipeType.VibrobladeLS1, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("longsword_1")
                .Component("ref_scordspar", 6)
                .Component("fine_wood", 2);
        }
        private void FinesseVibrobladeR1()
        {
            _builder.Create(RecipeType.FinesseVibrobladeR1, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("rapier_1")
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 2);
        }
        private void VibrobladeK1()
        {
            _builder.Create(RecipeType.VibrobladeK1, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("katana_1")
                .Component("ref_scordspar", 6)
                .Component("fine_wood", 2);
        }
        private void FinesseVibrobladeSS1()
        {
            _builder.Create(RecipeType.FinesseVibrobladeSS1, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("shortsword_1")
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 1);
        }
        private void BatonC1()
        {
            _builder.Create(RecipeType.BatonC1, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .Resref("club_1")
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);
        }
        private void BatonM1()
        {
            _builder.Create(RecipeType.BatonM1, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("mace_1")
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 1);
        }
        private void BatonMS1()
        {
            _builder.Create(RecipeType.BatonMS1, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .Resref("morningstar_1")
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 2);
        }
        private void Quarterstaff1()
        {
            _builder.Create(RecipeType.Quarterstaff1, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .Resref("morningstar_1")
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);
        }
        private void FinesseVibrobladeK1()
        {
            _builder.Create(RecipeType.FinesseVibrobladeK1, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .Resref("kukri_1")
                .Component("ref_scordspar", 7)
                .Component("fine_wood", 4);
        }
        private void Knuckles1()
        {
            _builder.Create(RecipeType.Knuckles1, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .Resref("knuckles_1")
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 1);
        }
        private void PolearmH1()
        {
            _builder.Create(RecipeType.PolearmH1, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .Resref("halberd_1")
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 2);
        }
        private void PolearmS1()
        {
            _builder.Create(RecipeType.PolearmS1, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .Resref("spear_1")
                .Component("ref_scordspar", 10)
                .Component("fine_wood", 5);
        }
        private void BlasterPistol1()
        {
            _builder.Create(RecipeType.BlasterPistol1, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .Resref("blaster_1")
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 2);
        }
        private void BlasterRifle1()
        {
            _builder.Create(RecipeType.BlasterRifle1, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .Resref("rifle_1")
                .Component("ref_scordspar", 7)
                .Component("elec_flawed", 5);
        }
        private void Dart1()
        {
            _builder.Create(RecipeType.Dart1, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .Resref("dart_1")
                .Component("ref_scordspar", 1)
                .Component("fine_wood", 1);
        }
        private void Shuriken1()
        {
            _builder.Create(RecipeType.Shuriken1, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .Resref("shuriken_1")
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);
        }
        private void ThrowingAxe1()
        {
            _builder.Create(RecipeType.ThrowingAxe1, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 2)
                .Resref("axe_1")
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 1);
        }
    }
}