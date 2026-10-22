namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The ids of the Kitchen's actions. Adjust Heat is one action with three settings, each its own id; Add has one id for every
/// committed ingredient line and one for each free pantry seasoning.
/// </summary>
public static class KitchenActionIds
{
    public const string HeatLow = "heat.low";
    public const string HeatMedium = "heat.medium";
    public const string HeatHigh = "heat.high";
    public const string Simmer = "simmer";
    public const string Sear = "sear";
    public const string Stir = "stir";
    public const string Cover = "cover";
    public const string AddSalt = "add.salt";
    public const string AddSugar = "add.sugar";
    public const string AddSpice = "add.spice";

    private const string IngredientPrefix = "add.ingredient.";

    /// <summary>The id of Add for the recipe's nth committed ingredient line, counting from 0.</summary>
    public static string AddIngredient(int line) => IngredientPrefix + line;

    public static bool TryParseIngredient(string actionId, out int line)
    {
        line = -1;
        return actionId != null &&
               actionId.StartsWith(IngredientPrefix, StringComparison.Ordinal) &&
               int.TryParse(actionId.Substring(IngredientPrefix.Length), out line) &&
               line >= 0;
    }

    public static string Heat(KitchenHeat heat) => heat switch
    {
        KitchenHeat.Low => HeatLow,
        KitchenHeat.Medium => HeatMedium,
        _ => HeatHigh
    };

    /// <summary>The pantry seasoning an id adds, or false when the id is not a pantry action.</summary>
    public static bool TryGetPantryAxis(string actionId, out KitchenAxis axis)
    {
        switch (actionId)
        {
            case AddSalt:
                axis = KitchenAxis.Savory;
                return true;
            case AddSugar:
                axis = KitchenAxis.Sweet;
                return true;
            case AddSpice:
                axis = KitchenAxis.Spice;
                return true;
            default:
                axis = KitchenAxis.Savory;
                return false;
        }
    }
}
