using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// One component the recipe commits to the pot. The whole stack is added in <see cref="Portions"/> portions. The portions add up
/// to exactly the stack's quantity times the ingredient's vector: when a stack does not divide evenly, the first portions added
/// carry the extra points.
/// </summary>
public sealed class KitchenIngredientLine
{
    private readonly FlavorVector[] _portionVectors;
    private readonly int[] _dilutePercents;

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

        var savory = Split(flavor.Vector.Savory * quantity, Portions);
        var sweet = Split(flavor.Vector.Sweet * quantity, Portions);
        var spice = Split(flavor.Vector.Spice * quantity, Portions);
        _portionVectors = Enumerable.Range(0, Portions).Select(i => new FlavorVector(savory[i], sweet[i], spice[i])).ToArray();
        _dilutePercents = Split(KitchenTuning.DilutePercentPerUnit * quantity, Portions);
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

    /// <summary>The push of the first portion at its listed strength, before the spread and Peak Aroma; later portions are no larger.</summary>
    public FlavorVector PortionVector => _portionVectors[0];

    /// <summary>The share of every axis the first portion removes, for an ingredient that dilutes.</summary>
    public int DilutePercent => _dilutePercents[0];

    /// <summary>The total push of the whole stack.</summary>
    public FlavorVector TotalVector => UnitVector.ScaledBy(Quantity, 1);

    /// <summary>The push of the nth portion added (counting from 0).</summary>
    public FlavorVector PortionVectorAt(int portion) => _portionVectors[portion];

    /// <summary>The share of every axis the nth portion added removes (counting from 0).</summary>
    public int DilutePercentAt(int portion) => _dilutePercents[portion];

    /// <summary>Splits a total into whole parts that add up to it exactly, the larger parts first.</summary>
    private static int[] Split(int total, int parts)
    {
        var quotient = (int)Math.Floor(total / (double)parts);
        var remainder = total - quotient * parts;
        var split = new int[parts];
        for (var index = 0; index < parts; index++)
            split[index] = quotient + (index < remainder ? 1 : 0);

        return split;
    }
}
