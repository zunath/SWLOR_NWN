using System.Text;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.RecipeParameters;

/// <summary>
/// Writes one profile's generated parameters into every recipe definition file under a folder.
/// </summary>
public static class RecipeFolderUpdater
{
    /// <summary>
    /// Rewrites the folder, or only reports what would change when <paramref name="write"/> is false.
    /// Every file is rewritten in memory first, and nothing is written when any file is refused or any
    /// problem is found, so a failed run never leaves the folder half regenerated.
    /// <paramref name="recipes"/> is the registry the game server builds, which supplies each recipe's
    /// skill, category, level and components; it must come from the same sources as the folder.
    /// Recipes the profile lists as derived are skipped, since they are built from another recipe.
    /// </summary>
    public static RecipeFolderUpdate Update(
        string folder,
        IRecipeParameterProfile profile,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
        bool write)
    {
        var problems = new List<string>();
        var plans = new Dictionary<string, RecipeParameterPlan>(StringComparer.Ordinal);
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var changed = new List<string>();
        var pending = new List<(string Path, bool HasBom, string Text)>();
        int chainEdits = 0, rowEdits = 0, plumbingEdits = 0, files = 0;

        RecipeParameterPlan PlanFor(string name)
        {
            if (plans.TryGetValue(name, out var cached))
                return cached;

            var plan = BuildPlan(name, profile, recipes, unknown);
            plans[name] = plan;
            return plan;
        }

        foreach (var path in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            files++;
            var relative = Path.GetRelativePath(folder, path).Replace('\\', '/');
            var bytes = File.ReadAllBytes(path);
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = new UTF8Encoding(false, true).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

            var rewrite = RecipeSourceWriter.Rewrite(relative, text, PlanFor);
            visited.UnionWith(rewrite.RecipesVisited);
            chainEdits += rewrite.ChainEdits;
            rowEdits += rewrite.RowEdits;
            plumbingEdits += rewrite.PlumbingEdits;
            if (!rewrite.Changed)
                continue;

            changed.Add(relative);
            pending.Add((path, hasBom, rewrite.Text));
        }

        foreach (var (type, detail) in recipes.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            if (detail.Skill == profile.Skill &&
                !profile.DerivedRecipes.ContainsKey(type) &&
                profile.AppliesTo(RecipeParameterContext.From(type, detail)) &&
                !visited.Contains(type.ToString()))
            {
                problems.Add($"{type} is registered but its Create call is not under {folder}");
            }
        }

        problems.AddRange(unknown.Select(name => $"RecipeType.{name} appears in the source but not in the built game server; rebuild before running"));

        var applied = write && problems.Count == 0 && pending.Count > 0;
        if (applied)
        {
            foreach (var (path, hasBom, text) in pending)
            {
                var encoded = new UTF8Encoding(false).GetBytes(text);
                File.WriteAllBytes(path, hasBom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(encoded).ToArray() : encoded);
            }
        }

        return new RecipeFolderUpdate
        {
            FilesScanned = files,
            Applied = applied,
            ChangedFiles = changed,
            ChainEdits = chainEdits,
            RowEdits = rowEdits,
            PlumbingEdits = plumbingEdits,
            Problems = problems,
        };
    }

    /// <summary>The argument list the profile's generated values take in source, such as <c>center: 50, width: 20</c>.</summary>
    public static string FormatArguments(IRecipeParameterProfile profile, IReadOnlyDictionary<string, double> values)
    {
        return string.Join(", ", profile.Definitions.Select(definition => $"{definition.Name}: {definition.FormatLiteral(values[definition.Name])}"));
    }

    private static RecipeParameterPlan BuildPlan(
        string name,
        IRecipeParameterProfile profile,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
        ISet<string> unknown)
    {
        if (!Enum.TryParse(name, false, out RecipeType type) || !Enum.IsDefined(typeof(RecipeType), type))
        {
            unknown.Add(name);
            return RecipeParameterPlan.Leave;
        }

        if (!recipes.TryGetValue(type, out var detail) || detail.Skill != profile.Skill)
            return RecipeParameterPlan.Leave;

        if (profile.DerivedRecipes.ContainsKey(type))
            return RecipeParameterPlan.Leave;

        var context = RecipeParameterContext.From(type, detail);
        if (!profile.AppliesTo(context))
            return RecipeParameterPlan.Remove(profile.ParametersType.Name);

        var generated = RecipeParameterGenerator.Generate(profile, context);
        return RecipeParameterPlan.Write(profile.ParametersType.Name, FormatArguments(profile, generated.Values));
    }
}
