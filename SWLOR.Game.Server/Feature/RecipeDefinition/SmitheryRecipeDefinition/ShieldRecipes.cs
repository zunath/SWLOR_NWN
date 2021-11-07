﻿using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShieldRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Shields();

            return _builder.Build();
        }

        private void Shields()
        {
            // Battlemaster Shield
            _builder.Create(RecipeType.BattlemasterShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("bm_shield")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 7)
                .Component("wood", 4);

            // Titan Shield
            _builder.Create(RecipeType.TitanShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("tit_shield")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_scordspar", 7)
                .Component("fine_wood", 4);

            // Quark Shield
            _builder.Create(RecipeType.QuarkShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("qk_shield")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 7)
                .Component("ancient_wood", 4);

            // Argos Shield
            _builder.Create(RecipeType.ArgosShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ar_shield")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 7)
                .Component("aracia_wood", 4);

            // Eclipse Shield
            _builder.Create(RecipeType.EclipseShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ec_shield")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 7)
                .Component("hyphae_wood", 4);
        }

    }
}