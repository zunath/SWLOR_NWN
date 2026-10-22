using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Reads what an enhancement item gives a crafted item and what it costs the craft. It is the shared form of the reading the
/// original crafting window did when an enhancement was slotted.
/// </summary>
public static class CraftEnhancementReader
{
    /// <summary>The properties the enhancement transfers to the recipe's item and the progress it adds to the craft.</summary>
    public sealed class Reading
    {
        public List<ItemProperty> Properties { get; } = new();

        /// <summary>Extra progress the enhancement adds to the progress meter's maximum.</summary>
        public int ProgressPenalty { get; set; }
    }

    /// <summary>Reads an enhancement item for a recipe. Properties made for another kind of item are ignored.</summary>
    public static Reading Read(uint enhancement, RecipeDetail recipe)
    {
        var reading = new Reading();
        var weaponDamageType = CombatDamageType.Invalid;

        for (var ip = GetFirstItemProperty(enhancement); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(enhancement))
        {
            if (GetItemPropertyType(ip) != ItemPropertyType.WeaponDamageType)
                continue;

            var subType = GetItemPropertySubType(ip);
            if (Enum.IsDefined(typeof(CombatDamageType), subType))
                weaponDamageType = (CombatDamageType)subType;
        }

        for (var ip = GetFirstItemProperty(enhancement); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(enhancement))
        {
            var type = GetItemPropertyType(ip);
            var subType = (EnhancementSubType)GetItemPropertySubType(ip);
            var amount = GetItemPropertyCostTableValue(ip);

            if (type == ItemPropertyType.ProgressPenalty)
            {
                reading.ProgressPenalty += amount;
            }
            else if (type == ItemPropertyType.ArmorEnhancement && recipe.EnhancementType == RecipeEnhancementType.Armor ||
                     type == ItemPropertyType.StructureEnhancement && recipe.EnhancementType == RecipeEnhancementType.Structure ||
                     type == ItemPropertyType.FoodEnhancement && recipe.EnhancementType == RecipeEnhancementType.Food ||
                     type == ItemPropertyType.StarshipEnhancement && recipe.EnhancementType == RecipeEnhancementType.Starship ||
                     type == ItemPropertyType.ModuleEnhancement && recipe.EnhancementType == RecipeEnhancementType.Module ||
                     type == ItemPropertyType.DroidEnhancement && recipe.EnhancementType == RecipeEnhancementType.Droid)
            {
                reading.Properties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
            }
            else if (type == ItemPropertyType.WeaponEnhancement && recipe.EnhancementType == RecipeEnhancementType.Weapon)
            {
                reading.Properties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount, weaponDamageType));
            }
        }

        return reading;
    }
}
