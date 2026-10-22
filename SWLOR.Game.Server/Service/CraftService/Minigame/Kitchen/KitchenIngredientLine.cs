namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// One component the recipe commits to the pot. The whole stack is added in <see cref="Portions"/> portions, so the full stack
/// pushes exactly its quantity times the ingredient's vector however many clicks it takes.
/// </summary>
public sealed class KitchenIngredientLine
{
    public KitchenIngredientLine(string key, string name, int quantity, KitchenComponentFlavor flavor)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("An ingredient needs a key.", nameof(key));
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "A committed ingredient has at least one unit.");

        Key = key;
        Name = string.IsNullOrWhiteSpace(name) ? key : name;
        Quantity = quantity;
        Kind = flavor.Kind;
        UnitVector = flavor.Vector;
        Portions = Math.Min(quantity, KitchenTuning.MaxPortionsPerLine);
        PortionVector = flavor.Vector.ScaledBy(quantity, Portions);
        DilutePercent = (int)Math.Round(
            KitchenTuning.DilutePercentPerUnit * (double)quantity / Portions,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>The component's resref.</summary>
    public string Key { get; }

    /// <summary>The name shown for the ingredient.</summary>
    public string Name { get; }

    /// <summary>The units the recipe commits.</summary>
    public int Quantity { get; }

    public KitchenIngredientKind Kind { get; }

    /// <summary>The push of one unit.</summary>
    public FlavorVector UnitVector { get; }

    /// <summary>How many times the line can be added.</summary>
    public int Portions { get; }

    /// <summary>The push of one portion at its listed strength, before the spread and Peak Aroma.</summary>
    public FlavorVector PortionVector { get; }

    /// <summary>The share of every axis one portion removes, for an ingredient that dilutes.</summary>
    public int DilutePercent { get; }

    /// <summary>The total push of the whole stack.</summary>
    public FlavorVector TotalVector => UnitVector.ScaledBy(Quantity, 1);
}
