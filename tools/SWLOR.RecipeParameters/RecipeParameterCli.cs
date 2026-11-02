using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.RecipeParameters;

/// <summary>
/// The command line behind <c>dotnet run --project tools/SWLOR.RecipeParameters -- ...</c>.
/// </summary>
public static class RecipeParameterCli
{
    private const string Usage =
        "Usage:\n" +
        "  list                                   List the registered minigame profiles.\n" +
        "  write <Key> [--source <folder>] [--check]\n" +
        "                                         Write the profile's generated parameters into the recipe definitions.\n" +
        "                                         --check changes nothing and exits 1 when a file would change.\n" +
        "  export <out.json> [<Key> ...]          Write the Bible column data that tools/UpdateRecipeParameterBible.py reads.";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        return Run(args, output, error, RecipeParameterProfiles.Game, null);
    }

    /// <summary>
    /// Runs a command against the given profiles and recipe registry; a null registry loads the game server's recipes when needed.
    /// </summary>
    public static int Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        IReadOnlyList<IRecipeParameterProfile> profiles,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return 2;
        }

        switch (args[0])
        {
            case "list":
                foreach (var profile in profiles)
                {
                    output.WriteLine($"{profile.Key}\t{profile.Skill}\t{profile.ParametersType.Name}");
                }

                if (profiles.Count == 0)
                    output.WriteLine("No recipe parameter profiles are registered.");
                return 0;
            case "write":
                return Write(args.Skip(1).ToArray(), profiles, recipes, output, error);
            case "export":
                return Export(args.Skip(1).ToArray(), profiles, recipes, output, error);
            default:
                error.WriteLine(Usage);
                return 2;
        }
    }

    private static int Write(
        string[] args,
        IReadOnlyList<IRecipeParameterProfile> profiles,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
        TextWriter output,
        TextWriter error)
    {
        string key = null;
        string source = null;
        var check = false;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--check")
                check = true;
            else if (args[index] == "--source" && index + 1 < args.Length)
                source = args[++index];
            else if (key == null && !args[index].StartsWith("--"))
                key = args[index];
            else
            {
                error.WriteLine(Usage);
                return 2;
            }
        }

        var profile = key == null ? null : Find(profiles, key);
        if (profile == null)
        {
            error.WriteLine(key == null ? Usage : UnknownProfile(key, profiles));
            return 2;
        }

        var problems = RecipeParameterAudit.ValidateProfile(profile);
        if (problems.Count > 0)
        {
            error.WriteLine($"Profile {profile.Key} is invalid: {string.Join("; ", problems)}");
            return 2;
        }

        var folder = source ?? Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "RecipeDefinition");
        var update = RecipeFolderUpdater.Update(folder, profile, recipes ?? RecipeDefinitionLoader.LoadGame(), !check);

        var verb = check || !update.Applied ? "Would change" : "Changed";
        output.WriteLine(
            $"{verb} {update.ChangedFiles.Count} of {update.FilesScanned} files " +
            $"({update.ChainEdits} recipe chains, {update.RowEdits} data rows, {update.PlumbingEdits} helper signatures).");
        foreach (var file in update.ChangedFiles)
        {
            output.WriteLine($"  {file}");
        }

        foreach (var problem in update.Problems)
        {
            error.WriteLine($"Problem: {problem}");
        }

        if (update.Problems.Count > 0 && !check && update.ChangedFiles.Count > 0)
            error.WriteLine("Nothing was written because of the problems above.");

        return update.Problems.Count > 0 || (check && update.ChangedFiles.Count > 0) ? 1 : 0;
    }

    private static int Export(
        string[] args,
        IReadOnlyList<IRecipeParameterProfile> profiles,
        IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
        TextWriter output,
        TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return 2;
        }

        var unknown = args.Skip(1).Where(key => Find(profiles, key) == null).ToList();
        if (unknown.Count > 0)
        {
            error.WriteLine(string.Join("\n", unknown.Select(key => UnknownProfile(key, profiles))));
            return 2;
        }

        var selected = (args.Length == 1 ? profiles : args.Skip(1).Select(key => Find(profiles, key)).Distinct()).ToList();
        if (selected.Count == 0)
        {
            error.WriteLine("No recipe parameter profiles are registered.");
            return 2;
        }

        var json = BibleExport.Build(selected, recipes ?? RecipeDefinitionLoader.LoadGame(), out var problems);
        foreach (var problem in problems)
        {
            error.WriteLine($"Problem: {problem}");
        }

        if (problems.Count > 0)
            return 1;

        File.WriteAllText(args[0], json);
        output.WriteLine($"Wrote Bible columns for {selected.Count} profile(s) to {args[0]}.");
        return 0;
    }

    private static IRecipeParameterProfile Find(IReadOnlyList<IRecipeParameterProfile> profiles, string key)
    {
        return profiles.FirstOrDefault(profile => string.Equals(profile.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static string UnknownProfile(string key, IReadOnlyList<IRecipeParameterProfile> profiles)
    {
        return $"No profile named '{key}'. Registered: {(profiles.Count == 0 ? "none" : string.Join(", ", profiles.Select(p => p.Key)))}";
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Run from inside the SWLOR_NWN repository or pass --source.");
    }
}
