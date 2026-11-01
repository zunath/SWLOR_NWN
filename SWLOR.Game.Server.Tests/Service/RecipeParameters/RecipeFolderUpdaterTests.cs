using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.RecipeParameters;
using static SWLOR.Game.Server.Tests.Service.RecipeParameters.RecipeSourceFixtures;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeFolderUpdaterTests
{
    private static readonly byte[] Bom = { 0xEF, 0xBB, 0xBF };

    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = Directory.CreateTempSubdirectory("recipe-folder-updater-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_folder, true);
    }

    private static Dictionary<RecipeType, RecipeDetail> Registry()
    {
        return new Dictionary<RecipeType, RecipeDetail>
        {
            [RecipeType.AdventTunic] = new()
            {
                Skill = SkillType.Smithery,
                Category = RecipeCategoryType.Tunic,
                Level = 10,
                Components = { ["ref_veldite"] = 5 },
            },
            [RecipeType.AmateurTunic] = new() { Skill = SkillType.Smithery, Level = 1, IsActive = false },
            [RecipeType.CombatTunic] = new() { Skill = SkillType.Engineering, Level = 3 },
        };
    }

    private string WriteFixture(string name, string content, bool bom)
    {
        var path = Path.Combine(_folder, name);
        var encoded = new System.Text.UTF8Encoding(false).GetBytes(content);
        File.WriteAllBytes(path, bom ? Bom.Concat(encoded).ToArray() : encoded);
        return path;
    }

    [Test]
    public void Update_WritesTheGeneratedArgumentsAndKeepsTheBomAndLineEndings()
    {
        var path = WriteFixture("Direct.cs", Crlf(DirectChains), true);
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        var registry = Registry();
        var expected = RecipeFolderUpdater.FormatArguments(
            profile,
            RecipeParameterGenerator.Generate(profile, RecipeParameterContext.From(RecipeType.AdventTunic, registry[RecipeType.AdventTunic])).Values);

        var update = RecipeFolderUpdater.Update(_folder, profile, registry, true);

        var bytes = File.ReadAllBytes(path);
        var text = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.That(update.Problems, Is.Empty);
        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { "Direct.cs" }));
        Assert.That(bytes.Take(3), Is.EqualTo(Bom));
        Assert.That(text, Does.Contain($".Component(\"ref_veldite\", 5)\r\n                .TestForgeParameters({expected}); // trailing note"));
        Assert.That(text, Does.Contain("RecipeType.AmateurTunic, SkillType.Smithery).Level(1);"), "inactive recipes take no parameters");
        Assert.That(text, Does.Contain("RecipeType.CombatTunic, SkillType.Smithery);"), "other skills are left alone");
        Assert.That(expected, Does.StartWith("center: ").And.Contain("style: TestForgeStyle."));
    }

    [Test]
    public void Update_ASecondRunChangesNothing()
    {
        WriteFixture("Direct.cs", Lf(DirectChains), false);
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        RecipeFolderUpdater.Update(_folder, profile, Registry(), true);
        var after = File.ReadAllBytes(Path.Combine(_folder, "Direct.cs"));

        var second = RecipeFolderUpdater.Update(_folder, profile, Registry(), true);

        Assert.That(second.ChangedFiles, Is.Empty);
        Assert.That(File.ReadAllBytes(Path.Combine(_folder, "Direct.cs")), Is.EqualTo(after));
    }

    [Test]
    public void Update_InCheckModeReportsWithoutWriting()
    {
        var path = WriteFixture("Direct.cs", Lf(DirectChains), false);
        var before = File.ReadAllBytes(path);

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), Registry(), false);

        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { "Direct.cs" }));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
    }

    [Test]
    public void Update_RemovesParametersFromARecipeThatIsNoLongerCovered()
    {
        WriteFixture("Direct.cs", Lf(DirectChains), false);
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        var registry = Registry();
        registry[RecipeType.AmateurTunic].IsActive = true;
        RecipeFolderUpdater.Update(_folder, profile, registry, true);
        Assert.That(File.ReadAllText(Path.Combine(_folder, "Direct.cs")), Does.Contain("Level(1).TestForgeParameters("));

        registry[RecipeType.AmateurTunic].IsActive = false;
        var update = RecipeFolderUpdater.Update(_folder, profile, registry, true);

        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { "Direct.cs" }));
        Assert.That(File.ReadAllText(Path.Combine(_folder, "Direct.cs")), Does.Not.Contain("Level(1).TestForgeParameters("));
    }

    [Test]
    public void Update_SkipsDerivedRecipesAndDoesNotExpectTheirCreateCall()
    {
        WriteFixture("Direct.cs", Lf(DirectChains), false);
        var registry = Registry();
        registry[RecipeType.SpiritmasterTunic] = new RecipeDetail { Skill = SkillType.Smithery, Level = 2 };
        var derived = new Dictionary<RecipeType, RecipeType> { [RecipeType.SpiritmasterTunic] = RecipeType.AdventTunic };

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false, derived), registry, false);

        Assert.That(update.Problems, Is.Empty);
        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { "Direct.cs" }));
    }

    [Test]
    public void Update_WritesNothingWhenALaterFileIsRefused()
    {
        var good = WriteFixture("A.cs", Lf(DirectChains), false);
        var bad = WriteFixture("B.cs", Lf(DirectChains).Replace("_builder.Create(RecipeType.CombatTunic, SkillType.Smithery);", "_builder.Create(Pick(), SkillType.Smithery);"), false);
        var goodBefore = File.ReadAllBytes(good);
        var badBefore = File.ReadAllBytes(bad);

        Assert.Throws<RecipeSourceWriterException>(
            () => RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), Registry(), true));

        Assert.That(File.ReadAllBytes(good), Is.EqualTo(goodBefore));
        Assert.That(File.ReadAllBytes(bad), Is.EqualTo(badBefore));
    }

    [Test]
    public void Update_WritesNothingWhileAProblemRemains()
    {
        var path = WriteFixture("Direct.cs", Lf(DirectChains), false);
        var before = File.ReadAllBytes(path);
        var registry = Registry();
        registry[RecipeType.SpiritmasterTunic] = new RecipeDetail { Skill = SkillType.Smithery, Level = 2 };

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), registry, true);

        Assert.That(update.Problems, Is.Not.Empty);
        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { "Direct.cs" }));
        Assert.That(update.Applied, Is.False);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
    }

    [Test]
    public void Update_ReportsACoveredRecipeWhoseCreateCallIsNotInTheFolder()
    {
        WriteFixture("Direct.cs", Lf(DirectChains), false);
        var registry = Registry();
        registry[RecipeType.SpiritmasterTunic] = new RecipeDetail { Skill = SkillType.Smithery, Level = 2 };

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), registry, false);

        Assert.That(update.Problems, Has.Count.EqualTo(1));
        Assert.That(update.Problems[0], Does.Contain("SpiritmasterTunic"));
    }

    [Test]
    public void Update_ReportsSourceThatNamesARecipeTheBuildDoesNotKnow()
    {
        WriteFixture("Stale.cs", Lf(DirectChains).Replace("RecipeType.CombatTunic", "RecipeType.NotARealRecipe"), false);

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), Registry(), false);

        Assert.That(update.Problems, Has.Some.Contain("RecipeType.NotARealRecipe"));
    }

    private const string CatalogLoop = @"using System.Collections.Generic;

namespace Fixture
{
    public class Catalog : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            foreach (var row in GeneratedCatalog.Rows)
            {
                var recipe = _builder.Create(row.RecipeType, SkillType.Smithery)
                    .Level(row.Level)
                    .MinigameParameters(RecipeParameterTable.For(row.RecipeType));
            }

            return _builder.Build();
        }
    }
}
";

    private const string Table = @"namespace Fixture
{
    public static class RecipeParameterTable
    {
        private static readonly Dictionary<RecipeType, RecipeMinigameParameters> _parameters = new()
        {
            [RecipeType.CombatTunic] = new OtherParameters(size: 3),
            [RecipeType.SpiritmasterTunic] = new TestForgeParameters(center: 1),
        };
    }
}
";

    [Test]
    public void Update_WritesALoopOverAGeneratedCatalogIntoTheParameterTable_AndKeepsOtherProfilesEntries()
    {
        WriteFixture("Catalog.cs", Crlf(CatalogLoop), false);
        var tablePath = WriteFixture(RecipeParameterTableFile.FileName, Crlf(Table), true);
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        var registry = Registry();
        var expected = RecipeFolderUpdater.FormatArguments(
            profile,
            RecipeParameterGenerator.Generate(profile, RecipeParameterContext.From(RecipeType.AdventTunic, registry[RecipeType.AdventTunic])).Values);

        var update = RecipeFolderUpdater.Update(_folder, profile, registry, true);

        var bytes = File.ReadAllBytes(tablePath);
        var text = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.That(update.Problems, Is.Empty);
        Assert.That(update.ChangedFiles, Is.EqualTo(new[] { RecipeParameterTableFile.FileName }), "the catalog loop itself is left alone");
        Assert.That(bytes.Take(3), Is.EqualTo(Bom));
        Assert.That(text, Does.Contain($"            [RecipeType.AdventTunic] = new TestForgeParameters({expected}),\r\n"));
        Assert.That(text, Does.Contain("[RecipeType.CombatTunic] = new OtherParameters(size: 3),"), "another profile's entry stays");
        Assert.That(text, Does.Not.Contain("SpiritmasterTunic"), "a recipe this profile no longer covers leaves the table");

        var second = RecipeFolderUpdater.Update(_folder, profile, registry, true);
        Assert.That(second.ChangedFiles, Is.Empty, "a second run changes nothing");
    }

    [Test]
    public void Update_ATableDrivenLoopWithoutATable_IsStillReportedAsMissing()
    {
        WriteFixture("Catalog.cs", Lf(CatalogLoop), false);

        var update = RecipeFolderUpdater.Update(_folder, new TestForgeProfile(SkillType.Smithery, false), Registry(), false);

        Assert.That(update.Problems, Has.Some.Contain("AdventTunic"));
    }
}
