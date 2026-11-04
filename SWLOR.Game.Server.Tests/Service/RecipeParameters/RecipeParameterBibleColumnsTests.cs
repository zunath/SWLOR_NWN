using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Feature;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterBibleColumnsTests
{
    private static readonly string[] Tabs = { "Smithery Recipes", "Engineering Recipes", "Cooking Recipes", "Fabrication Recipes" };
    // CombatBoots is one of the recipes whose other Bible fields are excluded while a design decision is pending.
    private const RecipeType ExcludedRecipe = RecipeType.CombatBoots;

    private static readonly RecipeType[] Rows =
    {
        RecipeType.BattlemasterBreastplate,
        RecipeType.SpiritmasterTunic,
        TestRecipeRegistry.OtherSkillRecipe,
        TestRecipeRegistry.InactiveRecipe,
        ExcludedRecipe,
    };

    private TestForgeProfile _profile;
    private Dictionary<RecipeType, RecipeDetail> _registry;

    [SetUp]
    public void SetUp()
    {
        UseProfile(new TestForgeProfile());
    }

    private void UseProfile(TestForgeProfile profile)
    {
        _profile = profile;
        _registry = TestRecipeRegistry.Create(profile);
        if (!_registry.ContainsKey(ExcludedRecipe))
        {
            var detail = new RecipeDetail { Skill = profile.Skill, Category = RecipeCategoryType.Boots, Level = 12 };
            detail.Components["comp_0"] = 2;
            detail.MinigameParameters = RecipeParameterGenerator.Generate(profile, RecipeParameterContext.From(ExcludedRecipe, detail)).Parameters;
            _registry[ExcludedRecipe] = detail;
        }
    }

    private static string Text(string reference, string text)
    {
        return $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{text}</t></is></c>";
    }

    private static string Number(string reference, string text)
    {
        return $"<c r=\"{reference}\"><v>{text}</v></c>";
    }

    /// <summary>Builds a workbook whose Smithery tab holds the profile's columns, filled as the Bible script writes them.</summary>
    private ZipArchive Workbook(
        Func<int, RecipeType, string, string> value = null,
        Func<IReadOnlyList<string>, IReadOnlyList<string>> headers = null)
    {
        var allHeaders = RecipeParameterBibleColumns.Headers(_profile);
        var shown = headers?.Invoke(allHeaders) ?? allHeaders;
        var rows = new StringBuilder();
        rows.Append("<row r=\"1\">" + Text("A1", "Skill") + Text("B1", "Recipe\nEnum"));
        for (var index = 0; index < shown.Count; index++)
        {
            rows.Append(Text($"{(char)('C' + index)}1", shown[index]));
        }

        rows.Append("</row>");

        for (var rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
        {
            var type = Rows[rowIndex];
            var detail = _registry[type];
            var rowNumber = rowIndex + 2;
            rows.Append($"<row r=\"{rowNumber}\">" + Text($"A{rowNumber}", "Smithery") + Text($"B{rowNumber}", type.ToString()));

            var cells = detail.MinigameParameters != null && detail.Skill == _profile.Skill
                ? RecipeParameterBibleColumns.Cells(
                    _profile,
                    detail.MinigameParameters,
                    RecipeParameterBibleColumns.OverrideReasonFor(_profile, type, _registry))
                : allHeaders.Select(header => new RecipeParameterBibleCell(header, string.Empty, false)).ToList();

            foreach (var cell in cells)
            {
                var column = shown.ToList().IndexOf(cell.Header);
                if (column < 0)
                    continue;

                var reference = $"{(char)('C' + column)}{rowNumber}";
                var text = value?.Invoke(rowNumber, type, cell.Header) ?? cell.Text;
                if (text.Length == 0)
                    continue;

                rows.Append(cell.IsNumber ? Number(reference, text) : Text(reference, text));
            }

            rows.Append("</row>");
        }

        return Build(rows.ToString());
    }

    private static ZipArchive Build(string smitheryRows)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            void Write(string name, string content)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }

            Write("xl/workbook.xml",
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" +
                string.Join(string.Empty, Tabs.Select((tab, index) => $"<sheet name=\"{tab}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>")) +
                "</sheets></workbook>");
            Write("xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                string.Join(string.Empty, Tabs.Select((tab, index) => $"<Relationship Id=\"rId{index + 1}\" Target=\"worksheets/sheet{index + 1}.xml\"/>")) +
                "</Relationships>");

            for (var index = 0; index < Tabs.Length; index++)
            {
                var rows = index == 0
                    ? smitheryRows
                    : "<row r=\"1\">" + Text("A1", "Skill") + Text("B1", "Recipe Enum") + "</row>";
                Write($"xl/worksheets/sheet{index + 1}.xml",
                    "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + rows + "</sheetData></worksheet>");
            }
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    private List<string> Mismatches(ZipArchive workbook)
    {
        return CombatUpgradeBibleRecipeParityTests.FindParameterColumnMismatches(workbook, new[] { _profile }, _registry);
    }

    [Test]
    public void ColumnsWrittenFromCode_MatchCode()
    {
        using var workbook = Workbook();

        Assert.That(Mismatches(workbook), Is.Empty);
    }

    [Test]
    public void ANumericCell_IsComparedByValueNotByText()
    {
        var cooling = ((TestForgeParameters)_registry[RecipeType.SpiritmasterTunic].MinigameParameters).Cooling;
        using var padded = Workbook((row, _, header) => header == "TestForge Cooling" && row == 3
            ? cooling.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
            : null);

        Assert.That(Mismatches(padded), Is.Empty);
    }

    [Test]
    public void RecipesWithAPendingDesignDecision_StillGetTheirParameterColumnsChecked()
    {
        using var workbook = Workbook((_, type, header) => type == ExcludedRecipe && header == "TestForge Width" ? "9" : null);

        var mismatches = Mismatches(workbook);

        Assert.That(mismatches, Has.Count.EqualTo(1));
        Assert.That(mismatches[0], Does.Contain($"{ExcludedRecipe}.TestForge Width: Bible='9'"));
    }

    [Test]
    public void ADerivedRecipe_ShowsItsSourcesOverrideReasonAndValues()
    {
        UseProfile(new TestForgeProfile(
            SkillType.Smithery,
            true,
            new Dictionary<RecipeType, RecipeType> { [RecipeType.SpiritmasterTunic] = RecipeType.BattlemasterBreastplate }));
        using var workbook = Workbook();

        Assert.That(Mismatches(workbook), Is.Empty);
        Assert.That(RecipeParameterBibleColumns.OverrideReasonFor(_profile, RecipeType.SpiritmasterTunic, _registry),
            Is.EqualTo("Breastplates are worked hot; Starter set stays forgiving"));

        using var tampered = Workbook((_, type, header) => type == RecipeType.SpiritmasterTunic && header == "TestForge Override" ? "" : null);
        Assert.That(Mismatches(tampered), Has.Count.EqualTo(1));
    }

    [Test]
    public void AValueThatDiffersFromCode_IsReportedWithItsRow()
    {
        using var workbook = Workbook((row, _, header) => header == "TestForge Center" && row == 3 ? "99" : null);

        var mismatches = Mismatches(workbook);

        Assert.That(mismatches, Has.Count.EqualTo(1));
        Assert.That(mismatches[0], Does.Contain("[Smithery Recipes row 3] SpiritmasterTunic.TestForge Center: Bible='99'"));
    }

    [Test]
    public void ARecordedOverrideReason_MustBeInTheBible()
    {
        using var workbook = Workbook((_, type, header) => header == "TestForge Override" && type == RecipeType.BattlemasterBreastplate ? "" : null);

        var mismatches = Mismatches(workbook);

        Assert.That(mismatches, Has.Count.EqualTo(1));
        Assert.That(mismatches[0], Does.Contain("BattlemasterBreastplate.TestForge Override"));
    }

    [Test]
    public void ParametersOnARecipeThatHasNoneInCode_AreReported()
    {
        using var workbook = Workbook((_, type, header) => type == TestRecipeRegistry.OtherSkillRecipe && header == "TestForge Center" ? "50" : null);

        var mismatches = Mismatches(workbook);

        Assert.That(mismatches, Has.Count.EqualTo(1));
        Assert.That(mismatches[0], Does.Contain($"{TestRecipeRegistry.OtherSkillRecipe}.TestForge Center"));
    }

    [Test]
    public void ATabMissingTheColumns_IsReportedWhenItsRecipesCarryParameters()
    {
        using var workbook = Workbook(headers: _ => Array.Empty<string>());

        var mismatches = Mismatches(workbook);

        Assert.That(mismatches, Has.Count.EqualTo(1));
        Assert.That(mismatches[0], Does.Contain("[Smithery Recipes] lacks the TestForge column(s)").And.Contain("carry TestForgeParameters"));
    }

    [Test]
    public void APartialColumnSet_IsReported()
    {
        using var workbook = Workbook(headers: all => all.Take(2).ToList());

        Assert.That(Mismatches(workbook), Has.Some.Contain("lacks the TestForge column(s) 'TestForge Cooling', 'TestForge Style', 'TestForge Override'"));
    }

    [Test]
    public void TabsWithoutColumns_PassWhileNoRecipeCarriesParameters()
    {
        foreach (var detail in _registry.Values)
        {
            detail.MinigameParameters = null;
        }

        using var workbook = Workbook(headers: _ => Array.Empty<string>());

        Assert.That(Mismatches(workbook), Is.Empty);
    }
}
