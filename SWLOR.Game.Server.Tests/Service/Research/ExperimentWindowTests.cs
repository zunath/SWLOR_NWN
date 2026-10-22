using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>Covers the Experiment window's layout, its bindings, and the words it shows for the engine's previews.</summary>
public class ExperimentWindowTests
{
    private static ExperimentEngine Engine(int level, int gearCp = 200, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), new ExperimentSetup(level, 30, 10, gearCp, false));

    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ResearchExperimentDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.ResearchExperiment);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheResearchWindowStillBuildsWithoutLayoutFindingsWithItsExperimentLines()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ResearchDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Research);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheWindowTypeIsUniqueAndAfterTheOtherCraftingWindows()
    {
        var values = Enum.GetValues<GuiWindowType>().Select(type => (int)type).ToList();

        ((int)GuiWindowType.ResearchExperiment).Should().Be(87);
        values.Count(value => value == 87).Should().Be(1);
    }

    [Test]
    public void EveryIndexedPropertyTheWindowBindsExistsOnTheViewModel()
    {
        var type = typeof(ResearchExperimentViewModel);
        var names = new List<string>();
        for (var slot = 0; slot < ResearchExperimentViewModel.EvidenceSlotCount; slot++)
        {
            names.Add(ResearchExperimentViewModel.EvidenceTextProperty(slot));
            names.Add(ResearchExperimentViewModel.EvidenceProjectionProperty(slot));
            names.Add(ResearchExperimentViewModel.EvidencePercentProperty(slot));
            names.Add(ResearchExperimentViewModel.EvidenceVisibleProperty(slot));
        }

        for (var index = 0; index < ResearchExperimentViewModel.ActionCount; index++)
        {
            names.Add(ResearchExperimentViewModel.ActionTextProperty(index));
            names.Add(ResearchExperimentViewModel.ActionSummaryProperty(index));
            names.Add(ResearchExperimentViewModel.ActionTooltipProperty(index));
            names.Add(ResearchExperimentViewModel.ActionColorProperty(index));
            names.Add(ResearchExperimentViewModel.ActionEnabledProperty(index));
            names.Add(ResearchExperimentViewModel.ActionVisibleProperty(index));
        }

        foreach (var name in names)
            type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance).Should().NotBeNull(name);
    }

    [Test]
    public void TheWindowHasAnActionSlotForEveryActionOfARandomLevel()
    {
        var engine = Engine(3);

        engine.Actions.Count.Should().Be(ResearchExperimentViewModel.ActionCount);
        engine.Setup.Candidates.Count.Should().Be(ResearchExperimentViewModel.EvidenceSlotCount);
    }

    [Test]
    public void TheViewModelRegistersTheExperimentAsItsMinigameAndOpensHelpOnFirstUse()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchExperimentViewModel.cs");

        source.Should().Contain("CraftMinigameType.Experiment");
        source.Should().Contain("OpenHelpOnFirstUse()");
        source.Should().NotContain("StartMinigameSession()", "that settles the player's item craft, which an experiment must not touch");
    }

    [Test]
    public void ClosingTheWindowSettlesTheExperimentInsteadOfFailingACraft()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchExperimentViewModel.cs");

        source.Should().Contain("public override Action OnWindowClosed()");
        source.Should().NotContain("CraftSessionService");
    }

    [Test]
    public void AJobCannotStartWhileAnExperimentWindowIsOpenBecauseTheToggleWouldCloseIt()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var validate = source[source.IndexOf("private string ValidateJob()", StringComparison.Ordinal)..];

        validate.IndexOf("Gui.IsWindowOpen(Player, GuiWindowType.ResearchExperiment)", StringComparison.Ordinal)
            .Should().BeGreaterThan(0).And.BeLessThan(validate.IndexOf("BuildResearchJobDetails(", StringComparison.Ordinal));
    }

    [Test]
    public void TheExperimentHasNoRankGatedActions()
    {
        // Every action is available from the first trial at any Fabrication rank; only CP limits Refine Method.
        foreach (var rank in new[] { 10, 25, 50 })
        {
            var engine = new ExperimentEngine(
                new ScriptedMinigameRandom(1, 1, 1),
                new ExperimentSetup(3, rank, 10, 1000, false));
            engine.Perform(ExperimentActionIds.TestCreditReduction);
            engine.Perform(ExperimentActionIds.TestCreditReduction);
            engine.Perform(ExperimentActionIds.TestTimeReduction);

            foreach (var action in engine.Actions)
            {
                var preview = engine.Preview(action.Id);
                preview.Enabled.Should().BeTrue($"{action.Id} at rank {rank}: {preview.DisabledReason}");
            }
        }
    }

    [Test]
    public void ATestShowsItsChanceAndTheEvidenceItCanAdd()
    {
        var view = ExperimentDisplay.DescribeAction(Engine(3).Preview(ExperimentActionIds.TestCreditReduction));

        view.Kind.Should().Be(ExperimentActionKind.Normal);
        view.Summary.Should().Be("75% Evidence found: Credit Reduction evidence +1 / 25% Inconclusive: no change to your evidence");
        view.Tooltip.Should().Contain("Cost: 1 trial.");
        view.Tooltip.Should().Contain("75% Evidence found");
    }

    [Test]
    public void RefineMethodShowsItsCpCostAndTheChanceItAdds()
    {
        var view = ExperimentDisplay.DescribeAction(Engine(1).Preview(ExperimentActionIds.RefineMethod));

        view.Summary.Should().Be("next Test chance +15%");
        view.Tooltip.Should().Contain("Cost: 30 CP.");
    }

    [Test]
    public void AnalyzeShowsTheEvidenceItSpendsAndTheTimeCutItBuys()
    {
        var engine = Engine(1, 200, 1, 1, 1);
        for (var trial = 0; trial < 3; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        var view = ExperimentDisplay.DescribeAction(engine.Preview(ExperimentActionIds.Analyze));

        view.Summary.Should().Be("Evidence -3, job time cut +15% (now 15%)");
        view.Tooltip.Should().Contain("Cost: 1 trial.");
    }

    [Test]
    public void TheLastTrialAndConcludeWarnThatTheyEndTheExperiment()
    {
        var engine = Engine(1, 200, Enumerable.Repeat(1, 6).ToArray());
        for (var trial = 0; trial < 5; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        var last = ExperimentDisplay.DescribeAction(engine.Preview(ExperimentActionIds.TestFixed));
        var conclude = ExperimentDisplay.DescribeAction(engine.Preview(ExperimentActionIds.Conclude));

        last.Kind.Should().Be(ExperimentActionKind.EndsExperiment);
        last.Summary.Should().StartWith(ExperimentDisplay.EndsTheExperiment);
        conclude.Kind.Should().Be(ExperimentActionKind.EndsExperiment);
        conclude.Tooltip.Should().Contain(ExperimentDisplay.EndsTheExperiment);
    }

    [Test]
    public void AnUnavailableActionShowsWhy()
    {
        var view = ExperimentDisplay.DescribeAction(Engine(1).Preview(ExperimentActionIds.Analyze));

        view.Kind.Should().Be(ExperimentActionKind.Unavailable);
        view.Summary.Should().Be("Unavailable: Run a Test first: there is no evidence to analyze.");
    }

    [TestCase(ResearchCandidate.CreditReduction, 0, "No evidence yet.")]
    [TestCase(ResearchCandidate.CreditReduction, 3, "Pays +5% (most 10%).")]
    [TestCase(ResearchCandidate.TimeReduction, 6, "Pays +10% (most 10%).")]
    [TestCase(ResearchCandidate.FixedGrant, 4, "Fixed grant. Evidence only buys a shorter job.")]
    public void AProjectionShowsWhatTheEvidenceOnACandidatePaysNow(ResearchCandidate candidate, int evidence, string expected)
    {
        ExperimentDisplay.Projection(candidate, evidence).Should().Be(expected);
    }

    [Test]
    public void AllPlayerFacingTextIsAsciiPunctuation()
    {
        var engine = Engine(3, 200, 1, 1, 1, 100, 100);
        var texts = new List<string>();
        for (var step = 0; step < 4; step++)
        {
            foreach (var preview in engine.PreviewAll())
            {
                var view = ExperimentDisplay.DescribeAction(preview);
                texts.Add(view.Summary);
                texts.Add(view.Tooltip);
                texts.Add(preview.Action.Name);
                texts.Add(preview.Action.Description);
            }

            engine.Perform(ExperimentActionIds.TestCreditReduction);
        }

        texts.AddRange(ResearchExperimentLifecycle.Describe(new SWLOR.Game.Server.Entity.ResearchExperimentRecord
        {
            Status = SWLOR.Game.Server.Entity.ResearchExperimentStatus.Settled,
            TargetLevel = 3,
            Winner = ResearchCandidate.CreditReduction,
            Magnitude = 4,
            ExtraBonus = true,
            TimeCutPercent = 10
        }));

        foreach (var text in texts)
            text.Should().MatchRegex("^[\\x20-\\x7E\\n]*$", "game text is ASCII");
    }

    [Test]
    public void TheStoredOutcomeIsDescribedForTheResearchWindow()
    {
        var record = new SWLOR.Game.Server.Entity.ResearchExperimentRecord
        {
            Status = SWLOR.Game.Server.Entity.ResearchExperimentStatus.Settled,
            TargetLevel = 5,
            Winner = ResearchCandidate.TimeReduction,
            Magnitude = 2,
            ExtraBonus = true,
            TimeCutPercent = 15
        };

        ResearchExperimentLifecycle.Describe(record).Should().Equal(
            "Blueprint bonus: Time Reduction +2%.",
            "Extra guaranteed bonus: tier 1.",
            "Job time cut by 15%.");
        ResearchExperimentLifecycle.Describe(null).Should().BeEmpty();
        ResearchExperimentLifecycle.Describe(new SWLOR.Game.Server.Entity.ResearchExperimentRecord()).Should().Equal("Experiment: still open.");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return File.ReadAllText(relativePath.Aggregate(Path.Combine(directory!.FullName, "SWLOR.Game.Server"), Path.Combine));
    }
}
