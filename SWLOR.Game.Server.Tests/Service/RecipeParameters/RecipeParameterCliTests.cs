using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterCliTests
{
    private static (int Exit, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = RecipeParameterCli.Run(args, output, error);
        return (exit, output.ToString(), error.ToString());
    }

    private static (int Exit, string Output, string Error) RunWith(TestForgeProfile profile, params string[] args)
    {
        return RunWith(profile, TestRecipeRegistry.Create(profile), args);
    }

    private static (int Exit, string Output, string Error) RunWith(
        TestForgeProfile profile,
        Dictionary<RecipeType, RecipeDetail> recipes,
        params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = RecipeParameterCli.Run(args, output, error, new[] { profile }, recipes);
        return (exit, output.ToString(), error.ToString());
    }

    [Test]
    public void WithoutACommand_ItPrintsTheUsage()
    {
        var (exit, _, error) = Run();

        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error, Does.Contain("Usage:").And.Contain("write <Key>").And.Contain("export <out.json>"));
    }

    [Test]
    public void List_Succeeds()
    {
        Assert.That(Run("list").Exit, Is.Zero);
    }

    [Test]
    public void Write_RefusesAnUnknownProfile()
    {
        var (exit, _, error) = Run("write", "NoSuchMinigame");

        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error, Does.Contain("No profile named 'NoSuchMinigame'"));
    }

    [Test]
    public void Write_RefusesAMissingProfileAndUnknownOptions()
    {
        Assert.That(Run("write").Exit, Is.EqualTo(2));
        Assert.That(Run("write", "Anything", "--bogus").Exit, Is.EqualTo(2));
    }

    [Test]
    public void Export_NeedsAnOutputPathAndARegisteredProfile()
    {
        Assert.That(Run("export").Exit, Is.EqualTo(2));
        Assert.That(Run("export", Path.Combine(Path.GetTempPath(), "never-written.json"), "NoSuchMinigame").Exit, Is.EqualTo(2));
    }

    [Test]
    public void Export_RefusesAnyUnknownKeyEvenWhenAnotherKeyIsValid()
    {
        var path = Path.Combine(Path.GetTempPath(), "recipe-parameters-" + Guid.NewGuid().ToString("N") + ".json");

        var (exit, _, error) = RunWith(new TestForgeProfile(), "export", path, "TestForge", "Typo");

        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error, Does.Contain("No profile named 'Typo'").And.Contain("Registered: TestForge"));
        Assert.That(File.Exists(path), Is.False);
    }

    [Test]
    public void Export_WritesTheNamedProfilesAndReportsThem()
    {
        var path = Path.Combine(Path.GetTempPath(), "recipe-parameters-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var (exit, output, _) = RunWith(new TestForgeProfile(), "export", path, "testforge");

            Assert.That(exit, Is.Zero);
            Assert.That(output, Does.Contain("1 profile(s)"));
            Assert.That(File.ReadAllText(path), Does.Contain("\"TestForge Center\""));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void List_PrintsEachProfile()
    {
        var (exit, output, _) = RunWith(new TestForgeProfile(), "list");

        Assert.That(exit, Is.Zero);
        Assert.That(output, Does.Contain("TestForge\tSmithery\tTestForgeParameters"));
    }

    [Test]
    public void Write_ChangesTheSourceFolderAndCheckModeThenReportsNothingToDo()
    {
        var folder = Directory.CreateTempSubdirectory("recipe-cli-").FullName;
        try
        {
            var path = Path.Combine(folder, "Direct.cs");
            File.WriteAllText(path, RecipeSourceFixtures.Lf(RecipeSourceFixtures.DirectChains));
            var profile = new TestForgeProfile(SkillType.Smithery, false);
            var recipes = new Dictionary<RecipeType, RecipeDetail>
            {
                [RecipeType.AdventTunic] = new() { Skill = SkillType.Smithery, Category = RecipeCategoryType.Tunic, Level = 10 },
                [RecipeType.AmateurTunic] = new() { Skill = SkillType.Smithery, Level = 1, IsActive = false },
                [RecipeType.CombatTunic] = new() { Skill = SkillType.Engineering, Level = 3 },
            };
            var before = File.ReadAllText(path);

            var check = RunWith(profile, recipes, "write", "TestForge", "--source", folder, "--check");
            Assert.That(check.Exit, Is.EqualTo(1));
            Assert.That(check.Output, Does.Contain("Would change 1 of 1 files"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));

            var write = RunWith(profile, recipes, "write", "TestForge", "--source", folder);
            Assert.That(write.Exit, Is.Zero);
            Assert.That(write.Output, Does.Contain("Changed 1 of 1 files (1 recipe chains, 0 data rows, 0 helper signatures)."));
            Assert.That(File.ReadAllText(path), Does.Contain(".TestForgeParameters(center: "));

            var again = RunWith(profile, recipes, "write", "TestForge", "--source", folder, "--check");
            Assert.That(again.Exit, Is.Zero);
            Assert.That(again.Output, Does.Contain("Would change 0 of 1 files"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    public void AnUnknownCommand_PrintsTheUsage()
    {
        Assert.That(Run("frobnicate").Exit, Is.EqualTo(2));
    }
}
