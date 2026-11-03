using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>Covers the Search window's layout, its id, its first-use help, and that every spot and action row has its bound properties.</summary>
public class SearchWindowTests
{
    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new SearchDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Search);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheWindowHasItsOwnId_AndNoOtherWindowSharesIt()
    {
        Enum.GetValues<GuiWindowType>().Count(window => window == GuiWindowType.Search).Should().Be(1);
        Enum.GetValues<GuiWindowType>().Select(window => (int)window).Should().OnlyHaveUniqueItems();
        ((int)GuiWindowType.Search).Should().BeGreaterThan((int)GuiWindowType.Line, "the id is the next free one after the Line's");
    }

    [Test]
    public void TheBoardHasNineSpotsAndThreeActionRows_EachWithItsBoundProperties()
    {
        SearchViewModel.SpotCount.Should().Be(SearchBoard.Spots);
        SearchViewModel.ActionCount.Should().Be(3);
        for (var spot = 0; spot < SearchViewModel.SpotCount; spot++)
        {
            foreach (var name in new[] { SearchViewModel.SpotTextProperty(spot), SearchViewModel.SpotTooltipProperty(spot), SearchViewModel.SpotColorProperty(spot) })
                typeof(SearchViewModel).GetProperty(name).Should().NotBeNull(name);
        }

        for (var action = 0; action < SearchViewModel.ActionCount; action++)
        {
            foreach (var name in new[]
                     {
                         SearchViewModel.ActionTextProperty(action), SearchViewModel.ActionSummaryProperty(action),
                         SearchViewModel.ActionTooltipProperty(action), SearchViewModel.ActionColorProperty(action),
                         SearchViewModel.ActionEnabledProperty(action)
                     })
            {
                typeof(SearchViewModel).GetProperty(name).Should().NotBeNull(name);
            }
        }
    }

    [Test]
    public void TheHelpPageOpensOnTheFirstVisitOnly_AndEachMinigameHasItsOwn()
    {
        var player = new Player("p1");

        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Search).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Search, DateTime.UtcNow).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Search, DateTime.UtcNow).Should().BeFalse();
        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Forge).Should().BeTrue("each minigame has its own first use");
        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Line).Should().BeTrue();
    }

    [Test]
    public void TheHelpTextIsAsciiPunctuation()
    {
        var help = (string)typeof(SearchDefinition)
            .GetField("HelpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        help.Should().NotBeEmpty();
        help.All(character => character < 128).Should().BeTrue();
        help.Should().Contain("Quick Search").And.Contain("Dig Deeper").And.Contain("Sift");
    }
}
