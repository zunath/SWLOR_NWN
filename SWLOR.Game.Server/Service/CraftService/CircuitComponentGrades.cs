using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// The grade of an Engineering component: 1 for the crudest parts to 5 for the finest, and 6 for capital ship parts. A recipe's
/// grade is its best component's, and it sets the Circuit's overload line.
/// </summary>
public static class CircuitComponentGrades
{
    public const int MaxGrade = 6;

    private static readonly Regex NumberedUnit = new(
        "^(c_unit|l_unit|ca_unit|discharge_unit|dmotive_sys|d_sensor|manip_arm|d_brain|dp_supply|d_chassis|diag_circuit)([1-5])$",
        RegexOptions.Compiled);

    private static readonly Regex NamedGrade = new("^(elec|lth|fiberp)_([a-z]+)$", RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, int> NamedGrades = new Dictionary<string, int>
    {
        ["ruined"] = 1,
        ["flawed"] = 2,
        ["good"] = 3,
        ["imperfect"] = 4,
        ["high"] = 5,
        ["perfect"] = 5
    };

    // Gems, metals, refined ores and fusion materials, by the tier of the recipes that use them.
    private static readonly IReadOnlyDictionary<string, int> FixedGrades = new Dictionary<string, int>
    {
        ["jade"] = 1, ["agate"] = 2, ["citrine"] = 3, ["ruby"] = 4, ["emerald"] = 5, ["diamond"] = 5,
        ["aluminum"] = 1, ["steel"] = 2, ["obsidian"] = 3, ["crystal"] = 4,
        ["quadrenium"] = 1, ["vintrium"] = 2, ["ionite"] = 3, ["katrium"] = 4, ["zinsiam"] = 5,
        ["ref_veldite"] = 1, ["ref_tilarium"] = 1,
        ["ref_scordspar"] = 2, ["ref_currian"] = 2,
        ["ref_plagionite"] = 3, ["ref_idailia"] = 3,
        ["ref_keromber"] = 4, ["ref_barinium"] = 4,
        ["ref_jasioclase"] = 5, ["ref_gostian"] = 5, ["ref_arkoxit"] = 5, ["ref_arda"] = 5,
        ["chiro_shard"] = 6
    };

    /// <summary>The grade of a component, or 0 when the component has none (a quest item, a token).</summary>
    public static int GradeOf(string resref)
    {
        if (string.IsNullOrEmpty(resref))
            return 0;

        if (FixedGrades.TryGetValue(resref, out var fixedGrade))
            return fixedGrade;

        var named = NamedGrade.Match(resref);
        if (named.Success && NamedGrades.TryGetValue(named.Groups[2].Value, out var namedGrade))
            return namedGrade;

        var numbered = NumberedUnit.Match(resref);
        if (numbered.Success)
            return int.Parse(numbered.Groups[2].Value);

        return resref.StartsWith("capc_") ? MaxGrade : 0;
    }

    /// <summary>The tier a recipe level belongs to, 1 to 5 in steps of ten levels and 6 for the capital ship recipes from level 51.</summary>
    public static int TierOfLevel(int level) => level >= 51 ? MaxGrade : Math.Clamp((level + 9) / 10, 1, 5);

    /// <summary>
    /// The grade of a recipe: its best component's. A recipe with no graded component takes the tier of its level.
    /// </summary>
    public static int ForRecipe(int level, IEnumerable<string> componentResrefs)
    {
        var best = componentResrefs.Select(GradeOf).DefaultIfEmpty(0).Max();
        return best > 0 ? best : TierOfLevel(level);
    }
}
