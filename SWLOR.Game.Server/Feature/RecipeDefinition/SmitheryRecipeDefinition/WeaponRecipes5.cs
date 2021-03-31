using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes5 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            HeavyVibrobladeGA4();
            VibrobladeBA4();
            VibrobladeBS4();
            FinesseVibrobladeD4();
            HeavyVibrobladeGS4();
            VibrobladeLS4();
            FinesseVibrobladeR4();
            VibrobladeK4();
            FinesseVibrobladeSS4();
            BatonC4();
            BatonM4();
            BatonMS4();
            Quarterstaff4();
            FinesseVibrobladeK4();
            Knuckles4();
            PolearmH4();
            PolearmS4();
            BlasterPistol4();
            BlasterRifle4();
            Dart4();
            Shuriken4();
            ThrowingAxe4();

            return _builder.Build();
        }

        private void HeavyVibrobladeGA4()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGA4, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .Resref("greataxe_4")
                .Component("ref_jasioclase", 9)
                .Component("hyphae_wood", 5);
        }

        private void VibrobladeBA4()
        {
            _builder.Create(RecipeType.VibrobladeBA4, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("battleaxe_4")
                .Component("ref_jasioclase", 8)
                .Component("hyphae_wood", 5);
        }

        private void VibrobladeBS4()
        {
            _builder.Create(RecipeType.VibrobladeBS4, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("bst_sword_4")
                .Component("ref_jasioclase", 7)
                .Component("hyphae_wood", 5);
        }

        private void FinesseVibrobladeD4()
        {
            _builder.Create(RecipeType.FinesseVibrobladeD4, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("dagger_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 1);
        }
        private void HeavyVibrobladeGS4()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGS4, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .Resref("greatsword_4")
                .Component("ref_jasioclase", 7)
                .Component("hyphae_wood", 5);
        }
        private void VibrobladeLS4()
        {
            _builder.Create(RecipeType.VibrobladeLS4, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("longsword_4")
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 5);
        }
        private void FinesseVibrobladeR4()
        {
            _builder.Create(RecipeType.FinesseVibrobladeR4, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("rapier_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void VibrobladeK4()
        {
            _builder.Create(RecipeType.VibrobladeK4, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("katana_4")
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 5);
        }
        private void FinesseVibrobladeSS4()
        {
            _builder.Create(RecipeType.FinesseVibrobladeSS4, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("shortsword_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void BatonC4()
        {
            _builder.Create(RecipeType.BatonC4, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .Resref("club_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void BatonM4()
        {
            _builder.Create(RecipeType.BatonM4, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("mace_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void BatonMS4()
        {
            _builder.Create(RecipeType.BatonMS4, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .Resref("morningstar_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void Quarterstaff4()
        {
            _builder.Create(RecipeType.Quarterstaff4, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .Resref("morningstar_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void FinesseVibrobladeK4()
        {
            _builder.Create(RecipeType.FinesseVibrobladeK4, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .Resref("kukri_4")
                .Component("ref_jasioclase", 7)
                .Component("hyphae_wood", 5);
        }
        private void Knuckles4()
        {
            _builder.Create(RecipeType.Knuckles4, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .Resref("knuckles_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void PolearmH4()
        {
            _builder.Create(RecipeType.PolearmH4, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .Resref("halberd_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void PolearmS4()
        {
            _builder.Create(RecipeType.PolearmS4, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .Resref("spear_4")
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 5);
        }
        private void BlasterPistol4()
        {
            _builder.Create(RecipeType.BlasterPistol4, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .Resref("blaster_4")
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 5);
        }
        private void BlasterRifle4()
        {
            _builder.Create(RecipeType.BlasterRifle4, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .Resref("rifle_4")
                .Component("ref_jasioclase", 7)
                .Component("elec_high", 5);
        }
        private void Dart4()
        {
            _builder.Create(RecipeType.Dart4, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .Resref("dart_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 1);
        }
        private void Shuriken4()
        {
            _builder.Create(RecipeType.Shuriken4, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .Resref("shuriken_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
        private void ThrowingAxe4()
        {
            _builder.Create(RecipeType.ThrowingAxe4, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 5)
                .Resref("axe_4")
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5);
        }
    }
}