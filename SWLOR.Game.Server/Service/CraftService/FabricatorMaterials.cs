using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// How hard a Fabrication component is on the fabricator's heads, from 1 (soft wood, crude fiber) to 6 (the finest metals and gems).
/// A recipe's hardness is the mean of its components' by unit, and it sets the wear rate the Fabricator costs Machine Condition at.
/// </summary>
public static class FabricatorMaterials
{
    public const double SoftestHardness = 1.0;
    public const double HardestHardness = 6.0;

    /// <summary>The hardness of a component nobody has rated, such as a quest token or a key.</summary>
    public const double DefaultHardness = 2.0;

    // Woods cut easily at the lowest grade and hold an edge at the highest.
    private static readonly IReadOnlyDictionary<string, double> Woods = new Dictionary<string, double>
    {
        ["wood"] = 1, ["fine_wood"] = 2, ["aracia_wood"] = 3, ["hyphae_wood"] = 4, ["ancient_wood"] = 5
    };

    // Assembled structure parts are dense and bonded; the reinforced ones more so.
    private static readonly IReadOnlyDictionary<string, double> StructureParts = new Dictionary<string, double>
    {
        ["pow_supp_unit"] = 3, ["const_parts"] = 3, ["r_pow_supp_unit"] = 4, ["r_const_parts"] = 4
    };

    private static readonly HashSet<string> HardGrades = new()
    {
        "jade", "agate", "citrine", "ruby", "emerald", "diamond", "zinsiam",
        "aluminum", "steel", "obsidian", "crystal",
        "quadrenium", "vintrium", "ionite", "katrium"
    };

    /// <summary>The hardness of one component.</summary>
    public static double HardnessOf(string resref)
    {
        if (string.IsNullOrEmpty(resref))
            return DefaultHardness;

        if (Woods.TryGetValue(resref, out var wood))
            return wood;

        if (StructureParts.TryGetValue(resref, out var part))
            return part;

        var grade = CircuitComponentGrades.GradeOf(resref);
        if (grade <= 0)
            return DefaultHardness;

        // Refined ores and gems are a grade harder than leather, fiber and electronics of the same grade.
        var hard = resref.StartsWith("ref_") || HardGrades.Contains(resref);
        return System.Math.Clamp(hard ? grade + 1 : grade, SoftestHardness, HardestHardness);
    }

    /// <summary>The mean hardness of a recipe's components, weighted by how many units of each it uses.</summary>
    public static double ForRecipe(IReadOnlyDictionary<string, int> components)
    {
        var units = components.Values.Where(count => count > 0).Sum();
        if (units <= 0)
            return DefaultHardness;

        return components.Where(pair => pair.Value > 0).Sum(pair => HardnessOf(pair.Key) * pair.Value) / units;
    }
}
