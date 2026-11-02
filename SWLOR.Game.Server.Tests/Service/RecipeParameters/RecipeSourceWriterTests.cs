using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.RecipeParameters;
using static SWLOR.Game.Server.Tests.Service.RecipeParameters.RecipeSourceFixtures;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeSourceWriterTests
{
    private const string Call = ".TestForgeParameters(center: 50, width: 20)";
    private const string Row = "parameters: new TestForgeParameters(center: 50, width: 20)";

    private static RecipeSourceRewrite Rewrite(string source, Func<string, RecipeParameterPlan> plan)
    {
        return RecipeSourceWriter.Rewrite("Fixture.cs", source, plan);
    }

    [Test]
    public void Chains_GetTheCallAtTheEndWithTheirOwnIndentation()
    {
        var source = Lf(DirectChains);

        var result = Rewrite(source, Writing("AdventTunic", "AmateurTunic", "CombatTunic"));

        var expected = source
            .Replace("5); // trailing note", "5)\n                " + Call + "; // trailing note")
            .Replace("SkillType.Smithery).Level(1);", "SkillType.Smithery).Level(1)" + Call + ";")
            .Replace("RecipeType.CombatTunic, SkillType.Smithery);", "RecipeType.CombatTunic, SkillType.Smithery)\n                " + Call + ";");
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.ChainEdits, Is.EqualTo(3));
        Assert.That(result.RecipesVisited, Is.EquivalentTo(new[] { "AdventTunic", "AmateurTunic", "CombatTunic" }));
        Assert.That(CompileErrors(result.Text), Is.Empty);
    }

    [Test]
    public void Chains_OfRecipesThePlanLeavesAreNotTouched()
    {
        var source = Lf(DirectChains);

        var result = Rewrite(source, Writing("AdventTunic"));

        Assert.That(result.ChainEdits, Is.EqualTo(1));
        Assert.That(result.Text, Does.Contain("RecipeType.AmateurTunic, SkillType.Smithery).Level(1);"));
        Assert.That(result.Text, Does.Contain("RecipeType.CombatTunic, SkillType.Smithery);"));
        Assert.That(result.RecipesVisited, Does.Contain("CombatTunic"));
    }

    [Test]
    public void RewritingTwiceWithTheSamePlanChangesNothing()
    {
        foreach (var fixture in new[] { DirectChains, VoidHelper, ReturningHelperWithChainedRow, RecordLoop })
        {
            var first = Rewrite(Lf(fixture), Writing("AdventTunic", "AmateurTunic", "CombatTunic", "TraceOne", "TraceTwo", "InstructionA", "InstructionB"));
            var second = Rewrite(first.Text, Writing("AdventTunic", "AmateurTunic", "CombatTunic", "TraceOne", "TraceTwo", "InstructionA", "InstructionB"));

            Assert.That(first.Changed, Is.True);
            Assert.That(second.Changed, Is.False);
            Assert.That(second.Text, Is.EqualTo(first.Text));
        }
    }

    [Test]
    public void ARewrittenCallTakesTheNewArgumentsAndNothingElseMoves()
    {
        var source = Lf(DirectChains);
        var first = Rewrite(source, Writing("AdventTunic"));

        var second = Rewrite(first.Text, name => name == "AdventTunic"
            ? RecipeParameterPlan.Write("TestForgeParameters", "center: 60, width: 25")
            : RecipeParameterPlan.Leave);

        Assert.That(second.Text, Is.EqualTo(first.Text.Replace("center: 50, width: 20", "center: 60, width: 25")));
        Assert.That(second.ChainEdits, Is.EqualTo(1));
    }

    [Test]
    public void RemovingAWrittenCallRestoresTheOriginalSource()
    {
        foreach (var fixture in new[] { DirectChains, VoidHelper, RecordLoop })
        {
            var source = Lf(fixture);
            var written = Rewrite(source, Writing("AdventTunic", "AmateurTunic", "CombatTunic", "InstructionA", "InstructionB"));

            var removed = Rewrite(written.Text, Removing("AdventTunic", "AmateurTunic", "CombatTunic", "InstructionA", "InstructionB"));

            Assert.That(removed.Text, Does.Not.Contain("new TestForgeParameters").And.Not.Contain(Call));
            Assert.That(CompileErrors(removed.Text), Is.Empty);
            if (fixture == DirectChains)
                Assert.That(removed.Text, Is.EqualTo(source));
        }
    }

    [Test]
    public void CrlfSourceStaysCrlfThroughout()
    {
        var source = Crlf(DirectChains);

        var result = Rewrite(source, Writing("AdventTunic", "CombatTunic"));

        Assert.That(result.Text, Does.Contain("5)\r\n                " + Call));
        Assert.That(Regex.IsMatch(result.Text, "(?<!\r)\n"), Is.False, "no lone line feeds");
        Assert.That(Rewrite(result.Text, Removing("AdventTunic", "CombatTunic")).Text, Is.EqualTo(source));
    }

    [Test]
    public void ASimpleHelperGainsAnOptionalParameterThatItPassesToTheBuilder()
    {
        var source = Lf(VoidHelper);

        var result = Rewrite(source, Writing("AdventTunic", "AmateurTunic"));

        var expected = source
            .Replace("\"ref_veldite\");", "\"ref_veldite\", " + Row + ");")
            .Replace("\"ref_flawed\");", "\"ref_flawed\", " + Row + ");")
            .Replace("string metal)", "string metal, RecipeMinigameParameters parameters = null)")
            .Replace(".Component(metal, 3);", ".Component(metal, 3)\n                .MinigameParameters(parameters);");
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.RowEdits, Is.EqualTo(2));
        Assert.That(result.PlumbingEdits, Is.EqualTo(2));
        Assert.That(CompileErrors(result.Text), Is.Empty);
    }

    [Test]
    public void AHelperIsLeftAloneWhenNoRowNeedsParameters()
    {
        var source = Lf(VoidHelper);

        var result = Rewrite(source, Removing("AdventTunic"));

        Assert.That(result.Changed, Is.False);
        Assert.That(Rewrite(source, Writing("TraceOne")).Changed, Is.False);
    }

    [Test]
    public void RowsTheProfileDoesNotCoverKeepTheirArgumentsWhileTheHelperIsPrepared()
    {
        var source = Lf(VoidHelper);

        var result = Rewrite(source, Writing("AdventTunic"));

        Assert.That(result.Text, Does.Contain("Add(RecipeType.AmateurTunic, 15, \"ref_flawed\");"));
        Assert.That(result.Text, Does.Contain("Add(RecipeType.AdventTunic, 5, \"ref_veldite\", " + Row + ");"));
        Assert.That(CompileErrors(result.Text), Is.Empty);
    }

    [Test]
    public void AHelperThatReturnsTheBuilderKeepsItsMultiLineSignatureAndChainedRows()
    {
        var source = Lf(ReturningHelperWithChainedRow);

        var result = Rewrite(source, Writing("TraceOne", "TraceTwo"));

        Assert.That(result.Text, Does.Contain("            string metal,\n            RecipeMinigameParameters parameters = null)"));
        Assert.That(result.Text, Does.Contain("Add(RecipeType.TraceOne, 5, \"elec_ruined\", " + Row + ");"));
        Assert.That(result.Text, Does.Contain("Add(RecipeType.TraceTwo, 15, \"elec_flawed\", " + Row + ")\n                .EffectDescription"));
        Assert.That(result.Text, Does.Contain(".Component(metal, 3)\n                .MinigameParameters(parameters);"));
        Assert.That(CompileErrors(result.Text), Is.Empty);
    }

    [Test]
    public void ARecordLoopGainsAnOptionalRecordMemberAndEveryRowCarriesItsOwnValues()
    {
        var source = Lf(RecordLoop);

        var result = Rewrite(source, name => RecipeParameterPlan.Write(
            "TestForgeParameters",
            name == "InstructionA" ? "center: 40, width: 10" : "center: 70, width: 30"));

        Assert.That(result.Text, Does.Contain("new InstructionRecipe(RecipeType.InstructionA, \"id_a\", Parameters: new TestForgeParameters(center: 40, width: 10)),"));
        Assert.That(result.Text, Does.Contain("new InstructionRecipe(RecipeType.InstructionB, \"id_b\", Parameters: new TestForgeParameters(center: 70, width: 30)));"));
        Assert.That(result.Text, Does.Contain("record struct InstructionRecipe(RecipeType Type, string Resref, RecipeMinigameParameters Parameters = null);"));
        Assert.That(result.Text, Does.Contain(".Component(recipe.Resref, 1)\n                    .MinigameParameters(recipe.Parameters);"));
        Assert.That(CompileErrors(result.Text), Is.Empty);
    }

    [Test]
    public void AChainWithTheCallTwiceIsRefused()
    {
        var source = Lf(DirectChains).Replace(".Level(10)", ".Level(10)" + Call + Call);

        var exception = Assert.Throws<RecipeSourceWriterException>(() => Rewrite(source, Writing("AdventTunic")));

        Assert.That(exception!.Message, Does.Contain("more than once"));
    }

    [Test]
    public void ACreateCallWithAnUnknownShapeIsRefusedWithItsFileAndLine()
    {
        var source = Lf(DirectChains).Replace("_builder.Create(RecipeType.CombatTunic, SkillType.Smithery);", "_builder.Create(Pick(), SkillType.Smithery);");

        var exception = Assert.Throws<RecipeSourceWriterException>(() => Rewrite(source, Writing("AdventTunic")));

        Assert.That(exception!.Message, Does.Contain("Fixture.cs(").And.Contain("unsupported recipe shape"));
    }

    [Test]
    public void AHelperCalledWithANonLiteralRecipeIsRefused()
    {
        var source = Lf(VoidHelper).Replace("Add(RecipeType.AmateurTunic, 15", "Add(Pick(), 15");

        var exception = Assert.Throws<RecipeSourceWriterException>(() => Rewrite(source, Writing("AdventTunic")));

        Assert.That(exception!.Message, Does.Contain("RecipeType literal"));
    }

    [Test]
    public void AHelperWhoseLastParameterIsAParamsArrayIsRefused()
    {
        var source = Lf(VoidHelper)
            .Replace("string metal)", "params string[] metal)")
            .Replace(".Component(metal, 3)", ".Component(metal[0], 3)");

        var exception = Assert.Throws<RecipeSourceWriterException>(() => Rewrite(source, Writing("AdventTunic")));

        Assert.That(exception!.Message, Does.Contain("params"));
    }

    [Test]
    public void SourceThatDoesNotParseIsRefused()
    {
        var exception = Assert.Throws<RecipeSourceWriterException>(() => Rewrite("class { ", Writing("AdventTunic")));

        Assert.That(exception!.Message, Does.Contain("does not parse"));
    }

    [Test]
    public void FilesWithoutRecipesAreReturnedUnchanged()
    {
        var source = "namespace Fixture { public class Empty { } }\n";

        var result = Rewrite(source, Writing("AdventTunic"));

        Assert.That(result.Changed, Is.False);
        Assert.That(result.Text, Is.EqualTo(source));
        Assert.That(result.RecipesVisited, Is.Empty);
    }
}
