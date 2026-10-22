using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// Runs the writer over a scratch copy of the real recipe definitions with the test-only profile.
/// </summary>
public class RecipeSourceWriterRealFolderTests
{
    private static readonly string[] LoopBuiltFiles =
    {
        "CookingRecipeDefinition/CookingRecipes.cs",
        "CookingRecipeDefinition/SlicingCacheCookingRecipes.cs",
        "EngineeringRecipeDefinition/DroidEnhancementRecipes.cs",
        "EngineeringRecipeDefinition/DroidInstructionRecipes.cs",
        "EngineeringRecipeDefinition/TraceFuseRecipes.cs",
        "EspionageRecipeDefinition/ConcentratedVenomRecipes.cs",
        "FabricationRecipeDefinition/SlicingTerminalFurnitureRecipes.cs",
        "SmitheryRecipeDefinition/SlicingCacheSmitheryRecipes.cs",
    };

    private static readonly Regex BuilderCreate = new(@"\b_?builder\.Create\(", RegexOptions.Compiled);

    private static readonly Regex Plumbing = new(
        @"\s*\.MinigameParameters\((?:parameters|recipe\.Parameters)\)|,\s*RecipeMinigameParameters (?:parameters|Parameters) = null",
        RegexOptions.Compiled);

    private static readonly Regex DerivedCopy = new(@"\s*MinigameParameters = source\.MinigameParameters,", RegexOptions.Compiled);

    private string _scratch;

    [SetUp]
    public void SetUp()
    {
        _scratch = Directory.CreateTempSubdirectory("recipe-definition-copy-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_scratch, true);
    }

    [Test]
    public void EveryRecipeFile_TakesParametersAndGivesThemBackWithNoOtherChange()
    {
        var recipes = RecipeDefinitionLoader.LoadGame();
        var sourceFolder = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "RecipeDefinition");
        var originals = CopyPristineFolder(sourceFolder, _scratch);
        Assert.That(originals.Values.Any(text => text.Contains("TestForgeParameters")), Is.False);

        var derived = TwinPistolSources(recipes);
        var profiles = recipes.Values.Select(recipe => recipe.Skill).Distinct()
            .Select(skill => new TestForgeProfile(skill, false, derived))
            .ToList();
        var changed = new HashSet<string>();
        var problems = new List<string>();
        foreach (var profile in profiles)
        {
            var update = RecipeFolderUpdater.Update(_scratch, profile, recipes, true);
            problems.AddRange(update.Problems);
            changed.UnionWith(update.ChangedFiles);
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));

        var sitesWithRecipes = originals.Where(pair => BuilderCreate.IsMatch(pair.Value)).Select(pair => pair.Key).ToList();
        Assert.That(originals.Keys.Except(sitesWithRecipes), Is.EqualTo(new[] { "SmitheryRecipeDefinition/TwinPistolRecipes.cs" }),
            "TwinPistolRecipes is the only file that builds recipes without a RecipeBuilder");
        Assert.That(changed, Is.EquivalentTo(sitesWithRecipes), "every file that creates recipes takes parameters");
        Assert.That(LoopBuiltFiles.Except(changed), Is.Empty);

        var rewritten = ReadFolder(_scratch);
        var generatedCalls = rewritten.Values.Sum(text => Regex.Matches(text, @"TestForgeParameters\(").Count);
        Assert.That(derived, Has.Count.EqualTo(11));
        Assert.That(generatedCalls, Is.EqualTo(recipes.Values.Count(recipe => recipe.IsActive) - derived.Count),
            "one generated call or data row per active recipe that is not derived from another");

        foreach (var profile in profiles)
        {
            Assert.That(RecipeFolderUpdater.Update(_scratch, profile, recipes, false).ChangedFiles, Is.Empty,
                $"a second run for {profile.Skill} changes nothing");
        }

        foreach (var (path, text) in rewritten)
        {
            var removed = RecipeSourceWriter.Rewrite(path, text, _ => RecipeParameterPlan.Remove("TestForgeParameters"));
            var stripped = Plumbing.Replace(removed.Text, string.Empty);
            Assert.That(stripped, Is.EqualTo(originals[path]), $"{path} differs from the original beyond the parameters");
        }
    }

    [Test]
    public void LoopBuiltRecipes_CarryTheirValuesInTheHelperDataRows()
    {
        var recipes = RecipeDefinitionLoader.LoadGame();
        var sourceFolder = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "RecipeDefinition");
        CopyPristineFolder(sourceFolder, _scratch);
        var derived = TwinPistolSources(recipes);
        foreach (var skill in recipes.Values.Select(recipe => recipe.Skill).Distinct())
        {
            RecipeFolderUpdater.Update(_scratch, new TestForgeProfile(skill, false, derived), recipes, true);
        }

        var traceFuses = File.ReadAllText(Path.Combine(_scratch, "EngineeringRecipeDefinition", "TraceFuseRecipes.cs"));
        var instructions = File.ReadAllText(Path.Combine(_scratch, "EngineeringRecipeDefinition", "DroidInstructionRecipes.cs"));

        Assert.That(traceFuses, Does.Contain("Add(RecipeType.CopperTraceFuse, \"trace_fuse_1\", 5, \"elec_ruined\", \"fiberp_ruined\", parameters: new TestForgeParameters("));
        Assert.That(traceFuses, Does.Contain("RecipeMinigameParameters parameters = null"));
        Assert.That(instructions, Does.Contain("new DroidInstructionRecipe(RecipeType.InstructionPowerCell3, \"id_powercell3\", Parameters: new TestForgeParameters("));
        Assert.That(instructions, Does.Contain("RecipeMinigameParameters Parameters = null"));
        Assert.That(instructions, Does.Contain(".MinigameParameters(recipe.Parameters)"));
    }

    /// <summary>Twin pistols copy their single pistol's detail in TwinPistolRecipes instead of using a builder chain.</summary>
    private static Dictionary<RecipeType, RecipeType> TwinPistolSources(Dictionary<RecipeType, RecipeDetail> recipes)
    {
        return recipes
            .Where(pair => pair.Value.Category == RecipeCategoryType.TwinPistol)
            .ToDictionary(pair => pair.Key, pair => Enum.Parse<RecipeType>(pair.Key.ToString().Replace("TwinPistol", "Pistol")));
    }

    /// <summary>
    /// Copies the folder as it was before any minigame wrote parameters: the parameter calls of every game profile, the helper
    /// plumbing they use and the derived recipes' copy of their source's parameters are taken out of the copy.
    /// </summary>
    private static Dictionary<string, string> CopyPristineFolder(string source, string destination)
    {
        var pristine = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (relative, text) in CopyFolder(source, destination))
        {
            var current = text;
            foreach (var profile in RecipeParameterProfiles.Game)
            {
                var builderMethod = profile.ParametersType.Name;
                current = RecipeSourceWriter.Rewrite(relative, current, _ => RecipeParameterPlan.Remove(builderMethod)).Text;
            }

            current = DerivedCopy.Replace(Plumbing.Replace(current, string.Empty), string.Empty);
            File.WriteAllBytes(Path.Combine(destination, relative), new UTF8Encoding(false).GetBytes(current));
            pristine[relative] = current;
        }

        return pristine;
    }

    private static Dictionary<string, string> CopyFolder(string source, string destination)
    {
        var copies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, path).Replace('\\', '/');
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
            copies[relative] = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
        }

        return copies;
    }

    private static Dictionary<string, string> ReadFolder(string folder)
    {
        return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).ToDictionary(
            path => Path.GetRelativePath(folder, path).Replace('\\', '/'),
            path => new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path)),
            StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
