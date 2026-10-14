using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;

/// <summary>Companions use the matching pistol's materials, research, prices and enhancement-slot budget.</summary>
public sealed class CompanionPistolRecipes : IRecipeListDefinition
{
    public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
    {
        var sourceRecipes = new WeaponRecipes().BuildRecipes();
        var definitions = new (RecipeType Source, RecipeType Companion, string Resref)[]
        {
            (RecipeType.BasicPistol, RecipeType.BasicCompanionPistol, "b_companion"),
            (RecipeType.FieldPistol, RecipeType.FieldCompanionPistol, "fld_companion"),
            (RecipeType.TitanPistol, RecipeType.TitanCompanionPistol, "tit_companion"),
            (RecipeType.SithPistol, RecipeType.SithCompanionPistol, "sith_companion"),
            (RecipeType.VeteranPistol, RecipeType.VeteranCompanionPistol, "vet_companion"),
            (RecipeType.DeltaPistol, RecipeType.DeltaCompanionPistol, "del_companion"),
            (RecipeType.PrimePistol, RecipeType.PrimeCompanionPistol, "prm_companion"),
            (RecipeType.ProtoPistol, RecipeType.ProtoCompanionPistol, "proto_companion"),
            (RecipeType.AscendantPistol, RecipeType.AscendantCompanionPistol, "asc_companion"),
            (RecipeType.OphidianPistol, RecipeType.OphidianCompanionPistol, "oph_companion"),
            (RecipeType.ChiroPistol, RecipeType.ChiroCompanionPistol, "chi_companion"),
        };
        var result = new Dictionary<RecipeType, RecipeDetail>();
        foreach (var (sourceId, companionId, resref) in definitions)
        {
            var source = sourceRecipes[sourceId];
            result[companionId] = new RecipeDetail
            {
                Skill = source.Skill, Category = RecipeCategoryType.CompanionPistol, Resref = resref,
                Quantity = source.Quantity, Level = source.Level, PracticeRankLimit = source.PracticeRankLimit,
                IsActive = source.IsActive, EnhancementType = source.EnhancementType,
                EnhancementSlots = source.EnhancementSlots, ResearchCostModifier = source.ResearchCostModifier,
                IsItemIntendedForCrafting = source.IsItemIntendedForCrafting,
                Components = new Dictionary<string, int>(source.Components),
                // Learning the Chiro Pistol recipe also teaches its companion construction.
                Requirements = new List<IRecipeRequirement>(source.Requirements),
                AllowedEnhancements = new[] { EnhancementSubType.DMG, EnhancementSubType.Accuracy },
                AllowedOutputProperties = Enum.GetValues<ItemPropertyType>()
                    .Where(TwinPistolEquipment.IsSupportedCompanionProperty).ToArray(),
                EffectLines = new List<string>(source.EffectLines)
                {
                    "Companion pistol: requires an eligible main-hand pistol to attack.",
                    "Accepts DMG and Accuracy enhancements, including typed DMG. Native and creature-wide bonus kits are incompatible.",
                },
            };
            if (sourceId == RecipeType.ChiroPistol)
                result[companionId].EffectLines.Add("Requires learning the Chiro Pistol recipe.");
        }
        return result;
    }
}
