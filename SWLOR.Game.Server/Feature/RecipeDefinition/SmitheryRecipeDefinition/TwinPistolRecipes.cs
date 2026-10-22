using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;

/// <summary>One crafted pair owns both guns' shared profile and enhancement budget.</summary>
public sealed class TwinPistolRecipes : IRecipeListDefinition
{
    public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
    {
        var sourceRecipes = new WeaponRecipes().BuildRecipes();
        var definitions = new (RecipeType Source, RecipeType Pair, string Resref)[]
        {
            (RecipeType.BasicPistol, RecipeType.BasicTwinPistol, "b_twinpistol"),
            (RecipeType.FieldPistol, RecipeType.FieldTwinPistol, "fld_twinpistol"),
            (RecipeType.TitanPistol, RecipeType.TitanTwinPistol, "tit_twinpistol"),
            (RecipeType.SithPistol, RecipeType.SithTwinPistol, "sith_twinpistol"),
            (RecipeType.VeteranPistol, RecipeType.VeteranTwinPistol, "vet_twinpistol"),
            (RecipeType.DeltaPistol, RecipeType.DeltaTwinPistol, "del_twinpistol"),
            (RecipeType.PrimePistol, RecipeType.PrimeTwinPistol, "prm_twinpistol"),
            (RecipeType.ProtoPistol, RecipeType.ProtoTwinPistol, "proto_twinpistol"),
            (RecipeType.AscendantPistol, RecipeType.AscendantTwinPistol, "asc_twinpistol"),
            (RecipeType.OphidianPistol, RecipeType.OphidianTwinPistol, "oph_twinpistol"),
            (RecipeType.ChiroPistol, RecipeType.ChiroTwinPistol, "chi_twinpistol"),
        };

        var result = new Dictionary<RecipeType, RecipeDetail>();
        foreach (var (sourceId, pairId, resref) in definitions)
        {
            var source = sourceRecipes[sourceId];
            result[pairId] = new RecipeDetail
            {
                Skill = source.Skill, Category = RecipeCategoryType.TwinPistol, Resref = resref,
                Quantity = 1, Level = source.Level, PracticeRankLimit = source.PracticeRankLimit,
                IsActive = source.IsActive, EnhancementType = source.EnhancementType,
                EnhancementSlots = source.EnhancementSlots, ResearchCostModifier = source.ResearchCostModifier,
                IsItemIntendedForCrafting = source.IsItemIntendedForCrafting,
                MinigameParameters = source.MinigameParameters,
                Components = source.Components.ToDictionary(component => component.Key, component => component.Value * 2),
                Requirements = new List<IRecipeRequirement>(source.Requirements),
                EffectLines = new List<string>(source.EffectLines)
                {
                    "One item equips both Twin Pistols. Removing either gun unequips the pair.",
                    "Weapon profile enhancements apply to both guns. Equipment bonuses apply once.",
                },
            };
            if (sourceId == RecipeType.ChiroPistol)
                result[pairId].EffectLines.Add("Requires learning the Chiro Pistol recipe.");
        }
        return result;
    }
}
