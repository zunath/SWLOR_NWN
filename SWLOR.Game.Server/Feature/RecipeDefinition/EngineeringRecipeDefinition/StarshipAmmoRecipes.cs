using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class StarshipAmmoRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }
        private void Tier1()
        {
            // Ship Missiles x5
            _builder.Create(RecipeType.Missile3, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_missile")
                .Level(5)
                .Quantity(5)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .Component("ref_tilarium", 1)
                .Component("elec_ruined", 1);

            // Hypermatter Capsule x1
            _builder.Create(RecipeType.FuelCapsule1, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_fuelcapsule")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 1);
        }

        private void Tier2()
        {
            // Ship Missiles x15
            _builder.Create(RecipeType.Missile15, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_missile")
                .Level(15)
                .Quantity(15)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .Component("ref_currian", 5)
                .Component("elec_flawed", 1);
        }

        private void Tier3()
        {
            // Ship Missiles x25
            _builder.Create(RecipeType.Missile10, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_missile")
                .Level(25)
                .Quantity(25)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .Component("ref_idailia", 7)
                .Component("elec_good", 2);

            // Hypermatter Capsule x3
            _builder.Create(RecipeType.FuelCapsule3, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_fuelcapsule")
                .Level(28)
                .Quantity(3)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .Component("ref_idailia", 3)
                .Component("elec_good", 1);
        }

        private void Tier4()
        {
            // Ship Missiles x35
            _builder.Create(RecipeType.Missile35, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_missile")
                .Level(25)
                .Quantity(35)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .Component("ref_barinium", 9)
                .Component("elec_imperfect", 2);
        }

        private void Tier5()
        {
            // Ship Missiles x45
            _builder.Create(RecipeType.Missile25, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_missile")
                .Level(45)
                .Quantity(45)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_gostian", 11)
                .Component("elec_high", 3);

            // Proton Bomb x5
            _builder.Create(RecipeType.ProtonBomb, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("proton_bomb")
                .Level(45)
                .Quantity(5)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2)
                .Component("ref_arda", 1);

            // Hypermatter Capsule x5
            _builder.Create(RecipeType.FuelCapsule5, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("ship_fuelcapsule")
                .Level(48)
                .Quantity(5)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_gostian", 3)
                .Component("elec_high", 1);

            // Assault Concussion Missile
            _builder.Create(RecipeType.AssaultConcMissile, SkillType.Engineering)
                .Category(RecipeCategoryType.StarshipAmmo)
                .Resref("acm_ammo")
                .Level(52)
                .Quantity(10)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("elec_high", 3)
                .Component("ref_arda", 3)
                .Component("ref_gostian", 5);
        }

    }
}