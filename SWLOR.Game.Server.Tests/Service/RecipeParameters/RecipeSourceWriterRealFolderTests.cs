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

    /// <summary>The space revamp's loops over generated catalogs, which read their values from the parameter table instead.</summary>
    private static readonly string[] TableDrivenFiles =
    {
        "EngineeringRecipeDefinition/RevampStarshipRecipes.cs",
        "EngineeringRecipeDefinition/ShipAmmoVariantRecipes.cs",
        "ShipComponentRecipes.cs",
    };

    /// <summary>Files whose every recipe the space revamp retired (inactive), so no profile covers them.</summary>
    private static readonly string[] RetiredFiles =
    {
        "EngineeringRecipeDefinition/CapitalModuleRecipes.cs",
        "EngineeringRecipeDefinition/ModuleRecipes.cs",
        "EngineeringRecipeDefinition/StarshipRecipes.cs",
    };

    private static readonly Regex BuilderCreate = new(@"\b_?builder\.Create\(", RegexOptions.Compiled);

    private static readonly Regex Plumbing = new(
        @"\s*\.MinigameParameters\((?:parameters|recipe\.Parameters)\)|,\s*RecipeMinigameParameters (?:parameters|Parameters) = null",
        RegexOptions.Compiled);

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
        var originals = CopyFolder(sourceFolder, _scratch);
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
        Assert.That(originals.Keys.Except(sitesWithRecipes),
            Is.EquivalentTo(new[] { RecipeParameterTableFile.FileName, "SmitheryRecipeDefinition/TwinPistolRecipes.cs" }),
            "TwinPistolRecipes is the only file that builds recipes without a RecipeBuilder, beside the parameter table");
        Assert.That(changed, Is.EquivalentTo(sitesWithRecipes.Except(TableDrivenFiles).Except(RetiredFiles).Append(RecipeParameterTableFile.FileName)),
            "every file that creates recipes takes parameters, and the catalog loops take theirs through the table");
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

        foreach (var (path, text) in rewritten.Where(pair => pair.Key != RecipeParameterTableFile.FileName))
        {
            Assert.That(
                WithoutParameters(path, text),
                Is.EqualTo(WithoutParameters(path, originals[path])),
                $"{path} differs from the original beyond the parameters");
        }
    }

    [Test]
    public void LoopBuiltRecipes_CarryTheirValuesInTheHelperDataRows()
    {
        var recipes = RecipeDefinitionLoader.LoadGame();
        var sourceFolder = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "RecipeDefinition");
        CopyFolder(sourceFolder, _scratch);
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

    /// <summary>Every parameter type a minigame has added to the game, which recipe files may already carry.</summary>
    private static readonly string[] GameParameterTypes = typeof(RecipeMinigameParameters).Assembly.GetTypes()
        .Where(type => typeof(RecipeMinigameParameters).IsAssignableFrom(type) && !type.IsAbstract)
        .Select(type => type.Name)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();

    /// <summary>The source with every minigame's parameters and the helper plumbing for them taken out.</summary>
    private static string WithoutParameters(string path, string text)
    {
        foreach (var name in GameParameterTypes.Append("TestForgeParameters"))
        {
            text = RecipeSourceWriter.Rewrite(path, text, _ => RecipeParameterPlan.Remove(name)).Text;
        }

        return Plumbing.Replace(text, string.Empty);
    }

    /// <summary>Twin pistols copy their single pistol's detail in TwinPistolRecipes instead of using a builder chain.</summary>
    private static Dictionary<RecipeType, RecipeType> TwinPistolSources(Dictionary<RecipeType, RecipeDetail> recipes)
    {
        return recipes
            .Where(pair => pair.Value.Category == RecipeCategoryType.TwinPistol)
            .ToDictionary(pair => pair.Key, pair => Enum.Parse<RecipeType>(pair.Key.ToString().Replace("TwinPistol", "Pistol")));
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
