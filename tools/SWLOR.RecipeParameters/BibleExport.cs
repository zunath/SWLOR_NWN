using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.RecipeParameters;

/// <summary>
/// Describes the parameters compiled into the recipe definitions as Bible columns, for
/// <c>tools/UpdateRecipeParameterBible.py</c> to write into the recipe tabs.
/// </summary>
public static class BibleExport
{
    /// <summary>
    /// Builds the export from the values in code. A recipe a profile covers must already carry parameters;
    /// otherwise the export is refused so the Bible never shows values the game does not use.
    /// </summary>
    public static string Build(
        IReadOnlyList<IRecipeParameterProfile> profiles,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
        out IReadOnlyList<string> problems)
    {
        var found = new List<string>();
        var profileNodes = new JsonArray();
        var recipeNodes = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);

        foreach (var profile in profiles)
        {
            profileNodes.Add(new JsonObject
            {
                ["key"] = profile.Key,
                ["skill"] = profile.Skill.ToString(),
                ["headers"] = new JsonArray(RecipeParameterBibleColumns.Headers(profile).Select(h => (JsonNode)JsonValue.Create(h)).ToArray()),
            });

            foreach (var (type, detail) in recipes.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
            {
                var context = RecipeParameterContext.From(type, detail);
                if (detail.Skill != profile.Skill || !profile.AppliesTo(context))
                    continue;

                if (!profile.ParametersType.IsInstanceOfType(detail.MinigameParameters))
                {
                    found.Add($"{type} has no {profile.ParametersType.Name}; run the write command first");
                    continue;
                }

                var reason = RecipeParameterGenerator.DescribeOverrides(profile, context);
                var row = recipeNodes.TryGetValue(type.ToString(), out var existing) ? existing : recipeNodes[type.ToString()] = new JsonObject();
                foreach (var cell in RecipeParameterBibleColumns.Cells(profile, detail.MinigameParameters, reason))
                {
                    row[cell.Header] = cell.IsNumber
                        ? JsonValue.Create(decimal.Parse(cell.Text, NumberStyles.Float, CultureInfo.InvariantCulture))
                        : JsonValue.Create(cell.Text);
                }
            }
        }

        problems = found;
        var recipesNode = new JsonObject();
        foreach (var (name, row) in recipeNodes)
        {
            recipesNode[name] = row;
        }

        var root = new JsonObject { ["profiles"] = profileNodes, ["recipes"] = recipesNode };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }
}
