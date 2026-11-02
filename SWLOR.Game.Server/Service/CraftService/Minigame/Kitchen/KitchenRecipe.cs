using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// What a recipe asks of the Kitchen: its dish, its flavor target, the Freshness it starts with and the ingredients it commits.
/// It is immutable configuration; an engine copies none of it into a state.
/// </summary>
public sealed class KitchenRecipe
{
    public KitchenRecipe(
        KitchenDish dish,
        KitchenTarget target,
        int freshness,
        int level,
        IReadOnlyList<KitchenIngredientLine> lines)
    {
        if (freshness < 1)
            throw new ArgumentOutOfRangeException(nameof(freshness), "A dish starts with some Freshness.");

        Dish = dish;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Freshness = freshness;
        Level = level;
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
    }

    public KitchenDish Dish { get; }

    public KitchenTarget Target { get; }

    /// <summary>The Freshness the dish starts with.</summary>
    public int Freshness { get; }

    public int Level { get; }

    public IReadOnlyList<KitchenIngredientLine> Lines { get; }

    /// <summary>
    /// Builds the recipe the Kitchen cooks from a recipe's stored parameters and its components.
    /// </summary>
    /// <param name="parameters">The recipe's Kitchen parameters.</param>
    /// <param name="level">The recipe's level.</param>
    /// <param name="components">The committed components by resref, in the recipe's order.</param>
    /// <param name="nameOf">Looks up the name shown for a component, or null to show the resref.</param>
    public static KitchenRecipe From(
        KitchenParameters parameters,
        int level,
        IEnumerable<KeyValuePair<string, int>> components,
        Func<string, string> nameOf = null)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var lines = components
            .Select(component => new KitchenIngredientLine(
                component.Key,
                nameOf?.Invoke(component.Key),
                component.Value,
                KitchenComponentTable.Get(component.Key)))
            .ToList();

        var center = new FlavorVector(parameters.TargetSavory, parameters.TargetSweet, parameters.TargetSpice);
        return new KitchenRecipe(
            parameters.Dish,
            new KitchenTarget(center, parameters.InnerRadius, parameters.OuterRadius),
            parameters.Freshness,
            level,
            lines);
    }
}
