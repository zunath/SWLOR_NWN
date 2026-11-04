using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Tests.Service.Fabricator.FabricatorTestData;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>Covers the Fabricator window's definition and route and the text its buttons show.</summary>
public class FabricatorWindowTests
{
    private static string ButtonText(FabricatorEngine engine, string actionId) => FabricatorActionText.ButtonText(engine.Preview(actionId));

    private static string ResultText(FabricatorEngine engine, string actionId) =>
        FabricatorActionText.ResultText(engine.Preview(actionId), engine.Meters.QualityPercent);

    [Test]
    public void Fabrication_IsRoutedToTheFabricatorWindow_AndNoOtherSkillIs()
    {
        var route = CraftMinigameRouting.GetRoute(SkillType.Fabrication);

        route.Window.Should().Be(GuiWindowType.CraftFabricator);
        route.Minigame.Should().Be(CraftMinigameType.Fabricator);
        CraftMinigameRouting.GetRoute(SkillType.Smithery).Window.Should().NotBe(GuiWindowType.CraftFabricator);
        CraftMinigameRouting.GetRoute(SkillType.Engineering).Window.Should().NotBe(GuiWindowType.CraftFabricator);
        Enum.GetValues<GuiWindowType>().Count(window => window == GuiWindowType.CraftFabricator).Should().Be(1);
        Enum.GetValues<GuiWindowType>().GroupBy(window => (int)window).Should().OnlyContain(group => group.Count() == 1);
        CraftMinigameRouting.GetCraftWindows().Should().Contain(GuiWindowType.CraftFabricator);
    }

    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new FabricatorCraftDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.CraftFabricator);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void AButtonShowsItsCpCost_AndTheResultShowsTheMetersTheActionMoves()
    {
        var engine = Engine(Setup(), PrimeThird);
        var retool = FabricatorRules.RetoolCp(25);

        ButtonText(engine, FabricatorActionIds.RetoolCutter).Should().Be("Retool (Cutter) (" + retool + " CP)");
        ButtonText(engine, FabricatorActionIds.RunFittings).Should().Be("Run (Fittings)");
        ResultText(engine, FabricatorActionIds.RunFittings).Should().StartWith("Fittings progress +").And.EndWith("Machine Condition -4");
        ResultText(engine, FabricatorActionIds.RunFrame).Should().StartWith("Frame progress +");
        ResultText(engine, FabricatorActionIds.RetoolCutter).Should().Be($"Machine Condition -{FabricatorRules.RetoolWear}, CP -{retool}");
    }

    [Test]
    public void ADisabledActionShowsWhyInPlaceOfTheResult()
    {
        var engine = Engine(Setup(rank: 5, level: 5));

        ResultText(engine, FabricatorActionIds.Service).Should().Be("Unlocks at Fabrication rank 10.");
        ResultText(engine, FabricatorActionIds.Assemble).Should().Be("Complete the Frame, the Shell and the Fittings first.");
        ResultText(engine, FabricatorActionIds.PrecisionPass).Should().StartWith("Run the ");
        ResultText(engine, FabricatorActionIds.RetoolBonder).Should().Be("The Bonder is already loaded.");
    }

    [Test]
    public void AssembleSaysItFinishesTheItem_AndAtWhatQuality()
    {
        var engine = Built(Setup());
        Refine(engine);

        var text = ResultText(engine, FabricatorActionIds.Assemble);

        text.Should().Be($"{FabricatorActionText.FinishesItem} at 100% quality.");
        FabricatorActionText.Tooltip(engine.Preview(FabricatorActionIds.Assemble), 100).Should().StartWith(engine.Actions.Single(a => a.Id == FabricatorActionIds.Assemble).Description);
    }

    [Test]
    public void AssembleNamesTheCappedQuality_WhenTheWeakestSubAssemblyHoldsItBack()
    {
        var engine = Built(Setup());
        while (engine.Part(SubAssembly.Fittings).Quality < FabricatorRules.MaxQuality)
            engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeTrue();

        engine.Meters.QualityPercent.Should().BeGreaterThan(engine.AssembledQuality);
        var text = ResultText(engine, FabricatorActionIds.Assemble);

        text.Should().Be($"{FabricatorActionText.FinishesItem} at {engine.AssembledQuality}% quality.");
        engine.AssembledQuality.Should().Be(FabricatorRules.AssembleCapPoints);
    }

    [Test]
    public void ARunThatWouldBreakThePieceSaysSo()
    {
        var (engine, breaking) = WearToTheBreak();

        ResultText(engine, breaking).Should().EndWith(FabricatorActionText.BreaksPiece);
    }

    [Test]
    public void ThePrimeLineNamesTheHeadAndTheBonus_OrSaysNoneIsAnnounced()
    {
        var engine = Engine(Setup(), NoPrime, PrimeSecond, NoPrime);

        FabricatorActionText.OpportunityText(engine.NextOpportunity).Should().Be("No Prime Feedstock is announced.");

        engine.Perform(FabricatorActionIds.RunFittings);
        engine.Perform(FabricatorActionIds.RunFittings);
        var text = FabricatorActionText.OpportunityText(engine.NextOpportunity);

        text.Should().StartWith("Prime Feedstock: ").And.Contain("+" + FabricatorRules.PrimeBonusPercent + "%");
    }

    [Test]
    public void EveryTextTheWindowShowsIsAsciiPunctuation()
    {
        var engine = Built(Setup());
        var quality = engine.Meters.QualityPercent;
        var texts = engine.PreviewAll().SelectMany(preview => new[]
        {
            FabricatorActionText.ButtonText(preview), FabricatorActionText.ResultText(preview, quality),
            FabricatorActionText.Tooltip(preview, quality), preview.Action.Description
        }).Append(FabricatorActionText.OpportunityText(null));

        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }
}
