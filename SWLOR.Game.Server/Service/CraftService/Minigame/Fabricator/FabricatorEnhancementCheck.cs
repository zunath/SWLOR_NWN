using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>Decides whether an item the player picked can be slotted into a recipe's enhancement slot.</summary>
public static class FabricatorEnhancementCheck
{
    /// <summary>The most levels an enhancement may sit above the recipe.</summary>
    public const int MaxLevelsAboveRecipe = 5;

    /// <summary>The reason the item cannot be used as an enhancement for the recipe, or an empty string when it can.</summary>
    public static string Problem(uint player, uint item, RecipeDetail recipe)
    {
        if (GetItemPossessor(item) != player)
            return "Item must be in your inventory.";

        var propertyType = PropertyTypeFor(recipe.EnhancementType);
        if (propertyType == ItemPropertyType.Invalid)
            return "Item must be an enhancement.";

        var hasProperty = false;
        var enhancementLevel = -1;
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
        {
            var type = GetItemPropertyType(ip);
            hasProperty |= type == propertyType;
            if (type == ItemPropertyType.EnhancementLevel)
                enhancementLevel = GetItemPropertyCostTableValue(ip);
        }

        if (!hasProperty || enhancementLevel == -1)
            return "Item must be an enhancement.";

        return enhancementLevel - recipe.Level > MaxLevelsAboveRecipe
            ? "Enhancement must be within 5 levels of recipe level."
            : string.Empty;
    }

    private static ItemPropertyType PropertyTypeFor(RecipeEnhancementType type) => type switch
    {
        RecipeEnhancementType.Armor => ItemPropertyType.ArmorEnhancement,
        RecipeEnhancementType.Weapon => ItemPropertyType.WeaponEnhancement,
        RecipeEnhancementType.Structure => ItemPropertyType.StructureEnhancement,
        RecipeEnhancementType.Food => ItemPropertyType.FoodEnhancement,
        RecipeEnhancementType.Starship => ItemPropertyType.StarshipEnhancement,
        RecipeEnhancementType.Module => ItemPropertyType.ModuleEnhancement,
        RecipeEnhancementType.Droid => ItemPropertyType.DroidEnhancement,
        _ => ItemPropertyType.Invalid
    };
}
