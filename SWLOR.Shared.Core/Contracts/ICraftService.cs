using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ICraftService
    {
        void CacheData();
        RecipeDetail GetRecipe(RecipeType recipeType);
        bool RecipeExists(RecipeType recipeType);
        EnhancementSubTypeAttribute GetEnhancementSubType(EnhancementSubType subType);
        Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetAllCategories();
        bool IsItemRecipe(uint item);
        bool IsItemComponent(uint item);
        bool IsItemEnhancement(uint item);
        Dictionary<RecipeType, RecipeDetail> GetAllRecipes();
        Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipes();
        Dictionary<RecipeType, RecipeDetail> GetAllRecipesBySkill(SkillType skill);
        Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipesBySkill(SkillType skill);
        Dictionary<RecipeType, RecipeDetail> GetRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category);
        Dictionary<RecipeType, RecipeDetail> GetResearchableRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category);
        Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetRecipeCategoriesBySkill(SkillType skill);
        void UseCraftingDevice();
        (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe, BlueprintDetail blueprint);
        bool CanPlayerCraftRecipe(uint player, RecipeType recipeType);
        bool CanPlayerResearchRecipe(uint player, RecipeType recipeType);
        RecipeLevelDetail GetRecipeLevelDetail(int level);
        ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount);
        void UseRefinery();
        void UseResearchTerminal();
        BlueprintDetail GetBlueprintDetails(uint blueprint);
        void SetBlueprintDetails(uint blueprint, BlueprintDetail blueprintDetail);
        int CalculateBlueprintCraftCreditCost(uint blueprint);
        int CalculateBlueprintResearchCreditCost(RecipeType recipe, int blueprintLevel, int reductionBonus);
        int CalculateBlueprintResearchSeconds(RecipeType recipe, int blueprintLevel, int reductionBonus);
        void OnRemoveProperty();
    }
}
