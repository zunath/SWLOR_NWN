using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes3 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            HeavyVibrobladeGA2();
            VibrobladeBA2();
            VibrobladeBS2();
            FinesseVibrobladeD2();
            HeavyVibrobladeGS2();
            VibrobladeLS2();
            FinesseVibrobladeR2();
            VibrobladeK2();
            FinesseVibrobladeSS2();
            BatonC2();
            BatonM2();
            BatonMS2();
            Quarterstaff2();
            FinesseVibrobladeK2();
            Knuckles2();
            PolearmH2();
            PolearmS2();
            BlasterPistol2();
            BlasterRifle2();
            Dart2();
            Shuriken2();
            ThrowingAxe2();

            return _builder.Build();
        }

        private void HeavyVibrobladeGA2()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGA2, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .Resref("greataxe_2")
                .Component("ref_plagionite", 9)
                .Component("ancient_wood", 5);
        }

        private void VibrobladeBA2()
        {
            _builder.Create(RecipeType.VibrobladeBA2, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("battleaxe_2")
                .Component("ref_plagionite", 8)
                .Component("ancient_wood", 4);
        }

        private void VibrobladeBS2()
        {
            _builder.Create(RecipeType.VibrobladeBS2, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("bst_sword_2")
                .Component("ref_plagionite", 7)
                .Component("ancient_wood", 4);
        }

        private void FinesseVibrobladeD2()
        {
            _builder.Create(RecipeType.FinesseVibrobladeD2, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("dagger_2")
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);
        }
        private void HeavyVibrobladeGS2()
        {
            _builder.Create(RecipeType.HeavyVibrobladeGS2, SkillType.Smithery)
                .Category(RecipeCategoryType.HeavyVibroblade)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .Resref("greatsword_2")
                .Component("ref_plagionite", 7)
                .Component("ancient_wood", 4);
        }
        private void VibrobladeLS2()
        {
            _builder.Create(RecipeType.VibrobladeLS2, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("longsword_2")
                .Component("ref_plagionite", 6)
                .Component("ancient_wood", 3);
        }
        private void FinesseVibrobladeR2()
        {
            _builder.Create(RecipeType.FinesseVibrobladeR2, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("rapier_2")
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);
        }
        private void VibrobladeK2()
        {
            _builder.Create(RecipeType.VibrobladeK2, SkillType.Smithery)
                .Category(RecipeCategoryType.Vibroblade)
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("katana_2")
                .Component("ref_plagionite", 6)
                .Component("ancient_wood", 3);
        }
        private void FinesseVibrobladeSS2()
        {
            _builder.Create(RecipeType.FinesseVibrobladeSS2, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("shortsword_2")
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);
        }
        private void BatonC2()
        {
            _builder.Create(RecipeType.BatonC2, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .Resref("club_2")
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);
        }
        private void BatonM2()
        {
            _builder.Create(RecipeType.BatonM2, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("mace_2")
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);
        }
        private void BatonMS2()
        {
            _builder.Create(RecipeType.BatonMS2, SkillType.Smithery)
                .Category(RecipeCategoryType.Baton)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .Resref("morningstar_2")
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);
        }
        private void Quarterstaff2()
        {
            _builder.Create(RecipeType.Quarterstaff2, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .Resref("morningstar_2")
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);
        }
        private void FinesseVibrobladeK2()
        {
            _builder.Create(RecipeType.FinesseVibrobladeK2, SkillType.Smithery)
                .Category(RecipeCategoryType.FinesseVibroblade)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .Resref("kukri_2")
                .Component("ref_plagionite", 7)
                .Component("ancient_wood", 4);
        }
        private void Knuckles2()
        {
            _builder.Create(RecipeType.Knuckles2, SkillType.Smithery)
                .Category(RecipeCategoryType.Martial)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .Resref("knuckles_2")
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);
        }
        private void PolearmH2()
        {
            _builder.Create(RecipeType.PolearmH2, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .Resref("halberd_2")
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);
        }
        private void PolearmS2()
        {
            _builder.Create(RecipeType.PolearmS2, SkillType.Smithery)
                .Category(RecipeCategoryType.Polearm)
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .Resref("spear_2")
                .Component("ref_plagionite", 10)
                .Component("ancient_wood", 5);
        }
        private void BlasterPistol2()
        {
            _builder.Create(RecipeType.BlasterPistol2, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .Resref("blaster_2")
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);
        }
        private void BlasterRifle2()
        {
            _builder.Create(RecipeType.BlasterRifle2, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .Resref("rifle_2")
                .Component("ref_plagionite", 7)
                .Component("elec_good", 5);
        }
        private void Dart2()
        {
            _builder.Create(RecipeType.Dart2, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .Resref("dart_2")
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);
        }
        private void Shuriken2()
        {
            _builder.Create(RecipeType.Shuriken2, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .Resref("shuriken_2")
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);
        }
        private void ThrowingAxe2()
        {
            _builder.Create(RecipeType.ThrowingAxe2, SkillType.Smithery)
                .Category(RecipeCategoryType.Throwing)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.RangedBlueprints, 3)
                .Resref("axe_2")
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);
        }
    }
}