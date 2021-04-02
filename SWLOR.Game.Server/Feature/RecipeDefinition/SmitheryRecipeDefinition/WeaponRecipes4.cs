using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes4 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            HeavyVibrobladeGA3();
            VibrobladeBA3();
            VibrobladeBS3();
            FinesseVibrobladeD3();
            HeavyVibrobladeGS3();
            VibrobladeLS3();
            FinesseVibrobladeR3();
            VibrobladeK3();
            FinesseVibrobladeSS3();
            BatonC3();
            BatonM3();
            BatonMS3();
            Quarterstaff3();
            FinesseVibrobladeK3();
            Knuckles3();
            PolearmH3();
            PolearmS3();
            BlasterPistol3();
            BlasterRifle3();
            Dart3();
            Shuriken3();
            ThrowingAxe3();

            return _builder.Build();
        }

        private void HeavyVibrobladeGA3()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGA3, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .Resref("greataxe_3")
                .Component("ref_keromber", 9)
                .Component("aracia_wood", 5);
        }

        private void VibrobladeBA3()
        {
            _builder.Create(RecipeType.VibrobladeBA3, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("battleaxe_3")
                .Component("ref_keromber", 8)
                .Component("aracia_wood", 4);
        }

        private void VibrobladeBS3()
        {
            _builder.Create(RecipeType.VibrobladeBS3, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("bst_sword_3")
                .Component("ref_keromber", 7)
                .Component("aracia_wood", 4);
        }

        private void FinesseVibrobladeD3()
        {
            _builder.Create(RecipeType.FinesseVibrobladeD3, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("dagger_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 1);
        }
        private void HeavyVibrobladeGS3()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGS3, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .Resref("greatsword_3")
                .Component("ref_keromber", 7)
                .Component("aracia_wood", 4);
        }
        private void VibrobladeLS3()
        {
            _builder.Create(RecipeType.VibrobladeLS3, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("longsword_3")
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 4);
        }
        private void FinesseVibrobladeR3()
        {
            _builder.Create(RecipeType.FinesseVibrobladeR3, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("rapier_3")
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 4);
        }
        private void VibrobladeK3()
        {
            _builder.Create(RecipeType.VibrobladeK3, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("katana_3")
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 4);
        }
        private void FinesseVibrobladeSS3()
        {
            _builder.Create(RecipeType.FinesseVibrobladeSS3, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("shortsword_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void BatonC3()
        {
            _builder.Create(RecipeType.BatonC3, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .Resref("club_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void BatonM3()
        {
            _builder.Create(RecipeType.BatonM3, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("mace_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void BatonMS3()
        {
            _builder.Create(RecipeType.BatonMS3, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .Resref("morningstar_3")
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 4);
        }
        private void Quarterstaff3()
        {
            _builder.Create(RecipeType.Quarterstaff3, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .Resref("morningstar_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void FinesseVibrobladeK3()
        {
            _builder.Create(RecipeType.FinesseVibrobladeK3, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .Resref("kukri_3")
                .Component("ref_keromber", 7)
                .Component("aracia_wood", 4);
        }
        private void Knuckles3()
        {
            _builder.Create(RecipeType.Knuckles3, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .Resref("knuckles_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void PolearmH3()
        {
            _builder.Create(RecipeType.PolearmH3, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .Resref("halberd_3")
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 4);
        }
        private void PolearmS3()
        {
            _builder.Create(RecipeType.PolearmS3, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .Resref("spear_3")
                .Component("ref_keromber", 10)
                .Component("aracia_wood", 5);
        }
        private void BlasterPistol3()
        {
            _builder.Create(RecipeType.BlasterPistol3, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .Resref("blaster_3")
                .Component("ref_keromber", 5)
                .Component("elec_imperfect", 4);
        }
        private void BlasterRifle3()
        {
            _builder.Create(RecipeType.BlasterRifle3, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .Resref("rifle_3")
                .Component("ref_keromber", 7)
                .Component("elec_imperfect", 5);
        }
        private void Dart3()
        {
            _builder.Create(RecipeType.Dart3, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .Resref("dart_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 1);
        }
        private void Shuriken3()
        {
            _builder.Create(RecipeType.Shuriken3, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .Resref("shuriken_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
        private void ThrowingAxe3()
        {
            _builder.Create(RecipeType.ThrowingAxe3, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 4)
                .Resref("axe_3")
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 4);
        }
    }
}